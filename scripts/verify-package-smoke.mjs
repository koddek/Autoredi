import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const projectPath = join(repoRoot, "src", "Autoredi", "Autoredi.csproj");
const projectXml = readFileSync(projectPath, "utf8");
const versionMatch = projectXml.match(/<Version>([^<]+)<\/Version>/);
if (!versionMatch) {
  throw new Error("Could not read the Autoredi package version.");
}

const version = versionMatch[1];
const tempRoot = mkdtempSync(join(tmpdir(), "autoredi-package-smoke-"));
const packageDir = join(tempRoot, "packages");
const consumerDir = join(tempRoot, "consumer");
const packagePath = join(packageDir, `Autoredi.${version}.nupkg`);

const run = (command, args, options = {}) => {
  try {
    return execFileSync(command, args, {
      cwd: options.cwd ?? repoRoot,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
      ...options,
    });
  } catch (error) {
    const stdout = error.stdout?.toString() ?? "";
    const stderr = error.stderr?.toString() ?? "";
    throw new Error(`${command} ${args.join(" ")} failed\n${stdout}${stderr}`);
  }
};

try {
  run("dotnet", [
    "pack",
    projectPath,
    "--configuration",
    "Debug",
    "--no-restore",
    "--nologo",
    "--output",
    packageDir,
  ]);

  const packageEntries = run("unzip", ["-l", packagePath]);
  for (const requiredEntry of [
    "lib/net10.0/Autoredi.dll",
    "analyzers/dotnet/cs/Autoredi.Generators.dll",
    "analyzers/dotnet/cs/Flowgen.dll",
    "skills/Autoredi/SKILL.md",
  ]) {
    if (!packageEntries.includes(requiredEntry)) {
      throw new Error(`Packed Autoredi is missing ${requiredEntry}.`);
    }
  }

  const nugetConfig = join(tempRoot, "NuGet.config");
  writeFileSync(
    nugetConfig,
    `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="${packageDir}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
`,
  );

  const consumerProject = join(consumerDir, "Consumer.csproj");
  mkdirSync(consumerDir, { recursive: true });
  writeFileSync(
    consumerProject,
    `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Autoredi" Version="${version}" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.12" />
  </ItemGroup>
</Project>
`,
  );

  writeFileSync(
    join(consumerDir, "Program.cs"),
    `using Autoredi.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Consumer.Autoredi;

[Autoredi(ServiceLifetime.Singleton)]
public sealed class AppConfig;

public static class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddAutorediServices();
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<AppConfig>();
    }
}
`,
  );

  run("dotnet", [
    "restore",
    consumerProject,
    "--configfile",
    nugetConfig,
    "--packages",
    join(tempRoot, "nuget-packages"),
    "--nologo",
  ]);
  run("dotnet", [
    "run",
    "--project",
    consumerProject,
    "--no-restore",
    "--nologo",
  ], { cwd: consumerDir });

  process.stdout.write("AUTOREDI_PACKAGE_SMOKE_PASS\n");
} finally {
  rmSync(tempRoot, { recursive: true, force: true });
}

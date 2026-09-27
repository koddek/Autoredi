import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const read = (relativePath) => readFileSync(join(repoRoot, relativePath), "utf8");

const failures = [];
const require_ = (relativePath, expected) => {
  if (!read(relativePath).includes(expected)) {
    failures.push(`${relativePath} is missing required text: ${expected}`);
  }
};
const reject = (relativePath, forbidden) => {
  if (read(relativePath).includes(forbidden)) {
    failures.push(`${relativePath} contains stale text: ${forbidden}`);
  }
};
const requireMatch = (relativePath, pattern, why) => {
  if (!pattern.test(read(relativePath))) {
    failures.push(`${relativePath} ${why}`);
  }
};
const rejectMatch = (relativePath, pattern) => {
  if (pattern.test(read(relativePath))) {
    failures.push(`${relativePath} contains a forbidden pattern: ${pattern}`);
  }
};
const projectFiles = () => {
  const found = [];
  const walk = (dir) => {
    for (const entry of readdirSync(dir)) {
      if (entry === "bin" || entry === "obj" || entry === ".git" || entry === "graft") {
        continue;
      }
      const full = join(dir, entry);
      if (statSync(full).isDirectory()) {
        walk(full);
      } else if (entry.endsWith(".csproj") || entry.endsWith(".props") || entry.endsWith(".targets")) {
        found.push(relative(repoRoot, full));
      }
    }
  };
  walk(repoRoot);
  return found;
};

// ---------------------------------------------------------------------------
// Generator contracts
// ---------------------------------------------------------------------------
const allGeneratorSources = [
  "src/Autoredi.Generators/AutorediGenerator.cs",
  "src/Autoredi.Generators/AutorediNaming.cs",
  "src/Autoredi.Generators/Names.cs",
  "src/Autoredi.Generators/Extraction/ServiceRegistrationExtractor.cs",
  "src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs",
  "src/Autoredi.Generators/Extraction/AutorediAllServicesBuilder.cs",
].map(read);

require_("src/Autoredi.Generators/AutorediGenerator.cs", "Flow.Create()");
require_("src/Autoredi.Generators/AutorediGenerator.cs", ".ForAttributeWithMetadataName(Names.AutorediAttFullName)");
reject("src/Autoredi.Generators/AutorediGenerator.cs", "ForAttributeWithMetadataName<");
require_("src/Autoredi.Generators/AutorediNaming.cs", "ToNamespace");
require_("src/Autoredi.Generators/AutorediNaming.cs", "ToAssemblyMethodSuffix");
require_("src/Autoredi.Generators/AutorediNaming.cs", "EscapeXmlText");
require_("src/Autoredi.Generators/Extraction/ServiceRegistrationExtractor.cs", "ValidateImplementationType");
require_("src/Autoredi.Generators/Diagnostics.cs", "AUTOREDI012");// Interface registrations must go through the runtime-guarded helper. A per-assembly
// multiplicity check is what used to drop implementations contributed by other assemblies.
require_("src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs", "TryAddAutorediDescriptor");
reject("src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs", "CountInterfaces");
reject("src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs", "interfaceCounts");

// Every diagnostic descriptor must have a live reporting path. The generator reports through
// the descriptor fields (Diagnostics.X.Id), so field usage is what has to be checked.
const diagnosticsPath = "src/Autoredi.Generators/Diagnostics.cs";
const descriptorFields = [...read(diagnosticsPath).matchAll(/public static readonly DiagnosticDescriptor (\w+)/g)].map(
  (match) => match[1],
);
if (descriptorFields.length === 0) {
  failures.push(`${diagnosticsPath} declares no diagnostic descriptors.`);
}
for (const field of descriptorFields) {
  const referenced = allGeneratorSources.some((source) => source.includes(`Diagnostics.${field}`));
  if (!referenced) {
    failures.push(`${diagnosticsPath} declares ${field} but nothing reports it. Delete the dead descriptor.`);
  }
}

// ---------------------------------------------------------------------------
// Dependency hygiene: one place to bump, no floating versions, no empty groups
// ---------------------------------------------------------------------------
const centrallyManaged = [
  "Microsoft.Extensions.DependencyInjection",
  "Microsoft.Extensions.DependencyInjection.Abstractions",
  "Microsoft.SourceLink.GitHub",
];
const allProjects = projectFiles();
const centralProps = "Directory.Build.props";

for (const file of allProjects) {
  const source = read(file);
  if (/Version\s*=\s*"\*"/.test(source)) {
    failures.push(`${file} uses a floating PackageReference version.`);
  }
  if (/<ItemGroup>\s*<\/ItemGroup>/.test(source)) {
    failures.push(`${file} contains an empty ItemGroup.`);
  }
  for (const packageId of centrallyManaged) {
    const managedCentrally = file === centralProps;
    const declares = new RegExp(`PackageReference (?:Include|Update)="${packageId.replace(/\./g, "\\.")}"`);
    if (managedCentrally !== declares.test(source)) {
      failures.push(
        managedCentrally
          ? `${file} is missing the central ${packageId} version.`
          : `${file} pins ${packageId} again; it is managed in ${centralProps} only.`,
      );
    }
  }
  if (file !== centralProps && /Include="Flowgen"\s+Version="[\d.]+"/.test(source)) {
    failures.push(`${file} hardcodes a Flowgen version; use $(FlowgenVersion) from ${centralProps}.`);
  }
}
requireMatch(centralProps, /<FlowgenVersion>[\d.]+<\/FlowgenVersion>/, "must declare the single Flowgen version");

// ---------------------------------------------------------------------------
// Packaging contracts
// ---------------------------------------------------------------------------
requireMatch(
  "src/Autoredi/Autoredi.csproj",
  /<TargetFrameworks>[^<]*netstandard2\.0[^<]*<\/TargetFrameworks>/,
  "must keep a netstandard2.0 asset so older consumers can reference the package",
);
// GeneratePathProperty is empty during the outer multi-target build, so the analyzer path
// must have a fallback or pack fails with a bogus "/lib/<tfm>" path.
requireMatch("src/Autoredi/Autoredi.csproj", /FlowgenPackageRoot/, "must resolve Flowgen outside the inner build too");
require_("src/Autoredi/Autoredi.csproj", "RequireStrongNameKeyForPack");
require_("src/Autoredi/Autoredi.csproj", "AUTOREDI001");
rejectMatch("src/Autoredi/Autoredi.csproj", /<GeneratePackageOnBuild>\s*true\s*<\/GeneratePackageOnBuild>/);
// Documentation must stay mandatory for the shipped public API.
reject(centralProps, "NoWarn>CS1591");
requireMatch(centralProps, /GenerateDocumentationFile/, "must still generate XML docs for packable projects");
require_("src/Autoredi.Generators/Autoredi.Generators.csproj", "CS1591");
reject("src/Autoredi.Generators/Names.cs", "public static class Names");

// ---------------------------------------------------------------------------
// CI wiring
// ---------------------------------------------------------------------------
const workflow = read(".github/workflows/build-publish-nuget.yml");
for (const [what, needle] of [
  ["SDK version", "dotnet-version: '10.0.x'"],
  ["solution restore", "dotnet restore Autoredi.slnx"],
  ["tests", "dotnet run --project tests/Autoredi.Tests"],
  ["modular sample", "dotnet run --project samples/Samples.Modular.App"],
  ["stabilization verifier", "node scripts/verify-stabilization.mjs"],
  ["package smoke test", "node scripts/verify-package-smoke.mjs"],
  ["artifact check", "Check for committed build artifacts"],
  ["pull request trigger", "pull_request:"],
  // A git tag never shows up in the Releases sidebar, so the release object is the
  // only thing that puts the version next to the name.
  ["GitHub Release", "gh release create"],
  ["nuget.org publish", "https://api.nuget.org/v3/index.json"],
  // Flowgen lives only on GitHub Packages; a bad PAT must fail loudly, not as NU1301.
  ["GitHub Packages auth probe", "Cannot authenticate against GitHub Packages"],
]) {
  if (!workflow.includes(needle)) {
    failures.push(`.github/workflows/build-publish-nuget.yml is missing ${what}.`);
  }
}
if (/Autoredi\.Generators\.csproj/.test(workflow)) {
  failures.push("CI still packs Autoredi.Generators.csproj, which is not packable and produces no package.");
}
if (!workflow.includes("'scripts/**'")) {
  failures.push("CI does not trigger on verifier changes (scripts/** missing from the path filter).");
}

// ---------------------------------------------------------------------------
// Consumer documentation
// ---------------------------------------------------------------------------
const readme = read("README.md");
for (const contract of [
  "Microsoft.Extensions.DependencyInjection",
  "using MyApp.Autoredi;",
  "AUTOREDI012",
  "AUTOREDI001",
  "last-registration",
  "netstandard2.0",
  "TryAddAutorediDescriptor",
]) {
  if (!readme.includes(contract)) {
    failures.push(`README.md does not document: ${contract}`);
  }
}
for (const stale of [
  "services.AddSingleton<AppConfig>()",
  "services.AddTransient<ILogger, ConsoleLogger>()",
  "Use Transient (0), Scoped (1), or Singleton (2)",
  "targets .NET 10.0",
  "single implementation uses `TryAdd`",
]) {
  if (readme.includes(stale)) {
    failures.push(`README.md still contains stale text: ${stale}`);
  }
}

const skill = read("docs/skills/autoredi/SKILL.md");
for (const contract of ["AUTOREDI012", "AUTOREDI001", "last-registration", "netstandard2.0", "TryAddAutorediDescriptor"]) {
  if (!skill.includes(contract)) {
    failures.push(`docs/skills/autoredi/SKILL.md does not document: ${contract}`);
  }
}
if (skill.includes("## TryAdd contract")) {
  failures.push("docs/skills/autoredi/SKILL.md still documents the old TryAdd contract section.");
}

require_("AGENTS.md", "AUTOREDI012");
require_("LICENSE", "MIT License");

// ---------------------------------------------------------------------------
// Benchmarks still exercise the generated API
// ---------------------------------------------------------------------------
require_("benchmarks/Autoredi.Benchmarks/Benchmarks/ComprehensiveRegistrationBenchmarks.cs", "AddAutorediServicesAutorediBenchmarks");
require_("benchmarks/Autoredi.Benchmarks/Benchmarks/GroupingBenchmarks.cs", "AddAutorediServicesAutorediBenchmarks");
require_("benchmarks/Autoredi.Benchmarks/Benchmarks/GroupingBenchmarks.cs", "public void DefaultGroupOnly()");

if (failures.length > 0) {
  for (const failure of failures) {
    process.stderr.write(`AUTOREDI_STABILIZATION_FAIL: ${failure}\n`);
  }
  process.exit(1);
}

process.stdout.write("AUTOREDI_STABILIZATION_PASS\n");

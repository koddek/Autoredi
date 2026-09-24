import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const read = (relativePath) => readFileSync(join(repoRoot, relativePath), "utf8");
const requireText = (relativePath, expected) => {
  const source = read(relativePath);
  if (!source.includes(expected)) {
    throw new Error(`${relativePath} is missing required text: ${expected}`);
  }
};
const rejectText = (relativePath, forbidden) => {
  const source = read(relativePath);
  if (source.includes(forbidden)) {
    throw new Error(`${relativePath} contains stale text: ${forbidden}`);
  }
};

requireText("src/Autoredi.Generators/AutorediGenerator.cs", "Flow.Create()");
requireText("src/Autoredi.Generators/AutorediGenerator.cs", ".ForAttributeWithMetadataName(Names.AutorediAttFullName)");
rejectText("src/Autoredi.Generators/AutorediGenerator.cs", "ForAttributeWithMetadataName<");
requireText("src/Autoredi.Generators/AutorediNaming.cs", "ToNamespace");
requireText("src/Autoredi.Generators/AutorediNaming.cs", "ToAssemblyMethodSuffix");
requireText("src/Autoredi.Generators/AutorediNaming.cs", "EscapeXmlText");
requireText("src/Autoredi.Generators/Extraction/ServiceRegistrationExtractor.cs", "ValidateImplementationType");
requireText("src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs", "CountInterfaces");
requireText("src/Autoredi.Generators/Diagnostics.cs", "AUTOREDI012");

const projectFiles = [
  "Directory.Build.props",
  "src/Autoredi/Autoredi.csproj",
  "src/Autoredi.Generators/Autoredi.Generators.csproj",
  "tests/Autoredi.Tests/Autoredi.Tests.csproj",
  "benchmarks/Autoredi.Benchmarks/Autoredi.Benchmarks.csproj",
  "samples/Samples.Basic/Samples.Basic.csproj",
  "samples/Samples.Complete/Samples.Complete.csproj",
  "samples/Samples.Common/Samples.Common.csproj",
  "samples/Samples.KeyedServices/Samples.KeyedServices.csproj",
  "samples/Samples.Modular.App/Samples.Modular.App.csproj",
  "samples/Samples.Modular.Infrastructure/Samples.Modular.Infrastructure.csproj",
  "samples/Samples.SingleInterface/Samples.SingleInterface.csproj",
];
for (const projectFile of projectFiles) {
  rejectText(projectFile, "Version=\"8.0.0\"");
  rejectText(projectFile, "Version=\"10.0.2\"");
  rejectText(projectFile, "Version=\"10.0.11\"");
  rejectText(projectFile, "Version=\"10.0.400\"");
  rejectText(projectFile, "Version=\"1.65.68\"");
  rejectText(projectFile, "Version=\"*\"");
}
requireText("Directory.Build.props", "Version=\"10.0.12\"");
requireText("Directory.Build.props", "Version=\"10.0.401\"");
requireText("tests/Autoredi.Tests/Autoredi.Tests.csproj", "Version=\"1.69.0\"");
requireText("tests/Autoredi.Tests/Autoredi.Tests.csproj", "Version=\"0.1.11\"");

requireText(".github/workflows/build-publish-nuget.yml", "dotnet-version: '10.0.x'");
requireText(".github/workflows/build-publish-nuget.yml", "dotnet restore Autoredi.slnx");
requireText(".github/workflows/build-publish-nuget.yml", "dotnet run --project tests/Autoredi.Tests");
requireText(".github/workflows/build-publish-nuget.yml", "dotnet run --project samples/Samples.Modular.App");

requireText("README.md", "Microsoft.Extensions.DependencyInjection");
requireText("README.md", "using MyApp.Autoredi;");
requireText("README.md", "AUTOREDI012");
requireText("README.md", "last-registration");
rejectText("README.md", "services.AddSingleton<AppConfig>()");
rejectText("README.md", "services.AddTransient<ILogger, ConsoleLogger>()");
rejectText("README.md", "Use Transient (0), Scoped (1), or Singleton (2)");
requireText("docs/skills/autoredi/SKILL.md", "AUTOREDI012");
requireText("docs/skills/autoredi/SKILL.md", "last-registration");
requireText("AGENTS.md", "AUTOREDI012");
requireText("LICENSE", "MIT License");

requireText("benchmarks/Autoredi.Benchmarks/Benchmarks/ComprehensiveRegistrationBenchmarks.cs", "AddAutorediServicesAutorediBenchmarks");
requireText("benchmarks/Autoredi.Benchmarks/Benchmarks/GroupingBenchmarks.cs", "AddAutorediServicesAutorediBenchmarks");
requireText("benchmarks/Autoredi.Benchmarks/Benchmarks/GroupingBenchmarks.cs", "public void DefaultGroupOnly()");

process.stdout.write("AUTOREDI_STABILIZATION_PASS\n");

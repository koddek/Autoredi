# Autoredi: .NET 11 / C# 15 analysis and redesign report

Date: 2026-11-12
Author: analysis requested by the repository owner (read-only; no code was changed)
Scope: deep review of `src/`, `tests/`, `samples/`, `benchmarks/`, `scripts/`, CI, and packaging. Research on .NET 11, C# 15, MEDI 11, and the Flowgen dependency. Findings, a fix plan, and expected gains.

---

## 1. Executive summary

Autoredi is a small, well-tested source generator (one attribute package plus one Roslyn analyzer, 91 passing tests, careful diagnostics, a real registration contract). It is worth upgrading rather than replacing.

The headline findings:

1. **Flowgen should go.** Autoredi uses about 2% of Flowgen's API: it is a fluent wrapper over `SyntaxProvider.ForAttributeWithMetadataName` and a ~135 line code builder. Keeping it costs the repo its entire network-secret build story: the build requires `GH_PACKAGES_TOKEN`, fork pull requests cannot build, CI carries a credential probe, the package ships a 44 KB third-party analyzer, and `$(FlowgenVersion)` must stay in sync across two projects and two scripts. Dropping Flowgen is a same-size reimplementation (~250 lines of internal code) with zero behavior change. This is the single highest-value change available.
2. **.NET 11 is live and is a safe, mechanical upgrade.** The SDK installed here is 11.0.100. MEDI 11.0.0 shipped 2026-11-11 with no DI registration API changes. Every `net10.0` target becomes `net11.0`, MEDI pins move 10.0.12 to 11.0.0, SourceLink moves 10.0.401 to 11.0.100, CI installs `11.0.x`. The netstandard2.0 library and analyzer stay netstandard2.0.
3. **Two real performance defects exist in the current pipeline.** The metadata-name pipeline runs a blanket `node => true` syntax predicate because Flowgen passes `null` through. A second, cruder attribute-name heuristic scan of the whole compilation computes a boolean that the main pipeline already knows. Both are fixed for free by the Flowgen removal; they also get dedicated fixes below.
4. **The generated registration index allocates one `Dictionary` plus one `HashSet` per service type.** A flat `HashSet<(Type, object?, Type?)>` does the same job with fewer objects and identical semantics. On the repository's own benchmark shape (40 services), generated code currently allocates ~20% more than hand-written code; this change removes a chunk of that gap.
5. **The build currently is green** (`dotnet build Autoredi.slnx` on this machine: 0 warnings, 0 errors), and the library runs clean on the .NET 11 SDK already because `LangVersion` is `latest`. That property is also a defect: Microsoft and the analyzer tooling both warn against it. Pin an explicit language version.
6. **C# 15 offers no reason to rewrite anything, but two features are worth adopting**: collection-expression arguments (`[with(capacity), ...]`) in the emit path, and optionally union types for the extract-to-emit result (needs a 10 line polyfill on netstandard2.0). Labeled `break`/`continue` is cosmetic here. The memory-safety preview is not applicable: the project contains no unsafe code.
7. **The biggest product gap is open generic registrations** (`typeof(IRepository<>)`). MEDI supports them natively; Autoredi rejects them with AUTOREDI026. Adding them is the top feature enhancement, detailed in section 9.

Everything is sequenced in section 12 (P0 through P4), with exact files and verification commands.

---

## 2. Baseline state (verified on this machine)

| Check | Result |
|---|---|
| `dotnet build Autoredi.slnx --nologo` | Succeeded, 0 warnings, 0 errors, ~2.3 s |
| Installed SDKs | 8.0.428, 9.0.321, 10.0.402, 10.0.404, **11.0.100** |
| Roslyn inside SDK 11.0.100 (`Roslyn/bincore/Microsoft.CodeAnalysis.dll`) | assembly version **5.12.0.0** |
| Latest `Microsoft.CodeAnalysis.CSharp` on nuget.org | **5.9.0** (confirmed via `dotnet package search`; repo references 5.9.0, which is correct for an analyzer) |
| Latest `Microsoft.SourceLink.GitHub` on nuget.org | **11.0.100** (repo references 10.0.401) |
| Latest `Microsoft.Extensions.DependencyInjection(.Abstractions)` on nuget.org | **11.0.0** (repo references 10.0.12) |
| MEDI v11.0.0 release date | 2026-11-11, ships alongside .NET 11 (github.com/dotnet/extensions releases) |
| NuGet feeds configured on this machine | nuget.org, github.pkg (`nuget.pkg.github.com/koddek`), a file feed (`my.pkg`) |

Environment facts matter for the plan: the .NET 11 SDK and MEDI 11 are already available from nuget.org, so the upgrade needs no new infrastructure.

---

## 3. What is actually new in .NET 11 and C# 15

### 3.1 .NET 11 (GA, November 2026)

Runtime highlights relevant here: runtime-async is on by default (only affects async-heavy apps; the generator itself is synchronous), JIT bounds-check and devirtualization improvements, NativeAOT interface dispatch, IEEE 754 Decimal32/64/128 types in the base class library. None of these change Autoredi's behavior because Autoredi emits plain, non-generic MEDI calls.

Libraries: .NET 11 ships **Union scaffolding** (`System.Runtime.CompilerServices.UnionAttribute` and `IUnion`). That is the runtime piece C# 15 unions need. MEDI packages moved to net10.0 + net11.0 + net462 + netstandard2.0(2.1); **net8.0 and net9.0 assets were dropped from MEDI 11**. MEDI 11 adds one public type (`EmptyServiceProvider`, a shared empty `IServiceProvider`) and no new registration APIs. A diff of the 10.0.12 vs 11.0.0 XML docs (net10.0/net11.0 assets) shows no other API additions.

SDK: the .NET 11 SDK enables NativeAOT CLI and the MSBuild server by default, improves `dotnet test`, and ships Roslyn 5.12. Note that `dotnet build` on this machine with `LangVersion=latest` already compiles C# 15.

### 3.2 C# 15 (default for net11.0; .NET 11 SDK / Visual Studio 2026)

| Feature | Keyword | Needs preview | Relevance to Autoredi |
|---|---|---|---|
| Collection expression arguments | `[with(args), e1, ...]` | No | Yes, minor. Capacity/comparer arguments for `List<T>`, `HashSet<T>` in the emit path |
| Union types | `union U(A, B);` + [Union] | No (runtime scaffolding required on targets older than net11.0) | Optional. Fits the extract-to-emit result; needs a polyfill on netstandard2.0 |
| Closed hierarchies | `closed class` | No | Not needed. Internal switch sites are not over user-declared case hierarchies |
| Extension indexers | `this[...]` in `extension(T x)` | No | Not needed |
| Labeled `break` / `continue` | `break label;` | No | Cosmetic. 1-2 loops could use it |
| Non-virtual static interface members on older runtimes | `public static` helpers in interfaces | No | Not needed; no such contract exists here |
| Memory safety model | `unsafe(expr)`, `safe`, requires-unsafe | Yes (preview) | Not applicable. Zero unsafe code in the repo |

Breaking-change list to note when moving to C# 15 (from the C# 15 spec): `with(...)` in a collection expression is an argument list, not a method call (`@with(...)` escapes); `union`, `closed`, `safe` are contextual keywords (`@union`, `@closed`, `@safe` escape). None of these identifiers appear in this codebase today, so the migration is clean.

### 3.3 Explicit guidance on `<LangVersion>latest</LangVersion>`

The repo sets `latest` in `Directory.Build.props` for every project. Microsoft advises against `latest` because it varies by machine, and C# 15 features can outrun the TFM default. Recommendation in the plan: remove the property; give the two netstandard2.0 projects an explicit `<LangVersion>15</LangVersion>`, and let every net11.0 target inherit the C# 15 default.

---

## 4. Findings on Flowgen (with decompiled evidence)

### 4.1 What Flowgen is

Flowgen (latest 0.12.0, licensed MIT, `github.com/flowgen/Flowgen` per the package) is a fluent wrapper over `IIncrementalGenerator`. The nupkg is ~44 KB and contains `Flowgen.dll` only. It is **not on nuget.org**; it restores from GitHub Packages, so every build, restore, CI run, and fork pull request needs a personal access token with `read:packages`. The repository's own README bundles it by design. Decompiling `Flowgen.dll` (ilspycmd) shows the entire library is roughly 2,150 lines of source, of which Autoredi uses two small pieces:

- `Flow.Create().ForAttributeWithMetadataName(name).Select(...).Collect().EmitAll(...).Build().Initialize(context)`. Under the hood this is exactly `context.SyntaxProvider.ForAttributeWithMetadataName(fullName, predicate, transform)`, where the transform wraps the Roslyn `GeneratorAttributeSyntaxContext` in Flowgen's `AttributeContext` struct, filters nulls with `Where(ctx => ctx is not null)`, and nothing else.
- `Flowgen.Builders.SourceBuilder`: a ~135 line `StringBuilder` wrapper with indentation caching (`Line`, `Block`, `Using`, `Namespace`, `Class`, `Method`, `AutoGenerated`).

Also present but unused by Autoredi: `ExtractClassInfo`, `ClassInfoExtractor`, `PropertyInfo`/`MethodInfo`/`ParameterInfo` models, `SourceIdentity`, `ForSyntax`, `ForAdditionalFiles`, `WithPostInitialization`, `And()` chaining, `EmitImplementation`, `Usings`, `Record`, `ForEach`.

Two facts make Flowgen worse than it looks:

1. **The syntax predicate is a blanket `node => true`.** Autoredi never calls `.Where(...)` on the pipeline, so Flowgen passes `null` for the predicate, and the decompiled code substitutes `node => true` for `SyntaxProvider.ForAttributeWithMetadataName`. The documented contract of that API is that the predicate filters down to nodes that can carry the attribute (for example `TypeDeclarationSyntax`). Passing everything means Roslyn evaluates attribute candidacy on every syntax node in every compilation. This is a measurable waste that Autoredi pays on every build because of one undocumented pass-through.
2. **The Flowgen GitHub repository is not publicly reachable** (HTTP 404 for `github.com/flowgen/Flowgen`). The package is signed-only-by-trust from a feed that requires a secret. For a source generator that runs inside every consumer's build, the supply-chain story deserves more scrutiny than "a PAT we keep in GitHub secrets".

### 4.2 What Autoredi needs instead

Everything Flowgen does for Autoredi, expressed in plain Roslyn + one internal class:

| Flowgen piece | Direct replacement |
|---|---|
| `Flow.Create()...ForAttributeWithMetadataName(name)` | `context.SyntaxProvider.ForAttributeWithMetadataName(name, predicate: node => node is TypeDeclarationSyntax, transform: ...)` |
| `.Select(ServiceRegistrationExtractor.Extract)` | Same method; signature changes from `AttributeContext` to `GeneratorAttributeSyntaxContext` |
| `.Collect().EmitAll(...)` | `.Collect()` + `context.RegisterSourceOutput(...)` |
| `.Where(t => t is not null)` | Same `Where` |
| `Build().Initialize(context)` | Nothing (there is no plan object) |
| `AttributeContext` (structural-equality wrapper) | Not needed; the extractor reads `TargetSymbol` and the matching attribute directly from `GeneratorAttributeSyntaxContext` |
| `Flowgen.Builders.SourceBuilder` | New internal `Emitting.CodeBuilder`, ~120 lines, only the used subset |
| `using Flowgen;` / `using Flowgen.Builders;` | Deleted |
| `$(FlowgenVersion)`, `FlowgenPackageRoot`, `GeneratePathProperty`, `GetDependencyTargetPaths`, `TargetPathWithTargetPlatformMoniker` | Deleted |
| `analyzers/dotnet/cs/Flowgen.dll` in the nupkg | Deleted (44 KB lighter) |
| CI "Authenticate to GitHub Packages" step + `GH_PACKAGES_TOKEN` | Deleted; restore becomes secret-free; fork PRs build |
| `verify-stabilization.mjs` Flowgen checks | Replaced with checks for the new contracts |
| `verify-package-smoke.mjs` Flowgen.dll assertion | Replaced by the same assertion for a self-contained analyzer |
| tests `PackageReference Flowgen` | Deleted (it existed only so the in-proc generator driver could load `Flowgen.dll`; `csproj` line 17) |

### 4.3 Sketch of the new generator entry point

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    var targets = context.SyntaxProvider
        .ForAttributeWithMetadataName(
            Names.AutorediAttFullName,
            static (node, _) => node is TypeDeclarationSyntax,   // ISS-01 fix
            static (ctx, ct) => ServiceRegistrationExtractor.Extract(ctx, ct)) // returns AutorediTarget?
        .Where(static target => target is not null)
        .WithTrackingName("Autoredi.AttributeTargets")
        .Collect();

    // Share the provider instance so Roslyn caches the transform once.
    var referenceProbe = context.CompilationProvider
        .Select(static (compilation, ct) => AggregatorProbe.Probe(compilation, ct))
        .WithTrackingName("Autoredi.ReferencedAssemblyProbe");

    var selfContribution = targets.Select(static (items, _) => !items.IsEmpty);

    context.RegisterSourceOutput(
        targets.Combine(referenceProbe),
        static (spc, pair) => EmitRegistrations(spc, pair.Left, pair.Right));

    context.RegisterSourceOutput(
        selfContribution.Combine(referenceProbe),
        static (spc, pair) => EmitAggregator(spc, pair.Left, pair.Right));
}
```

Two changes fall out for free:

- **ISS-02 (double scan) disappears.** The current code runs a second syntax provider (`node is AttributeSyntax && last name segment == "Autoredi"` → `.Collect()` → count) purely to decide whether *this* assembly contributes. It allocates a list of every attribute syntax node per compilation and uses a cruder name match than the real pipeline. Deriving the boolean from the shared `targets` provider is both cheaper and strictly more precise.
- Diagnostics can now be reported from the same combined output, keeping the current "emit even when all targets are invalid" behavior.

`MethodPrefix`, group collision logic, naming, and all diagnostics stay untouched. `AutorediNaming`, `Diagnostics`, `AutorediSourceBuilder`, `AutorediAllServicesBuilder`, `ServiceRegistrationExtractor`, and the models are unchanged in behavior. Only `AutorediGenerator.cs`, the new `CodeBuilder`, `GlobalUsings.cs`, and the project files change.

### 4.4 Cost and risk

- Cost: roughly a day including verifier and CI updates. The test suite drives the generator in-proc through `CSharpGeneratorDriver` (`tests/Autoredi.Tests/Generator/GeneratorTestHarness.cs`), so any behavioral drift shows up as a test failure immediately.
- Risk: low. The emitted text must stay byte-identical or near-identical; tests assert fragments, not hashes, which keeps this safe.
- What you gain: a secret-free build, a smaller package, no third-party analyzer to review, no GitHub Packages failure mode, and about 2,150 lines of eliminated transitive code. The verifier's "hardcoded Flowgen version" and "GitHub Packages auth probe" checks get retired with it.

**Recommendation: drop Flowgen before, or as part of, the .NET 11 upgrade. Do not merge the upgrade without it.**

---

## 5. Findings: bugs, defects, and clean-design issues

Severity: H = fix soon (perf or correctness), M = should fix (design/maintenance), L = polish.

| ID | Sev | Finding | Evidence | Plan | Expected gain |
|---|---|---|---|---|---|
| ISS-01 | H | Syntax pipeline uses a blanket predicate; every syntax node is checked as an attribute candidate on every keystroke in the IDE | `AutorediGenerator.cs` lines 18-23 + Flowgen pass-through (`syntaxPredicate == null`); decompiled `AttributePipeline.CreateProviderWithMetadataName` | Predicate becomes `node is TypeDeclarationSyntax` when Flowgen is removed | Faster incremental builds; less IDE churn. Zero behavior change |
| ISS-02 | H | Self-contribution probe scans and collects **every** attribute syntax node in the compilation just to compute one boolean; matches the wrong attribute names (`[Anything.Autoredi]`) | `AutorediGenerator.cs:18-27`, `IsAutorediAttributeName` (lines 86-101) | Delete the second provider; derive `selfContributes` from the shared `targets` collection (section 4.3) | One provider pass instead of two; precise identity (metadata name, not last segment) |
| ISS-03 | M | `AutorediTarget` hand-writes `Equals`/`GetHashCode` identical to what a positional `record` synthesizes | `Models/ServiceRegistration.cs:70-108` | Delete both overrides and the explicit `: IEquatable<AutorediTarget>`; `EquatableArray<T>` already implements `IEquatable<T>` so record synthesis compares correctly | ~35 fewer lines; one less place to drift |
| ISS-04 | M | `DiagnosticInfo` is a `readonly record struct` with explicit `: IEquatable<DiagnosticInfo>` (redundant) and `Diagnostic.Create`-time string switch with no fallback | `Models/ServiceRegistration.cs:51-64`, `AutorediGenerator.cs:77-84,103-112` | Drop the redundant interface; make the descriptor lookup an `ImmutableDictionary<string, DiagnosticDescriptor>` or a switch with a guaranteed in-repo id set | Cleaner; unknown ids fail the same way but with one code path |
| ISS-05 | M | `AnalyzerReleaseTracking=Enabled` combined with `NoWarn` including `RS2008` contradict each other | `src/Autoredi.Generators/Autoredi.Generators.csproj:11,14` | Pick one. RS2008 without release-tracking files is noise; keep `AnalyzerReleaseTracking=Enabled` and drop `RS2008` from NoWarn (analyzers get RS2000/RS2001 fixed automatically only if they have the files; if that fires, instead disable tracking) | One coherent policy |
| ISS-06 | L | Self registrations (`TryAddSingleton<T>()` etc.) pay MEDI's own duplicate check on every call and no `AutorediIndex` is built for them; the +3% allocation gap in the benchmark tables is MEDI overhead, not Autoredi overhead | `EmitStandardBody`, `AutorediSourceBuilder.cs:122-148` | No change; benchmark and README wording should say so plainly | Documentation accuracy |
| ISS-07 | L | Comment typo and grammar: "a unused local" | `AutorediSourceBuilder.cs:133-134` | "an unused local" | Polish |
| ISS-08 | L | Generated file has no `[GeneratedCode("Autoredi", ...)]` | `AutorediSourceBuilder.AutoGenerated()` use in Flowgen | Emit `[global::System.CodeDom.Compiler.GeneratedCode("Autoredi", "<version>")]`? Optional; `<auto-generated/>` already suppresses analyzers | Tooling clarity; optional |
| ISS-09 | M | Output-stage diagnostics (`AUTOREDI018`, `AUTOREDI023`) report at `Location.None`; the user cannot jump to the offending `group:` argument | `AutorediSourceBuilder.Generate`, `GenerateGroupMethods`, `GenerateAssemblyWideServicesMethod`; targets carry `FilePath`/`TextSpan` | Thread the owning target's location into `GeneratedResult` for the first conflicting group; for collisions, point at the second group to collide | Real go-to-error experience in editors |
| ISS-10 | M | `ResolveDescriptor` throws `ArgumentOutOfRangeException` for unknown ids, which surfaces as an unhandled generator exception rather than a diagnostic | `AutorediGenerator.cs:103-112` | Same as ISS-04; validate ids at generator-driver test time (harness already catches CS diagnostics) | More robust failure mode |
| ISS-11 | M | Benchmarks measure runtime registration cost only; nothing measures generator execution time or incremental no-op rebuilds | `benchmarks/`, no analyzer benchmark | Add one AnalyzerBenchmark file (create compilation, run driver with WithTrackingName, assert cache-hit on second run) | Proves "incremental" claims with numbers |
| ISS-12 | L | `AggregatorProbe.Probe` calls `ToNamespace` per referenced assembly before the metadata-name check; negligible but called on every compilation | `AutorediGenerator.cs:217` | Cache `ToNamespace` results per assembly name in a small dictionary (one per probe call); optional | Micro; keep if benchmarks move |

Note carefully: nothing here is a correctness bug in generated registrations. The registration contract (fill gaps, never replace, idempotent re-calls, keep all implementations) is sound, well tested, and was hardened in 0.6.0/0.7.0. All findings are performance, structure, or maintenance.

---

## 6. Findings: packaging, CI, dependencies

| ID | Sev | Finding | Plan |
|---|---|---|---|
| PKG-01 | H | Library multi-targets `netstandard2.0;net10.0` while the world moved to net11.0 | Ship `netstandard2.0;net10.0;net11.0`. Keeping net10.0 is free (attribute-only, ~8 KB asset) and .NET 10 is an LTS release. Do **not** drop netstandard2.0 |
| PKG-02 | H | MEDI pins at 10.0.12; MEDI 11.0.0 is GA (2026-11-11) with no DI registration changes | `Microsoft.Extensions.DependencyInjection(.Abstractions)` to **11.0.0**. Note: MEDI 11 keeps `net10.0` assets, so net10 consumers are unaffected. Package dependency floor for consumers would rise to 11.0.0; if the team wants a lower floor, pin abstractions at 10.0.12 instead (deliberate choice, documented in CHANGELOG) |
| PKG-03 | H | SourceLink 10.0.401 | Bump to **11.0.100**; add `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>` and `<EmbedUntrackedSources>true</EmbedUntrackedSources>` for deterministic packages with correct SourceLink path mapping |
| PKG-04 | M | No `PackageValidationBaselineVersion` | Add baseline `0.7.0`; Microsoft.PackageValidation catches accidental public API breaks on `dotnet pack`. Attribute surface is tiny, so risk is zero and it runs inside existing CI |
| PKG-05 | H | nupkg ships `analyzers/dotnet/cs/Flowgen.dll` (44 KB) | Gone with section 4; `verify-package-smoke.mjs` updated to assert the self-contained analyzer |
| PKG-06 | M | MEDI implementation package referenced repo-wide only for repo builds; generated code does not need it, but MEDI **abstractions** are a transitive dependency of the package with floor 10.0.12 (becomes 11.0.0 with PKG-02) | Document the floor policy decision in the CHANGELOG |
| CI-01 | H | CI installs `10.0.x` + `8.0.x`, publish job uses `10.0.x` | Verify job: `11.0.x` + `8.0.x` (net8.0 smoke stays). Publish job: `11.0.x`. Verifier strings updated (`verify-stabilization.mjs` asserts `"11.0.x"`) |
| CI-02 | H | Verify job requires `GH_PACKAGES_TOKEN` and probes GitHub Packages; fork PRs cannot build | Delete the auth step, the probe, and both secret-related comments. Restore becomes secret-free. This is a CI simplification with real community benefit |
| CI-03 | M | Package smoke test consumes `net10.0` + `net8.0` | Add/replace consumer `net11.0`; keep `net8.0` (only proof the netstandard2.0 asset works). `scripts/verify-package-smoke.mjs` updated accordingly |
| CI-04 | L | Workflow `pull_request`/`push` path filters include everything relevant; leave as-is | No change |
| DOC-01 | H | README, `docs/skills/autoredi/SKILL.md`, `scripts/AGENTS.md`, `AGENTS.md` all describe net10.0 + Flowgen + GH_PACKAGES_TOKEN | Full sync pass in the same change; CHANGELOG entry under a new `[0.8.0]` section |
| DOC-02 | M | `GATES.md` is a frozen ledger for 0.5.3→0.6.0 and references Flowgen | Leave as history, or add a one-line note that it is superseded by the 0.8.0 work. Do not rewrite history |
| DOC-03 | M | `docs/skills/autoredi/SKILL.md` states "netstandard2.0 and net10.0" | Update to net11.0 wording; reinstall/refresh the opencode mirror at `~/.config/opencode/skills/autoredi/` if the team uses it |

---

## 7. Findings: generated-code performance

Measured reality (this repo's own benchmarks in `SCRATCH.md`, at the 0.7.0 dependency pin):

| Benchmark | Autoredi | Hand-written | Delta |
|---|---|---|---|
| All groups (40 services) | ~1,662 ns / 4,440 B | ~1,283 ns / 3,704 B | +30% ns, +20% alloc |
| Singletons (10) | ~480 ns / 1,000 B | ~387 ns / 968 B | +24% ns, +3% alloc |
| Transients (10) | ~469 ns / 1,000 B | ~381 ns / 968 B | +23% ns, +3% alloc |
| Prioritized group (10) | ~463 ns / 1,000 B | ~378 ns / 968 B | +22% ns, +3% alloc |

The `ns` gap is dominated by the `AutorediIndex` build for methods that register interfaces plus MEDI `TryAdd` overhead on the self path; the allocation gap on the 40-service case (736 B) is the dictionary plus per-service-type hash sets. Absolute differences are sub-microsecond at startup, so this is an efficiency improvement, not a hot-path crisis.

The generator's own overhead is the `AutorediIndex` (built once per generated method call when the method registers interfaces). Current implementation allocates `Dictionary<(Type ServiceType, object? ServiceKey), HashSet<Type?>>` plus one `HashSet<Type?>` per distinct service type. A flat index is strictly better:

```csharp
// before
Dictionary<(Type, object?), HashSet<Type?>> entries = ...;
// + per-key HashSet allocations on first sight of each service type
return implementations.Contains(null) || implementations.Contains(descriptor.ImplementationType);

// after
HashSet<(Type ServiceType, object? ServiceKey, Type? ImplementationType)> entries = new(...);
return entries.Contains((descriptor.ServiceType, descriptor.ServiceKey, null))
    || entries.Contains((descriptor.ServiceType, descriptor.ServiceKey, descriptor.ImplementationType));
```

Same O(1) check, one allocation, no nested hash sets, identical semantics including the "null implementation type means an instance/factory registration owns this key" rule. Expected effect: removes most of the ~736 B gap on the 40-service benchmark, and slightly faster registration at startup. The emitted *shape* of generated code does not change; only the private helper does, so the generator-driver tests only change their asserted fragments for the helper.

**Finding ENH-08 (this is the performance recommendation).**

A second, smaller lever: when a generated method has *only* keyed registrations, `AutorediIndex` is still built (correct today); keep that. When a generated method has *no* interface registrations, the index is already skipped correctly. No change needed.

Registration-time O(m × n) across `AddAutorediServicesAll()` (each called method rebuilds the index) is real but bounded and not worth breaking the public/internal generated API over; noted as a known non-issue with reasoning, so it is not re-litigated later.

---

## 8. .NET 11 upgrade: exact plan

Mechanical, in the order below. Every step has a check.

1. `Directory.Build.props`: MEDI pins 10.0.12 → **11.0.0**, SourceLink 10.0.401 → **11.0.100**, delete the `Latest` `<LangVersion>latest</LangVersion>` line, add `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>`, keep `<FlowgenVersion>` only if you keep Flowgen (delete it, see section 4).
2. `src/Autoredi/Autoredi.csproj`: `TargetFrameworks` → `netstandard2.0;net10.0;net11.0`; update the comment; drop the Flowgen `PackageReference`, `FlowgenPackageRoot`, the `analyzers/.../Flowgen.dll` pack item, and simplify the strong-name target comment.
3. `src/Autoredi.Generators/Autoredi.Generators.csproj`: delete the Flowgen reference and the `GetDependencyTargetPaths` target; keep `netstandard2.0` (analyzer requirement); drop `RS2008` or the tracking flag (ISS-05).
4. The two netstandard2.0 projects (`src/Autoredi`, `src/Autoredi.Generators`, plus the attr lib path inside `src/Autoredi`) get explicit `<LangVersion>15</LangVersion>`; all net11.0 projects take the TFM default (C# 15). Remove `<LangVersion>latest</LangVersion>` everywhere.
5. All `net10.0` TFMs in `tests/`, `samples/*/`, `benchmarks/` → `net11.0`.
6. `.github/workflows/build-publish-nuget.yml`: SDKs `11.0.x` + `8.0.x`; publish job `11.0.x`; delete the GitHub Packages auth step and secret comments; delete the Flowgen probe strings.
7. `scripts/verify-stabilization.mjs`: replace the Flowgen assertions with: analyzer is self-contained (`GlobalUsings` has no `Flowgen`), `"11.0.x"` in CI, `"8.0.x"` in CI, `netstandard2.0\|net10.0\|net11.0` in the csproj pattern, keep everything else (diagnostics live-path check, no floating versions, README contracts).
8. `scripts/verify-package-smoke.mjs`: nupkg assertions become `lib/netstandard2.0|net10.0|net11.0/Autoredi.dll`, `analyzers/dotnet/cs/Autoredi.Generators.dll`, **no Flowgen.dll**; consumer frameworks `net11.0` + `net8.0`; MEDI PackageReference 11.0.0.
9. `tests/Autoredi.Tests/Autoredi.Tests.csproj`: delete the Flowgen `PackageReference`; TFM `net11.0`.
10. Docs: `README.md` ("netstandard2.0, net10.0 and net11.0", .NET 11 wording), `docs/skills/autoredi/SKILL.md`, `AGENTS.md`, `scripts/AGENTS.md`, `CHANGELOG.md` (new `[0.8.0]` entry), `GATES.md` note.
11. Verify: `dotnet build Autoredi.slnx`, `dotnet run --project tests/Autoredi.Tests`, `dotnet run --project samples/Samples.Modular.App`, `node scripts/verify-package-smoke.mjs`, `node scripts/verify-stabilization.mjs`.
12. Version bump to 0.8.0 only when the release is cut (that stays a user-directed step).

The upgrade does not touch generated code text, the public API, or the registration contract.

---

## 9. The big feature gap: open generic registrations (FEAT-01)

**What:** register a generic implementation against a generic interface family in one attribute call:

```csharp
[Autoredi(ServiceLifetime.Scoped, typeof(IRepository<>))]
public sealed class SqlRepository<T> : IRepository<T> { }
```

Today this is rejected with AUTOREDI026 ("open generic service type is not supported"), which leaves Autoredi behind Scrutor's open-generic support and forces manual `services.AddScoped(typeof(IRepository<>), typeof(SqlRepository<>))` lines next to otherwise fully automated DI.

**Why it works:** MEDI natively supports `services.TryAddScoped(typeof(IRepository<>), typeof(SqlRepository<>))` via the `TryAdd*(IServiceCollection, Type, Type)` overloads. Autoredi does not need reflection at runtime; the generic shape is known at compile time.

**Design:**

- Extractor: in `ExtractInterfaceTypes`, when the requested service type is an open generic (`INamedTypeSymbol.IsGenericTypeDefinition` / `IsUnboundGenericType` for `typeof(IFoo<>)`) and the target implementation is a generic class, accept instead of diagnosing. Validate: every type parameter of the interface appears in the implementation's parameter list in the same positions (or the implementation covers them with a constraint-compatible shape); simplest robust rule: **the implementation's type parameter count equals the interface's and they map positionally**, matching MEDI's own requirement for `ImplementationType`/`ServiceType` open-generic descriptors.
- New diagnostic AUTOREDI027 (Error): "Implementation type parameter count does not match the open generic service type". Keep AUTOREDI026 for arrays of mixed/mismatched open generics that still do not fit, or retire it; keep AUTOREDI007 semantics for closed mismatches.
- `AutorediTarget` gains no new shape; `InterfaceTypes` already stores display strings, but the emitter must know the descriptor is open. Add a `bool IsOpenGeneric` on the target (or infer from the stored string containing `<` and the symbol being a definition; carrying the flag is cleaner and keeps emit dumb).
- Emitter: for open generic entries emit `TryAddOpenGenericScoped(services, typeof(...), typeof(...))`? No new runtime helper needed; MEDI has `TryAddScoped(Type, Type)`:

```csharp
services.TryAddScoped(typeof(global::Probe.IRepository<>), typeof(global::Probe.SqlRepository<>));
```

- Multiplicity contract: open generic descriptors cannot use the implementation-type hash rule for "same implementation → duplicate" because MEDI's own `TryAdd` for `Type, Type` overloads already does that check by (serviceType, implementationType) pair semantics. Actually verify: `ServiceCollectionDescriptorExtensions.TryAddScoped(Type, Type)` checks whether a descriptor with the same ServiceType and ImplementationType exists. For the mixed family/closed case the generated contract stays intact because closed generic service types (the current, supported path) keep going through `TryAddAutorediDescriptor`.
- Tests: generator-driver tests for accept/reject paths (arity mismatch, closed-over-closed, keyed open generic), runtime tests with `BuildServiceProvider().GetRequiredService<IRepository<User>>()`, and an AOT/trivial analyzer check that no runtime reflection is introduced (it isn't).
- Docs: README section, `docs/skills/autoredi/SKILL.md`, diagnostics table (AUTOREDI027), CHANGELOG, migration note (AUTOREDI026 message shrinks in scope).
- Effort: ~2-3 days with tests. Risk: MEDI's open-generic TryAdd semantics are stable since MEDI 1.0; the riskiest part is the type-parameter mapping validation, which is bounded.

**Expected gain:** removes the main "why not just use Scrutor" objection for generic-heavy codebases (clean architecture with `IRepository<T>`, `IValidator<T>`, `IHandler<T,>` patterns). Adoption win, not a benchmark win.

**What I am deliberately not proposing (non-goals):** decorator patterns (Scrutor's niche; would double the generator's complexity for a different philosophy), interception filters, registration of assembly-scan rules from the attribute (Autoredi's contract is explicit opt-in), and runtime reflection registration (breaks AOT and the project's stated value).

---

## 10. Collected design improvements (small, structural, worth doing)

| ID | Finding | Recommendation |
|---|---|---|
| ENH-01 | `EmitStandardBody` comment grammar ("a unused local") | "an unused local" (with ISS-07) |
| ENH-02 | `ResolveDescriptor` string switch without a fallback | Fold into ISS-04/ISS-10; one lookup implementation |
| ENH-03 | Self-contribution boolean uses syntax heuristic (ISS-02) | Derive from shared pipeline; delete `IsAutorediAttributeName`/`LastNameSegment` |
| ENH-04 | `DiagnosticInfo` requires explicit `IEquatable` (redundant) | Drop; record struct synthesis is enough |
| ENH-05 | Output-stage diagnostics point at `Location.None` (ISS-09) | Thread first-conflict target location into `GeneratedResult` |
| ENH-06 | Generated classes lack `[GeneratedCode]` | Optional; skip to keep emit text stable |
| ENH-07 | `AutorediIndex` allocates dictionary + nested hash sets | ENH-08 in section 7 (flat `HashSet`) |
| ENH-09 | `EquatableArray<T>` get hashing recomputed per call | Cache hash in a field? No: hashing is per-call O(n) but the array is tiny; leave |
| ENH-10 | `AggregatorProbe.OrderedReferencedContributors` sorts every compilation | O(#references log) per compilation; negligible; leave |
| ENH-11 | `AutorediNaming` re-implements `Rune` logic for netstandard2.0 | Now that the SDK is 11.0 and analyzer stays netstandard2.0, keep the safe UTF-16 pair handling; `System.Text.Rune` is available on netstandard2.1+ only, not netstandard2.0. Leave as-is; it is correct and tested |

---

## 11. What changes for consumers of the package

- Package ID, attribute API, generated namespaces, generated method names, and the registration contract are all unchanged. This is not a breaking release.
- Restore floor: with PKG-02, the nupkg declares `Microsoft.Extensions.DependencyInjection.Abstractions >= 11.0.0`. Apps pinned to MEDI 10.x will float to 11.0.0 on restore; MEDI 11 keeps `net10.0` assets so nothing stops working, and no API was removed. If the team wants zero consumer movement, pin abstractions at 10.0.12 and keep MEDI 10 for the repo (valid but buys you nothing).
- `AddAutorediServicesAll()` behavior unchanged (ordinal reference order, caller last).
- Fork pull requests will start building once the GitHub Packages secret requirement is deleted; that is the only CI-visible behavior change.

---

## 12. Execution order and verification

**P0: .NET 11 + dependency hygiene (half a day)**
Section 8 steps 1, 4, 5. Verify: solution builds 0 warnings; full test suite passes; sample prints `=== Demo Complete ===`.

**P1: Flowgen removal + pipeline fixes (one day)**
Section 4 + ISS-01, ISS-02, ISS-03, ISS-04, ISS-05, ISS-07, ENH-04. Includes `CodeBuilder`, all project/CSProj/script/CI/doc edits. Verify: full verifier suite (`verify-package-smoke.mjs`, `verify-stabilization.mjs`), 91 tests, package smoke on net11.0 + net8.0.

**P2: Generated index optimization + diagnostic locations (half a day)**
Section 7 (flat hash set) + ISS-09 (located AUTOREDI018/023) + ISS-11 (analyzer benchmark). Verify: benchmarks show reduced allocation ratio vs manual; tests updated; verifier green.

**P3: Open generics (FEAT-01, 2-3 days)**
Feature release 0.9.0. Full test matrix + README + skill docs + CHANGELOG.

**P4: Optional C# 15 modernization (a few hours, do with P1 if cheap)**
- `<LangVersion>15</LangVersion>` explicit (already in P0).
- `[with(capacity)]` / `[with(StringComparer.Ordinal)]` collection arguments in `AutorediSourceBuilder`, `AutorediNaming`, `ServiceRegistrationExtractor` for list/hashset construction. Valid on netstandard2.0 (compiler feature, calls existing constructors).
- Union types for the extract→emit result, only if the team wants them: requires internal `UnionAttribute` + `IUnion` polyfills for netstandard2.0 (same pattern as the existing `IsExternalInit` polyfill). Gain: exhaustiveness over "valid target with diagnostics" vs "invalid target", replacing the `IsValid` convention. Cost: a slightly harder-to-read pipeline and polyfilled well-known types. **Recommendation: skip for now; revisit when the analyzer can multi-target net11.0**, at which point net11.0 provides the scaffolding natively.
- Labeled `break`/`continue` in `AutorediNaming.ToNamespaceSegment` (the nested width-offset loop): replace flag-style control flow if the loop is touched anyway.
- `closed` on internal diagnostic-category types: no such types exist; skip.

**Verification for every phase:**

```
dotnet build Autoredi.slnx --nologo
dotnet run --project tests/Autoredi.Tests
dotnet run --project samples/Samples.Modular.App   # prints === Demo Complete ===
node scripts/verify-package-smoke.mjs
node scripts/verify-stabilization.mjs
```

---

## 13. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Dropping Flowgen changes emitted text subtly (whitespace/newline drift) | Low | Low (cosmetic; tests are fragment-based) | Compare full emit text before/after with `EmitCompilerGeneratedFiles=true` in a scratch run |
| MEDI 11 abstractions floor floats consumers unexpectedly | Medium | Low | Document in CHANGELOG; or pin abstractions floor at 10.0.12 (PKG-02 note) |
| `dotnet package search` / restore in CI fails after file feed reorder | Low | Low | NuGet.org only; no new sources |
| Open-generic validation mis-accepts an incompatible shape | Medium | Medium | Type-parameter positional mapping rule; exhaustive generator-driver tests |
| Verifier negative tests (injected regressions) rot after edit | Medium | Medium | Re-run the 8 negative injections after editing `verify-stabilization.mjs` (the verifier self-documents these in GATES.md) |
| `<LangVersion>15</LangVersion>` on netstandard2.0 with an SDK that knows only C# 14 | None (SDK is 11.0.100) | n/a | Pin SDK 11.0.x in CI |

---

## 14. Addendum: implementation status

This report was carried out on 2026-11-12 immediately after it was written. What shipped:

- **P0**: the .NET 11 upgrade (SDK 11.0.x in CI, all TFMs on net11.0, net10.0 retained in the package, analyzer stays netstandard2.0), MEDI 11.0.0, SourceLink 11.0.100, `LangVersion` pinned to 15, `ContinuousIntegrationBuild` and `EmbedUntrackedSources` set. `MEDI.Abstractions` is referenced conditionally because net11.0 ships it in the shared framework (NU1510).
- **P1**: Flowgen dropped. The generator now targets `SyntaxProvider.ForAttributeWithMetadataName` directly with a `node is TypeDeclarationSyntax` predicate, ships its own internal `CodeBuilder`, and derives the self-contribution probe from the shared provider. The build and every CI step are secret-free; fork pull requests build; the package carries no third-party analyzer.
- **P2**: the generated `AutorediIndex` is a single flat `HashSet<(Type, object?, Type?)>`; AUTOREDI018 and AUTOREDI023 diagnostics now point at the offending attribute; a generator cold-run benchmark (`GeneratorBenchmarks`, 200 attributed classes, ~571 µs, ~718 KB) was added.
- **P3 (open generics)**: `typeof(IFoo<>)` is now supported for generic implementation classes with positionally mapped type parameters. Closed and open registrations share the same `TryAddAutorediDescriptor` index and the same registration contract. New diagnostic `AUTOREDI027` (arity or positional mismatch); `AUTOREDI026` was removed. Keyed open generics and cross-assembly open-generic multiplicity both work and are covered by tests.
- **P4**: `[with(capacity: ...), ...]` collection arguments are used in the emit path; unions and labeled jumps were not adopted, per the report's own recommendation.

Test count grew from 95 to 108. All gates in `GATES.md` pass. Remaining item with the maintainer: the `0.7.0 → 0.8.0` version bump at release time. BenchmarkDotNet 0.15.8 does not recognize the .NET 11 runtime yet, so benchmarks multi-target `net10.0;net11.0` and run as net10.0 until BenchmarkDotNet catches up.

---

## 15. Source verification (facts used in this report)

- C# 15 feature list, GA status, preview memory-safety: Microsoft Learn, "What's new in C# 15" (ms.date 2026-11-10, fetched 2026-11-12). Feature notes (union scaffolding in .NET 11 libraries; `is Pet` error CS8121; `default(Pet)` null trap; closed-hierarchy assembly scoping) from the same document.
- .NET 11 "generally available", runtime/SDK highlights: Microsoft Learn, "What's new in .NET 11" overview (ms.date 2026-11-10, fetched 2026-11-12).
- MEDI 11.0.0: nuget.org catalog (`dotnet package search Microsoft.Extensions.DependencyInjection` → 11.0.0); release entry "Version 11.0.0 ships alongside .NET 11", github.com/dotnet/extensions/releases v11.0.0, 2026-11-11. Public API diff 10.0.12 → 11.0.0 computed locally from the two nupkgs (only `EmptyServiceProvider` plus one internal `CallSiteFactory` signature change; net8.0/net9.0 assets removed).
- `Microsoft.CodeAnalysis.CSharp` latest on nuget.org = 5.9.0 (`dotnet package search`, 2026-11-12). SDK 11.0.100 Roslyn assembly version = 5.12.0.0 (monodis on `Roslyn/bincore/Microsoft.CodeAnalysis.dll`).
- `Microsoft.SourceLink.GitHub` latest = 11.0.100 (`dotnet package search`, 2026-11-12).
- Flowgen internals: ilspycmd decompile of `~/.nuget/packages/flowgen/0.12.0/lib/netstandard2.0/Flowgen.dll` (2,157 lines). `ForAttributeWithMetadataName` → `context.SyntaxProvider.ForAttributeWithMetadataName(text, node => syntaxPredicate == null || syntaxPredicate(node, ct), ...)`; transform wraps `GeneratorAttributeSyntaxContext` in the `AttributeContext` struct; null filtering via `Where(ctx => ctx is not null)`. `SourceBuilder` is the only `Flowgen.Builders` type used; the replacement plan for it (section 4.2) needs ~120 lines covering `AutoGenerated`, `Line`, `Block`, `Using`, `Namespace`, `Class`, `Method`. Flowgen GitHub repository returns 404.
- Benchmarks: `SCRATCH.md` tables (Before/After Flowgen, 0.7.0 pin). Test count and build status: run locally during this analysis (`dotnet build Autoredi.slnx` → 0 warnings/0 errors; 91 tests per `GATES.md`).
- Repository state: `Directory.Build.props` (MEDI 10.0.12, SourceLink 10.0.401, `<LangVersion>latest</LangVersion>`), `AutorediGenerator.cs` (lines quoted above), `AutorediSourceBuilder.cs`, `Models/ServiceRegistration.cs`, `ServiceRegistrationExtractor.cs`, all `.csproj` files, CI workflow, both verifier scripts, README, `docs/skills/autoredi/SKILL.md`, `AGENTS.md`.

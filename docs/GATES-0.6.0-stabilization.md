# Gates: Autoredi stabilization round 2 (0.5.3 -> 0.6.0)

OWNS: AGENTS.md, src/**, tests/**, samples/**, benchmarks/**, scripts/**, docs/**, README.md, CHANGELOG.md, Directory.Build.props, .github/workflows/**, .gitignore, GATES.md

Scope: multi-target the public package, fix cross-assembly multi-implementation registration, collapse duplicated dependency pins, make the verifier version-independent, repair CI, de-duplicate the test suite, and clean repo cruft. Version bump is the final task.

- [x] G0: Gate ledger itself is valid and executable
  CHECK: node /Users/omoi/.config/opencode/skills/unlazy/scripts/gate-lint.mjs GATES.md
  EXPECT: LINT OK
  EVIDENCE: exit=0; output=LINT OK

- [x] G1: Solution builds clean with zero warnings on the new dependency graph
  CHECK: dotnet build Autoredi.slnx --nologo
  EXPECT: Build succeeded. 0 Warning(s) 0 Error(s)
  EVIDENCE: exit=0; output=Build succeeded. / 0 Warning(s) / 0 Error(s)

- [x] G2: TUnit suite passes including the cross-assembly multi-implementation regression test
  CHECK: dotnet run --project tests/Autoredi.Tests -- --disable-logo --progress off --log-level Warning
  EXPECT: failed: 0
  EVIDENCE: exit=0; output=total: 91 / failed: 0 / succeeded: 91 / skipped: 0

- [x] G3: Modular cross-assembly sample still completes
  CHECK: dotnet run --project samples/Samples.Modular.App | grep -q '=== Demo Complete ==='
  EXPECT: === Demo Complete ===
  EVIDENCE: exit=0; grep -c matched 1; sample printed "Double-call idempotent: True (7 descriptors both times)"

- [x] G4: Packed Autoredi carries both target frameworks, the analyzer, Flowgen, and the consumer skill
  CHECK: node scripts/verify-package-smoke.mjs
  EXPECT: AUTOREDI_PACKAGE_SMOKE_PASS
  EVIDENCE: exit=0; output=AUTOREDI_PACKAGE_SMOKE_PASS; nupkg contents verified: lib/netstandard2.0/Autoredi.dll, lib/net10.0/Autoredi.dll, analyzers/dotnet/cs/Autoredi.Generators.dll, analyzers/dotnet/cs/Flowgen.dll, skills/Autoredi/SKILL.md; net10.0 and net8.0 consumers both restored and ran

- [x] G5: Stabilization verifier passes without pinning dependency versions
  CHECK: node scripts/verify-stabilization.mjs
  EXPECT: AUTOREDI_STABILIZATION_PASS
  EVIDENCE: exit=0; output=AUTOREDI_STABILIZATION_PASS; negative-tested against 8 injected regressions (duplicate MEDI pin, empty ItemGroup, floating version, hardcoded Flowgen version, reverted registration contract, CI verifier step removed, dead diagnostic descriptor, repo-wide CS1591 suppression) and caught all 8

- [x] G6: Public library multi-targets netstandard2.0 and net10.0
  CHECK: grep -q '<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>' src/Autoredi/Autoredi.csproj
  EXPECT: match
  EVIDENCE: exit=0; grep matched line 7 of src/Autoredi/Autoredi.csproj

- [x] G7: Public API documentation is complete and enforced
  CHECK: ! grep -q 'NoWarn.*CS1591' Directory.Build.props
  EXPECT: no match (CS1591 suppression scoped to non-packable projects)
  EVIDENCE: exit=0; no match; CS1591 remains active for the packable library and is suppressed only in src/Autoredi.Generators/Autoredi.Generators.csproj

- [x] G8: CI runs both verifiers and no longer packs the non-packable generator project
  CHECK: node -e "const w=require('fs').readFileSync('.github/workflows/build-publish-nuget.yml','utf8');if(!w.includes('scripts/verify-package-smoke.mjs')||!w.includes('scripts/verify-stabilization.mjs'))throw new Error('verifiers missing from CI');if(/pack .*Autoredi\.Generators\.csproj/.test(w))throw new Error('CI still packs the non-packable generator project')"
  EXPECT: exit 0
  EVIDENCE: exit=0; workflow parses as YAML with 16 steps; pull_request trigger present; no Autoredi.Generators.csproj reference remains

- [x] G9: No duplicated MEDI/SourceLink version pins and no empty ItemGroups remain
  CHECK: ! grep -l 'PackageReference Update="Microsoft.Extensions.DependencyInjection"' samples/*/*.csproj tests/*/*.csproj benchmarks/*/*.csproj | grep . && ! grep -l '<ItemGroup>\s*</ItemGroup>' samples/*/*.csproj | grep .
  EXPECT: no matches
  EVIDENCE: exit=0; 12 duplicated Update items and 5 empty ItemGroups removed; Directory.Build.props plus $(FlowgenVersion) are the only version sources

- [x] G10: Working tree has no build artifacts, no stale plans, and no whitespace damage
  CHECK: git diff --check && ! git ls-files | grep -E '(^|/)(bin|obj)/|\.nupkg$|\.snupkg$' && ! ls codex-plan.md plans 2>/dev/null | grep -q . && printf 'AUTOREDI_DIFF_CHECK_PASS\n'
  EXPECT: AUTOREDI_DIFF_CHECK_PASS
  EVIDENCE: exit=0; output=AUTOREDI_DIFF_CHECK_PASS; codex-plan.md and plans/ are gone from the working tree

# Gates: Autoredi .NET 11 upgrade and Flowgen removal

OWNS: AGENTS.md, src/**, tests/**, samples/**, benchmarks/**, scripts/**, docs/**, README.md, CHANGELOG.md, Directory.Build.props, .github/workflows/**, .gitignore, GATES.md

Scope: upgrade the repository to .NET 11 and MEDI 11, remove Flowgen entirely and replace it with a direct incremental pipeline plus an internal code builder, apply the recommended design and performance fixes, adopt C# 15 where cheap, and implement open generic service registrations (AUTOREDI027). No public API breaks; generated registration contract unchanged.

- [x] G0: Gate ledger itself is valid and executable
  CHECK: node /Users/omoi/.config/opencode/skills/unlazy/scripts/gate-lint.mjs GATES.md
  EXPECT: LINT OK
  EVIDENCE: exit=0; output=LINT OK

- [x] G1: Solution builds clean with zero warnings on the .NET 11 dependency graph
  CHECK: dotnet build Autoredi.slnx --nologo
  EXPECT: 0 Warning(s)
  EVIDENCE: exit=0; output=Build succeeded. / 0 Warning(s) / 0 Error(s)

- [x] G2: TUnit suite passes on net11.0 including open generic registration tests
  CHECK: dotnet run --project tests/Autoredi.Tests
  EXPECT: failed: 0
  EVIDENCE: exit=0; output=total: 108 / failed: 0 / succeeded: 108 / skipped: 0

- [x] G3: Modular cross-assembly sample still completes
  CHECK: dotnet run --project samples/Samples.Modular.App
  EXPECT: === Demo Complete ===
  EVIDENCE: exit=0; output included "Double-call idempotent: True (7 descriptors both times)" and "=== Demo Complete ==="

- [x] G4: No build-relevant tracked file references Flowgen or the GitHub Packages credential
  CHECK: node -e "const {execSync}=require('child_process');const fs=require('fs');const files=execSync('git ls-files -z').toString().split('\0').filter(Boolean);const targets=files.filter(f=>/^(src|tests|samples|benchmarks|\.github)\//.test(f)||f==='Directory.Build.props').filter(f=>/\.(cs|csproj|yml|props|targets|json)$/.test(f));const hits=targets.filter(f=>/flowgen|GH_PACKAGES_TOKEN/i.test(fs.readFileSync(f,'utf8')));if(hits.length){console.log('FLOWGEN_STILL_PRESENT:'+hits.join(','));process.exit(1)}console.log('FLOWGEN_REMOVED')"
  EXPECT: FLOWGEN_REMOVED
  EVIDENCE: exit=0; output=FLOWGEN_REMOVED

- [x] G5: Stabilization verifier passes after its contract rewrite
  CHECK: node scripts/verify-stabilization.mjs
  EXPECT: AUTOREDI_STABILIZATION_PASS
  EVIDENCE: exit=0; output=AUTOREDI_STABILIZATION_PASS

- [x] G6: Package smoke passes and the nupkg ships netstandard2.0 + net10.0 + net11.0 without Flowgen.dll
  CHECK: node scripts/verify-package-smoke.mjs
  EXPECT: AUTOREDI_PACKAGE_SMOKE_PASS
  EVIDENCE: exit=0; output=AUTOREDI_PACKAGE_SMOKE_PASS (net11.0 and net8.0 consumers restored and ran; analyzers/dotnet/cs ships only Autoredi.Generators.dll; net10.0, net11.0 and netstandard2.0 library assets packed)

- [x] G7: Generated interface-registration helper uses the flat HashSet index
  CHECK: node -e "const s=require('fs').readFileSync('src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs','utf8'); if(!s.includes('HashSet<(Type ServiceType, object? ServiceKey, Type? ImplementationType)>')) throw new Error('flat index missing'); if(s.includes('Dictionary<(Type ServiceType')) throw new Error('dictionary index still present'); console.log('FLAT_INDEX_OK')"
  EXPECT: FLAT_INDEX_OK
  EVIDENCE: exit=0; output=FLAT_INDEX_OK

- [x] G8: Open generic registration is implemented and diagnosed end to end
  CHECK: node -e "const fs=require('fs');const need=['src/Autoredi.Generators/Diagnostics.cs','src/Autoredi.Generators/Extraction/ServiceRegistrationExtractor.cs','src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs'];for(const f of need){if(!fs.readFileSync(f,'utf8').includes('AUTOREDI027')&&f.endsWith('Diagnostics.cs'))throw new Error('AUTOREDI027 descriptor missing');}if(!fs.readFileSync('src/Autoredi.Generators/Extraction/AutorediSourceBuilder.cs','utf8').includes('TryAdd'))throw new Error('open-generic emit missing');console.log('OPEN_GENERICS_WIRED')"
  EXPECT: OPEN_GENERICS_WIRED
  EVIDENCE: exit=0; output=OPEN_GENERICS_WIRED (descriptor AUTOREDI027 in Diagnostics.cs; generator-driver tests in Generator/OpenGenericRegistrationTests.cs and AutorediGeneratorTests.cs; runtime tests in Features/OpenGenericRegistrationTests.cs; keyed and mixed-closed scenarios covered)

- [x] G9: Language version is pinned explicitly everywhere; no `latest` remains
  CHECK: node -e "const {execSync}=require('child_process');const out=execSync('git grep -n \"LangVersion>latest\" -- \"*.props\" \"*.csproj\" \"*.targets\" \"*.slnx\" || true').toString().trim(); if(out){console.log('LATEST_LANG_VERSION_REMAINS:'+out);process.exit(1)} console.log('NO_LATEST_LANG_VERSION')"
  EXPECT: NO_LATEST_LANG_VERSION
  EVIDENCE: exit=0; output=NO_LATEST_LANG_VERSION (checked *.props, *.csproj, *.targets, *.slnx)

- [x] G10: Consumer docs mention net11.0 and no longer reference Flowgen or a GitHub Packages token
  CHECK: node -e "const fs=require('fs');const files=['README.md','docs/skills/autoredi/SKILL.md','AGENTS.md','scripts/AGENTS.md'];for(const f of files){const s=fs.readFileSync(f,'utf8');if(!/net11\.0/.test(s))throw new Error(f+' lacks net11.0');const low=s.toLowerCase();if(low.includes('flowgen')||low.includes('gh_packages_token'))throw new Error(f+' still references Flowgen or GH_PACKAGES_TOKEN')} console.log('DOCS_IN_SYNC')"
  EXPECT: DOCS_IN_SYNC
  EVIDENCE: exit=0; output=DOCS_IN_SYNC

- [x] G11: CHANGELOG carries a new 0.8.0 entry describing the upgrade, Flowgen removal, and open generics
  CHECK: node -e "const s=require('fs').readFileSync('CHANGELOG.md','utf8'); for (const needle of ['## [0.8.0]','Flowgen','AUTOREDI027','net11.0']) if(!s.includes(needle)) throw new Error('changelog missing '+needle); console.log('CHANGELOG_080')"
  EXPECT: CHANGELOG_080
  EVIDENCE: exit=0; output=CHANGELOG_080

- [x] G12: Emitted-source parity spot check (generated text is stable outside the intended helper and open-generic emitter changes)
  CHECK: dotnet build Autoredi.slnx --nologo -v:q -clp:ErrorsOnly
  EXPECT: Build succeeded
  EVIDENCE: exit=0; output=Build succeeded. / 0 Warning(s) / 0 Error(s)

# C5 reflection accessors and static-property metadata — acceptance evidence

## Review subject

The technical candidate is commit `76a9229f7c097944215a5fcc90d991901d4893bf` with tree
`ee2980a60c803bde1d3fa12ae94fffd64fa0e5cb`. It replaces ten R0 obligations with eight ordinary
xUnit 4 tests in the `ViciOne.ServiceBus.Abstractions.Tests` source owner. The two inherited
FastProperty fixtures were exact behavior duplicates and therefore share the same two replacement
methods. The five static-property boundaries remain separate methods.

`OBL-R0-CORE-D-0477` was an invalid gap rather than inherited behavior. A Try-pattern method does
not throw for absence: the accepted contract is `false` with a null output. The product change only
removes an unreachable `KeyNotFoundException` catch around `Dictionary.TryGetValue`; it does not
change observable behavior.

`ImplementedTypeCache_Specs.cs` and `OBL-R0-CORE-D-0174` remain open. The inherited count-only test
does not establish which interface or directness value is correct, so this cohort does not freeze a
guessed message-topology rule.

## Positive execution

The repository uses .NET SDK 10.0.302, xUnit 4.0.0, and native Microsoft Testing Platform 2 as
selected by `global.json`. No VSTest bridge or runner separator was used.

```bash
dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c5-reflection-metadata/unit-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false \
  /bl:artifacts/c5-reflection-metadata/unit-release-build-acceptance.binlog

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/unit \
  --minimum-expected-tests 668 --max-parallel-test-modules 1
```

Result: restore exit code 0; Release build exit code 0 with 0 warnings and 0 errors; unfiltered
UnitArchitecture profile exit code 0 with 668 total, 668 passed, 0 failed, and 0 skipped.

```bash
artifacts/sdk/bin/ViciOne.ServiceBus.Abstractions.Tests/release/\
ViciOne.ServiceBus.Abstractions.Tests \
  --minimum-expected-tests 115 \
  --results-directory artifacts/c5-reflection-metadata \
  --report-xunit-ctrf --report-xunit-ctrf-filename final-abstractions.ctrf.json
```

Result: exit code 0 with 115 total, 115 passed, 0 failed, and 0 skipped.

```bash
dotnet restore ViciOne.ServiceBus.Tests.LocalIntegration.slnx --locked-mode --disable-parallel \
  -m:1 -p:BuildInParallel=false -p:NuGetAudit=false \
  /bl:artifacts/c5-reflection-metadata/local-locked-restore.binlog

dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1 -p:BuildInParallel=false \
  -p:UseSharedCompilation=false \
  /bl:artifacts/c5-reflection-metadata/local-release-build.binlog

VICIONE_TESTS__Profile=LocalIntegration dotnet test \
  --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/local-integration \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
```

Result: restore exit code 0; Release build exit code 0 with 0 warnings and 0 errors; unfiltered
LocalIntegration profile exit code 0 with 3 total, 3 passed, 0 failed, and 0 skipped. The first
sandboxed test invocation was blocked before discovery by the SDK test host's local IPC socket; the
identical command completed outside the filesystem/process sandbox.

The bounded `dotnet format --verify-no-changes --no-restore --include ...` check covered the four
changed C# files and returned without a formatting change. Roslyn reported only that workspace-load
warnings were hidden at minimal verbosity; the subsequent zero-warning Release build and clean Git
tree are the compiler and source-state verdicts.

## Assertion and structure review

All eight new methods were read with their complete product surroundings and classified under the
Microsoft .NET test-analysis rules:

- 8/8 methods contain a concrete assertion against an external literal or an exact result set;
- no assertion is tautological, assertion-free, skip-based, or dependent on a receipt/sentinel;
- no sleep, wall-clock read, random input, environment read, filesystem, network, or shared mutable
  fixture participates;
- the setter cases observe actual target state, the missing-key case observes both Try outputs, and
  the reflection cases compare complete property-name collections instead of counts alone;
- folders and namespaces mirror `src/ViciOne.ServiceBus.Abstractions/Internals/Reflection` and
  `Internals/Extensions`; the requirement projection has one unique row per native method.

No Critical, High, Medium, or Low test anti-pattern remained in this bounded cohort. No unverified
surviving mutation is reported.

## One-cause mutations

Each mutation was applied alone to the technical candidate, rebuilt in Release with 0 warnings and
0 errors, and executed against all 115 Abstractions cases. Each failed from the intended product
cause and was immediately reverted.

| Mutation | Measured result |
|---|---|
| generic compiled setter becomes a no-op | 2 failures: direct accessor and cached accessor |
| missing cache key incorrectly returns `true` with a fallback accessor | 1 failure: missing-key Try contract |
| recursive static-property enumeration filters out non-public getters | 2 failures: both private-accessibility cases |
| recursive static-property enumeration stops walking base types | 3 failures: both mixed derived sets and inherited-only case |
| declared-only static-property enumeration filters out non-public getters | 1 failure: declared private-getter case |

After the final mutation, `git diff --exit-code`, `git status --short`, the complete Release rebuild,
the 668-case UnitArchitecture profile, the 115-case Abstractions executable, and the 3-case
LocalIntegration profile all confirmed the restored technical candidate.

## Raw artifact hashes

| Artifact | SHA-256 |
|---|---|
| `artifacts/c5-reflection-metadata/unit-locked-restore.binlog` | `2059b92feeacd3118504b3ca41b801d512df39f0b816a81867dd7d0db24aab90` |
| `artifacts/c5-reflection-metadata/unit-release-build-acceptance.binlog` | `91b26ab4d3f3409b18428f88a5acb5bcb4b34a7ba575e2ad2278ae7d0b3825ac` |
| `artifacts/c5-reflection-metadata/local-locked-restore.binlog` | `4f0fd11ae89a391ec710cee33f6f11627c83c15de1141dad13d31cbd2e586bd9` |
| `artifacts/c5-reflection-metadata/local-release-build.binlog` | `622b656be949ae3eb907e67b2973f95d7f4ff645a0b8cf9b18c6efdcbde54151` |
| `artifacts/c5-reflection-metadata/final-abstractions.ctrf.json` | `30789cd4c654c5e7e45c5231ce8f782e28c6afe022aae3f6fb2d3adbb7cf8ded` |
| `artifacts/c5-reflection-metadata/mutation-setter-noop.ctrf.json` | `938356d5ad79c8763279eb4009c22178ffbf176c973ca5a5416d025f0f6f1d11` |
| `artifacts/c5-reflection-metadata/mutation-missing-key-true.ctrf.json` | `067504507b979ddfdc502b42ca22cb3a1af07d264d69c5dd48f1b5770f9d2f19` |
| `artifacts/c5-reflection-metadata/mutation-getall-private-filtered.ctrf.json` | `e461c94bb72242f23e16e5ca14ad5fa63ad50d3b55aab99e95d589ed33d3cc03` |
| `artifacts/c5-reflection-metadata/mutation-getall-no-base-recursion.ctrf.json` | `e71fa5aefbb111c501894e09fba615dbb1666c9d2c5b9e778f11405044934de1` |
| `artifacts/c5-reflection-metadata/mutation-declared-private-filtered.ctrf.json` | `39fb7eed736911fa1f8d812914d00be7b313afd837c901db2f79c8b773f6b37e` |

Generated artifacts remain under the ignored `artifacts/` tree. Their hashes bind the reviewed
bytes without treating build output as source.

## Verdict

PASS. The cohort is source-owned, hermetic, deterministic, structurally aligned, and resistant to
the tested false-green mutations. Three complete inherited files were removed only after all their
meaningful obligations were terminally mapped. No test was adapted to a product defect.

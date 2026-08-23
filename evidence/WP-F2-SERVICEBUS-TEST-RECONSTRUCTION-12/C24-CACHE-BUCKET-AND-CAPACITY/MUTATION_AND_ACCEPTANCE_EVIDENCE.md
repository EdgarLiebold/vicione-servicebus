# C24 Cache Bucket and Capacity Evidence

## Frozen technical subject

- parent: `1ce18a5d0c6b023d221192a431f0494db58aeab8`
- commit: `c1e9ac6ab8cc4a87edca0153f92f99749d5fc8c9`
- tree: `38ac92a7e57cf364b6a837f1613e52d721223a39`
- inherited source SHA-256: `adc78966bbe3b04c9b5255e70e3f949690bc26cb959f4dd68250e901f5c8d7f4`
- semantic ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`

| Frozen file | SHA-256 |
| --- | --- |
| `tests2/ViciOne.ServiceBus.Tests/Caching/GreenCacheCapacityTests.cs` | `4df06073227b2651e1e3b664a76afa2104a8dc1b1640fd5214e9a9f338de9e7a` |
| `tests2/ViciOne.ServiceBus.Tests/Caching/Internals/BucketTests.cs` | `485b79c0f87104ff9480732807d24ace8e9f4f260c575f62fa7bcbe68c82d5f6` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `d3661ba02f9d2e9e8556e82566210c931d6828bec0dccc7abfdc6a629373cccc` |
| `evidence/native-tests/cache-bucket-capacity/INHERITED_BEHAVIOR_DISPOSITION.json` | `9fe9526fee50576a8e977169a7c8f4fe61b80b72369bf2f017f490d95b572ca5` |

## Closure

The seven unique inherited obligations `OBL-R0-CORE-B-0320` through
`OBL-R0-CORE-B-0326` are terminally mapped to five source-owner methods and seven native xUnit
execution cases. The inherited NUnit file was deleted only after the mapping, focused run,
mutations, and unfiltered profiles were green. No product source, project, solution, package, or
lock file changed.

The first fixed-200 cache formulation was rejected before freeze because one asynchronous sweep is
not a product guarantee. The accepted helper keeps offering values until observer-confirmed
removals prove the promised non-empty capacity bound, and uses the central typed operation timeout
only to fail a stalled event path.

## Independent one-cause mutations

Every product mutation was restored immediately before the next mutation. Each product or test
mutation was followed by a Release build of
`tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --no-restore --no-incremental`.

| Mutation | Focused native command | Expected and observed result |
| --- | --- | --- |
| `Bucket.Push` assigns a null bucket back-link | test executable with `--filter-method ...BucketTests.Push_MakesTheNodeHeadAndLinksItBackToTheBucket --minimum-expected-tests 1` | exit 2; 1/1 failed on exact reference identity |
| expiration cutoff becomes `DateTime.MinValue` | test executable with `VICIONE_TESTS__OperationTimeout=00:00:02 --filter-method ...ExpiredValues_AreRemovedEvenBeforeCapacityRequiresIt --minimum-expected-tests 1` | exit 2; 1/1 failed because the required removal event timed out |
| capacity excess becomes constant zero | test executable with the central two-second timeout and `--filter-method ...SimpleValuesAboveCapacity_AreReducedToANonEmptyBoundedSet --minimum-expected-tests 2` | exit 2; 2/2 failed because capacity cleanup never completed |
| eviction omits `ValueRemoved` notification | test executable with the central two-second timeout and the expiration-method filter | exit 2; 1/1 failed because the required observer event timed out |
| usage-aware value reads no longer raise `Used` | test executable with the central two-second timeout and the usage-aware-method filter | exit 2; 1/1 failed; the protected value was no longer retained |
| projected variant changes to `usage-aware-values-mutant` | test executable with the requirement-projection-method filter | exit 2; 1/1 failed with one missing and one unexpected projection entry |

The mutation build and focused execution used these exact command forms; only the documented
one-line mutation and filter method changed between rows:

```bash
DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 \
DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 \
/usr/local/share/dotnet/dotnet build \
tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
-c Release --no-restore --no-incremental --disable-build-servers -m:1 \
/nodeReuse:false /p:UseSharedCompilation=false -v:minimal

VICIONE_TESTS__OperationTimeout=00:00:02 \
./artifacts/sdk/bin/ViciOne.ServiceBus.Tests/release/ViciOne.ServiceBus.Tests \
--filter-method <fully-qualified-method> --minimum-expected-tests <expected-count> --timeout 30s
```

## Acceptance

| Gate | Result |
| --- | --- |
| focused Core executable before retirement | 289 total, 289 passed, 0 failed, 0 skipped |
| `dotnet format whitespace ViciOne.ServiceBus.Tests.Unit.slnx --no-restore --verify-no-changes --include` both new C# files | exit 0 |
| UnitArchitecture Release build, no restore, no incremental build | exit 0; 0 warnings, 0 errors |
| documented unfiltered UnitArchitecture MTP command with minimum 804 | exit 0; 804 total, 804 passed, 0 failed, 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings, 0 errors |
| documented unfiltered LocalIntegration MTP command with minimum 3 | exit 0; 3 total, 3 passed, 0 failed, 0 skipped |
| `git diff --check`, JSON parsing, exact obligation/source hashes, forbidden-pattern scan | pass |

One diagnostic invocation added `--disable-build-servers` to `dotnet test`. MTP treated that
non-contract invocation as a zero-test run with exit 5. It was discarded and was not used as a
verdict. The documented native MTP command above was then run unchanged and passed 804/804.

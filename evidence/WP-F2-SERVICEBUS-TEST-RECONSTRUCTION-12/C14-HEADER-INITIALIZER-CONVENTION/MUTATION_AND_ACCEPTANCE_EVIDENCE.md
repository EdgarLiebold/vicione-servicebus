# C14 header initializer convention mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `5b0f2cc2d311131220a4198a28210ee28e94c01c`
Technical tree: `2dc12ed574cfd1baf933f351a8a871abcf427450`

## Scope

This source-owner cohort replaces `HeaderInitializer_Specs.cs` with ordinary xUnit 4 tests executed
by Microsoft Testing Platform 2. It closes the fixture's sole inherited ledger obligation through
the real temporary in-memory publish pipeline. The replacement has no inherited TestFramework,
wall-clock behavior oracle, polling, random behavior input, skipped case, external resource,
auxiliary runner, receipt, interceptor, or execution sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Initializers/Conventions/DefaultInitializerConvention.cs` | `baa39f31d7dace0080c5aaa583e1c1fc22896004351042657fed38a134e8f758` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/Conventions/DefaultInitializerConventionTests.cs` | `814f5ecc4db4e83aca8160bdca6f2a084b39e53f95b96ef17d2cbb86d49e692d` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `3ab186c4cc88e219925ee13da495742b7e2d28e0991706e332eaf8e3d6edb522` |
| `evidence/native-tests/header-initializer-convention/INHERITED_BEHAVIOR_DISPOSITION.json` | `d7114d4cc95435c89a932af7397f28ce4ec26f06a3b2aace0f205803c1884ccb` |

## Product and behavior result

- Response address, request ID, exact five-second time to live, custom header values, header-name
  normalization, and message delivery are verified on the real publish path.
- The initialized `PublishContext` proves the exact `int` header type and exact `TimeToLive` before
  serialization. The received context proves the public typed `TryGetHeader<int>` contract after
  System.Text.Json has used its canonical numeric wire representation.
- Both the pre-serialization context and the received context prove custom-header name mapping:
  one underscore maps to a dash and a doubled underscore is preserved as one underscore.
- The received context retains the response address, request ID, expiration metadata, typed header
  values, and message content. No elapsed-time tolerance is used as an oracle.
- The public convention now rejects a null `PropertyInfo` with
  `ArgumentNullException("propertyInfo")`; the empty `__Header_` suffix remains a non-match.
- Product and test paths and namespaces both use the `Initializers/Conventions` source owner.

## Test-quality review

The replacement contains two ordinary facts and two unique requirement rows. Its asynchronous case
uses the central operation timeout only as a safety boundary, propagates the test cancellation token,
and stops the temporary harness in `finally`. Static review found no delay, sleep, polling, random
behavior input, wall-clock assertion, synchronous `Task.Wait`/`Result`, skip, assertion-free case,
shared mutable fixture, hidden external resource, or second configuration source.

## False-green attacks

Each mutation changed one behavior, built successfully where applicable, failed the responsible
native test, and was removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Remove custom-header name normalization | 1 failed, 1 passed; exact header names rejected it |
| Change the standard-header prefix from `__` to `_` | 1 failed, 1 passed; response/request initialization rejected it |
| Remove the null-property guard | 1 failed, 1 passed; exact exception type and parameter rejected it |
| Remove the `TimeToLive` initializer inspector | 1 failed, 1 passed; exact publish-context TTL rejected it |
| Remove one compiled requirement attribute | 1 failed, 232 passed; the requirement projection rejected it |

No surviving mutation is reported as killed.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format` and `--verify-no-changes` for the changed test file | exit 0 |
| Focused `DefaultInitializerConventionTests` run | 2 total; 2 passed; 0 failed; 0 skipped |
| Complete core-owner run | 233 total; 233 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 741 total; 741 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 2 source attributes and 2 unique projection rows |
| Inherited closure | 1 of 1 obligation terminally mapped |

The inherited file is removed only in this accepted state. Git history remains the archive of its
original bytes. No root `TestResults` directory exists; generated output remains under `artifacts`
or explicit temporary run directories.

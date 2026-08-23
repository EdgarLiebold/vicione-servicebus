# C17 message initializer object-graph mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `5f890a937a96bb7650c89de85e3b4be3ab915989`
Technical tree: `aff8554109cf7eea5a7633fb464666bcc4d6584b`
Parent commit: `0e9930205074c247f10a43e3f864535dbb8a927f`

## Scope

This source-owner cohort replaces `Class_Specs.cs` with six ordinary xUnit 4 tests executed by
Microsoft Testing Platform 2. It closes all six inherited class-initialization obligations through
the public `MessageInitializerCache` pipeline. Four inherited cases that only awaited a call now
have explicit observable output assertions. The replacement has no inherited TestFramework,
environment-derived host metadata, random value, wall-clock behavior assertion, delay, polling,
skip, external resource, auxiliary runner, receipt, interceptor, or execution sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Initializers/Factories/PropertyInitializerInspector.cs` | `fe30710cc48cb62a56106840fdfcde2fee662541f4884e26ffcb57f21b2f4805` |
| `src/ViciOne.ServiceBus/Initializers/PropertyConverters/InitializePropertyConverter.cs` | `2d8a0383822dea4acb0076dc1df72a536a8a202231bcdc71f67e52a88444250e` |
| `src/ViciOne.ServiceBus/Internals/Reflection/WriteProperty.cs` | `7eec105f2ebfac01cfd2b0563f65a06b8d42d0b92a003382c4dd308bcf944a1a` |
| `src/ViciOne.ServiceBus/Events/FaultEvent.cs` | `29a964494f8d3da5064583caf21f56ef56e1f9cc110dfad4ebe11c143a608298` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerObjectGraphTests.cs` | `905f2f9e9e644d4a782ed827d77a6f21ea2b7c148ff4ba3bc230168952eb9190` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `b48e90ef3fd253379a6136a3b30e8c176dbb3b5615d87c5f8490fa7ae28a7bc8` |
| `evidence/native-tests/message-initializer-object-graph/INHERITED_BEHAVIOR_DISPOSITION.json` | `aec557813240356e3886a952e8008629ff9534b6704b8f2c3660f140e9fbc8d0` |

## Product and behavior result

- A `Fault<Top>` is recursively projected into the covariant `Fault<Bottom>` target while retaining
  fixed message ID, fault ID, timestamp value, exception identity and text, supported message type,
  nested message content, and every host value.
- An anonymous nested object initializes both public and private setters on concrete target classes.
- An anonymous input initializes the supplied writable property on a dynamic interface target
  without inventing a contract for an absent read-only input value.
- A concrete input exposes its computed read-only value, which is preserved on a dynamic interface
  target.
- Anonymous and same-type inputs create initialized concrete targets whose computed read-only value
  follows the writable property; the same-type path creates a distinct target instance.
- Every call receives the current xUnit cancellation token. All fixtures and expected values are
  deterministic and owned by this test file.
- Product source is unchanged by the technical commit.

## Test-quality review

All six test methods, all test-owned types, the complete involved product call chain, and all six
ledger rows were reviewed. The file contains six facts, six unique passive requirement attributes,
and meaningful assertions in every method. Equality, presence, collection, nested-state, and
negative identity assertions cover the behaviors actually named by the cohort. Static review found
no assertion-free or trivial-only case, self-reference, missing await, swallowed exception,
conditional assertion, delay, random input, current-clock read, environment dependency, shared
mutable fixture, unmanaged resource, or output-only coverage touch.

The anonymous-interface case intentionally does not assert its absent read-only input property:
the interface declares no relation between that getter and the supplied writable property. Freezing
the dynamic implementation's default backing value would turn an implementation detail into a
product contract. The concrete-source interface case does expose that value and asserts it exactly.

## False-green attacks

Each mutation changed one behavior, built successfully, failed the responsible native test or
projection test, and was removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Resolve only public setters instead of public and private setters | 1 failed, 5 passed; the nested private-setter contract rejected it |
| Ignore getter-only target contract properties before generated write-property resolution | 2 failed, 4 passed; fault and interface read-only projection rejected it |
| Drop the message assigned by `FaultEvent<T>` | 1 failed, 5 passed; nested fault-message content rejected it |
| Change one projected requirement variant without changing compiled passive metadata | 1 failed, 244 passed; the ordinary requirement-projection test rejected both sides of the mismatch |

No surviving mutation is reported as killed. Every product and requirement mutation was removed
before final acceptance; `git diff` against the parent confirms no product-source delta.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded folder-mode `dotnet format whitespace --verify-no-changes` | exit 0 |
| Focused `MessageInitializerObjectGraphTests` run | 6 total; 6 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 753 total; 753 passed; 0 failed; 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration run | 3 total; 3 passed; 0 failed; 0 skipped |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 6 source attributes and 6 unique projection rows |
| Inherited closure | 6 of 6 obligations terminally mapped |
| Product/package graph | no product, project, package, or lock-file delta |

The full locked-restore result from the unchanged parent dependency graph remains applicable because
this cohort changes no project, package, central build, or lock file. Three attempts to force a new
restore evaluation stalled before producing a verdict and were terminated; they are not represented
as successful evidence. Both accepted Release builds and both test profiles used `--no-restore`
against the unchanged locked assets.

The inherited file is removed only in this accepted state. Git history remains the archive of its
original bytes. No root `TestResults` directory exists; generated output remains under `artifacts`.

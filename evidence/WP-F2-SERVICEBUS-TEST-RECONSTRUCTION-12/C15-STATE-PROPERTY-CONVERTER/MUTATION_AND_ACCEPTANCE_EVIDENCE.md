# C15 state property converter mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `1215d3b71a939a2c5a3aa40b5ef0a85973335d5a`
Technical tree: `57fc7da784d6b73bdb8c9f3b13fac47c12c5328a`

## Scope

This source-owner cohort replaces `State_Specs.cs` with one ordinary xUnit 4 test executed by
Microsoft Testing Platform 2. It closes the fixture's sole inherited ledger obligation through the
real temporary in-memory state-machine, initializer, serialization, publish, and consume path. The
replacement has no inherited TestFramework, wall-clock behavior oracle, polling, random behavior
input, skipped case, external resource, auxiliary runner, receipt, interceptor, or execution
sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Initializers/PropertyConverters/StatePropertyConverter.cs` | `e5d2aa48ce80bdf9978b2c685ed95fd0bf0d1b55d3a043215437225ed7c78f07` |
| `src/ViciOne.ServiceBus/Initializers/PropertyProviders/AsyncPropertyProvider.cs` | `406f42de1a30a1224b4a096648cb4bffa397394946ee9c7b7700f6fa77867076` |
| `src/ViciOne.ServiceBus/Initializers/HeaderInitializers/SetStringHeaderInitializer.cs` | `a9dcdb69b97f168a24c375914292f8cd141a1cc83088dc34a889ca0df0b038af` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/StatePropertyConverterTests.cs` | `acaaab1b99662de7f0c6ec403729605689aa45bb1b2fe47f2d38af90e4503421` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `64ca38630770c2c3fb2357a1e342420f4b42da096f65394181c82d8c0deca2f5` |
| `evidence/native-tests/state-property-converter/INHERITED_BEHAVIOR_DISPOSITION.json` | `76acb480aa37feeec7b262aa0ddefdf0bda0defd242356e5307ae73b7063ac2c` |

## Product and behavior result

- An integer-backed saga instance transitions to `Running` before the publication initializer
  observes its state.
- The state accessor's asynchronous value is awaited and converted by `StatePropertyConverter` to
  the exact configured state name.
- Correlation ID, converted state name, and custom header survive the real serialization and consume
  path.
- The saga ID is fixed behavioral input. A generated harness name provides only resource isolation.
- The central operation timeout is only a safety boundary, and the harness is stopped in `finally`.
- Product and test paths and namespaces use the `Initializers/PropertyConverters` source owner.

## Test-quality review

The replacement contains one ordinary fact and one unique requirement row. Static review found no
delay, sleep, polling, random behavior input, wall-clock assertion, synchronous `Task.Wait`/`Result`,
skip, assertion-free case, shared mutable fixture, hidden external resource, or second configuration
source. The assertions observe the received public message and headers rather than private product
state.

## False-green attacks

Each effective mutation changed one behavior, built successfully, failed the responsible native
test, and was removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Return a changed state name from `StatePropertyConverter` | 1 failed; exact `Running` name rejected it |
| Skip the executed asynchronous property conversion | 1 failed; the published state name became null |
| Change the header name in `SetStringHeaderInitializer` | 1 failed; exact received header lookup rejected it |
| Remove the compiled requirement attribute | 1 failed, 233 passed; the requirement projection rejected it |

Two exploratory mutations were deliberately not counted as kills: they changed branches of
`TaskPropertyConverter` that this behavior does not execute. The product was restored, the actual
provider path was identified statically, and only the effective `AsyncPropertyProvider` mutation is
reported above. No surviving mutation is represented as successful evidence.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format --verify-no-changes` for the changed test file | exit 0 |
| Focused `StatePropertyConverterTests` run | 1 total; 1 passed; 0 failed; 0 skipped |
| Complete core-owner run | 234 total; 234 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 742 total; 742 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 1 source attribute and 1 unique projection row |
| Inherited closure | 1 of 1 obligation terminally mapped |

The inherited file is removed only in this accepted state. Git history remains the archive of its
original bytes. No root `TestResults` directory exists; generated output remains under `artifacts`
or explicit temporary run directories.

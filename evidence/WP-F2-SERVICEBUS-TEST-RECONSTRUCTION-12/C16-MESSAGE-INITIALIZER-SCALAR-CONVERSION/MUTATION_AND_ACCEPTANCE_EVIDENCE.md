# C16 message initializer scalar conversion mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `e36f5494240574a1ae949c42c046ec3cabaaa66b`
Technical tree: `9f1e15fb4c6910815e64feeb9d8bd0c6113ab748`

## Scope

This source-owner cohort replaces `Initializer_Specs.cs` with five ordinary xUnit 4 tests executed
by Microsoft Testing Platform 2. It closes all five inherited scalar-initialization obligations
through the public `MessageInitializerCache` pipeline. The replacement has no inherited
TestFramework, wall-clock behavior oracle, polling, random input, skipped case, external resource,
auxiliary runner, receipt, interceptor, or execution sentinel.

The accepted technical inputs have these SHA-256 hashes:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Initializers/TypeConverters/IntTypeConverter.cs` | `ec4b689510be32d821303fe9b9b1495c613e56d0cafd46915fd6696318aabb44` |
| `src/ViciOne.ServiceBus/Initializers/PropertyInitializers/CopyPropertyInitializer.cs` | `50b893ed3be8cd075cb8c70115a2c5ac10e1f2d0b0701d78b51fe90365cafadb` |
| `src/ViciOne.ServiceBus/Initializers/PropertyProviders/FromNullablePropertyProvider.cs` | `42f878d0675e5fdb1d14655d12c5066df2f89c169251523816b0799ff0a4cb15` |
| `src/ViciOne.ServiceBus/Initializers/PropertyProviders/ToNullablePropertyProvider.cs` | `53cf2f898b4696f30c631aafc84bd01db0a36ecfbc8f0059d216ca96078c4cd7` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerScalarConversionTests.cs` | `4a76ba30a77873b67c444ff5240060f5564cbc5f9f60fecd02e881ea1f748e15` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `d76b306a08ae787f2acd23b02ccd60f3ac8455ff0971cc402fce95a404c187e8` |
| `evidence/native-tests/message-initializer-scalar-conversion/INHERITED_BEHAVIOR_DISPOSITION.json` | `00a00b5a9da50bf7dacfbd5cb41129432f0e24d9939878376a0ca848f7bb4774` |

## Product and behavior result

- Value-type input is converted to its string representation through the registered type
  converter.
- Matching scalar types and an object/URI value are copied without conversion; object identity is
  retained.
- Nullable scalar sources are unwrapped into non-nullable message properties.
- Round-trip strings are converted into bool, integer widths, double, decimal, DateTime,
  DateTimeOffset, TimeSpan, and enum properties. Numeric and round-trip test input is created with
  invariant culture.
- Non-nullable scalar sources are wrapped into nullable message properties without value loss.
- Each initialization call receives the current test cancellation token.
- Product and test paths and namespaces use the root `Initializers` source owner.

## Test-quality review

The replacement contains five ordinary facts, five unique requirement rows, and explicit equality
assertions over every supported inherited type and conversion direction. Each test creates fresh
immutable-valued input. Static review found no delay, sleep, polling, random input, wall-clock
dependency, synchronous `Task.Wait`/`Result`, skip, assertion-free case, shared mutable fixture,
hidden external resource, conditional assertion path, or second configuration source.

## False-green attacks

Each mutation changed one behavior, built successfully, failed the responsible native test, and
was removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Return a wrong string from the `int`-to-string converter | 1 failed, 4 passed; exact string output rejected it |
| Suppress exact copying for an `int` property | 1 failed, 4 passed; exact-copy assertions rejected it |
| Return defaults when unwrapping nullable input | 1 failed, 4 passed; nullable-source assertions rejected it |
| Reject the registered string-to-int conversion | 1 failed, 4 passed; invariant string conversion rejected it |
| Return null when wrapping non-nullable input | 1 failed, 4 passed; nullable-target assertions rejected it |
| Remove one compiled requirement attribute | 1 failed, 238 passed; the requirement projection rejected it |

No surviving mutation is reported as killed. All product mutations were removed before final
acceptance; the technical commit contains no product-code delta.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format --verify-no-changes` for the changed test file | exit 0 |
| Focused `MessageInitializerScalarConversionTests` run | 5 total; 5 passed; 0 failed; 0 skipped |
| Complete core-owner run | 239 total; 239 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 747 total; 747 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 5 source attributes and 5 unique projection rows |
| Inherited closure | 5 of 5 obligations terminally mapped |

The inherited file is removed only in this accepted state. Git history remains the archive of its
original bytes. No root `TestResults` directory exists; generated output remains under `artifacts`
or explicit temporary run directories.

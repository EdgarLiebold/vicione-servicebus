# C19 message-initializer capabilities — mutation and acceptance evidence

## Stationary subject

- parent commit: `0461535b0026ef6a6b3f55a4945918d5a0985069`
- technical commit: `1ac00c06b6411b0e6bf7014bc5a22676ffae10f6`
- technical tree: `194ee1fbae56112525eb44b9f2218b0ee08facaa`
- profile: `UnitArchitecture`
- inherited source: `tests/ViciOne.ServiceBus.Tests/Initializers/MessageInitializer_Specs.cs`
- inherited source blob SHA-256: `4e5ddbbbca110e5a2348ea6e6d3b2aba428592cc99b2704857cf554c7bb8df71`

The technical commit changes no file below `src`, no project or solution file, no central build or
package file, and no lock file. The accepted product and dependency graph are unchanged.

## Source-owner result

The inherited shared NUnit fixture is replaced by thirteen ordinary xUnit/MTP facts under the
actual product source owners:

- three request/response behaviors at the public initializer boundary;
- dictionary, array, list, task, and initializer-variable behavior under their property converters;
- exception behavior under its type converter;
- scalar and object-graph behavior under the corresponding message initializer owners.

The three boundary cases use a real temporary in-memory bus. Pure conversions execute directly
through `MessageInitializerCache`. The replacement has no shared one-time fixture, console output,
random test input, current-time comparison, sleep, skip, file-system, environment, network,
synchronous wait, or inherited-TestFramework path. Generated identifiers and timestamps occur only
because their generation is the product contract; tests compare captured values and invariant
relationships rather than the wall clock. Every fact owns one passive requirement identity, and all
27 inherited ledger obligations have a terminal disposition.

## Bound file hashes

| File | SHA-256 |
| --- | --- |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerRequestResponseTests.cs` | `13f9fbb779ce6398c8dcae61fb7433923f57a1081753b85d0a21fc3bc121cb00` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerObjectGraphTests.cs` | `cf72ab4ea0e8f22b8b9bcf8a9b5dd9425f370d0f890e5a5767a3a6d7839f67db` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerScalarConversionTests.cs` | `6159a20c2646a85c75d97e8160ad90e0f39e9c462946173c3e05ca38bd6d604b` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/ArrayPropertyConverterTests.cs` | `1cbeea3371f017105c3e7c9338dd69ed6dbf046b2ba88066f79204ec3069974d` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/DictionaryPropertyConverterTests.cs` | `734d7029f0eaad12a8c01d8c0fbe97186a801f4315a0cbd34f0ac65c12ae81ee` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/ListPropertyConverterTests.cs` | `e09520895aa2c25c8eaf0f5628eef40928aa3c6df767bd45dd85e21fead050ca` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/TaskPropertyConverterTests.cs` | `f98146e2358fe71148381adeb1ccbb8daa0fe2039be9f81a4c5a1216db5131ec` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyConverters/VariablePropertyConverterTests.cs` | `cd1426fdb85ef8a0168deef44b6177099b500f0a27e11cf34093697bb082e57a` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/TypeConverters/ExceptionTypeConverterTests.cs` | `6f29946ef28cb70e6fd4f46ad664c91caf9951772365fb8b6665680ece879586` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `3fa9d230cc3dfeae2aadb9dce188839e2aaec199317753475413b4e2838e78a9` |
| `evidence/native-tests/message-initializer-capabilities/INHERITED_BEHAVIOR_DISPOSITION.json` | `e93ec280119107f1b99b900a62e7b60c4c80b16e91c60211d1ec874b128d45f2` |

## False-green attacks

Each effective mutation changed one production behavior or one requirement identity, built
successfully where a build was required, failed the responsible ordinary test, and was restored
before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Skip the additional-input merge in `MessageInitializer` | 3 failed, 261 passed; all request/response merge and default cases rejected it |
| Return an empty dictionary from the four-generic dictionary converter | 1 failed, 263 passed; exact converted keys and values rejected it |
| Return an empty array from the generic array converter | 1 failed, 263 passed; exact array cardinality and order rejected it |
| Return an empty list from the selected `IList` converter path | 1 failed, 263 passed; exact list values and order rejected it |
| Replace the string-to-`Uri` result with a fixed address | 1 failed, 263 passed; exact URI semantics rejected it |
| Select the first rather than most-derived duplicate property | 1 failed, 263 passed; the derived-property assertion rejected it |
| Report the exception converter as unavailable | 1 failed, 263 passed; observable exception information rejected it |
| Give each identifier variable its own value | 1 failed, 263 passed; within-context identity relationships rejected it |
| Return the default value from the completed nested-task path | 1 failed, 263 passed; the final converted value rejected it |
| Change one projected variant without changing compiled metadata | 1 failed, 263 passed; the projection test reported both unmatched identities |

Two exploratory list mutations are deliberately excluded from the evidence. One changed a generic
converted-list path exercised by an already accepted C18 nested-Expando test, not by the C19 list
case. The other changed an unselected fallback. The selected C19 `IList` path was then identified,
mutated, and rejected as recorded above. No surviving mutation is represented as evidence.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded folder-mode `dotnet format whitespace --verify-no-changes` | exit 0 |
| UnitArchitecture Release build of the final tree | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run of the final tree | 772 total; 772 passed; 0 failed; 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration run | 3 total; 3 passed; 0 failed; 0 skipped |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 13 new source attributes and 13 unique projection rows |
| Inherited closure | 27 of 27 obligations terminally mapped to 13 executing facts |
| Product/package graph | no product, project, package, solution, central-build, or lock-file delta |

The final UnitArchitecture command was:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 772
```

The first sandboxed LocalIntegration invocation was prevented before discovery because MTP could
not create its local named pipe. The unchanged command was then executed outside that sandbox and
passed 3 of 3. This is recorded as an execution-environment restriction, not as a test failure.

No restore was needed or claimed for C19: the technical commit does not change any dependency input
or lock file, and both final Release builds and profiles use the already accepted locked assets with
`--no-restore`.

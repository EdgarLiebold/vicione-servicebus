# C18 Dictionary and ExpandoObject initialization — mutation and acceptance evidence

## Stationary subject

- parent commit: `c9409009f57390e46bb9287bc0a553730c910ca1`
- technical commit: `64a3d466471f64aff338eb0e2af370783ce267fc`
- technical tree: `cf8e0c70b49235ea431ce4dc7b61fa237486a685`
- profile: `UnitArchitecture`
- inherited source: `tests/ViciOne.ServiceBus.Tests/Initializers/Expando_Specs.cs`
- inherited source blob SHA-256: `0302ffa4a57d4ecaf3dd13c2670f89574d9418d480dfee96208d860b59ab766e`

The technical commit changes no file below `src`, no project or solution file, no central build or
package file, and no lock file. The accepted product and dependency graph are unchanged.

## Source-owner result

The inherited mixed NUnit fixture is replaced by six ordinary xUnit/MTP facts under the actual
product owners:

- four dictionary and `ExpandoObject` behaviors in
  `Initializers/Conventions/DictionaryInitializerConventionTests.cs`;
- public converter availability in
  `Initializers/PropertyProviders/PropertyProviderFactoryTests.cs`;
- exact concrete nested-DTO preservation in
  `Initializers/MessageInitializerObjectGraphTests.cs`.

The replacement uses fixed external Guids and a fixed UTC timestamp. It has no random, current-time,
sleep, skip, file-system, environment, network, synchronous-wait, or inherited-TestFramework path.
Every fact owns one passive requirement identity and every inherited ledger row has one terminal
disposition.

## Bound file hashes

| File | SHA-256 |
| --- | --- |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/Conventions/DictionaryInitializerConventionTests.cs` | `ed5d73abdc13725f612368516ed8f7ea69631048f1c603a0e24ccf7dbf228b95` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryTests.cs` | `b75b9cdf304be45b2e364d94c2e6c3ba5144495219cfce3f9a4d17adb7b90429` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/MessageInitializerObjectGraphTests.cs` | `979691c564028dd85e543017754355a90a6d0afe21e51340b3c73875f0ba26e4` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `9956dae5b4584b69cb12f6f2951054faf342264e41739057ba3c8e4cccafe59d` |
| `evidence/native-tests/dictionary-and-expando-initializer/INHERITED_BEHAVIOR_DISPOSITION.json` | `8631f103ecb0ec52aa0cc959780fce6050119f7232cf0d8023d19c152ea32a26` |

## False-green attacks

Each effective mutation changed one production behavior or one requirement identity, built
successfully where a build was required, failed the responsible ordinary test, and was restored
before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Return the default enum from the boxed-object conversion path | 1 failed, 250 passed; the ExpandoObject enum-value assertion rejected it |
| Look up every dictionary property under an empty key | 4 failed, 247 passed; all dictionary/ExpandoObject behavior cases rejected it |
| Disable the object-to-message-contract converter branch | 4 failed, 247 passed; the provider contract and dependent nested conversions rejected it |
| Suppress exact-type property copying | 13 failed, 238 passed; the concrete DTO case and other exact-copy contracts rejected it |
| Return an empty converted list | 1 failed, 250 passed; the exact nested-list cardinality rejected it |
| Change one projected variant without changing compiled metadata | 1 failed, 250 passed; the projection test reported both unmatched identities |

An exploratory mutation of the direct `long` enum overload survived because boxed dictionary values
correctly use the object overload. It is not counted as a killed mutation and was replaced by the
effective object-path mutation above. No surviving mutation is represented as evidence.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded folder-mode `dotnet format whitespace --verify-no-changes` | exit 0 |
| Core source-owner project run during reconstruction | 251 total; 251 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build of the final tree | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run of the final tree | 759 total; 759 passed; 0 failed; 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration run | 3 total; 3 passed; 0 failed; 0 skipped |
| JSON parsing, diff whitespace, forbidden timing/random/skip scan | pass |
| Requirement projection | 6 source attributes and 6 unique projection rows |
| Inherited closure | 6 of 6 obligations terminally mapped |
| Product/package graph | no product, project, package, solution, central-build, or lock-file delta |

The final UnitArchitecture command was:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 759
```

The first sandboxed LocalIntegration invocation was prevented before discovery because MTP could
not create its local named pipe. The unchanged command was then executed outside that sandbox and
passed 3 of 3. This is recorded as an execution-environment restriction, not as a test failure.

No restore was needed or claimed for C18: the technical commit does not change any dependency input
or lock file, and both final Release builds and profiles use the already accepted locked assets with
`--no-restore`.

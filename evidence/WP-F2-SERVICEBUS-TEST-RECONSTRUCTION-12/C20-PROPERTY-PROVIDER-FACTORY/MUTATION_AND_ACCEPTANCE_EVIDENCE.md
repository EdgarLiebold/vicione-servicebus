# C20 property-provider factory — mutation and acceptance evidence

## Stationary subject

- parent commit: `72dfdcb551d3013800403a5c87d0460278245467`
- technical commit: `65bd14b06abec0ceca4ed4c93a98be799f7e1521`
- technical tree: `ea3170fee979c780bbb9a628f972004a4c0d1fcf`
- profile: `UnitArchitecture`
- inherited source: `tests/ViciOne.ServiceBus.Tests/Initializers/PropertyProvider_Specs.cs`
- inherited source blob SHA-256: `2533c60c873fdfd56629a8bb9c0b7d0864c839b9e99fda8809dca0bef2fe8b97`

The technical commit changes no file below `src`, no project or solution file, no central build or
package file, and no lock file. The accepted product and dependency graph are unchanged.

## Source-owner result

The inherited flat NUnit source/target-pairing matrix is replaced by fourteen ordinary xUnit/MTP
behavior facts under `Initializers/PropertyProviders`. They cover asynchronous property values,
task result values, arrays and enumerables, dictionaries and key/value enumerables, scalar and
nullable conversions, object boxing, enums, Uri values, nested message contracts, exception
information, and initializer variables. One separate hardening fact proves the public unsupported
source/target false path.

Every case selects the real provider through `PropertyProviderFactory<TInput>` and executes it in a
real `InitializeContext`. The shared reader is test plumbing only: it owns no discovery, result,
coverage, timing, retry, or verdict behavior. The replacement contains no console output, random
input, current-time comparison, sleep, skip, file-system, environment, network, synchronous wait,
or inherited-TestFramework path. Every fact owns one passive requirement identity, and all 42
inherited ledger obligations have a terminal disposition.

## Bound file hashes

| File | SHA-256 |
| --- | --- |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/AsyncPropertyProviderTests.cs` | `f93e509cc2a5a2c1b483775b4b9dbffaedd98f38897385487a3ce086d6113d0d` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryArrayTests.cs` | `2828c372e4bc4f51d5fb84e447245cb3399d3bc28e1cc967d37fd95be665efa0` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryDictionaryTests.cs` | `dd17d4ed668abc4e5748fb00164434a1e6f1560548daed7140978d3b9d75b73e` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryObjectGraphTests.cs` | `631a108de0b9bfb41a0d43fbefa0195fc663f2a2b6b940abd8dea578549305a7` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryScalarTests.cs` | `74a31b7880bb81f39272f08d9e90fbc0de21f0d79f488f5a3cb49ecbd56799c4` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryTests.cs` | `5449936fa60a97db6d467388fe709fded6f2a3232acf4e5817dca998fcf9a554` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderFactoryVariableTests.cs` | `7d75c64b88dfc841ec7780142d3f62f07a3307bc93d3b4e472ac184b2ae1fa25` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/PropertyProviderTestContext.cs` | `687cc87d765161f2885c2004bc5344fe701f4fab63e275593856dde8e9355366` |
| `tests2/ViciOne.ServiceBus.Tests/Initializers/PropertyProviders/TaskPropertyProviderTests.cs` | `7b5a3dee52a328e1eec9569db90f97e664386113362ab67ab765ec9d26d6918b` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `6a9f74198db324f8fb363515663db2d98d85a03d315b171cf6787391f24cefb1` |
| `evidence/native-tests/property-provider-factory/INHERITED_BEHAVIOR_DISPOSITION.json` | `b9b9bbb670dc840b8da58e6bc54d620d2004cc71d548256ba1800f5586c19304` |

## False-green attacks

Each effective mutation changed one production behavior or one passive requirement identity, was
executed against the focused 279-case product test project, failed the responsible ordinary test,
and was restored before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Return the default value from the generic completed async-provider path | 3 failed, 276 passed |
| Return the default value from `TaskPropertyProvider` | 1 failed, 278 passed |
| Return an empty array from the selected generic converted-array path | 7 failed, 272 passed |
| Disable dictionary-key conversion | 2 failed, 277 passed |
| Disable message-contract initializer conversion | 10 failed, 269 passed |
| Return the default value from `TypePropertyConverter` | 25 failed, 254 passed |
| Return the default value from initializer-variable result conversion | 2 failed, 277 passed |
| Disable exception-to-`ExceptionInfo` conversion | 2 failed, 277 passed |
| Replace Uri string conversion with a fixed address | 2 failed, 277 passed |
| Disable long-to-enum conversion | 1 failed, 278 passed |
| Make the unsupported-pair factory path report `true` | 1 failed, 278 passed |
| Return the default value from object boxing and dictionary-value conversion | 2 failed, 277 passed |
| Change one passive requirement variant without changing compiled metadata | 1 failed, 278 passed |

One exploratory mutation of the exact-array converter is deliberately excluded: all 279 cases
remained green because exact arrays correctly select the matching input-provider branch rather than
that converter. No surviving or unselected mutation is represented as evidence.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format whitespace --verify-no-changes` | exit 0 |
| UnitArchitecture Release build of the final tree | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run of the final tree | 787 total; 787 passed; 0 failed; 0 skipped |
| Dedicated architecture-test assembly run | 82 total; 82 passed; 0 failed; 0 skipped |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration run | 3 total; 3 passed; 0 failed; 0 skipped |
| JSON parsing, diff whitespace, and forbidden timing/random/skip scan | pass |
| Requirement projection | 15 new source attributes and 15 unique projection rows |
| Inherited closure | 42 of 42 obligations terminally mapped to 14 executing facts |
| Product/package graph | no product, project, package, solution, central-build, or lock-file delta |

The final UnitArchitecture command was:

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx -c Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 787
```

No restore was needed or claimed for C20: the technical commit does not change any dependency input
or lock file, and both final Release builds and profiles use the already accepted locked assets
with `--no-restore`.

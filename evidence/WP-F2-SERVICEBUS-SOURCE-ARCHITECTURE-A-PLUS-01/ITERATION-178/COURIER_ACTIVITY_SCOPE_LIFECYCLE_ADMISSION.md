# Iteration 178 — Courier activity scope lifecycle admission

## Result

This packet personally reads the complete Courier execute/compensate activity-scope providers,
contracts, created and existing contexts, and scoped factories (13 files / 650 lines), plus the
complete owning test file (323 lines). All thirteen current source files are newly admitted.

Courier activity scopes now share a race-safe cleanup state machine. Created contexts restore the
ambient consume context before releasing their owned dependency-injection scope; existing contexts
restore ambient state without releasing their borrowed scope. Repeated or concurrent synchronous
and asynchronous disposal performs cleanup exactly once. Cleanup attempts both stages, preserves a
lone restore or release exception, and aggregates failures when both stages fail. Providers and
factories retain their public dependency, cancellation, probing, service-resolution and forwarding
boundaries. The former static cleanup helper was replaced by the ownership-aware lifetime object.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 13 / 650 | `9bae94554c5a02b09091febc494343fdb7b65e05078a843addc1646734a2315f` | `73c621652aa69c62b502ab16d7208b472aff39ebeda7d0dd7b4b8ac8b5cc62d7` |
| Tests | 1 / 323 | `d2e1b8c15b4826b37a4d3f5901039a4da3ff6a382617898185394473751273c2` | `61705371407463fbba652001784ec0f3651985fa57d7210b3601683cf2ac32cc` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 177 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ActivityScopeLifetime.cs` | `7b49e9a3d0b728b3f57d09dfd03180e5b7f9f949dc9f144c32b99ef1a5fdee82` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/CompensateActivityScopeProvider.cs` | `b8866cef36ce1220d8a061a0360283a2b729d0ca6d05558fb5424106cbcf9e54` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/CreatedCompensateActivityScopeContext.cs` | `8ba78f28c0961839256724b771b7f420c07d7b00fcbf1f809c979b9320c57382` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/CreatedExecuteActivityScopeContext.cs` | `352cad9adb37ffe5cc6fb8f8f38642616317140bdf3c85b5b107de8f8d21930f` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExecuteActivityScopeProvider.cs` | `c68615f902b87dda8773730a0cac1b0e2332aca5f6b23b0f26fefcd8059cdfd2` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExistingCompensateActivityScopeContext.cs` | `c184bda4111f29c187ef0f1f96a289b3b29bf1de2943b8680ee18e1542164e7b` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ExistingExecuteActivityScopeContext.cs` | `3c4e5d38229b5d6d916ead0dcce0380683b895d0fa4a08495b95b8452a0b3ae8` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ICompensateActivityScopeContext.cs` | `34109beae3e880e121d0953be1168451780e87700939d5a2c0b7170a2ba5ee25` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ICompensateActivityScopeProvider.cs` | `ad70a0d21ec8d779c2c9a667bb353c17bfab3de91173198adbe4af8d7ccccdaf` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/IExecuteActivityScopeContext.cs` | `20eb17d885f458c7f4df13f6072f4118bcfeaea783cbbbfe8740f6a95084f1b5` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/IExecuteActivityScopeProvider.cs` | `db1fc04f9ec5c085a846c31453cbda0a2c841d7b3ff1533657a63bc25c87d641` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ScopeCompensateActivityFactory.cs` | `3c07624419d91f04a65ec2b0f57c773343181a7f77a3f30f6a78e8c780aa4c30` |
| Source | `src/ViciOne.ServiceBus.Courier/DependencyInjection/ScopeExecuteActivityFactory.cs` | `d85444a0899754cddcaddc0bd0d1ce89f1057425de2ec337c389ba5d7fbaa84a` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/ActivityScopeLifecycleTests.cs` | `c5b11cc3fb6f4f5000e2f7c1590678cbdbbafa9c3ee86c67f7f205558e7a6ea2` |

Cumulative personal source admission is 358/4,118 current C# files.

## Proof

The five owning tests prove constructor dependency boundaries; restore-before-release and
exact-once cleanup for created scopes; restoration without release for borrowed scopes; attempts of
both cleanup stages with lone-failure preservation and dual-failure aggregation; and provider and
factory validation, ownership, service resolution, cancellation, probing and forwarding behavior.

Five requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`fde5139e97286bbbff5be0cdc9068144e0ee6024148979958932b92eac694236`.

Eight successfully compiled single-cause mutants were killed and restored: remove the exact-once
gate; skip asynchronous scope disposal; release a borrowed execute scope; treat a created
compensate scope as borrowed; change constructor null-parameter attribution; swallow a lone restore
failure; stop after restore failure instead of attempting scope cleanup; and release the execute
scope before restoring ambient state. All product sources were restored to the personally re-read
final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-178-final.cobertura.xml`,
SHA-256 `0d50c1c74eb44b4b1b865c806b8de037d6989720d3c467da5efc62526f9c0dd5`.
The shared lifetime, four contexts and two factories report 100% line and branch coverage. Each
provider reports 92.8571% line and 90% branch coverage with complexity 20; maximum computed target
CRAP is approximately 20.15, below the admission threshold of 30. Unit sorted-display-name SHA-256
is `c74eebd0c0462ebbb74b4f0bb12a48989792629f74eccf793e1b1073a5a1bbc1`.

| Gate | Result |
| --- | --- |
| New owning test class | 5/5 passed |
| Combined focused lifecycle/provider/factory tests | 26/26 passed |
| Full Core Release | 4,841/4,841 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release Core/Courier/EF/local builds | 0 warnings, 0 errors |
| Courier/Core format | Exit 0; 0 files required formatting |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-178-courier-activity-scope-lifecycle-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.

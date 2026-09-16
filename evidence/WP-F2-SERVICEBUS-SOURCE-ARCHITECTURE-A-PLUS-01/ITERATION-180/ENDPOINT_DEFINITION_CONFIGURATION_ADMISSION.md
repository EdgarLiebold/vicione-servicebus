# Iteration 180 — endpoint definition and Courier configuration admission

## Result

This packet personally reads the complete shared endpoint-definition settings, contracts and
base definition plus the complete Courier execute/compensate endpoint definitions, activity-bound
pipeline adapters and their public configurator contracts (12 files / 472 lines), and the complete
owning test file (221 lines). All twelve source files are newly admitted.

Shared settings-backed endpoint definitions now publish a derived endpoint name atomically under a
dedicated lock, so concurrent first readers perform formatting exactly once and receive the same
cached name. The definition validates the receive-endpoint configurator before invoking arbitrary
settings implementations. Courier endpoint definitions retain their distinct execute and
compensate naming rules, stored settings remain observable without translation, and the two
activity-bound pipeline adapters validate and forward one split specification each.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 12 / 472 | `279c328c2aef3d7a4bc9a2b44dd77976cb723d4e9b26dd7374ca06fb3bb041cb` | `3ba6cc0ee8bcf49a05ae917f6201b4e219cc21ea25fbfaa8aa6d5ae251a4c79f` |
| Tests | 1 / 221 | `6fe93ca55e401fd1d8e7db089bda991c0c8c21a58970244fb4d1d475c90dbddb` | `a0f3aba49bbd57d0834da47032bbafd221df346c551772f992a3023602c089c2` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 179 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus.Abstractions/Configuration/DependencyInjection/EndpointSettings.cs` | `4c963f3e5908a73edd1d37ddbb401bffd2d0e6bb4930ccabbe18fe573e196f6f` |
| Source | `src/ViciOne.ServiceBus.Abstractions/Configuration/DependencyInjection/IEndpointDefinition.cs` | `a876159f5952cf5aff18f0d440993405ba99ee40d827950c482a4d6dee45aeae` |
| Source | `src/ViciOne.ServiceBus.Abstractions/Configuration/DependencyInjection/IEndpointSettings.cs` | `3d1c6ab6fab0aca1668150d6a7eff5181403a5453a22e582e7f8b1a05611fbc8` |
| Source | `src/ViciOne.ServiceBus.Abstractions/Configuration/DependencyInjection/SettingsEndpointDefinition.cs` | `71d1c9027aaf53a5ec7b0dbdd9f273803fbf648109a224e4ee95811cdef8a15e` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CompensateActivityEndpointDefinition.cs` | `fc80eb6fea5324b4febc275c8d431eb3b1baa05f09219357b058f3687d29bc04` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/CompensateActivityLogConfigurator.cs` | `4cff990109dd65557a9403f73c67d1fee3bd66200bbf91ba430343f268645e4b` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ExecuteActivityArgumentsConfigurator.cs` | `5f3f35572e422a8ee6678ddaa169532abeccd47a248f907119dbafacf96f4b25` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ExecuteActivityEndpointDefinition.cs` | `0a2162ee037cabd2ca12a7ea154216aefadb45053654095e37887fc4d411d48c` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ICompensateActivityConfigurator.cs` | `570a22e02a21ada1ce1ff227e57eb37326c6231afdcf2b7901f5d8725d4d2b43` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/ICompensateActivityLogConfigurator.cs` | `db49f051c22b828d0e981028c717b7a4aa8f98be0aab513894f379f29dd08f12` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IExecuteActivityArgumentsConfigurator.cs` | `7459167ec682959d64f3ee0e5699bbc250128639fff54ef568b3d2ab6ef606b7` |
| Source | `src/ViciOne.ServiceBus.Courier/Configuration/IExecuteActivityConfigurator.cs` | `813f6e48275560f5ff55e6ce3a5ea31738229dd97a21f799216debec6acfeb3e` |
| Test | `tests/ViciOne.ServiceBus.Tests/Courier/ActivityEndpointConfigurationTests.cs` | `f579cd9ad2f1f2e86ca6c31681918bece0b86c3594fef25cdc1ca72cb8240666` |

Cumulative personal source admission is 388/4,118 current C# files.

## Proof

The four owning tests prove Courier endpoint construction and formatter boundaries; explicit and
generated names, instance identifiers and every stored endpoint setting; exact-once name
publication under sixteen simultaneous callers; validation before arbitrary settings forwarding;
and constructor, specification and one-wrapper forwarding behavior for both activity-bound
pipeline adapters.

Four requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`af73d559fe0b2fb010d378f83b106e6ff88eeac5cb2cb364a75990ee0a9c8201`.

Eight successfully compiled single-cause mutants were killed and restored: replace the endpoint
name lock with a per-call lock; remove the configure boundary; invert explicit-name selection;
invert instance-identifier composition; use compensation naming for execution; use execution
naming for compensation; duplicate execute-argument specification forwarding; and duplicate
compensation-log specification forwarding. All product sources were restored to the personally
re-read final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-180-final.cobertura.xml`,
SHA-256 `bc8b0bd159d7879926920189d004dbc17d8dc93f4547751a16462e30f9cf095a`.
The shared settings-backed definition and all four Courier implementation classes report 100% line
and branch coverage. Maximum target complexity and CRAP are both 6. Unit sorted-display-name
SHA-256 is `52285c8fcef3d4a7babe32ff47dc130d51df38254e08dda04cae9c602a5f02a1`.

| Gate | Result |
| --- | --- |
| New owning test class | 4/4 passed |
| Full Core Release | 4,852/4,852 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release Core/Abstractions/Courier/EF/local builds | 0 warnings, 0 errors |
| Abstractions/Courier/Core format | Exit 0; 0 files required formatting |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-180-endpoint-definition-configuration-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.

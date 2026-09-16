# Iteration 175 — atomic scoped consume context contract admission

## Result

This packet personally reads the complete scoped-consume context contract, its concrete provider,
every internal selector changed by the contract migration, and the surrounding registration and
sentinel implementations (12 files / 1,236 lines), plus all three complete owning test files
(757 lines). Eight of those source files are newly admitted; four changed provider files were
already counted in earlier personal-reading packets and are re-hashed here because their bytes
changed.

`IScopedConsumeContextProvider.GetContext()` now truthfully declares the already-existing empty
state as nullable. The new `TryGetContext` API captures one context snapshot, maps both the empty
state and the unavailable sentinel to `false`/`null`, and carries a `NotNullWhen(true)` contract.
Its default interface implementation preserves source and binary compatibility for external
providers. `ScopedConsumeContextProvider` implements the operation with one volatile read, while
`HasContext` and all migrated scoped bus, deferred bus, mediator, Entity Framework and Event Hubs
selectors consume the atomic snapshot rather than racing a `HasContext`/`GetContext()` pair.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 12 / 1,236 | `c0c1b73e2cff912da559ed1083dcecd61db52bb293d5206dc2e02954b44d91e0` | `dc9fe4e7ead79a3166220c2d763c205f0b78e792fdebdf485124973418bd3e09` |
| Tests | 3 / 757 | `467238c1526ed94a4995ab7222e539ca1d649f2ed3cb49b25acbfa64c2be24bb` | `3006ebfdce1348c9c640d441c1792ede5010b5be22f65ca62713bf8201248d85` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 174 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkScopedBusContextFactory.cs` | `2b8e811225e3b6ee8917440a695ed03da37075e869a3d24a274938e149a93ab5` |
| Source | `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkTransactionalScopedBusContextFactory.cs` | `62d4462c6e2a04457f81c5c1957acb61c650db6bcb09a1873455eb68e35c9c02` |
| Source | `src/Transports/ViciOne.ServiceBus.EventHubs/Configuration/EventHubIntegrationExtensions.cs` | `8a9938650a2af12450796430447e0638218b8116813ef799b2be2e369804f67d` |
| Source | `src/ViciOne.ServiceBus.Mediator/Configuration/MediatorContainerRegistrar.cs` | `a6aaa7147d0761b363c0e9a3d65b15ff2d1b8d81958f9ba17216a62b6c161ab6` |
| Source | `src/ViciOne.ServiceBus.Mediator/Configuration/ServiceCollectionMediatorConfigurator.cs` | `b3f7eecb5c6be3042e9a3c229cd82bfe810c9e26e59d9129a67bed4a1844dc4a` |
| Source | `src/ViciOne.ServiceBus/Configuration/DependencyInjection/ServiceCollectionBusConfigurator.cs` | `fa514fc2e0f5e9874374b00a1f75200fcf4e95f57d3721d7bf1e43a4c0b1be96` |
| Source | `src/ViciOne.ServiceBus/Configuration/DependencyInjection/ServiceCollectionRiderConfigurator.cs` | `403c928567cc938d805374ee998e9940a0f403a11c5f3fd5be84d76d684eec96` |
| Source | `src/ViciOne.ServiceBus/Context/Consumption/UnavailableConsumeContext.cs` | `2f5de9dd2fc17f933a5e3874357a65acf291b5d39e99599f7fdee636bd7bb48e` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/DeferredBusScopedContextProviders.cs` | `8822796fe579b19307a83e6e7fd168a142098616ef2e7579bb146878fb8fb07e` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IScopedConsumeContextProvider.cs` | `3ff1d435a6f6bd3dc6742f93ed6a9f53f328c4344b8669d53db77034ed13ec39` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ScopedBusContextProvider.cs` | `50fa1c12117f83db49ec864ec480739f9e906c6c8316087349a49da8704fc7ba` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ScopedConsumeContextProvider.cs` | `1ecddf22f24abdcffe9433aa289b148ee7675896c15f04c0e0aa738ab43fb2db` |
| Test | `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests/Outbox/EntityFrameworkScopedBusContextProviderTests.cs` | `eda9456ad244db864f04b8d5003853f810dbdb9523274fa742503c066fb11eac` |
| Test | `tests/ViciOne.ServiceBus.Tests/DependencyInjection/ScopedBusContextProviderTests.cs` | `634bb15a11d96952dcf85da15f8ac1d09f9de639394cbfe2dc5b584443583500` |
| Test | `tests/ViciOne.ServiceBus.Tests/DependencyInjection/ScopedConsumeContextProviderTests.cs` | `f67c4dbd3be21386396e20862b724f5b8f607c11ebc4048d9c1ef220087d9199` |

Cumulative personal source admission is 322/4,116 current C# files.

## Proof

`Contract_DeclaresNullableContextAndReturnsOnlyAvailableSnapshots` proves nullable reflection
metadata on the interface and implementation, and proves empty, unavailable and available states
for both the concrete implementation and the compatibility default implementation. The normal and
deferred selector tests use providers whose legacy `HasContext` member throws, proving the
selectors obtain and reuse one atomic snapshot. The Entity Framework factory test applies the same
probe to both reliable and transactional factories.

The Core and Entity Framework requirement variants are embedded in their requirement projections.
Final requirement-file SHA-256 values are
`635040c2a706aeb63d150451b85faca4ae231fe3e3c2ab92c3486deaf059e880` and
`2255a3600454328932518d25e31b46d2e638e68be7641481fafb5a89164f3e63`, respectively.

Seven successfully compiled single-cause mutants were killed and restored: make the default
implementation advertise the unavailable sentinel; retain the concrete unavailable sentinel in a
failed `out` result; restore the normal selector's two-call check; restore the deferred selector's
two-call check; restore the reliable EF factory's two-call check; restore the transactional EF
factory's two-call check; and restore non-null metadata on the concrete `GetContext()` method. All
product sources were restored to the personally re-read final bytes before the final gates.

Final focused Core Cobertura is
`/private/tmp/vicione-servicebus-iteration-175-core-final.cobertura.xml`, SHA-256
`38e8e2f4d42f0c4b24589e5c524715b45044f5e1ee51253fbdbe0d1fe5901a98`. The interface,
concrete provider, normal selector and both deferred selectors report 100% line and branch
coverage. Full EF Cobertura is
`/private/tmp/vicione-servicebus-iteration-175-ef-full-final.cobertura.xml`, SHA-256
`f7479431fb123b16bf03a916844d47952a0e9929a6c5aaf78b41c0a8911c6ae0`; both changed EF
factories report 100% line and branch coverage. Maximum measured target complexity and CRAP is 4.
Core and EF sorted-display-name SHA-256 values are
`2af393f303235cf744133e22e70b2ffdd84f2edf9aa52f67c973b350c0330f43` and
`ab080c37a5b8134a2269808cb03c6424c76cd90c2e997efe7eb49a3bc5946ebb`.

| Gate | Result |
| --- | --- |
| Focused Core owning tests | 8/8 passed |
| Focused EF owning tests | 13/13 passed |
| Full Core Release | 4,819/4,819 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release product/Core/EF/local builds | 0 warnings, 0 errors |
| Product/Core/EF format | Exit 0 |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 7/7 compiled mutants killed |

The optional full local-integration invocation was also attempted: its one configuration-only test
passed and 59 PostgreSQL-dependent tests failed before execution because the canonical local
endpoint/credential profile is not present in User Secrets. No credential was inferred or written;
the independent local requirement projection remains green. Core remains serialized because
Iteration 155 demonstrated a pre-existing parallel-suite race.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-175-atomic-scoped-consume-context-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.

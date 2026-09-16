# Iteration 177 — activity and filter scope lifecycle admission

## Result

This packet personally reads the complete filter-scope provider and contracts plus the complete
execute/compensate scope providers, contracts, created contexts and existing contexts (12 files /
477 lines), and the complete owning test file (469 lines). All twelve source files are newly
admitted.

Filter scopes now select the nearest service provider in deterministic order: the pipe-context
payload, the nested consume-context payload, or a newly created dependency-injection scope. Only a
newly created scope is released, synchronous and asynchronous scopes are both supported, and
repeated or concurrent disposal releases the scope exactly once. Execute and compensate providers
validate the context before observing caller cancellation. Their created contexts restore ambient
consume state before releasing the owned scope through the shared cleanup state machine; existing
contexts restore ambient state without releasing the borrowed scope. All public constructors,
scope acquisition methods and probe methods validate required inputs.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 12 / 477 | `6217951d1042d3d10fcd634ba67f763a2b9f7d1c8578c5c8181f55728bb07b0a` | `ef174871064a2c3a8846aeddca95940b4c931eec225b008bb4063a0f15ef4244` |
| Tests | 1 / 469 | `26066c542e34fdeda0c0300996099afa5ee77a9a66d68ac041ee9a9509377cc9` | `12f67e146a612279b26d807c1c21e0aa9db6e78f71635705546781cfc27cddab` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 176 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/CompensateScopeProvider.cs` | `f7ebf33d566aaa425e5d7c895fdf4af018fedf74ca6473d5e4279e4771de90ba` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/CreatedCompensateScopeContext.cs` | `f140e289d4f566a02f0011aab8119217cae782af7cb7b7e69395b821a39ebbb8` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/CreatedExecuteScopeContext.cs` | `5e1c4ec53881998984757aaaca46f19041806ca0376174fc688d9f3fbbf94def` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ExecuteScopeProvider.cs` | `5f468235a9c4a8f79c7652b7e3ecf38b370fd397dbf6f51ca583c10c60438569` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ExistingCompensateScopeContext.cs` | `c26aa7e1d7e3eedc10e4104249967f776d742fea9890f2c6b6d321450bcb07f2` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ExistingExecuteScopeContext.cs` | `db2b8323cf5bbe898a8f52320404163c66bcfcfccf65dd0156e1bad1d8c791c8` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/FilterScopeProvider.cs` | `96edef6c6e20813e23abaf59ffaa0ab5c706f94b1ac8983aeb8fb1207c396cc9` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ICompensateScopeContext.cs` | `7a7a47e617ffe2f135a8b04f7213f3348776ea8cc65c89000822e350a660db12` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IExecuteScopeContext.cs` | `9a636425696999725d08adfab3d7de7d54ee9d7f79db60e9db36ca1240521fe9` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IFilterScopeContext.cs` | `f5e436a15b27adf3814e9c1af20a031558db10e3e1751c3cb258345bd59281e4` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IFilterScopeProvider.cs` | `8e1055239af543b4ba0d4be3593de9fd032910ddd11230e0e47d90edc43370cb` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ServiceScopeLifetime.cs` | `93ffade48143e2093298861aa6e4dc6c7eca57fd915e18df13ab263f94f3dcc0` |
| Test | `tests/ViciOne.ServiceBus.Tests/DependencyInjection/ActivityAndFilterScopeLifecycleTests.cs` | `0b77628ae069dd1a36c294940a6535b0569e9d7949ad01807c593c09e0abd433` |

Cumulative personal source admission is 345/4,118 current C# files.

## Proof

The six owning tests prove public dependency boundaries, malformed null service-provider payloads,
registration-context construction, direct and nested borrowed provider selection, created-provider
fallback, synchronous and asynchronous scope release, cached filter resolution, exact-once cleanup
under repeated and concurrent disposal, restore-before-release ordering, borrowed-scope ownership,
service resolution, both execute/compensate provider paths, probe boundaries, and preservation of
the caller's cancellation token.

Six requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`7d7879cbba3833016ab475e05a6a3dcb9e31c4ff540e417fd5d266944243f130`.

Eight successfully compiled single-cause mutants were killed and restored: remove the service-scope
exactly-once gate; skip asynchronous service-scope disposal; ignore the nested consume-context
provider; skip filter-scope cleanup; observe cancellation before compensate-context validation;
observe cancellation before execute-context validation; treat a created compensate scope as
borrowed; and release a borrowed execute scope. All product sources were restored to the personally
re-read final bytes before the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-177-final.cobertura.xml`,
SHA-256 `c2d6726e60ce31d3d6e41cd66d4c8b13a6b4715ee81077b3788b884ad5b1f8c1`.
Every executable target class reports 100% line and branch coverage. Maximum reported target
complexity and CRAP are both 8. Unit sorted-display-name SHA-256 is
`2490a9295fe119a578e2937e7d5b375b841846df64b4a49477101b6cc12e01f4`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 6/6 passed |
| Full Core Release | 4,836/4,836 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release product/Core/EF/local builds | 0 warnings, 0 errors |
| Product/Core format | Exit 0; 0 files required formatting |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-177-activity-filter-scope-lifecycle-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.

# Iteration 176 — consume scope lifecycle admission

## Result

This packet personally reads the complete consume-scope provider, contracts, created and existing
context implementations, and consumer factory (11 files / 723 lines), plus the complete owning
test file (574 lines). All eleven source files are newly admitted.

Created consume contexts now own one shared, race-safe lifetime that restores the ambient consume
context before releasing the dependency-injection scope, attempts both cleanup stages, preserves
one failure exactly, aggregates two failures, and runs at most once under repeated or concurrent
disposal. Existing contexts release only their restoration handle and never release the borrowed
scope. All six public context variants validate constructor dependencies; the generic service and
context operations validate their behavioral inputs. The scope provider validates context before
observing cancellation, and the consumer factory validates its provider, context, next pipe and
probe context before invoking collaborators.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 11 / 723 | `0aa4160bb9e4518bdfad4aaab0a8355867ca51971cfc96c8d07173b52ff1a831` | `c4d707cd5702a97cfb78dda3a586102c03aee02dbcec4e48f2b9880123d1ee4c` |
| Tests | 1 / 574 | `a52b08509c98920e6271e6ba639bf5b8c6d6409b1504c98797edefbabbcb4005` | `bb925038a10fa43d6786ed3129eb6e032afddada4bcd28821f53e3bc83131264` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 175 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/BaseConsumeScopeProvider.cs` | `c3b78593ce7d2d4b28939dedf9a76f992a3dcc627212e1c21e9e31c0c151c7b1` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ConsumeScopeLifetime.cs` | `f99a3c70b5db3e507ee1124a4c5cf887c5a1e60774df1050ec06697303d106f3` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ConsumeScopeProvider.cs` | `3750b38ebbda662be62c63b589fbab1a06d9deb4e2dc58d8884c244dc49d31ce` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/CreatedConsumeScopeContext.cs` | `e1382db4f9c8b4d02a877724c19e15bc7b7c10ccad537b9a22abbc4fa7906a36` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/CreatedConsumerConsumeScopeContext.cs` | `5acb1230e873284bc83d80b6651f6562df4ddc59eb3e6ce7af06b38761031044` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ExistingConsumeScopeContext.cs` | `0b43522c6d8d4a62607f372a769276dad1559bc7347e0ffd2c3c2a34aae1105e` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ExistingConsumerConsumeScopeContext.cs` | `10e23b41188c31f1d397a062b9cbe314e7be19042d7466246cf819d82e861d7c` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IConsumeScopeContext.cs` | `949d25896661d3a91caf2745c3aae5dfe8871d290241c029c703f1c542bc6d54` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IConsumeScopeProvider.cs` | `5b5c6949fedc0a80084de5b02d5b2878837494f7c273f582942e0bc9251fabf4` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/IConsumerConsumeScopeContext.cs` | `cdf6881daf80975e996dfc4390e244cfcf1b53e5f3b789afb547e2850335e3b5` |
| Source | `src/ViciOne.ServiceBus/DependencyInjection/ScopeConsumerFactory.cs` | `4ac00b8633c77db1ce1c1add7585c734b42885c51955a61e5a83cfb96330d016` |
| Test | `tests/ViciOne.ServiceBus.Tests/DependencyInjection/ConsumeScopeLifecycleTests.cs` | `06ee5449ca690dfb0aab41f789888ac92fa2965774314a0e1ab76bb65c3c61d1` |

Cumulative personal source admission is 333/4,116 current C# files.

## Proof

The eleven owning tests prove exact public constructor and method boundaries, registration-context
compatibility, diagnostic probing, caller-cancellation identity, all three scope-provider overloads,
created and borrowed ownership, consumer-resolution success and failure, service resolution and
activation, ambient-context publication, consumer-factory forwarding, synchronous and asynchronous
scope disposal, restore-before-scope order, repeated/concurrent idempotence, and preservation of
single, dual, operation and cleanup failures.

Eleven requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`033d37c60577ad66eb38a772932874463da16b0aab718e883fcd45ed9b9ef7af`.

Eight successfully compiled single-cause mutants were killed and restored: remove the exactly-once
disposal gate; reverse restore/scope order; stop cleanup after restoration failure; observe
cancellation before validating context; remove the created-scope constructor guard; release a
borrowed scope; remove the consumer factory's next-pipe guard; and swallow a lone restoration
failure. An intermediate `throw exception` probe was rejected by the analyzer and is not counted as
a compiled mutation. All product sources were restored to the personally re-read final bytes before
the final gates.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-176-final.cobertura.xml`,
SHA-256 `31ec8097befdaad4d2558bd11c54e0d41dac19ea6842b653a7362ba14ac654bc`.
The lifetime, provider, all six created/existing context types and consumer factory report 100% line
and branch coverage. Maximum measured target complexity and CRAP is 14 for the cleanup state
machine; every other measured target is at most 8. The surrounding unchanged base provider reports
87.18% line and 65% branch coverage; remaining paths are its scheduler-payload rebinding and rare
pre-publication cleanup combinations, explicitly retained for a connected scheduling/scope packet.
Unit sorted-display-name SHA-256 is
`cf92876224088356eb02dbc74923802fb5e59cc587355d3795681a61e4973bf8`.

| Gate | Result |
| --- | --- |
| Focused owning tests | 11/11 passed |
| Full Core Release | 4,830/4,830 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Strict Release product/Core/EF/local builds | 0 warnings, 0 errors |
| Product/Core format | Exit 0 |
| Unit/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The local database-dependent matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-176-consume-scope-lifecycle-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication remains an independent delivery step and cannot pause or
deactivate the active goal.

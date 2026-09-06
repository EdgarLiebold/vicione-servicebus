# Iteration 9 Validation

## Scope

Iteration 9 makes dependency ownership and package consumption explicit and verifiable:

- all centrally managed projects enable NuGet central transitive pinning;
- all 82 project and file-tool lock files remain tracked, with 599 central-transitive entries resolving the exact centrally declared minimum;
- unused central declarations are rejected and four stale entries were removed;
- provider-testing packages depend on the dependency-injection abstractions rather than the container implementation;
- capability packages expose only the intended direct project and package edges;
- all 30 documented ViciOne packages are listed in the NuGet catalog;
- the package gate packs the 19 packages needed by the developer journeys and executes three isolated provider-testing consumers, each with exactly one direct ViciOne package reference; and
- the public API inventory excludes nondeterministic archive and binary hashes and is byte-identical across independent fresh-package runs.

The Amazon SQS test project directly uses `ServiceCollection`, `ServiceProvider`, and
`BuildServiceProvider`; it therefore declares its own container dependency instead of receiving it
accidentally through a product testing package. The Entity Framework Core package no longer
declares a redundant memory-cache dependency, and the state-machine visualizer now depends on the
abstraction and saga layers without an unnecessary core-project edge.

## Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Central transitive pinning | disabled in 76 centrally managed projects | enabled and evaluated as `true` in all 76 |
| Central transitive lock versions | 15 mismatches across 7 package identities | 599 entries equal their declared central minimum |
| Central package catalog | 4 stale declarations | no unused declaration |
| Provider-testing dependency boundary | 4 packages referenced the full DI container | all 4 reference DI abstractions only |
| Capability dependency boundaries | visualizer and EF Core each had an unnecessary direct edge | both exact dependency sets pass |
| Provider-testing package acceptance | packages were neither all packed nor isolated from sibling packages | 3 package-only consumers restore, build, and execute |
| NuGet package documentation | 11 delivered packages absent | all 30 packages documented |
| Packed API inventory determinism | identical packs produced different archive hashes | 2 independent runs produced the same byte stream and hash |

The initial complete architecture run after introducing the guards reported exactly seven failing
contracts among 215 tests. The corrected architecture profile passed 215 of 215 with no skipped
tests. No production feature was removed; the newly packaged testing providers are additionally
executed from their delivery artifacts.

## Mutation evidence

Four isolated dependency regressions were introduced and removed:

1. Disabling central transitive pinning caused the evaluation guard to fail for all 76 centrally managed projects.
2. Changing one `CentralTransitive` lock entry from `3.1.8` to `3.1.7` caused the exact lock-version guard to fail.
3. Restoring the full dependency-injection container in one provider-testing project caused the package-boundary guard to fail.
4. Adding a second direct ViciOne dependency to an isolated Event Hubs testing consumer caused the one-package isolation guard to fail.

An additional determinism probe demonstrated the corrected behavior: two identical pre-correction
NuGet packs had different archive SHA-256 values, while two complete corrected API inventories were
byte-identical at SHA-256
`9ab6338dc80bde415609ac282c141f4c006168d6a9e10b964aea7c2137f44c27`.
All mutations were removed before final validation.

## Test-quality review

The architecture assertions evaluate effective MSBuild properties, direct package/project edges,
all governed lock files, the complete central catalog, the CI script, and the workflow entry point.
The three executable consumers restore solely from freshly packed ViciOne artifacts plus NuGet.org;
each consumer directly names one provider-testing package and exercises its public surface. This
prevents a sibling package from masking a missing NuGet dependency. Tests use exact collection,
identity, version, and path assertions and introduce no sleeps, polling, skipped cases, or swallowed
exceptions.

## Repository validation

| Gate | Result |
|---|---|
| Engineering locked restore | PASS — all 76 solution projects restored in locked mode |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,818 passed, 0 failed, 0 skipped |
| Complete Architecture profile | PASS — 215 passed, 0 failed, 0 skipped |
| Fresh-package developer journey gate | PASS — 18 journeys, 19 packages, 3 isolated consumers |
| Independent packed API inventory repetitions | PASS — byte-identical, 17 assemblies, 21,456 lines |
| Shipping solution pack | PASS — 26 packable artifacts from 30 shipping-solution projects |
| Current NuGet advisory inventory | PASS — 0 findings, 0 unresolved dependency paths |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Git whitespace validation | PASS |

On this macOS x64 host, the unsigned NuGet-cache copy of `Grpc.Tools` `protoc` remained in an
uninterruptible kernel state. Copying it to `/private/tmp`, applying an ad-hoc signature, and setting
`PROTOBUF_PROTOC` made the same full Engineering build complete normally. The reproducible local
workaround is recorded in `docs/build.md`; no tracked dependency or product behavior is changed by
it.

This is internal engineering and adversarial-review evidence, not independent external acceptance.

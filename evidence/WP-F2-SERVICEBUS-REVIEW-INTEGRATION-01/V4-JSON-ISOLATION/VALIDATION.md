# V4 per-bus and per-endpoint JSON-isolation validation

## Bound inputs

- Integration baseline commit: `44374d18b8413b86d81512675d3a2365db2c3813`
- Integration baseline tree: `4237d1f38c197f86f02c035536f8d81f592101dd`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head:
  `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commit:
  `08ef1bfaf36d9b25f59dd5396511e29de5551157`
- V5 and V5.1 contain no later correction to this JSON-isolation core. Their payload-admission
  changes remain a separate package.
- `review/**` remained unchanged and untracked throughout the integration.

## Integrated behavior

Every bus now owns a private System.Text.Json configuration chain. Materialization creates a
defensive, read-only `JsonSerializerOptions` snapshot and binds each JSON serializer factory to that
snapshot. A receive endpoint inherits the parent policy, can transform its own copy, and cannot
modify its parent or siblings. Envelope and raw serializer/deserializer pairs share both the local
policy and the same bound runtime instance.

ServiceBus-owned infrastructure values use the separate immutable `ServiceBusMetadataJson` codec.
Persisted headers, outbox state, scheduler data, transport properties, saga metadata, message-data
references, health data and MessagePack interop therefore no longer depend on a user payload policy.
The ambient `ObjectDeserializer.Current` path and mutable default serializer state were removed.

The donor was not copied mechanically. The current native test tree was migrated instead of its
retired test framework, all surviving tests stopped installing and restoring process-wide JSON state,
and the three benchmark callers that the donor left on the removed static singleton were migrated.
The obsolete TODO entry was removed after its acceptance conditions passed.

## Public API disposition

This greenfield A+ baseline deliberately removes the mutable public
`SystemTextJsonMessageSerializer.Options`, the process-wide `Instance`, the parameterless serializer
constructor, and the ambient `ObjectDeserializer.Default`/`Current` setters. Keeping deprecated
setters would preserve the unsafe global ownership under another name. Direct serializer construction
now requires a read-only options snapshot; normal applications configure a bus or endpoint through
`ConfigureJsonSerializerOptions`, and serializer factories reject use until a configuration binds
them. Tests cover each of these boundaries explicitly.

## Executing evidence

- Focused `SystemTextJsonIsolationTests`: 9/9 passed.
- Focused `SystemTextJsonRuntimeIsolationTests`: 2/2 passed.
- All System.Text.Json classes: 71/71 passed.
- Complete MessagePack project: 55/55 passed.
- Final complete Unit/Architecture execution: 3,029/3,029 passed, 0 failed, 0 skipped.
- General LocalIntegration profile: 338/338 passed, including PostgreSQL, Azure Table/Azurite,
  LocalStack, ActiveMQ/Artemis and Event Hubs.
- SQL Server LocalIntegration profile: 60/60 passed.
- Azure Service Bus emulator profile: 24/24 passed.
- RabbitMQ LocalIntegration profile: 17/17 passed.
- Across the five unfiltered profiles, 3,468/3,468 cases passed with no failure or skip.
- Final non-incremental Release Unit/Architecture build with analyzers enabled: 0 warnings, 0 errors.
- All four LocalIntegration solutions build in Release with 0 warnings and 0 errors.
- `CoreRequirements.json` parses and its compiled requirement projection passes.
- `git diff --check`, the changed-C# global-state scan, and the scoped whitespace gate for all 69
  changed or added C# paths pass.
- No active product, test, or benchmark C# source references the removed global JSON state.

The first solution test diagnostic passed `--disable-build-servers` to `dotnet test --solution` and
correctly discovered zero tests with exit 5. Repository guidance already records the cause: the .NET
10 CLI forwards that build-only switch to MTP modules. The authoritative rerun omitted it and passed
3,029/3,029. The first provider attempt likewise stopped before discovery because the post-restart
LocalIntegration executables had not yet been built; all four profiles were built and then executed
unfiltered. Neither diagnostic caused a product or test workaround.

The macOS workspace sandbox denies .NET/MSBuild/MTP IPC. Authoritative builds and tests used the
established external execution profile with isolated `DOTNET_CLI_HOME`, explicit `DOTNET_ROOT`,
disabled multilevel lookup, disabled node reuse and the existing package cache.

## Assertion and anti-pattern review

The isolation cohort contains eleven cohesive facts with exact wire-property, value, exception,
parameter-name, identity, inheritance and mutability assertions. Two real in-memory runtime tests
exercise simultaneously active buses and an endpoint-local override; the latter sends the wire shape
that the endpoint explicitly declares, then proves that an ordinary bus publication still uses the
parent policy. Envelope and raw bodies are inspected independently.

No test uses sleep, wall-clock delay, polling, randomness, a quiet-window verdict, skipped execution,
or timeout-as-success. Bounded waits are failure boundaries only. The endpoint-routing mutation is
killed by that bounded failure when its callback is disconnected; the normal test completes from a
product-owned handler signal.

## One-cause mutation evidence

Each mutation was applied independently, compiled by its focused owner, required to fail, and
immediately restored. The final builds and every unfiltered run used only restored product sources.

| ID | Single changed cause | Causal native owner and result |
| --- | --- | --- |
| M01 | Drop every registered JSON options transform | independent configuration expected snake case but received camel case |
| M02 | Ignore the parent JSON snapshot | child inheritance lost the parent's indentation and naming policy |
| M03 | Freeze the caller's replacement object instead of a defensive copy | the caller could no longer mutate its own replacement after materialization |
| M04 | Bind the envelope factory to the metadata codec | independent bus payload policies collapsed to camel case |
| M05 | Bind the raw factory to the metadata codec | the raw body lost the local snake-case policy |
| M06 | Do not cache the bound factory | serializer and deserializer were different runtime instances |
| M07 | Allow JSON configuration after materialization | the closed-configuration assertion observed no exception |
| M08 | Accept a null transform result as the old policy | deterministic materialization failure disappeared |
| M09 | Disconnect the public bus configurator from serialization | the simultaneous-bus runtime test observed camel case on both buses |
| M10 | Disconnect the public endpoint configurator from serialization | the explicitly kebab-case endpoint could not consume its declared wire shape and hit the bounded failure |
| M11 | Change the stable metadata codec to kebab case | the exact `messageId` metadata contract disappeared |
| M12 | Allow mutable options in the envelope serializer constructor | the immutable construction boundary accepted the invalid input |
| M13 | Allow mutable options in the raw serializer constructor | the raw boundary surfaced the nested envelope parameter instead of rejecting `serializerOptions` itself |
| M14 | Let an unbound envelope factory silently use metadata options | direct factory use no longer failed closed |
| M15 | Let an unbound raw factory silently use metadata options | direct raw factory use no longer failed closed |

No mutation marker remains in the restored tree.

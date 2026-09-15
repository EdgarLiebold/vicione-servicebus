# Iteration 118 — Transport-independent Topology validation

## Verdict

The transport-independent Topology capability meets the iteration's A+ acceptance criteria. The
complete source and every source comment were read manually. No source, test, comment, namespace,
file, or directory was generated. Feature behavior is retained while empty compatibility surface,
pass-through wrappers, unreachable policy, and implementation-only public types are removed.

The overall repository A+ program remains active because other complete source owners still require
the same depth of manual review.

## Architectural boundary

`src/ViciOne.ServiceBus` is the Core project, not an umbrella directory. Independent package and
assembly projects correctly remain its direct siblings. Provider families remain grouped beneath
`src/Persistence`, `src/Scheduling`, and `src/Transports` because they have independent project,
dependency, package, and deployment boundaries.

Transport-independent Topology code now has six exact owners:

| Project | Folder | Files | Lines | Namespace |
|---|---|---:|---:|---|
| Core | `Advanced/Topology` | 4 | 573 | `ViciOne.ServiceBus.Advanced.Topology` |
| Core | `Configuration/Topology` | 28 | 961 | `ViciOne.ServiceBus.Configuration` |
| Core | `Topology` | 11 | 373 | `ViciOne.ServiceBus.Topology` |
| Abstractions | `Advanced/Topology` | 17 | 394 | `ViciOne.ServiceBus.Advanced.Topology` |
| Abstractions | `Configuration/Topology` | 33 | 1,084 | `ViciOne.ServiceBus.Configuration` |
| Abstractions | `Topology` | 7 | 946 | `ViciOne.ServiceBus.Topology` |
| Total |  | 100 | 4,331 |  |

An architecture test declares the complete filename manifest for all six folders, checks every
declared namespace, and rejects both former `Topology/Configuration` directories. No C# product
file is stored directly in `src`; the only direct file is `Directory.Build.props`, which applies
build policy to the project tree.

## Remediation

- Removed the empty public `IMessageTypeTopologyConfigurator` marker.
- Removed four one-method observable wrappers; each root topology now owns `Connectable<TObserver>`
  directly.
- Internalized concrete correlation selectors, per-message partition/routing/serializer
  conventions, and their filter topology implementations.
- Renamed `TryGetSetCorrelationId` to `TryGetCorrelationIdResolver`.
- Renamed `SetSerializerSendConventionExtensions` to `SerializerConventionExtensions` while
  preserving both public `UseSerializer` overloads.
- Removed the dummy convention-cache constructor argument and made concurrent first access publish
  exactly one lazily created value.
- Removed the unreachable default exclusion of the `JsonElement` struct and tested exclusion with
  a real reference message contract.
- Preserved explicit correlation-resolver precedence over inferred interfaces and properties.
- Enforced structural, name, and identifier identity independently in entity collections.
- Made entity-name calculation single-evaluation under concurrency and rejected empty formatter
  output.
- Preserved delegated and implemented state through every child topology builder.
- Added owning-boundary guards for required collaborators, factory results, update results,
  formatters, content types, messages, runtime types, and addresses.
- Closed concrete adapters, formatters, pipe specifications, and root conventions that have no
  supported inheritance contract.
- Updated ActiveMQ and SQL tests to the central precise entity-conflict exception contract after
  the canonical full profile exposed their obsolete provider-specific text assertion.

## Mutation evidence

### Core group

Four simultaneous counterchanges bypassed the cache, reported the wrong invalid-type argument,
reversed explicit correlation precedence, and silently reused a conflicting named entity. The
focused 35-case profile produced six causal failures. The accepted files were restored to:

| File | SHA-256 |
|---|---|
| `TopologyConventionCache.cs` | `683a18c4abc503ca843503b58f0860f7e62008f3cce7d479d170236e4b388988` |
| `ConsumeTopology.cs` | `94a9559b73c4825ede4e59497f59cbc7d7f23bf2061dec949bf90a193909e2c6` |
| `CorrelationIdMessageSendTopologyConvention.cs` | `ead08a8f3affd9d167200a7d4b48e60e291e2a5cd9d70bfa4e0b35fdb8474c83` |
| `NamedEntityCollection.cs` | `927874f9e609e007d5f7db0ee25cba0138873dec72242da2adb0f615d2b1078e` |

### Abstractions group

Four simultaneous counterchanges removed the entity-name double check, discarded consume
child-builder state, accepted a null publish-convention factory result, and inverted publish-to-send
exclusion. Exactly four of 50 focused cases failed. The accepted files were restored to:

| File | SHA-256 |
|---|---|
| `MessageTopology.cs` | `480b9bfd6e660bcabfb241d810518d7e793460706563f6e2b267a91554c3d50b` |
| `MessageConsumeTopologyPipeSpecification.cs` | `fc01975ef15f651f6a7b0e73de8a0ed7dfd916f45cfac8f9a3442e2085930a6b` |
| `MessagePublishTopology.cs` | `47ad358d9891ea335721660a3ea2f33828b4310b778bfe1e8868225933f66b6f` |
| `PublishToSendTopologyConfigurationObserver.cs` | `8967dcc15d77607b5043a3f1adc48ae57db96738e96579deebbb8286bb46ce5b` |

The restored focused profiles pass 35/35 and 50/50. Full Core and Abstractions hosts pass
3,611/3,611 and 692/692 respectively.

## Coverage and risk

| Scope | Line coverage | Branch coverage | Maximum CRAP | CRAP above 30 |
|---|---:|---:|---:|---:|
| Core Topology | 520/536 (97.0149%) | 124/140 (88.5714%) | 10 | 0 |
| Abstractions Topology | 495/495 (100%) | 157/176 (89.2045%) | 8 | 0 |
| Combined owner | 1,015/1,031 (98.4481%) | 281/316 (88.9241%) | 10 | 0 |
| Complete loaded product in Core run | 49,224/63,608 (77.3865%) | 17,143/24,451 (70.1117%) | — | — |

The two Core methods reported with zero visits are instrumenter projections: the one-shot
`SeparatePublishFromSend` lambda is covered through its public behavior, and the non-generic
enumerator forwards to the covered generic enumerator and has an explicit behavior assertion.

| Artifact | SHA-256 |
|---|---|
| `/private/tmp/vsb-iteration118-current/core-final.cobertura.xml` | `7849d9d884f5da04d69434cffb973ba63e9cbf03c214c39b5446b8a7dbb3fea6` |
| `/private/tmp/vsb-iteration118-current/abstractions-final.cobertura.xml` | `a9ca59b210b2f7e8de35bb292cf6664c171918c28e34f9a66c3cd0d30054eedb` |

## Test-quality review

The manual audit accounts for 86 new or changed test methods and 90 executed cases. It found one
unnecessary use of `Guid.NewGuid` in a null-boundary setup and replaced it with deterministic
`Guid.Empty`. The final audit has zero Critical, High, Medium, or Low findings.

- Every test has an observable, causal assertion.
- Exceptions use exact types and owning parameter names.
- Ordering, identity, state, filters, addresses, and public/internal type contracts are asserted
  explicitly.
- There are no skips, broad catches, swallowed failures, assertion-free paths, shared mutable
  fixtures, mystery dependencies, random inputs, or coverage-only calls.
- Concurrency tests use explicit signals and cancellation-bounded waits rather than sleeps; each
  kills its corresponding race counterchange.

## Public API and packages

The first package run intentionally failed only the unchanged golden contract after successfully
packing and compiling all consumers. The complete reviewed diff contains 38 additions and 93
removals. The removals are empty markers, pass-through wrappers, or implementation mechanics; the
additions are sealed metadata, corrected factory signatures, the resolver name, and the renamed
serializer extension owner. No feature entry point is lost.

The explicit update gate then passed:

- 18 developer journeys;
- 31 freshly packed ViciOne packages;
- 3 isolated provider-testing consumers;
- 30 runtime package APIs;
- 18,824 API lines;
- SHA-256 `493a793a915b88ac2ea9b81cb6be8057ecf9beff80535f063aab8c3040d12a4f`.

## Final gates

| Gate | Result |
|---|---|
| Shipping locked restore | PASS |
| Engineering locked restore | PASS |
| Unit/Architecture locked restore | PASS |
| Engineering format verification | PASS — no change |
| Unit/Architecture format verification | PASS — no change |
| Serial Engineering Release build | PASS — 77 projects, 0 warnings, 0 errors |
| Canonical Unit/Architecture profile | PASS — 6,638/6,638, no skip |
| Async and source-layout profile | PASS — 54/54 |
| Requirements | PASS — 36 JSON files, 5,027 unique variants |
| Package/API gate | PASS |
| C# preprocessor directives | PASS — none under `src` |
| Retired Topology identities and paths | PASS — none |
| SDK patch pinning | PASS — stable `10.0.x`; `global.json` selects only MTP |
| Empty source or test directories | PASS — none |
| Git whitespace | PASS |

The sole `NotImplementedException` product reference classifies the real BCL exception as
non-retryable. RabbitMQ's `NotImplemented` constant is AMQP reply code 540. Neither is dummy or
placeholder functionality.

Protected `review/` and `TestResults/` were neither modified nor staged.

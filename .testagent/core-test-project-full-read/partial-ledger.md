# Core test project personal-read ledger (incomplete)

Baseline commit `92887378900c0a70747662249e50db48be45da90`, owning test tree
`0eed11cd40ed9528f1f1046b70ab6d41c9815ede`: 702 tracked paths at this commit.
The lead must personally read **all** tracked project files and effective shared
build/package/fixture/CI inputs. This partial ledger is not a §4.3 clearance;
the historical pre-edit order deviation also requires explicit disposition.

| Personally read complete path | Git blob at baseline | Semantic boundary inspected |
| --- | --- | --- |
| `tests/ViciOne.ServiceBus.Tests/ApiSurfaceGlobalUsings.cs` | `257bd3e84080702d54f24deb2764cee408fcd302` | Global namespace imports cover core, advanced, configuration, courier, futures, job service, operations, providers and sagas. |
| `tests/ViciOne.ServiceBus.Tests/Architecture/OutboundNetworkBoundaryTests.cs` | `b8d07f7cdac6b9ec48660db1269b2096bb506b79` | InMemory lifecycle + publish checked for HTTP diagnostics; synthetic listener control and two-product-assembly byte scan bound the negative assertion. Listener's nested subscription lifetime deserves later review. |
| `tests/ViciOne.ServiceBus.Tests/Architecture/RemovalBoundaryTests.cs` | `9b66da863b8ed1a289e64b7f1971d9e355e9af24` | Removed/retained namespace and type inventories, registration/assembly-byte controls, metadata namespace and exported-type visibility checks; byte scan is narrower than behavioral absence. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheConcurrencyTests.cs` | `feafac0e79d322a5f9c5e01702fc37bed20c176a` | Single-flight winner/loser, pending identity and fault recovery, capacity backpressure, direct-add ownership/cancellation, clear/reuse and index visibility. Several direct failure awaits rely on test-run cancellation rather than their own `WaitAsync`; reconcile the file-level boundedness comment later. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheContractTests.cs` | `4f9a739b648ad5c2d8552bf1f309ddf131f1443e` | Invalid options/metrics, null/pre-canceled boundaries, direct-add identity, factory creation, index propagation/clear/removal, pending removal, duplicate/projection-failure ownership. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheExpirationTests.cs` | `9656ef7bb3521913e71c60d4bd2692447d619233` | Fake/frequency clocks for TTL and boundary ticks; LRU/usage retention, capacity/high-churn eviction, periodic/explicit cleanup, pre-canceled cleanup and disposal signals. Some requirement variant names refer to scheduler fallbacks while individual oracles exercise only cache cleanup behavior. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheGenerationAndLockingTests.cs` | `1514c588ba1efb6866b26c7c6580f7403e0c1c93` | Keyed facade identity/disposal, observer commit visibility, controlled clock/subscription reentrancy against lock inversion, generation invalidation, caller/lifetime cancellation, selector resource lease during disposal. Blocking callbacks use bounded `WaitAsync`; some ordinary async assertions rely on test-run cancellation. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheInitializationTests.cs` | `2095fadab70ed882a5e7d9844f6cdf02dcc69607` | Constructor timer fault releases token/gate; successful timer policy and once-only async disposal checked via explicit timer provider/field ownership probes. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheLifecycleTests.cs` | `a6f1b9547fcd7535e52cd2976ce69f7e50add9d0` | Absolute/sliding usage, rejected factory-result disposal and ownership, async/sync disposal precedence, clear/dispose in-flight cancellation, partial event-subscription compensation. Some direct async assertions lack an individual timeout despite the file header. |
| `tests/ViciOne.ServiceBus.Tests/Caching/ResourceCacheObserverAndDisposalTests.cs` | `7cd01def21f8813779916e871b9ec74a02231a58` | Observer fanout under exceptions/logging failures, serialized and reentrant callbacks, add/evict/clear disposal waits, one-time resource/timer cleanup. Several requirement names promise rethrow, while the assertions establish isolated logging/continuation instead. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ClientFactoryExtensionsBoundaryTests.cs` | `fb834438796c12063b306c05a1f39e8174416e0f` | Null entry boundaries across bus/context/connector/handle; advanced client-factory timeout, response endpoint kind and handle stop ownership via strict dynamic proxies. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ClientFactoryRequestContractTests.cs` | `926ea0981f098b1cb7ae2555e0740657e5bd0749` | Eight direct and eight scoped CreateRequest shapes plus four scoped client-resolution shapes, preserving route/consume context, message/values, request metadata, timeout and cancellation/timer disposal through a recording endpoint. No physical transport is exercised. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ContainerRequestClientRegistrationTests.cs` | `3578fe1d11687464a8cf953d555e27690b0e5d4d` | Harness round-trips for addressed generic/runtime and open-generic registration plus scoped nested client; asserts transport routing, correlation/causation and DI scope identity. InMemory harness, not durable-provider acceptance. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ReceiveEndpointClientFactoryContextTests.cs` | `d0d83d40fbb23d5eb25f212c0612a31f142f355b` | Client factory response address/clock/pipe connection projection, readiness-gated addressed/publish resolution token and exact send arguments, owned endpoint stop and constructor null guards via recording endpoint/handle. |
| `tests/ViciOne.ServiceBus.Tests/Clients/RequestClientBoundaryTests.cs` | `7847d60887ae1de2101e0cb4b0d47e4dfba9eeb4` | Constructor/entry-point null boundaries over direct/scoped request clients, response registration close, probe/host metadata, shared idempotent disposal/failure and pre-canceled endpoint resolution token via fail-fast probes. |
| `tests/ViciOne.ServiceBus.Tests/Clients/RequestClientLifecycleTests.cs` | `22aa095285bce8056698909eb4b5ae4528173c03` | Request deadline/transport TTL separation, timer ownership on duplicate send and cleanup failure, cancellation-vs-timeout terminal race, handler registration/order, late timer release, synchronization-context avoidance, fault-vs-response first-wins and null send message failures. |
| `tests/ViciOne.ServiceBus.Tests/Clients/RequestClientMetadataTests.cs` | `197ff0e4eab953498e64a4892912dad2143da9a6` | InMemory request-option envelope/correlation, caller header snapshot under gated send, lifetime/deadline with fake clock and delayed endpoint, unsupported partition, header round trip, independent side-effect TTL and typed fault envelope. |
| `tests/ViciOne.ServiceBus.Tests/Clients/RequestExtensionsBoundaryTests.cs` | `8388da46271c1d07b166b8742319775f5ba63c4b` | Bus and consumed-context RequestAsync overloads reject required null bus/address/message/values/context before proxy bus invocation. |
| `tests/ViciOne.ServiceBus.Tests/Clients/RequestExtensionsContractTests.cs` | `e73d6430890c93cb982693e7b9cea698a07cf345` | InMemory bus and consume-scope typed/values, explicit/convention request overloads with callback header/response identity; initialized two-/three-response branches. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ResponseAcceptanceTests.cs` | `7d211aa068ac4986cf2c31191b30fa31fde19116` | Response acceptance requires response address plus exact case-sensitive accepted-contract URN; missing/wrong/case-changed forms rejected under InMemory harness. |
| `tests/ViciOne.ServiceBus.Tests/Clients/ResponseMatchingTests.cs` | `9e3275d3b8707e502f1f2aa79b449f21f14f975a` | Transport and mediator single/multi-response selection, typed fault/rejection, pattern deconstruction and canceled loser tasks; negative task assertions have two-second safety timeout. |
| `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj` | `d78fb5af245de421d4d2e9bda7a51ed9fc6161a9` | MTP xUnit executable, package and product/support references, Protobuf fixture generation and embedded Core requirements; CS1591 suppressed for this test project. |
| `tests/ViciOne.ServiceBus.Tests/packages.lock.json` | `5cc36f6191c45dfd05c2be832143c717a0bf86ec` | net10.0 locked direct graph (Google.Protobuf, Grpc.Tools, DI, fake clock, MTP coverage and xUnit) plus transitive MTP/telemetry dependencies and referenced product/support projects; content hashes bind the resolution. |

Current personal-read count in this ledger: 23/702 tracked owning-project files.
The three newly committed iteration-221 files were read in targeted work, but are
not counted here until their complete current blob and semantic reading are
recorded consistently against the same baseline. No parser/set-equality claim yet.

Effective external build/test configuration personally read at this baseline:

| Path | Git blob | Effective boundary |
| --- | --- | --- |
| `Directory.Build.props` | `1648ed1c2d432a8142c3e4c67295357a67bfbf0b` | Nullable/warnings-as-errors, locked restore, deterministic build and shared artifacts root. |
| `Directory.Build.targets` | `223b46defa68639bd909eea137d07bf7e592de1e` | Native-test path classification, lock/TFM/packager fence and MTP portable-PDB exception. |
| `Directory.Packages.props` | `e1ff2a6c99563193c1de899b771916c8453e85ce` | Central package versions and test-tree-only package graph including xUnit v3 MTP v2. |
| `signing.props` | `83c89da42e450db6dfc4a33ceabd187384b5fa4f` | Strong-name key and signing of assembly project. |
| `tests/Directory.Build.props` | `42a535cae9035bb304c293dbd5787a426b1482ac` | Required parent import, net10.0, shared secrets ID and core test artifact identity. |
| `tests/Directory.Build.targets` | `e3635a28e817211db440dd29bd1efaf9fc5b76fd` | Required parent import, canonical MTP config/settings links and entry-package/classification/forbidden-package gates. |
| `tests/testconfig.json` | `cc5dd647d1b9f1efc6c1e39f39f0c913f900f14c` | xUnit skips and warnings both fail. |
| `tests/testsettings.json` | `17b3ed9fd0db4a827dcbf0616c53c5e0b8e7fc9d` | Secret-free UnitArchitecture defaults, 30-second operation timeout and provider localhost endpoints. |
| `.github/workflows/native-tests.yml` | `06d3173f60dacec025faea0cbd0a868163e33e82` | Required separate engineering, complete unfiltered UnitArchitecture, six local provider matrices and package/security/policy gates. Core's solution job enforces at least 4,900 discovered tests; the CI provider lifecycle and policy scripts remain to be read as relevant closure. |

This external configuration list is not yet the exhaustive fixture/CI closure.

# R0-BRK — Anchor reconciliation

Cohort `R0-BRK` (broker transports), work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.

## 1. Scope and completeness (TLP-017)

`READ_MANIFEST.tsv` covers 488 tracked files as `path<TAB>sha256`, sorted `LC_ALL=C`:

| Scope | Tracked files |
|---|---:|
| `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | 102 (98 `.cs`, 1 `.csproj`, `client.p12`, `docker-compose.yml`, `packages.lock.json`) |
| `tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests` | 38 (34 `.cs`, 1 `.csproj`, `docker-compose.yml`, `jolokia-access.xml`, `packages.lock.json`) |
| `src/Transports/ViciOne.ServiceBus.RabbitMqTransport` | 183 |
| `src/Transports/ViciOne.ServiceBus.ActiveMqTransport` | 154 |
| `build/test-infrastructure` | 9 |
| `build/verification/expected/{rabbitmq,activemq}.txt` | 2 |

Verified in both directions against `git ls-files` for the same scope, and byte-for-byte against
`evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/BASELINE_TRACKED_FILE_MANIFEST.tsv`:
488 paths in scope in the baseline manifest, 488 in this manifest, 0 unread, 0 hash mismatches, 0 extra paths.

Note on format: the baseline manifest is `sha256<TAB>path`; the rules file asks cohorts for
`path<TAB>sha256`. This manifest follows the rules file. The integrator has to transpose one of the two.

Reading depth. Every `.cs` file of both test projects was read completely. Of the two product projects,
the public surface and every type the tests touch was read in full — `RabbitMqTestHarness`,
`ActiveMqTestHarness`, `RabbitMqHostAddress`/`RabbitMqEndpointAddress`, `RabbitMqHostConfigurator`/
`RabbitMqSslConfigurator`/`RabbitMqSslOptions`/`ConfigurationHostSettings`, `RabbitMqAddressExtensions`,
`RabbitMqConnectionException`, `TransportLifetime`, the RabbitMQ topology builders and entities,
`ActiveMqHostAddress`, `ActiveMqMessageBody`, `ConsumerEntity`/`TopicEntity`/`QueueEntity`,
`OpenWireHostSettings`/`AmqpHostSettings`, `ActiveMqConsumeTopology`. The remaining product files were
enumerated from `git ls-files` and hashed into the manifest but were not read line by line; they carry no
obligation of their own in this cohort and are declared here as enumerated rather than read.

## 2. Anchor binding

| Anchor | SHA-256 recomputed at this commit | Matches plan §9 | Lines | Identities |
|---|---|---|---:|---:|
| `build/verification/expected/rabbitmq.txt` | `8dbdd5d925abe11cca7f720acff26ee7950b825beff9f04d47fe529fdb459d8e` | yes | 373 | 370 |
| `build/verification/expected/activemq.txt` | `d5bdacf408db44548aaa7328811ea129c0593dfbac4ca2940b141c024f4e7f83` | yes | 181 | 178 |

Three documenting header lines per file, as the plan states.

## 3. Census method

The census was derived from the test sources at the baseline commit, not from the anchors. For every
`.cs` file the enclosing namespace and class chain and every method carrying `[Test]`, `[TestCase]` or
`[TestCaseSource]` were extracted, then each discovered method was reconciled with the anchor on the
canonical form `<Class>.<Method>` after removing NUnit's resolved argument groups. `<Class>.<Method>` is
unique inside each project — checked, no collisions.

| | RabbitMQ | ActiveMQ |
|---|---:|---:|
| test methods discovered in source | 354 | 112 |
| ledger rows written | 354 | 112 |
| anchor identities | 370 | 178 |
| anchor identities bound to a ledger row | 370 | 178 |
| anchor identities bound to more than one row | 0 | 0 |
| ledger rows with no anchor identity | 1 | 0 |

Total: 466 obligations carrying all 548 anchor identities, each exactly once.

## 4. Every difference, explained — never adopted as truth

### 4.1 Anchor identities whose name is not a method name (11)

These are NUnit rendering, not missing or extra obligations. Each is bound to the obligation that carries it.

| Anchor identity | Bound to | Why the names differ |
|---|---|---|
| `Deciding_what_the_management_api_said.the field is absent` (and `…is null`, `…is a string`, `…is a number`, `the answer is not an object`) | `Deciding_what_the_management_api_said.Should_read_an_answer_it_cannot_type_as_unknown` | five `[TestCase(..., TestName = "…")]` on one method; NUnit reports the alias instead of the method name |
| `Should_schedule_in_direct_exchange_type.Should_get_both_messages`, `…fanout…`, `…headers…`, `…topic…` | `Should_schedule_in_any_exchange_type.Should_get_both_messages` | the method lives on the abstract fixture `Should_schedule_in_any_exchange_type`; NUnit reports the four concrete subclasses |
| `Recovering_from_a_broker_outage.OpenWire`, `…AMQP` | `Recovering_from_a_broker_outage.Should_deliver_again_after_the_broker_came_back` | two `[TestCase(..., TestName = "OpenWire"/"AMQP")]` on one method |

### 4.2 Ledger row without an anchor identity (1)

`OBL-R0-BRK-0004` —
`ViciOne.ServiceBus.RabbitMqTransport.Tests.AmazonMqTests.Connecting_to_RabbitMQ_via_Amazon.Should_connect`
(`tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/AmazonMQ_Specs.cs`).

It is `[Explicit]`, so it never ran and is correctly absent from the recorded anchor. Per plan §9 that is
never a deletion rationale on its own, so it is carried as an obligation with disposition `QUESTION`.
It is also the one obligation in this cohort that is not local: it connects to a real Amazon MQ for
RabbitMQ endpoint and reads host, user and password from `TestContext.Parameters` or from
`VICIONE_SERVICEBUS_AMQ_RMQ_HOST/USER/PASS` through `Configuration.cs`. The cohort brief states that
nothing here should be `External`; this row contradicts that and is raised rather than reclassified.

### 4.3 Class-name segments that differ between census and anchor

Where a test class sits below a sibling `namespace` block inside the same file — `PublishSpecs`,
`PublishContracts`, `HarnessContracts`, `JobConsumerTests`, `ConductorTests.Contracts`, `Send_Specs`,
`Batching`, `ObserverTests`, `TopologyTestTypes`, `DelaySubjects`, `TestMessages`, `AmazonMqTests`,
`RoutingKeyDirect`, `RoutingKeyTopic`, `FaultMessages`, `ObserverableMessages`, `Turnout` — the anchor
carries the namespace the runtime type really has and the source census, which resolves namespaces by
indentation, sometimes attaches a sibling namespace that had already closed. Reconciliation was therefore
performed on `<Class>.<Method>`, which is unique in both projects. This is a limitation of the census
tool, not a difference in the tested set; the `sourceSymbol` field of the ledger keeps the census form so
the artefact stays visible to the integrator.

### 4.4 Identity counts per category

370 RabbitMQ identities come from 354 obligations, measured from the generated ledger:

| identities per obligation | obligations | which |
|---:|---:|---|
| 0 | 1 | the `[Explicit]` Amazon MQ obligation of §4.2 |
| 1 | 341 | one identity each |
| 2 | 10 | nine fixtures of `Owning_a_transport_subject` (`[TestFixture("channel")]` and `[TestFixture("connection")]`) plus `HostConfigurator_Specs.Should_set_client_certificate_as_authentication_identity_when_configured` (`[TestCase(true)]`/`[TestCase(false)]`) |
| 4 | 1 | `Should_schedule_in_any_exchange_type.Should_get_both_messages`, reported as its four concrete subclasses |
| 5 | 1 | `Deciding_what_the_management_api_said.Should_read_an_answer_it_cannot_type_as_unknown`, five `TestName` aliases |

341·1 + 10·2 + 4 + 5 = 370.

178 ActiveMQ identities come from 112 obligations:

| identities per obligation | obligations | which |
|---:|---:|---|
| 1 | 61 | one identity each |
| 2 | 44 | 43 obligations on a fixture carrying `[TestFixture("activemq")]` and `[TestFixture("amqp")]`, plus `Recovering_from_a_broker_outage` with its two `TestName` aliases |
| 3 | 5 | four `Configuring_ActiveMQ`/`Delayed_redelivery` cases with an added `"artemis"` `[TestCase]`, plus `Asking_the_runner_to_take_the_broker_away.Should_accept_an_interrupt_the_runner_saw_take_effect` with `[TestCase("exited"/"stopped"/"absent")]` |
| 7 | 2 | the two `ConsumerEntity_Specs` comparer obligations, each with a seven-case `[TestCaseSource]` |

61·1 + 44·2 + 5·3 + 2·7 = 178.

Profile distribution of the ledger: RabbitMQ 242 `LocalIntegration`, 111 `UnitArchitecture`, 1 `External`;
ActiveMQ 72 `LocalIntegration`, 40 `UnitArchitecture`, 0 `External`.

## 5. Open questions for the Lead

1. **`Connecting_to_RabbitMQ_via_Amazon` is an External obligation inside a local cohort.** Open an
   `*.ExternalTests` owner for it, or dispose it explicitly. It may not be dropped for being `[Explicit]`.
2. **The three `"artemis"` flavors** (`Configuring_ActiveMQ.Should_do_a_bunch_of_requests_and_responses`,
   `…_explicit_configuration`, `…Should_succeed_and_connect_when_properly_configured`,
   `Delayed_redelivery.Should_properly_redeliver`, and `Sending_to_a_shared_topic_subscription_endpoint`)
   execute against a second broker that `images.lock.json` itself calls "not the ViciOne fixture and not a
   required gate". They are in the anchor, so they executed. Does the target keep an Artemis fixture, or is
   Artemis compatibility a separate owner?
3. **`OpenTelemetry_Specs`, `Container_Specs` and `PublishTopology`-style DI harnesses set no `VHost`.**
   `ApplyRunScopedCredentials` binds host, port, management port and account but leaves `VHost` at the
   product default `/`. Those obligations therefore ran on the root virtual host while the classic
   fixtures ran on `test`. This is a real behavioural difference in the baseline and it decides which
   virtual host the new fixtures must provide.
4. **`Failure_Specs.DeleteVirtualHost` falls back to management port 15672** when
   `VICIONE_SERVICEBUS_RMQ_MGMT_PORT` is unset, unlike `ExclusiveQueueProbe` and `BrokerTopologyProbe`,
   which throw. In an unset environment this teardown deletes a virtual host on a foreign broker.
5. **`Security_Specs.cs` carries no code at all** — the whole file is a commented-out fixture. It carries
   no obligation and no anchor identity, and it is the only plausible historic consumer of `client.p12`.

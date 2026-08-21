# R0-BRK — Findings

Cohort `R0-BRK`, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`,
baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
Everything below is read from the tracked sources listed in `READ_MANIFEST.tsv`. Nothing was executed and
no container was started.

## 1. Environment classification

Both brokers are local. The profile is stated per obligation in `LEDGER_DRAFT.jsonl`; the required
infrastructure is stated per obligation in the `notes` field.

| Project | `UnitArchitecture` | `LocalIntegration` | `External` |
|---|---:|---:|---:|
| RabbitMQ tests | 111 | 242 | 1 |
| ActiveMQ tests | 40 | 72 | 0 |

`LocalIntegration` — RabbitMQ: `rabbitmq:4.2-management@sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d92400
2773933ab881da49c073b85d`, built with the community plugin
`rabbitmq_delayed_message_exchange-4.2.0.ez` bound by
`sha256:f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1`; `enabled_plugins` is
`[rabbitmq_management, rabbitmq_delayed_message_exchange]`. AMQP 5672 and management 15672 are published
as `127.0.0.1::<port>`, so both ports are ephemeral per run. The account is `vicione_ci` with a per-run
16-byte secret; the virtual host `test` is created over the management API.

`LocalIntegration` — ActiveMQ: `apache/activemq-classic:6.2.0@sha256:992bc29a8459f6772ff14d168a8b011c9cd
e7a769ec01d72788c9cc4e9229564`, one delta against the stock configuration (`schedulerSupport="true"`,
required by the delayed-redelivery obligations) plus `groups.properties` binding `admins=vicione_ci` so
the run account is authorised for the web console and Jolokia. OpenWire 61616, AMQP 5672 and Jolokia 8161
are published as `127.0.0.1::<port>`. Readiness is a successful Jolokia read, not a running process.

Two additional pieces of infrastructure are needed by a subset:

- **Artemis** — `quay.io/artemiscloud/activemq-artemis-broker@sha256:f22d0bf16d7dbb7b928a048f27e4e4f537a
  c9af00a5fd31cf4b73a889cc0bd2a`, endpoint from `VICIONE_SERVICEBUS_ARTEMIS_HOST` /
  `_OPENWIRE_PORT` / `_USER` / `_PASS`. Needed by six anchor identities (three `"artemis"`
  `[TestCase]` variants in `Configuring_ActiveMQ`, one in `Delayed_redelivery`) and by
  `Sending_to_a_shared_topic_subscription_endpoint`, which is a shared durable topic subscription and is
  an Artemis feature. `images.lock.json` says explicitly that Artemis "is not the ViciOne fixture and is
  not a required gate".
- **HAProxy relay plus the runner's outage service** — `haproxy:3.4.3-alpine@sha256:fb87fc81943143b9acae
  a7442973e6ba654035fff76ffe7af6829dd1bcb0f7a5`, `retries 0`, no health-based failover, resolving
  `activemq` on every connection through Docker's embedded DNS at `127.0.0.11:53`. Needed by exactly one
  obligation, `Recovering_from_a_broker_outage` (two identities, OpenWire and AMQP), together with the
  control directory named by `VICIONE_SERVICEBUS_FIXTURE_CONTROL`. Without the relay a restarted
  container comes back on a new ephemeral port and no client can reconnect to the address it holds.

## 2. Credential and configuration inventory

### 2.1 How the tests reach the broker today

Nothing is baked into an image and nothing is on disk. `build/test-infrastructure/new-run-credentials.sh`
generates 32 hex characters from `/dev/urandom` per broker per run, exports them, and under GitHub
Actions registers them as masked secrets. The account **name** is fixed (`vicione_ci`, and `sa` for SQL
Server) because ActiveMQ authorises the console by role and the role binding lives in a config file; the
secret is regenerated every run. `compose.yaml` uses `${VAR:?…}`, so an unset variable fails the run
instead of falling back to a well known account.

The canonical runner starts compose under a project name equal to the run identity, asks Docker which
loopback ports were actually bound, and hands the test process a projection through these variables:

| Variable | Read by |
|---|---|
| `VICIONE_SERVICEBUS_RMQ_HOST` / `_PORT` / `_MGMT_PORT` / `_USER` / `_PASS` | `RabbitMqTestHarness` (product), `RunScopedBroker`, `RunScopedCredentials`, `ExclusiveQueueProbe`, `BrokerTopologyProbe`, `Failure_Specs` |
| `VICIONE_SERVICEBUS_AMQ_HOST` / `_OPENWIRE_PORT` / `_AMQP_PORT` / `_JOLOKIA_PORT` / `_USER` / `_PASS` | `ActiveMqTestHarness` (product), `RunScopedBroker` |
| `VICIONE_SERVICEBUS_ARTEMIS_HOST` / `_OPENWIRE_PORT` / `_USER` / `_PASS` | `ArtemisBroker` |
| `VICIONE_SERVICEBUS_FIXTURE_CONTROL` | `BrokerOutageClient` |
| `VICIONE_SERVICEBUS_AMQ_RMQ_HOST` / `_USER` / `_PASS` | `Configuration.cs`, only for the `[Explicit]` Amazon MQ obligation |

`RabbitMqTestSetUpFixture` refuses to start the whole RabbitMQ assembly when any of the five RabbitMQ
variables is missing, and names the missing one.

### 2.2 Every direct environment-variable or hard-coded credential read — the complete list

Plan §7 requires that no single test read the environment or user secrets directly. Every site below is a
finding for the rebuild.

**Direct `Environment.GetEnvironmentVariable` in the test projects (8 files):**

| File | What it reads | Fallback when unset |
|---|---|---|
| `…RabbitMqTransport.Tests/RunScopedBroker.cs` | host, AMQP port, management port, user, secret | `localhost`, `5672`, `15672`, **`guest`/`guest`** |
| `…RabbitMqTransport.Tests/RunScopedCredentials.cs` | host, port, management port, user, secret | `localhost`, port `0`, **`guest`/`guest`** |
| `…RabbitMqTransport.Tests/RabbitMqTestSetUpFixture.cs` | presence check of the five RabbitMQ variables | fails the run (correct) |
| `…RabbitMqTransport.Tests/ExclusiveQueueProbe.cs` | `VICIONE_SERVICEBUS_RMQ_MGMT_PORT` | **throws** (correct) |
| `…RabbitMqTransport.Tests/BrokerTopologyProbe.cs` | `VICIONE_SERVICEBUS_RMQ_MGMT_PORT` | **throws** (correct) |
| `…RabbitMqTransport.Tests/Failure_Specs.cs:435` | `VICIONE_SERVICEBUS_RMQ_MGMT_PORT` | **falls back to `15672`** — and then issues `DELETE api/vhosts/<name>` |
| `…RabbitMqTransport.Tests/Configuration.cs` | `VICIONE_SERVICEBUS_AMQ_RMQ_HOST/USER/PASS`, or NUnit `TestContext.Parameters` | throws `ConfigurationException` |
| `…ActiveMqTransport.Tests/RunScopedBroker.cs` | host, OpenWire, AMQP and Jolokia ports, user, secret | `localhost`, `61616`, `5672`, `8161`, **`admin`/`admin`** |
| `…ActiveMqTransport.Tests/ArtemisBroker.cs` | host, port, user, secret | **throws** (correct, and deliberately so) |
| `…ActiveMqTransport.Tests/BrokerOutageClient.cs` | `VICIONE_SERVICEBUS_FIXTURE_CONTROL` | `Assert.Fail` |

The product-side harnesses `RabbitMqTestHarness` and `ActiveMqTestHarness` read the same variables
themselves, with the same `guest`/`guest` and `admin`/`admin` fallbacks
(`src/Transports/ViciOne.ServiceBus.RabbitMqTransport/Testing/RabbitMqTestHarness.cs:84-85`,
`src/Transports/ViciOne.ServiceBus.ActiveMqTransport/Testing/ActiveMqTestHarness.cs:56-57`). This is
product code shipped to consumers, so the fallbacks cannot simply be removed; the new fixtures have to
supply the values rather than rely on the defaults.

**Well known credentials written into test code:** `Insufficient_Specs.cs` configures
`rabbitmq://localhost/test` with `unguest`/`guest` — a deliberately wrong account, but on a fixed host and
the default port, so it addresses whatever holds 5672 rather than the run fixture.
`Container_Specs.When_using_a_connection_factory_refresh_callback` starts with `totally-bogus` /
`not-real-at-all`, which is intentional and is replaced by the refresh callback.
`RabbitMqAddress_Specs.Given_encoded_credentials_are_provided_in_the_uri` uses the synthetic literals
`te%24t` / `Pa%24%24word`; per plan §7 those stay visible because their value *is* the semantics.

**Hard-coded endpoints that do not address the run fixture** (each one can make an obligation pass or fail
for a reason that has nothing to do with the pinned broker):

- `DifferentHost_Specs.cs:56` — `new Uri("rabbitmq://127.0.0.1/test/input_queue")`, no port, so 5672.
- `Insufficient_Specs.cs:16` — `rabbitmq://localhost/test`.
- `BrokerContract_Specs.cs` — host, port and account come from `RunScopedCredentials`, but the virtual
  host is the literal `"test"` rather than `RunScopedBroker.VirtualHost`.
- `Failure_Specs.cs:437` — management port fallback `15672`.
- `Configure_Specs.cs`, `HostConfigurator_Specs.cs`, `RabbitMqAddress_Specs.cs` use fixed URIs, but none of
  them opens a connection; those are hermetic and correct.

**TLS material:** no test in either project configures `UseSsl` with a certificate path or passphrase.
`HostConfigurator_Specs` exercises the SSL configurator but opens no connection and loads no certificate.

### 2.3 `client.p12` — definitive answer

**No test in either project references `client.p12`, directly or indirectly.**

Measured at the baseline commit:

- Tracked: yes. `git ls-files -s` gives blob `171aa0821251418f9f16dda228049cf05114482d`, path
  `tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/client.p12`, 2797 bytes,
  SHA-256 `ce1d251211441505ec80d66772213f4d70d2879ddf7c7524f91fabd6e4be7cab`.
- Referenced from code or build: **no**. `git grep -i "client\.p12"` over `src`, `tests`, `benchmarks`,
  `build`, `tools` and every `.csproj`/`.props`/`.targets`/`.sln*` returns exactly one hit, and it is not
  a use: `tools/identity/identity_rules.py:64` lists the path in
  `COMMENTLESS_OR_BINARY_EXCEPTIONS`, the allowlist of files that cannot carry an in-file identity
  comment. The other mentions are documentation only — `NOTICE:31`, `MODIFICATIONS.md:48`,
  `CHANGELIST.md:5810` and `:6239`.
- Deployed: **no**. `ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj` contains no `Content`, `None` or
  `EmbeddedResource` item for it, so it is not even copied to the output directory. It cannot be loaded
  at run time by a relative path.
- Historic consumer: the only plausible one is `Security_Specs.cs`, whose entire fixture is commented out
  at this commit. Its history is `Import MassTransit v8.5.10 source baseline` → identity rename → folder
  move; it was never touched on its own.
- Content, without exposing key material: PKCS#12 version 3. `openssl asn1parse` shows two content infos —
  an encrypted `pkcs7-data` bag (PBES2, AES) and an encrypted `pkcs7-data` containing a
  **`pkcs8ShroudedKeyBag`** with a `localKeyId` attribute — under a SHA-1 MAC with 2048 iterations. In
  other words the file **does contain a private key**, encrypted under a passphrase this cohort did not
  and will not attempt to recover.

**Disposition proposed:** plan §7 says "without a real relation, remove as an unused fixture; on any
indication of usable key material send a `QUESTION` for revocation or rotation. Deleting it from HEAD is
not by itself evidence that a real key was revoked." Both halves apply here: there is no real relation in
this repository *and* the file carries an encrypted private key. So the recommendation is
**`QUESTION` to the Lead, not a silent removal**: the certificate's subject and issuer cannot be read
without the passphrase, so this cohort cannot state whether the key is self-signed throwaway material from
the upstream MassTransit fixture or something that was ever trusted anywhere. Whoever can answer that has
to answer it before the file is dropped, and if it was ever real, deletion from HEAD does not revoke it —
the blob stays in the history from `Import MassTransit v8.5.10 source baseline` onward.

## 3. TLP-005 — run isolation as it stands today

### 3.1 What is isolated

Per **run**, the isolation is genuinely good and should be kept:

- Compose project name = run identity (`VICIONE_SERVICEBUS_COMPOSE_PROJECT`, set in
  `tools/ci/run_broker_category.py`), so two runs on one machine address different containers.
- Every published port is `127.0.0.1::<container-port>`: Docker assigns a free loopback port and
  publishes it atomically, and the runner asks Docker afterwards which ports it bound. There is no fixed
  host port and no probe-then-bind race.
- Every broker account is generated per run and never written to disk.
- Every output and control file lives under a run root that is claimed with a token rather than adopted.

`RunScopedBroker.cs` documents the measurement that made this necessary: on 2026-08-08, with a foreign
broker on 5672, the required run reported 302 passed / 4 failed, and with that broker stopped and nothing
else changed, 289 passed / 17 failed. Fourteen obligations had been green against the wrong broker.

### 3.2 What is not isolated — the mutable fixture ownership TLP-005 is about

**RabbitMQ: one shared virtual host, destroyed and recreated by every classic fixture.**
`RabbitMqTestFixture.SetupRabbitMqTestFixture` runs `RabbitMqTestHarness.RecreateVirtualHost()` in its
`[OneTimeSetUp]`, which is `DELETE api/vhosts/test` followed by `PUT`. That is not a clean, it is a
demolition of everything any other fixture has in that virtual host. Roughly 190 of the 242 RabbitMQ
`LocalIntegration` obligations derive from `RabbitMqTestFixture` and therefore do this. The only thing
that makes it work today is `[assembly: LevelOfParallelism(1)]` in `RabbitMqTestSetUpFixture.cs:3`.
Three fixtures already had to escape the shared virtual host, and each says why in its own comment:
`Failure_Specs` (`test-exclusive-<method>`, measured: one bus left running failed the clean up of
eighteen later specs with `queue.delete` answered by `RESOURCE_LOCKED`), `TransientStartupFailure_Specs`
(`test-transient-<method>`, measured: five unrelated specs timed out on queues deleted underneath them),
`BusEndpointReady_Specs` (`test-unavailable-bus-endpoint`), `ConsumerTimeout_Specs` (`test-redelivery`)
and `JobConsumer_Specs.Should_cancel_on_shutdown_and_then_restart_the_job` (`test-job-restart`, measured:
a recovered connection reached into the next spec's freshly cleaned virtual host and seven sibling specs
faulted with `ObjectDisposedException on channel`).

**ActiveMQ: no isolation boundary exists at all.** ActiveMQ Classic has no virtual host. Every one of the
72 ActiveMQ `LocalIntegration` obligations shares one broker namespace. `ActiveMqTestHarness.Clean`
deletes *every* queue and *every* topic the broker holds — that is the whole isolation mechanism, and it
is global. `Cleaning_the_broker.Should_not_disable_itself_on_a_non_default_admin_port` exists precisely
because a port condition once turned that reset into a silent no-op for every ViciOne run.

**Fixed entity names shared across fixtures.** `input_queue` is the default input queue of both harnesses
and is additionally named literally in ten RabbitMQ `ReceiveEndpoint` calls; `input_queue_error` in four;
`input-fault` in three; `second_queue` in three; and `input-queue`, `ack_queue`, `handle-fault`,
`monitor`, `second-queue`, `priority_input_queue`, `start-stop-queue`, `forward-reply-to`, `durable-ttl`,
`observers`, `odd-job`, `job-events`, `outbox-priority`, `outbox-normal`, `observer-priority`,
`unbound-service`, `bound-exchange`, `rebound-exchange`, `publish-not-delivered`, `world-examiner`,
`TestCase-Buffer`, `Quotes`, `Stock-<key>`, `TestCase-R-<key>` once each. Only three names in the whole
cohort are run-unique: `recovery-input-<NewId>` in `BrokerRecovery_Specs`, and the two harness input
queues in `UniqueInstance_Specs`, which are unique per fixture but not per run.

**Global process state mutated and never restored.**
`Reconnecting_Specs` (both projects) writes `SendEndpointCacheDefaults.MinAge` and `.Capacity`;
`TopologyCorrelationId_Specs.When_the_correlation_id_is_configured_explicitly` writes
`MessageCorrelation.UseCorrelationId<T>`; `ConsumerTimeout_Specs` writes the static
`RedeliveryConsumer.Probe`; `InMemoryOutboxRedelivery_Specs` (both projects) uses a static
`TestHandler.Count` shared by three sibling fixtures and never reset;
`RoutingKeyDirect_Specs`/`RoutingKeyTopic_Specs` hold static `TaskCompletionSource` fields;
`ConcurrencyFilter_Specs` holds a static `_complete`; `DelayRetry_Specs` holds a static
`Consumer.PingCount`. None of these is restored, so obligation order inside the assembly is load-bearing.

### 3.3 What breaks if two runs execute concurrently

Two runs *of the canonical runner* on one machine do not collide: different compose project, different
containers, different ephemeral ports, different accounts, different run root. That part holds.

What breaks is everything else:

1. **Two test processes against one fixture.** Both would receive the same
   `VICIONE_SERVICEBUS_RMQ_*` projection and both would run `DELETE api/vhosts/test`. Each recreation
   destroys the other's queues, exchanges, bindings and the delayed-message-exchange scheduled store
   mid-flight. On ActiveMQ it is worse: `Clean` deletes every destination on the broker, so a concurrent
   run loses its queues without any recreate involved.
2. **Any parallelism inside one assembly.** Removing `[assembly: LevelOfParallelism(1)]` produces the
   same damage inside a single process. The RabbitMQ suite is single-threaded by construction, and the
   ActiveMQ suite has no such attribute at all — it is only saved by having no shared `[Parallelizable]`.
3. **A run without the environment set.** `RunScopedBroker`/`RunScopedCredentials` fall back to
   `localhost:5672` with `guest`/`guest`, and `ActiveMq` to `localhost:61616` with `admin`/`admin`.
   That is the failure mode the 2026-08-08 measurement documents; only the RabbitMQ `SetUpFixture` guards
   against it, and only for the five RabbitMQ variables.
4. **Fixed entity names.** Even with per-run containers, two fixtures inside one run that both own
   `input_queue` are competing consumers on the same queue. `UniqueInstance_Specs` carries a comment
   describing exactly that: with two leftover consumers on `input_queue`, RabbitMQ hands a message to
   whichever consumer is next in turn, the leftover one discards it and the waiting fixture never sees
   it — measured red with two consumers, green with none.
5. **The outage obligation is exclusive by nature.** `Recovering_from_a_broker_outage` stops the whole
   ActiveMQ container. Nothing else may be running against that broker while it does, and
   `BrokerOutageClient.Restore()` is deliberately not cancellable for exactly that reason.

### 3.4 Design input for the new Testcontainers fixtures

1. One broker container per **owning fixture**, not per run and not per assembly. That is the only
   construction that removes both the shared virtual host and the shared ActiveMQ namespace, and it is
   what makes `Clean`/`RecreateVirtualHost` unnecessary rather than merely safer.
2. If a container per fixture is too expensive for RabbitMQ, the fallback is one virtual host per fixture
   with a run-unique name, never `test`. It does not work for ActiveMQ, which has no such boundary; there
   a container per fixture is the only option.
3. Every queue, exchange, topic and consumer name must be run-bound and fixture-bound. Today three names
   in 466 obligations are.
4. The fixture owns start, readiness, run credential and an awaited `DisposeAsync`, and a cleanup failure
   must be red — `Failure_Specs.StopEverythingStarted` already implements exactly that rule (it fails a
   green test whose cleanup failed) and is the model to generalise.
5. Readiness must be the broker answering the protocol the obligations use, as `compose.yaml` already
   does: RabbitMQ needs `check_running` **and** the management API, because the setup creates the virtual
   host over HTTP; ActiveMQ needs Jolokia, because the harness inspects and cleans through it.
6. The delayed message exchange plugin, `schedulerSupport="true"` and the `admins=vicione_ci` role binding
   are load-bearing and must survive the move to Testcontainers. `UseDelayedMessageScheduler` has 20 call
   sites in this cohort alone (`images.lock.json` counts fifteen repository-wide against an older tree),
   and the ActiveMQ delayed-redelivery obligations do not exist without scheduler support.
7. The outage obligation needs a stable address across a container restart. Testcontainers rebinds the
   published port on restart exactly as compose does, so the relay — or an equivalent — is still needed,
   and the obligation must own its own broker.
8. No fixture may inherit a fallback endpoint or account. Both `RunScopedBroker` variants and both
   product harnesses currently do.

## 4. Broker semantics captured per obligation

Section 3 of the brief is answered per row in `LEDGER_DRAFT.jsonl` (`boundaries` for the broker facets,
`negativeAndFailurePaths` for fixture inheritance and setup/teardown effect, `notes` for the required
infrastructure). The coverage across the cohort, so the rebuild can see what would be lost:

| Facet | Where it is bound today |
|---|---|
| topology / exchange / queue declaration | `PublishTopology_Specs` (read back from the management API), `BuildTopology_Specs`, `ExchangeBind_Specs`, `ExcludeTopology_Specs` (passive declare answered 404), `EntityName_Specs`, `AlternateExchange_Specs`, `DeadLetterQueue_Specs`, `BindQueue_Specs`, `ManyQueues_Specs`, `DurableTtl_Specs`, ActiveMQ `PublishTopology_Specs` (virtual topics read from Jolokia), `TopicEndpoint_Specs`, `VirtualTopicEndpoint_Specs` |
| routing and binding semantics | `RoutingKeyDirect_Specs`, `RoutingKeyTopic_Specs`, `TopologyRoutingKey_Specs`, `DelayDirectExchange_Specs`, `MessageTopology_Specs`, `SendToPublishExchange_Specs`, `ConsumerBind_Specs` |
| publish confirms | `Request_Specs` (three fixtures set `PublisherConfirmation = false` explicitly), `PublisherConfirm_Specs`, `Mandatory_Specs` (`basic.return` / `MessageReturnedException`) |
| consumer acknowledgement and redelivery | `ConsumerTimeout_Specs` (the delivery-acknowledgement timeout, the discard, the redelivery, exactly-once effect), `KillSwitch_Specs` (`ReceiveContext.Redelivered`), `Restart_should_not_lose_messages` (ActiveMQ) |
| prefetch | `EndpointConfiguration_Specs` (both projects), `Batching_Specs` (prefetch below the batch limit), `Stream_Specs`, `RabbitMqAddress_Specs.Given_a_prefetch_count`, `PurgeOnStartup_Specs` |
| dead-letter | `DeadLetterQueue_Specs` (declaration only — the case body is empty) |
| retry / redelivery policy | `Retry_Specs` (4 fixtures), `DelayRetry_Specs` (both projects), `InMemoryOutboxRedelivery_Specs` (both projects) |
| error and fault queues | `ErrorQueue_Specs` (5 fixtures, 27 obligations), `PublishFaultChannel_Specs`, `FaultPoly_Specs`, `Skip_Specs` (skipped queue), ActiveMQ `ErrorQueue_Specs`, `DeserializationError_Specs`, `InvalidMessage_Specs` |
| connection loss and reconnect | ActiveMQ `BrokerRecovery_Specs` — **the only obligation in the cohort that takes a broker away and brings it back**. `Reconnecting_Specs` (both projects) is named for reconnection but exercises send-endpoint-cache turnover; the ActiveMQ file states this in its own summary, the RabbitMQ file still prints "Okay, restart RabbitMQ" to a console nobody reads. `FailedConnection_Specs` and `Insufficient_Specs` cover the refusal, not the recovery |
| cluster / failover | `UsingCluster_Specs` (RabbitMQ logical host name with a single node), `Configuring_ActiveMQ.Failover_should_take_precendence_in_uri_construction_{activemq,amqp}` (URI construction only, no failover is exercised) |
| HAProxy usage | `BrokerRecovery_Specs` only, indirectly through the relay in front of ActiveMQ |
| timeouts | `PublishTimeout_Specs` (cancellation token identity), `Request_Specs.Sending_a_request_to_a_missing_service`, `ConsumerTimeout_Specs`, `Failure_Specs` (20 s reporting budget against the 60 s readiness limit), `BusEndpointReady_Specs` (90 s bound on a call that used to hang forever) |
| cancellation | `PublishTimeout_Specs`, `ConsumerTimeout_Specs` (the consumer observes the discard as its own cancellation), `JobConsumer_Specs` (job cancellation) |
| message ordering | `ConcurrencyFilter_Specs.Using_a_consumer_concurrency_limit_set_to_1` only |
| concurrency limits | `ConcurrencyFilter_Specs`, `EndpointConfiguration_Specs` (both projects), `Stream_Specs`, `KillSwitch_Specs` |
| quorum / mirrored queues | `JobConsumer_Specs` and `JobDistributionStrategy_Specs` — `SetQuorumQueue` on the bus, on every endpoint and on the job endpoint. **No obligation asserts that the queue really is a quorum queue**; it is configured and never read back |
| streams | `Stream_Specs` — `x-queue-type=stream`, `MaxAge`, `FromFirst` |
| priority | `PriorityQueue_Specs` (`x-max-priority`, basic properties `Priority`, and the priority surviving the outbox) |
| transport-specific headers | `HeaderObject_Specs` (object and timestamp headers through the AMQP header table), `RawJson_Specs` (`VSB-*` transport header isolation between raw and enveloped), `ErrorQueue_Specs` (`VSB-Fault-Message`, `VSB-Reason`, `VSB-Host-MachineName`, `FaultInputAddress`), `PublishHeader_Specs` and `OpenTelemetry_Specs` (activity baggage), ActiveMQ `RequestReply_Specs` (`NMSReplyTo`) |
| serialization | `RawJson_Specs` (3 fixtures), `Bytes_Specs`, `DeserializationError_Specs`, `MessageBodyLength_Specs` (ActiveMQ body length invariant), `Compression_Specs` (zlib interoperability behind `nms.useCompression`) |

## 5. Quality defects for the rebuild

Each defect below is recorded on the affected rows of `LEDGER_DRAFT.jsonl` in the `notes` field. The
counts are exact for this cohort.

### 5.1 Fixed sleeps used as synchronization

`RabbitMqTestFixture.SetupRabbitMqTestFixture` ends with `await Task.Delay(200)` after starting the
harness — **that one sits under 184 of the 354 RabbitMQ obligations** (measured: 132 test classes derive
from `RabbitMqTestFixture`, `RabbitMqActivityTestFixture`, `ConsumerBindingTestFixture` or
`Should_schedule_in_any_exchange_type`). Beyond it:
`ErrorQueue_Specs.A_serialization_exception_from_a_bad_message` (3 s), `…An_empty_message_body` (5 s),
`Insufficient_Specs` (15 s after the assertion), `Skip_Specs` (10 s after the assertion),
`PublishFaultObserver_Specs` (30 × 1 s), `Reconnecting_Specs` RabbitMQ (20 × 1 s) and ActiveMQ
(6 × 500 ms), `Shutdown_Specs` (2 × 5 × 1 s inside the consumer), `Connecting_receive_endpoints`
(10 × 1 s inside the consumer), `ConsumeReceiveTask_Specs` (1 s after the assertion),
`ConcurrencyFilter_Specs` (100 × 10 ms), `JobConsumer_Specs.Should_cancel_on_shutdown_and_then_restart…`
(3 × 1 s), `JobDistributionStrategy_Specs` (1 s per job), ActiveMQ `KillSwitch_Specs` (1 s),
`Configuring_a_topology` (1 s and no assertion at all), and ActiveMQ
`Restart_should_not_lose_messages`, which sleeps 5 s per message plus a fixed 15 s and 40 s — at least
55 s of pure sleeping, after which a counter is read rather than observed.

### 5.2 Unbounded or wall-clock polls

`TransientStartupFailure_Specs` and `BrokerContract_Specs` poll against a `DateTime.UtcNow` deadline;
`ConsumerTimeout_Specs.ReadSettledQueueState` polls every 2 s against a `DateTime.UtcNow` deadline. A wall
clock is not a duration — `ExclusiveQueueProbe` and `BrokerOutageClient` already use a monotonic
`Stopwatch` and are the pattern to follow. No poll in the cohort is genuinely unbounded.

### 5.3 Retry masking failure

`Request_Specs.Sending_a_request_to_a_faulty_service_using_reply_to` carries `[Retry(3)]`.
33 RabbitMQ test classes and 6 ActiveMQ test classes carry a class-level `[Category]` — almost all of
them `Flaky` — which is not a retry but is the same admission recorded as metadata instead of as a defect.
`BrokerContract_Specs` is the one that carries `Integration` rather than `Flaky`.

### 5.4 `async void`, unawaited tasks, swallowed exceptions

- No `async void` anywhere in either project — measured, zero hits across all 132 `.cs` files.
- Unobserved tasks: `ErrorQueue_Specs.A_request_client_exception` /
  `An_aggregate_exception` / `An_additional_exception_header` start `_responseTask` in `[OneTimeSetUp]`
  and never await it in seven of eight cases — a faulted `Task` nobody observes.
  `InMemoryOutboxRedelivery_Specs` (both projects) calls `context.Publish(...)` / `context.Send(...)`
  inside a consumer without awaiting, immediately before throwing — which is the very race the outbox is
  supposed to close, and the case then asserts on the result of that race.
- Swallowed exceptions: `RabbitMqTestHarness.CleanUpVirtualHost` and
  `ActiveMqTestHarness.CleanUpVirtualHost` both wrap everything in `catch (Exception) { Console.WriteLine }`
  — a failed cleanup is invisible and surfaces later as an unrelated failure. `RawJson_Specs.SendRawMessage`
  does the same for the raw publish, so a publish failure becomes a timeout in a different place.
  `PublishFaultObserver_Specs` catches and prints every publish exception, and asserts nothing.
- Async methods with no `await`: 19 measured by signature scan in this cohort (18 RabbitMQ, 1 ActiveMQ) —
  among them `ErrorQueue_Specs.A_serialization_exception_from_a_bad_message.Should_have_the_host_machine_name`
  and `.Should_have_the_reason`, the same pair on `An_empty_message_body`, five `[OneTimeSetUp] Setup`
  methods in `Request_Specs` and `ErrorQueue_Specs`, four consumer `Consume` implementations in
  `ConsumerBind_Specs`, two in `DelayDirectExchange_Specs`,
  `AlternateExchange_Specs.Should_have_the_proper_address`,
  `OpenTelemetry_Specs.RequestActivityProxy.BuildRoutingSlip` and
  `Configuring_ActiveMQ.Pub_Sub_Queue_Names_Should_Not_Contain_Periods`. The scan misses generic
  signatures, so the six observer implementations in `SendObserver_Specs` (`PrePublish<T>`,
  `PostPublish<T>`, `PublishFault<T>`, `PreSend<T>`, `PostSend<T>`, `SendFault<T>`) come on top: at least
  25 in total.

### 5.5 Tests depending on execution order

`[Order]` is used on 19 RabbitMQ obligations across five fixtures: `Turnout/Faulted_Specs` (5),
`TwoActivityCourier_Specs.Executing_a_routing_slip_with_two_activities` (9),
`Request_Specs.Sending_a_request_using_the_new_request_client_in_a_consumer` (3),
`PublishFaultChannel_Specs` (1 `[Order(0)] Setup`), `ConsumerBind_Specs.Binding_an_untyped_consumer`
(1 `[Order(0)] Setup`). In the last two the first `[Test]` is the arrangement of the others.

Beyond `[Order]`, whole fixtures share one `[OneTimeSetUp]`-produced message and one `Task`, so their
cases are not independent even though NUnit reports them separately:
`ErrorQueue_Specs.A_serialization_exception` (10 cases), `A_request_client_exception` (8),
`A_serialization_exception_from_a_bad_message` (3), `An_empty_message_body` (3),
`EventPublishRQ_Specs` (3), ActiveMQ `A_serialization_exception` (9 per protocol),
ActiveMQ `Using_the_handler_test_factory` (6 per protocol).

### 5.6 Shared static state

Listed in §3.2. The two that matter most for correctness are `InMemoryOutboxRedelivery_Specs`
(`static int TestHandler.Count`, shared by three sibling fixtures, never reset, and the assertion is
`Count == 0`) and `ConsumerTimeout_Specs` (`static RedeliveryProbe Probe`, assigned per run of the case).

### 5.7 Cleanup that is not awaited or not checked

`ExclusiveConsumer_Specs` starts a second harness **inside** an `Assert.That(async () => …)` delegate, so
`secondHarness.Stop()` is skipped exactly when the expected exception is raised — the connection and the
consumer are left behind.
`Configuring_ActiveMQ` creates harnesses in several cases and never stops them.
`HarnessSetupFailure_Specs` creates four virtual hosts (`f6-setup`, `f6-stack`, `f6-close`, `f6-success`)
and removes none of them.
`Linking_a_service_that_later_gets_a_second_instance` stops `instanceA` inside the `try` and again in the
`finally`.
The positive counter-examples, and the pattern the rebuild should generalise, are
`Failure_Specs.StopEverythingStarted` (runs every cleanup even after one throws, records failures
side by side, and fails a green test whose cleanup failed) and
`BrokerRecovery_Specs.StopWithoutHanging` (bounds the teardown and fails when it is exceeded).

### 5.8 Obligations with no assertion call in their body

Measured: **93 of the 466 obligations** (76 RabbitMQ, 17 ActiveMQ) contain no `Assert`, no `Throws` and no
`Should…` call anywhere in their body. None of them may be dropped on that ground (plan §9), and they are
not all equal:

**They observe something, just not with an assertion (the majority).** They `await` a
`Task<ConsumeContext<T>>` that completes only when the expected message arrives, bounded by the fixture's
`TestTimeout`. `ExchangeBind_Specs` (3), `ConsumerBind_Specs` (8), `EventPublishRQ_Specs` (3),
`SimpleConnect_Specs` (2 of 3), `Using_the_reply_to_address` (2), `Turnout/Faulted_Specs` (4),
`ScheduleMessage_Specs` (2 RabbitMQ, 3 ActiveMQ), `ErrorQueue_Specs.Should_move_the_message_to_the_error_queue`
(4 RabbitMQ, 1 ActiveMQ), `FaultPoly_Specs`, `SendToPublishExchange_Specs`, `Observer_Specs`,
`ConsumeReceiveTask_Specs`, `BindQueue_Specs`, `DifferentHost_Specs`, `LocalBusName_Specs`,
`Skip_Specs`, `RoutingKey{Direct,Topic}_Specs`, `UniqueInstance_Specs`, `Publish_Specs.Should_be_received`
(temporary endpoints), `JobConsumer_Specs.Should_cancel_on_shutdown_and_then_restart_the_job`,
`Deserialization`/`InvalidMessage` on ActiveMQ. For these the rebuild's job is to turn the wait into an
explicit assertion, not to invent an obligation.

**They establish nothing at all.** `When_a_dead_letter_queue_is_specified.Should_create_and_bind_the_exchange_and_properties`
and `Creating_a_service_with_many_queues.Should_not_exploded` have an **empty body** — everything they
carry is the fixture starting. `Configuring_a_topology.Should_not_consume_the_messages` publishes and
sleeps a second. `TestRegularExpression_Specs.Verify_regex` computes a result and discards it.
`PublishFaultObserver_Specs.Should_handle_the_observer_fault` loops thirty times, catches everything and
prints. `PublishStop_Specs` (both), `Publishing_without_a_listener.Should_not_fault`,
`When_publishing_messages_from_the_bus.Should_support_multiple_types` (both projects),
`When_configuring_a_durable_ttl_queue.Should_comply_with_new_broker_rules`, `Stopping_the_bus` (both),
`StartStop_Specs` publish-only / send-only / without-auto-start (RabbitMQ),
`Configuring_ActiveMQ.Should_connect_locally` and its three harness siblings, and the `[Explicit]`
`AmazonMQ_Specs.Should_connect` all reduce to "this did not throw".
`RabbitMqAddress_Specs.Should_be_valid_for_international_characters` and
`Configure_Specs.Should_not_fail_with_warnings` are the hermetic members of that group.

**They are the arrangement of a sibling, not an obligation of their own.**
`ConsumerBind_Specs.Binding_an_untyped_consumer.Setup` (`[Order(0)]`),
`PublishFaultChannel_Specs.Setup` (`[Order(0)]`), `Request_Specs.…in_a_consumer.Get_response` (`[Order(0)]`),
`TwoActivityCourier_Specs.A_routing_slip` (`[Order(1)]`) and `Executing_with_no_observers.Setup`.
`TwoActivityCourier_Specs.Show_timeline` (`[Order(9)]`) only writes the timeline to the test output; it is
a `DIAGNOSTIC_ONLY` candidate.

Separately, `A_request_client_exception.Write_out_message_body` writes the body to the console instead of
asserting on it, and `A_request_client_exception.Should_have_the_correlation_id` asserts a
`RequestFaultException` rather than the correlation id its name promises.

### 5.9 Names that contradict their body

- `HostConfigurator_Specs.Should_set_tls10_protocol_by_default` asserts `SslProtocols.Tls12`.
- `Reconnecting_Specs` (RabbitMQ) is named for reconnection and never interrupts the connection; it
  prints "Okay, restart RabbitMQ" to the console and proceeds regardless. The ActiveMQ sibling was
  renamed in its body documentation and states plainly what it does and does not prove.
- `PublisherConfirm_Specs.Should_call_the_ack_method_upon_delivery` never observes an ack.
- `Executing_many_activities_in_a_row_with_a_fault_one.Should_receive_the_routing_slip_completed_event`
  waits for `RoutingSlipFaulted`.
- `A_request_client_exception.Should_have_the_correlation_id` (see §5.8).

### 5.10 Other findings worth a decision

- **`Security_Specs.cs` is a file of comments.** It contains no compiled code, carries no obligation and
  is the only plausible historic consumer of `client.p12` (§2.3).
- **Quorum queues are configured and never verified** (§4). `SetQuorumQueue` appears on the bus, on the
  endpoint callback and on the job endpoint in `JobConsumer_Specs` and `JobDistributionStrategy_Specs`,
  and no obligation reads `x-queue-type` back from the broker. A rewrite that silently dropped
  `SetQuorumQueue` would stay green.
- **`GetConcurrencyLimit` is dead code** in `EndpointConfiguration_Specs` of both projects.
- **The probe-JSON navigation asserts by array index** (`["filters"][2]…`) in both
  `EndpointConfiguration_Specs`; a probe-shape change would break it in a way that names nothing.
- **`DateTime.Now` rather than a time abstraction** in `HeaderObject_Specs.Should_support_date_time`
  (process time zone dependent) and in the ActiveMQ `ScheduleMessage_Specs`/`SchedulePublish_Specs`.
- **`new Random()` without a seed** in `Bytes_Specs`: the payload that failed cannot be reproduced.
- **`GC.Collect()` / `GC.WaitForPendingFinalizers()`** inside
  `Owning_a_transport_subject.Should_survive_a_failing_disposal_that_nobody_is_waiting_for`.
- **Both test projects violate plan §6.1 as they stand**: they reference `Microsoft.NET.Test.Sdk`,
  `NUnit`, `NUnit3TestAdapter` and `NUnit.Analyzers` directly. That is expected of the baseline and is
  named here only so the integrator sees it measured rather than assumed.
- **`NUnit1032` is suppressed** with `#pragma warning disable` in four ActiveMQ files and none of the
  RabbitMQ files, around
  `Task<ConsumeContext<T>>` fields — the analyzer exception plan §6.10 requires to be removed from
  `.editorconfig` is additionally suppressed locally in this cohort.

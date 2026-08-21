# R0-CLOUD — findings

Cohort `R0-CLOUD`, baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`. Read-only.
Every count below names its derivation. Nothing here is a proposal to delete: `BLOCKED` and `QUESTION` are
non-terminal (§4) and no forbidden deletion rationale was used.

---

## A. Blocking findings

### F-01 — This cohort has no identity anchor. None of these 246 tests has a recorded green run anywhere.

None of the eleven inherited anchors under `build/verification/expected/` contains a single identity from the
three projects; `grep -rn "Azure"`, `grep -rn "EventHub"` and `grep -rni "sqs"` over all eleven files return zero
lines each. `build/verification/VERIFICATION_MODEL.json` declares no `azure`, `eventhub` or `sqs` category, so
`tools/ci/run_test_category.py` would refuse to run one. `build/test-infrastructure/images.lock.json` pins no
Azure, Event Hubs, Azurite or LocalStack image.

Every other R0 cohort can reconcile against evidence that its tests once passed. This one cannot. The two
compose files that exist here (`EventHubIntegration.Tests/docker-compose.yml`,
`AmazonSqsTransport.Tests/docker-compose.yml`) sit outside the pinned image lock, use the moving tag `latest`
(Event Hubs emulator and Azurite) or a tag without a digest (`localstack/localstack:3.0.2`), and are referenced by
no CI job — they violate Lead plan §6.4 (version- and digest-bound images) as they stand.

**Consequence for the rebuild:** the 246 inherited obligations must be treated as *statements of intent whose
truth is unverified*, not as a passing baseline being ported. That is the honest framing, and it is exactly the
situation the C4a/C4b split was created for. **QUESTION to the Lead:** should the two compose files be lifted into
`images.lock.json` with digests as part of C3, so that the LocalIntegration profile has a pinned substrate?

### F-02 — A live-looking Azure Service Bus shared access key is committed in the test source.

`tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests/Configuration.cs` line 26 supplies a 44-character
base64 value as the `??` fallback for `SharedAccessKey`, together with a default key name (line 16) and a default
namespace (line 21). Shape and length are consistent with a real Service Bus SAS key. **The value is deliberately
not reproduced in this evidence, and neither is its hash** (§7: evidence must not allow secret changes to be
tracked).

This is exactly the "inherited SAS fallback" Lead plan §7 forbids migrating, and it is worse than a fallback: it
is a committed default that is used whenever the environment variable is unset, so a developer running the suite
silently authenticates with it.

**Recommended action, which R0 does not take unilaterally:** treat as a compromised credential — rotate or
disable the key on the named namespace, and record the disposition alongside `SECURITY_DISPOSITION_CLIENT_P12.md`.
Deleting the line from HEAD is not evidence of revocation (the plan says this explicitly about `client.p12`; the
same reasoning applies here). **QUESTION to the Lead: is the namespace named on line 21 a real ViciOne namespace,
and has this key been rotated?**

### F-03 — The Azure test assembly deletes every queue and every topic in the target namespace at start-up.

`AzureServiceBusTestSetUpFixture.Before_any` (an assembly-level `[SetUpFixture]`, so it runs before *any* test in
the assembly) enumerates `GetTopicsAsync()` and `GetQueuesAsync()` and deletes every entity in a loop until both
lists are empty. `AzureServiceBusTestHarness.Clean()` in **product** code
(`src/.../Azure.ServiceBus.Core/Testing/AzureServiceBusTestHarness.cs`) does the same.

Combined with F-02 — a committed default namespace and a committed default key — running this assembly with no
environment configured performs an unconditional wipe of whatever namespace those defaults point at. Both methods
swallow every exception (`catch (Exception exception) { Console.WriteLine(exception); }`), so a partial failure is
invisible.

The rebuild must not carry this shape forward: §11 requires run-unique names and cleanup whose result is checked,
which means deleting only entities this run created, never everything the credential can reach.

### F-04 — The §7 credential model is not merely un-migrated; it is currently untestable on every emulator.

Complete credential and configuration inventory for the three projects (no secret values reproduced):

| Source | Where | What | Disposition under §7 |
|---|---|---|---|
| NUnit `TestContext.Parameters` (10 sites) | `Azure.../Configuration.cs` | `KeyName`, `ServiceNamespace`, `SharedAccessKey`, `StorageAccount`, `Compress` — read first, before the environment | Runner-level parameter injection; disappears with NUnit. Replace with the central `TestConfigurationProvider`. |
| Environment variables (5, read directly by a test type) | `Azure.../Configuration.cs` | `VICIONE_SERVICEBUS_ASB_KEYNAME`, `..._ASB_NAMESPACE`, `..._ASB_KEYVALUE`, `..._AZURE_STORAGE_ACCOUNT`, `..._AZURE_STORAGE_COMPRESS` | §7 forbids tests reading environment variables directly, and requires the `VICIONE_TESTS__…` namespace. All five must move behind the configuration owner. |
| **Committed literal fallbacks (3)** | `Azure.../Configuration.cs` | default key name, default namespace, **default shared access key** | F-02. Must not be migrated. |
| Storage account default | `Azure.../Configuration.cs` | defaults to the **empty string** | Means `MessageData_Specs` (4 obligations) cannot have run at the baseline without external configuration — further evidence for F-01. |
| Hard-coded emulator connection string (2 sites) | `EventHub.../Configuration.cs`, `EventHub.../Filter_Specs.cs` | the Event Hubs emulator development string (`UseDevelopmentEmulator=true`) and `UseDevelopmentStorage=true` | Not a secret — these are the emulator's documented fixed development values. But `Filter_Specs.cs` **inlines** them rather than using `Configuration`, so there are two copies to keep in step. |
| Hard-coded AWS credentials (18 `AccessKey(`/`SecretKey(` sites) | `AmazonSqs.../Configure_Specs.cs`, `Fifo_Specs.cs`, `EndpointConfiguration_Specs.cs`, `StartStop_Specs.cs` | the literal `admin` / `admin` | LocalStack accepts anything; not a secret. But it hard-wires the tests to explicit-key authentication, which is the shape §7 replaces. |
| **Hard-coded AWS credentials in PRODUCT code** | `src/.../AmazonSqsTransport/Configuration/AmazonSqsBusFactoryConfiguratorExtensions.cs` → `LocalstackHost()` | `admin`/`admin` plus `http://localhost:4566` | A test-only convenience shipped in the product assembly. **QUESTION to the Lead:** does this belong in the product at all, or in `ViciOne.ServiceBus.Testing`? Moving it would be a product change (§15 stop rule). |
| **Hard-coded AWS credentials in PRODUCT code** | `src/.../AmazonSqsTransport/Testing/AmazonSqsTestHarness.cs` | `AccessKey = "admin"; SecretKey = "admin";` and `http://localhost:4566`, with a stale `amazonsqs://localhost:4576` host address | Same question. The 4576/4566 mismatch is a latent bug masked by the `ServiceURL` overrides. |
| **Process-global environment mutation** | `AmazonSqs.../Persistence/Storage_Specs.cs` lines 19–21 | sets `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` **for the whole process** and never restores them | Directly violates Lead plan §8 ("tests do not change global process culture, time zone or environment variables"). Leaks into every later test in the run. |
| Hard-coded endpoints | 37 sites across the three projects | `localhost:4566` (26), `localhost:4576` (2), `*.servicebus.windows.net` (9 — six of them are the legitimate parser fixtures in `ServiceBusHostConfigurator_Specs.cs`) | Must come from `tests2/testsettings.json`. |
| Synthetic values that are legitimately literals | `ServiceBusHostConfigurator_Specs.cs` (`SomeKeyName`/`SomeKey`), `MessageBody_Specs.cs` (an SNS ARN with an account id) | §7 last-but-one bullet: values whose *content* is the semantics under test stay visible. Keep — but replace the AWS account id in `MessageBody_Specs.cs` with an obviously fake one. |

There is **no** `.runsettings`, no `UserSecretsId` and no `appsettings*.json` anywhere in the repository, so today
`TestContext.Parameters` can only be fed from a runner argument that does not exist in any committed CI job.

**The finding that matters most:** the §7 target — OIDC / workload identity through the central credential factory
(Azure) and the SDK provider chain (AWS) — is **External on every one of the three owners** and cannot be proven
by any emulator: the Service Bus and Event Hubs emulators issue no AAD tokens and evaluate no RBAC; LocalStack
Community evaluates no IAM and accepts `admin`/`admin`. Three gap obligations (`OBL-R0-CLOUD-0247` Azure,
`OBL-R0-CLOUD-0258` Event Hubs, `OBL-R0-CLOUD-0265` Amazon SQS) carry this and are the first rows that must run in C4b.

---

## B. Quality defects for the rebuild (TLP-011)

Counts are over the three test projects only, at the baseline commit.

### B.1 Fixed sleeps used as synchronisation — 34 `Task.Delay` sites, of which 21 are pure synchronisation
`grep -rn "Task.Delay"` over the three test projects returns **34** occurrences. Thirteen of them are legitimate
consumer work simulation (`await Task.Delay(context.Job.Duration)`, the 160 s and 30 s slow consumers, the 15 min
lock-renewal consumer). The remaining **21** are ordering or completion devices — a fixed sleep standing in for an
observable state. The worst are the ones that *are* the arrangement or the assertion:

* `Send_Specs.cs` — `A_fault_on_the_receive_endpoint` sleeps **30 000 ms in `[OneTimeSetUp]`** as the window in
  which a human is meant to break the broker; the sleep is the arrangement.
* `DeletedQueue_Specs.cs` — 30 000 ms and 40 000 ms are the entire test body for both cases.
* `PublishFaultCache_Specs.cs` — two 10 000 ms `Console.WriteLine`-prompted waits.
* `BuildTopology_Specs.cs` — `await Task.Delay(10000)` before asserting a counter.
* `StartStop_Specs.cs` (SQS) — 3 000 ms before the first start and 1 000 ms after the second, standing in for
  `AutoStart` having completed. Marked `[Category("Flaky")]`; the sleeps are why.
* `Recycle_Specs.cs` (EH) — 500 ms between stop and start, standing in for "partition ownership released".
  Against real Event Hubs the lease expiry is far longer, so this will not hold in C4b.
* `ScheduleTimeout_Specs.cs`, `StateMachineRequest_Specs.cs`, `Subscription_Specs.cs` — a trailing `Task.Delay`
  after the last `await` with **nothing asserted after it**, so the sleep is pure superstition.

### B.2 Unbounded or absurd waits — 2 sites
`OrTimeout(TimeSpan.FromSeconds(10000))` — 2 h 47 m — at `ConfiguringAzure_Specs.cs:55` and `Publish_Specs.cs:146`.
Under a runner with no outer budget these are unbounded in practice.

Related: `Renewing_a_lock_on_an_existing_message` sets `TestTimeout = 30 minutes`,
`When_message_process_exceeds_maxAutoRenewDuration` 10 minutes, `Setting_up_a_bunch_of_receive_endpoints` and both
`LongConsumer_Specs` 5 minutes. Four fixtures alone account for ~50 minutes of wall clock.

### B.3 Retry or tolerance masking failure — 4 sites
* `Scheduling_a_message_in_the_future` asserts `>= 2 s` for a **3 s** schedule; `Should_schedule_in_the_future`
  (SQS) asserts `>= 3.5 s` for a **5 s** schedule. Both have **no upper bound at all**, so an arbitrarily late
  delivery passes.
* `When_using_delayed_redelivery_at_the_bus_level` asserts only `attempts <= (limit+1)*2`; a redelivery that never
  happened also passes.
* `Sending_multiple_messages_to_a_session` asserts ordering by comparing only the **last** sequence number per
  session — a swap in the middle that ends on the right number passes. The assertion is weaker than its name.

### B.4 Unobserved tasks and non-awaited work — 7 sites
* `InMemoryOutboxRedelivery_Specs.cs` (both projects, 6 fixtures): `context.Publish(...)` / `context.Send(...)`
  inside `Consume` is **not awaited** before the `throw`. This is the direct cause of the `[Category("Flaky")]`
  marking on all six.
* `BuildTopology_Specs.cs`: `_receivedB` is assigned a `Task` that is never awaited.
* **Product code:** `AmazonSqsTestHarness.CleanUpQueue` wraps its work in `Task.Run(...)` and returns without
  awaiting — a fire-and-forget cleanup whose result can never be checked, contrary to §11. Two `RawJson_Specs`
  fixtures register it as their `OnCleanupVirtualHost` callback.

### B.5 Swallowed exceptions — 4 sites
`AzureServiceBusTestSetUpFixture.CleanupNamespace`, `AmazonSqsTestHarness.CleanUpVirtualHost` (product),
`AmazonSqsTestHarness.CleanUpQueue` (product, catches `QueueDoesNotExistException` silently), and all three
`try/catch { Console.WriteLine }` blocks in `PublishFaultCache_Specs`. Under §11 a cleanup failure must be red.

### B.6 Tests depending on execution order — 2 fixtures, 11 methods (4 `[Order(...)]` attributes, all in `Turnout/Complete_Specs.cs`)
* `Turnout/Complete_Specs.cs` uses NUnit `[Order(1)]`…`[Order(4)]` explicitly. One job is submitted by test 1;
  tests 2–4 assert on it and are meaningless alone.
* `Configure_Specs.cs` `Using_the_handler_test_factory` — seven methods share one `[OneTimeSetUp]`, and the
  seventh (`Should_support_a_simple_handler_on_publish`) **publishes an extra message**, changing what the other
  six observe. Six of the seven are also non-`async` `[Test]` methods asserting `Select().Any()` without awaiting
  the harness — they read whatever has arrived by the time NUnit reaches them. That is a race, not an assertion.

### B.7 Shared mutable static state — 6 sites
* `TopologyCorrelationId_Specs.cs` calls **`MessageCorrelation.UseCorrelationId<T>(...)`**, process-global static
  registration, from `ConfigureServiceBusBus` (1 site), and never resets it. It leaks into every other fixture in the
  assembly. The single worst instance in the cohort.
* `Storage_Specs.cs` mutates the global `MessageDataDefaults.TimeToLive` / `ExtraTimeToLive` and never restores.
* `Filter_Specs.cs` (EH) keeps its three assertion values in `static int` fields; only one is interlocked.
* `LockTimeout_Specs.cs` uses `static readonly TaskCompletionSource` in two fixtures — the fixtures cannot be
  re-run in one process.
* `InMemoryOutboxRedelivery_Specs.cs` uses `public static int Count` with a non-interlocked `++Count`.

### B.8 Resource names that are not run-unique — every single one
**Zero** queue, topic, subscription or endpoint name in this cohort is derived from a per-run value.
`grep` for `NewId.NextGuid().ToString()` used in an entity name returns 0 hits. Instead:
`"input_queue"` (7), `"input-queue"` (9), `"second_queue"` (7), `"input-fault"` (6), `"second-queue"` (4),
`"subscription-input-queue"` (4), `"saga_input_queue_session"` (3), `"my-sub"` (3), `"input_queue_dl"` (3),
`"private-topic"` (2), `"input_queue_error"` (2), plus singletons such as `"long_multi_subs_queue"`,
`"partitioned-input-queue"`, `"in-order.fifo"`, `"ordered-queue.fifo"`, `"client-update-27"`, `"part-sub"`.
Two concurrent runs against one namespace or one LocalStack instance interfere on every one of them.

Two names additionally hard-code a **lowercased type path**
(`"vicione-servicebus.azure.servicebus.core.tests.topologytesttypes/messagea"` and `…/messageb`), which breaks
silently if the namespace or the name formatter changes — and `messageb` refers to a `MessageB` type that
`SubscriptionRule_Specs.cs` does not declare.

### B.9 Non-awaited or unchecked cleanup — 3 sites
`Filter_Specs.cs` (SQS) creates two `HostReceiveEndpointHandle` values and **never stops them** — two receive
endpoints are leaked per run. Plus the two product-code cleanup paths in B.4/B.5.

### B.10 `async void` — 0 sites
`grep -rn "async void"` over the three test projects returns 0. The related smell present instead is `async Task` with no `await`
(`ServiceBusHostConfigurator_Specs.cs`, three methods) and blocking `.Result` reads on a task the framework also
awaits (`RequestClient_Specs.cs`, `Send_Specs.cs`).

---

## C. Correctness defects found while reading (these are bugs, not style)

* **C-1 `LockTimeout_Specs.Renewing_a_lock_on_an_existing_message`** — `public async Task Setup()` carries **no
  NUnit attribute**, so the `PingMessage` is never sent. The test can only ever time out. It is `[Explicit]`, so
  nobody noticed.
* **C-2 `ExistingSubscription_Specs`** — the second half re-reads the subscription into a **discarded** value
  (`await managementClient.GetSubscriptionAsync(...)` with no assignment) and then re-asserts the *first*
  `response`. The second assertion block proves nothing, which is precisely the idempotence claim the test exists
  to make.
* **C-3 `ProducerPipe_Specs` (EH)** — `SendFilter.Send` sets its result and **never calls `next.Send(context)`**,
  truncating the pipeline, yet the test also awaits the consumer's completion source at the end. The two halves of
  the test contradict each other; one of them must be wrong.
* **C-4 `Filter_Specs` (SQS)** — both filtered queues share **one** `Consumer` instance and **one** pair of
  completion sources, so a message arriving at the *wrong* queue satisfies the assertion identically to one
  arriving at the right queue. The test cannot distinguish correct filter-policy routing from no routing at all.
  This is the cohort's clearest case of a test proving the easy half.
* **C-5 `Fifo_Specs.When_sending_messages_using_fifo_topics_and_queues`** — awaits
  `Consumed.Any<PingMessage>(...)` although no `PingMessage` is ever sent.
* **C-6 `RawJson_Specs`** — two fixtures declare methods with the **same name**
  (`Should_not_forward_transport_headers_from_raw_json`) while asserting **opposite** outcomes for the same header.
  The second name is simply wrong. Both use `SetResult` rather than `TrySetResult`, which throws on a redelivery —
  a plausible cause of their `[Category("Flaky")]`.
* **C-7 `ScheduleMessage_Specs.SchedulePublishNoScope_Specs` (SQS)** — byte-identical to `SchedulePublish_Specs`
  apart from the class name; it does not configure the "no scope" its name promises.
* **C-8 `Configure_Specs.Should_connect_with_accessKey_and_secretKey` / `Should_connect_with_credentials`** —
  byte-identical apart from their names. The second does not use
  `IAmazonSqsHostConfigurator.Credentials(AWSCredentials)` at all, so the API its name promises is untested.
* **C-9 `Publish_Specs.MessageType_Specs.Should_not_allow_array_message_types_but_does`** — the name asserts the
  opposite of the assertion. `Assert.That(await harness.Consumed.Any<T>())` is also called with no constraint
  argument (also in `Redelivery_Specs`), which asserts truthiness rather than a named expectation.
* **C-10 Duplicate contract names in one assembly** — `Faults_Receive_Specs.cs` (EH) declares a local
  `EventHubMessage` with an `int Index` that shadows `Contracts.EventHubMessage` with a `string Text`;
  `SubscriptionRule_Specs.cs` declares a root-namespace `ClientUpdated` shadowing the one in
  `SubscriptionFilter_Specs.cs`'s `TopologyTestTypes`.
* **C-11 Dead code** — `GetConcurrencyLimit` in both `EndpointConfiguration_Specs.cs` files is never called;
  `_entityNameFormatter` in three `BuildTopology_Specs` fixtures is assigned and never read; `_testOffset` and
  `QuartzAddress` in five `ScheduleMessage_Specs` fixtures likewise; `AwsAccessKey` / `AwsSecretKey` constants in
  `Configure_Specs.cs` hold the placeholder text `{YOUR AWS ACCESS KEY}`.

---

## D. Inherited identities that must not survive the rebuild (Lead plan §12.2 item 10)

* **`phatboyg_you_know_me`** — an upstream author's personal handle used as a **live subscription name** in three
  `Subscription_Specs` fixtures. It reaches the broker, not only the source.
* **`KafkaMessageConsumer`** — the consumer class name in the Event Hubs `Filter_Specs.cs`, a leftover from the
  Kafka rider.
* **`Should_create_the_exchanges`** — RabbitMQ vocabulary ("exchanges") used for SNS topics in the SQS
  `PublishTopology_Specs.cs`, and for Service Bus topics in the Azure one.
* **`namespace ViciOne.ServiceBus.Tests`** in `FaultPoly_Specs.cs` — a foreign namespace inside the Azure test
  assembly.
* **`Connector_Specs.Should_connect_subscription_endpoint`** (SQS) — the name is copied from the Azure twin; the
  SQS version connects a *receive* endpoint, and AmazonSQS has no subscription endpoints.
* **`TopologySetParitioning_Specs` / `TopologySetParitioningSubscription_Specs`** — the typo is in the identity.
* **`S3MessageDataTestLoadFetchAsync`**, **`Should_schedule_in_the_future`** (a class named like a method),
  **`BagOfCrap` / `CrapConsumed`** — names that are not behaviour sentences (§11 first bullet).

---

## E. Open questions for the Lead

1. **F-01** — lift the two compose files into `images.lock.json` with digests as part of C3?
2. **F-02** — is the committed Azure SAS key live, and has it been rotated? R0 will not delete it as a substitute
   for revocation.
3. **F-04** — do `LocalstackHost()` and `AmazonSqsTestHarness` stay in the product assembly? Moving them is a
   product change and triggers the §15 stop rule.
4. **Azure SAS capability** — §7 forbids the SAS *fallback*; does the product keep `AzureSasCredential` as a
   supported capability (then it needs an External test) or is it deprecated (then it needs a
   `REMOVED_WITH_PRODUCT_CAPABILITY` decision and a removal commit)? `OBL-R0-CLOUD-0248`.
5. **Native SQS redrive** — the SQS product sets no `RedrivePolicy` anywhere and implements its own
   `_error`/`_skipped` move transports. Is that the intended, documented statement? `OBL-R0-CLOUD-0267`.
6. **`Should_not_allow_array_message_types_but_does`** — is the target behaviour to accept or to reject array
   message types? A rebuild cannot pick a side. (C-9)
7. **Event Hubs `UnitArchitecture` project** — zero inherited rows would land in it. Create it from gap analysis,
   or do not create it? (§5 forbids empty placeholders.)
8. **Owner corrections** — `Storage_Specs.cs` (`OBL-R0-CLOUD-0228`) belongs to
   `src/Persistence/ViciOne.ServiceBus.AmazonS3` and the four `MessageData_Specs` rows to
   `src/Persistence/ViciOne.ServiceBus.Azure.Storage`; `SOURCE_OWNER_MAP.json` records both of those projects as
   having no inherited tests. Cohort R0-PER should be informed.
9. **Long-running fixtures** — four fixtures need ~50 minutes of wall clock (30 min, 10 min, 5 min, 5 min). What
   is the acceptable scaled-down shape against real Azure in C4b?
10. **Emulator capability probes P-1 … P-5** (`RECONCILIATION.md` §5.4) — these must be recorded in C4a before any
    DOC-derived External classification is treated as settled. P-1 (does the Service Bus emulator honour
    `ForwardTo`?) decides whether an Azure `LocalIntegration` profile is worth building at all.

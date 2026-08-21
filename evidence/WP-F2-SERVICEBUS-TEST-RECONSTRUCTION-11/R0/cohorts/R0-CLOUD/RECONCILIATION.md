# R0-CLOUD — reconciliation

Cohort `R0-CLOUD`, Team 1, work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`.
Baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Read-only. No product, test, tool or build file was created, modified or deleted.

Owners in scope:

| Source owner | tracked files | `.cs` | inherited test project | tracked files | `.cs` |
|---|---:|---:|---|---:|---:|
| `src/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core` | 222 | 219 | `tests/Transports/ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests` | 57 | 55 |
| `src/Transports/ViciOne.ServiceBus.EventHubIntegration` | 90 | 87 | `tests/Transports/ViciOne.ServiceBus.EventHubIntegration.Tests` | 22 | 18 |
| `src/Transports/ViciOne.ServiceBus.AmazonSqsTransport` | 165 | 162 | `tests/Transports/ViciOne.ServiceBus.AmazonSqsTransport.Tests` | 31 | 28 |

Derivation of every count in this document: `git ls-files <path>` at the baseline commit; `.cs` columns exclude
`*.csproj`, `*.csproj.DotSettings`, `packages.lock.json`, `config.json` and `docker-compose.yml`. The `.cs` figures
are the 55 / 18 / 28 and 219 / 87 / 162 named in the cohort assignment; they agree exactly.

## 1. Completeness (TLP-017)

`READ_MANIFEST.tsv` — 587 lines, `path<TAB>sha256`, sorted `LC_ALL=C` — equals `git ls-files` over all six
directories exactly, in both directions (110 test files + 477 source files).

Read depth, stated honestly per §8:

* **Read completely, every line:** all **110** tracked files of the three test projects — this is the cohort's
  assigned complete-reading scope and it is closed. That includes the three `.csproj`, the three `packages.lock.json`,
  `config.json` and the two `docker-compose.yml`.
* **Read completely, every line, in the source projects (32 files):** the three test harnesses and their host
  configuration, the public contracts and configurators the tests touch, and the transport-specific mechanism
  implementations needed to classify the environment — notably
  `AzureServiceBusTransport/Contexts/ServiceBusConnectionContext.cs`,
  `AmazonSqsTransport/AmazonSqsReceiveLockContext.cs`,
  `EventHubIntegration/Checkpoints/BatchCheckpointer.cs`,
  `EventHubIntegration/ProcessorLockContext.cs`,
  `Configuration/AmazonSqsBusFactoryConfiguratorExtensions.cs`,
  `Testing/AmazonSqsTestHarness.cs`, `Testing/AzureServiceBusTestHarness.cs`,
  and the full public configurator surface of all three transports.
* **Enumerated, not read line by line:** the remaining **445** source files. Enumeration is by declared type:
  **224** declared types in the Azure project, **91** in Event Hubs, **167** in Amazon SQS (482 total), extracted
  by pattern from every one of the 468 source `.cs` files. Every type name is therefore accounted for and every
  file is hashed in the manifest; what is not claimed is that every method body of those 445 files was read.
  Per §8 this is reported, not glossed.

## 2. The anchor question — answered

**None of the eleven inherited identity anchors under `build/verification/expected/` carries a single identity of
these three projects. This cohort has no anchor at all.**

Derivation, three independent searches over `build/verification/expected/*.txt`:

1. `grep -rl "ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests"` → no match.
2. `grep -rl "ViciOne.ServiceBus.EventHubIntegration.Tests"` → no match.
3. `grep -rl "ViciOne.ServiceBus.AmazonSqsTransport.Tests"` → no match.

Widening the search removes any doubt about a renaming: `grep -rn "Azure"`, `grep -rn "EventHub"` and
`grep -rni "sqs"` over the same eleven files return **zero lines each**. The eleven anchors cover eleven other
categories (abstractions, activemq, analyzer, benchmarks, core, diagnostics, entity-framework-core, quartz,
rabbitmq, signalr, sql-transport) and total 3114 identities, none of them from this cohort.

This is a deliberate absence, not a lost file. `build/verification/VERIFICATION_MODEL.json` defines the complete
required acceptance scope as `all = local ∪ fixture`, where `local` is analyzer, core, abstractions, signalr,
quartz, benchmarks, diagnostics and `fixture` is rabbitmq, activemq, sql-transport, entity-framework-core.
No `azure`, `eventhub` or `sqs` category is declared, so `tools/ci/run_test_category.py` would refuse to run one
("the verification model declares no run for category X"). `build/test-infrastructure/images.lock.json` pins
ActiveMQ, Artemis, HAProxy, SQL Server, PostgreSQL and RabbitMQ — no Azure Service Bus emulator, no Event Hubs
emulator, no Azurite, no LocalStack. The two compose files that do exist for this cohort
(`tests/.../EventHubIntegration.Tests/docker-compose.yml`, `tests/.../AmazonSqsTransport.Tests/docker-compose.yml`)
are outside that lock file and are not referenced by any CI job.

**Consequence, stated plainly: these 246 inherited test methods have no recorded green run anywhere in this
repository.** Every other R0 cohort reconciles its census against a file that proves its tests once executed.
This one cannot. There is nothing to reconcile per identity, and therefore also nothing to adopt — §6's
"differences are reported, never adopted" is satisfied vacuously.

Both directions of §6 are still answered explicitly:

* **Anchor identities not covered by a ledger row:** none — there are no anchor identities for this cohort.
* **Ledger rows without an anchor identity:** all 273. 246 of them are inherited test methods that no anchor
  ever recorded; 27 are gap obligations derived from current product behaviour, which by definition predate no
  anchor.

The finding this produces is in `FINDINGS.md` (F-01) and it is the strongest argument in the cohort for why the
C4a/C4b split exists: there is no historical evidence that any of this ever passed.

## 3. Census

| | Azure Service Bus | Event Hubs | Amazon SQS/SNS | total |
|---|---:|---:|---:|---:|
| Test fixtures (classes carrying `[Test]`) | 85 | 18 | 41 | 144 |
| Test methods (`[Test]`) | 142 | 18 | 86 | 246 |
| of which effectively `[Explicit]` (method- or fixture-level; never run unattended) | 23 | 0 | 5 | 28 |
| of which `[Category("Flaky")]` | 3 | 1 | 12 | 16 |
| Parameterized (`[TestCase]`) methods, variants resolved | 0 | 0 | 3 methods → 6 variants | 6 |
| **Inherited ledger rows** | **142** | **18** | **86** | **246** |
| **Gap rows from current product surface** | **11** | **7** | **9** | **27** |
| **Total ledger rows** | **153** | **25** | **95** | **273** |

`LEDGER_DRAFT.jsonl` reconciles against an independently re-derived enumeration of `[Test]`-attributed methods
in both directions with zero difference (verified: 246 measured, 246 inherited rows, empty symmetric difference).
`obligationId` runs `OBL-R0-CLOUD-0001` … `OBL-R0-CLOUD-0273`, unique, stable, file order.

## 4. Environment split — the cohort's primary output

| Profile | Azure SB | Event Hubs | Amazon SQS | total |
|---|---:|---:|---:|---:|
| `UnitArchitecture` (hermetic) | 31 | 0 | 27 | **58** |
| `LocalIntegration` (emulator) | 15 | 22 | 60 | **97** |
| `External` (real cloud only) | 107 | 3 | 8 | **118** |

Dispositions: 247 `PROPOSED_REPLACED_EXECUTING`, 25 `QUESTION` (non-terminal), 1 `PROPOSED_DIAGNOSTIC_ONLY`.
No row is `REMOVED_*`; §9's forbidden deletion rationales were not used anywhere.

**118 obligations wait for wave C4b.** They stay `NOT_EXECUTED` and non-terminal until real access arrives, and
their inherited test files stay untouched under `tests/` until then (§9).

The Azure Service Bus column is the striking one: 107 of 142 inherited obligations are External. The reason is a
single, measurable product fact rather than a hundred separate judgements — see M-1 below.

## 5. Capability matrix (REQ-TEST-106)

Each row names the mechanism that makes the emulator insufficient, and states how the claim was derived:

* **MEASURED** — derived from files in this repository at the baseline commit; reproducible without any network.
* **DOC** — a documented limitation of the vendor emulator, held from published documentation. Marked as a claim,
  not as a measurement, per §8. Every DOC row carries a required one-time capability probe in C4a.

### 5.1 Azure Service Bus

| # | Mechanism the emulator does not reproduce | Why it matters here | Derivation | Rows |
|---|---|---|---|---:|
| **M-1** | **The management plane.** `ServiceBusConnectionContext.CreateQueue` / `CreateTopic` / `CreateTopicSubscription` / `DeleteTopicSubscription` call `ServiceBusAdministrationClient.{Create,Get,Update,Delete}{Queue,Topic,Subscription,Rule}Async` — the HTTPS management API — on **every bus start**. The official Service Bus emulator exposes only the AMQP 1.0 data plane on port 5672; its entity set comes from a `Config.json` mounted at container start and is immutable thereafter. There is no management endpoint to call. | Any obligation whose subject *is* entity create / update / delete cannot exist on the emulator. `SubscriptionRule_Specs`, `SubscriptionFilter_Specs`, `ExistingSubscription_Specs`, `DeletedQueue_Specs`, both `PublishTopology_Specs` cases and both `Configuring_a_topology` cases are entirely management-plane obligations. | MEASURED (`src/.../AzureServiceBusTransport/Contexts/ServiceBusConnectionContext.cs`) | ~40 |
| **M-2** | **Runtime-derived entity names + per-namespace entity quota.** ViciOne's publish topology derives one topic per CLR message type at bus start and binds it to the receive queue through a subscription with `ForwardTo`. Names are computed from the type, so they cannot be listed in a static `Config.json` ahead of time; and the Azure test cohort publishes roughly 90 distinct message types, far above the emulator's per-namespace entity cap. | Every publish/subscribe obligation — the majority of the cohort — is unreachable on the emulator, not because the feature is missing but because the entity set cannot be declared. | MEASURED (topology builders + the message types declared across the 55 test files) | ~45 |
| **M-3** | **Partitioned entities.** `EnablePartitioning` is a create-time entity property; the emulator implements no partitioned entities, so a partitioned queue or topic cannot exist there and partition-key routing and per-partition ordering are unobservable. | `Container_Specs`, `SagaRepository_Specs`, `TopologySetParitioning_Specs`, `TopologySetParitioningSubscription_Specs`. | MEASURED (use sites) + DOC (emulator does not support partitioning) | 4 |
| **M-4** | **Broker-side clocks: peek-lock expiry, `MaxAutoLockRenewalDuration`, session lock, TTL, duplicate-detection window, scheduled enqueue.** The emulator implements these APIs in-process; its timers are an approximation, not the service's. An obligation that *is* a timing claim about the service cannot be settled by an approximation of it. | `DuplicateDelivery_Specs`, both `LockTimeout_Specs`, `SessionLockLost_Specs`, `SessionConcurrency_Specs`, `SendContext_Specs`, the ten `ScheduleMessage_Specs` cases, `Publishing_to_a_duplicate_detection_topic`. | MEASURED (the assertions are timing assertions) | ~18 |
| **M-5** | **`AutoDeleteOnIdle`.** No idle-based entity lifecycle exists in the emulator; the property has no effect there, so a test would pass without the behaviour existing. | Temporary endpoints, `Connecting_a_temporary_endpoint`, the request client's temporary response queue, and gap row for the property itself. | MEASURED (`Defaults.AutoDeleteOnIdle`, `ServiceBusEndpointAddress` query emission) + DOC | 3 |
| **M-6** | **Premium-tier features.** `MaxMessageSizeInKilobytes` above 256 KB is Premium-only; the emulator has no tier model and no message-size ceiling. Setting it on a Standard namespace must be *rejected*, and that rejection is the valuable half. | Gap row `MaxMessageSizeInKilobytes`; also the reason `MessageData_Specs` exists at all. | MEASURED (public property, zero test use sites) + DOC | 1 |
| **M-7** | **AAD / RBAC.** The emulator issues no AAD tokens and evaluates no role assignments. `IServiceBusHostConfigurator.TokenCredential` — the OIDC / workload-identity path Lead plan §7 *mandates* — cannot be exercised locally at all, and neither can an authorisation *denial*. | Gap row for the §7 credential obligation. | MEASURED (product surface + emulator auth model) | 1 |
| **M-8** | **`AmqpWebSockets` transport.** The emulator exposes plain AMQP on 5672 and no WebSocket endpoint. | Gap row `TransportType`. | MEASURED (public property, zero use sites) + DOC | 1 |
| **M-9** | **Auto-forwarding (`ForwardTo`, `ForwardDeadLetteredMessagesTo`).** ViciOne's entire topic→queue delivery model is built on subscription auto-forward. Whether the emulator honours `ForwardTo` for statically declared subscriptions is the single most consequential open question for this cohort. | If it does not, *every* publish/subscribe row is External regardless of M-2. | **DOC / UNRESOLVED — probe required (P-1)** | see M-2 |

### 5.2 Event Hubs

| # | Mechanism | Why it matters | Derivation | Rows |
|---|---|---|---|---:|
| **M-10** | **AAD / managed identity.** The emulator accepts only the fixed development SAS connection string (`UseDevelopmentEmulator=true`) and issues no AAD tokens; Azurite likewise. `Host(fullyQualifiedNamespace, TokenCredential)` and `Storage(Uri, TokenCredential)` — the §7 OIDC path — cannot be exercised. All 18 inherited tests use the one hard-coded development string. | Gap row for the §7 credential obligation. | MEASURED (`Configuration.cs` + `IEventHubFactoryConfigurator`) | 1 |
| **M-11** | **Partition ownership and rebalancing at realistic scale.** `config.json` declares 2 partitions per hub — the bare minimum at which rebalancing is observable — and the ownership-lease and load-balancing intervals are `EventProcessorClient`/service timings the emulator does not reproduce. A rebalancing claim proven on 2 emulated partitions is not a claim about a 32-partition production hub. | Gap row for ownership/rebalancing; also why `HealthCheck_Specs` proves less than its name suggests. | MEASURED (`config.json`, `ProcessorLockContext`) + DOC | 1 |
| **M-12** | **Retention and offset expiry.** The emulator does not age events out on a retention schedule, so a checkpoint offset can never fall out of range there — the exact failure mode the product must survive. | Gap row for offset expiry. | MEASURED (`BatchCheckpointer`, no retention config) + DOC | 1 |

The remaining 22 Event Hubs obligations are `LocalIntegration`: this project already ships the official emulator
plus Azurite, and `config.json` pre-declares every hub and consumer group the tests use. Event Hubs is the one
owner in this cohort whose emulator story is genuinely good.

### 5.3 Amazon SQS / SNS

| # | Mechanism LocalStack does not reproduce | Why it matters | Derivation | Rows |
|---|---|---|---|---:|
| **M-13** | **IAM evaluation.** LocalStack Community performs no IAM authorisation — it accepts the literal `admin`/`admin` the entire inherited cohort uses. Neither a successful `AssumeRoleWithWebIdentity` nor an `AccessDenied` can be observed. The §7 path (`UseDefaultHost` → `DefaultAWSCredentialsIdentityResolver.GetCredentials()`) has zero call sites in the tests. | Gap row for the §7 credential obligation; plus the two byte-identical `Should_connect_with_*` cases. | MEASURED (product + test credential sources) | 3 |
| **M-14** | **Visibility-timeout accounting and in-flight quotas.** `AmazonSqsReceiveLockContext.RenewMessageVisibility` extends visibility at 70 % of the timeout, clamped to 12 h / ≥60 s. LocalStack's visibility timer is in-process and it enforces no in-flight message limit, so a renewal claim proven there is not a claim about SQS. | Both `LongConsumer_Specs` cases. | MEASURED (`AmazonSqsReceiveLockContext.cs`) | 2 |
| **M-15** | **Long polling.** LocalStack returns immediately whether `WaitTimeSeconds` is 0 or 20, so the property has no observable effect and a test cannot distinguish short from long polling. | Gap row `WaitTimeSeconds`. | MEASURED (3 set sites, 0 assert sites) + DOC | 1 |
| **M-16** | **Message-attribute and payload quotas (10 attributes, 256 KB).** LocalStack enforces neither, so a test written against it is green regardless of what the product does — the classic case of an emulator making a test lie. This also determines whether the S3/MessageData offload path is required. | Gap row for quotas. | MEASURED (no enforcement anywhere in the product) + DOC | 1 |
| **M-17** | **Partial batch-failure response shape.** `PublishBatcher` / `SendBatcher` / `DeleteBatcher` depend on AWS reporting per-entry success and failure within one batch; LocalStack does not reproduce that shape faithfully, and the 10-entry / 256 KB batch caps are unenforced. | Gap row for the batchers. | MEASURED (three untested public types) + DOC | 1 |
| **M-18** | **Queue-policy document size.** `Should_create_queue_with_multiple_subscriptions` subscribes 23 SNS topics to one queue. The real boundary is the AWS queue-policy document size limit, which grows with each subscription and is enforced by AWS only. The literal 23 in the source suggests the limit was found empirically against the real service. | Recommend a paired External row for the real limit. | MEASURED (the test) | (recommended) |

The remaining 60 SQS obligations are `LocalIntegration`. LocalStack genuinely carries the SQS/SNS data plane,
which is why this owner has the healthiest split in the cohort.

### 5.4 Required probes before any of this is treated as settled

Nine rows above are marked DOC. Per §8 a claim is not a measurement. Wave C4a must record one capability probe
per emulator, as evidence, before an External classification resting on a DOC row is treated as final:

* **P-1 (highest value):** does the Service Bus emulator honour `ForwardTo` on a statically declared subscription?
  If yes, a meaningful `LocalIntegration` subset of the Azure cohort becomes reachable by pre-declaring entities;
  if no, all 107 External rows are confirmed and no Azure emulator profile is worth building.
* **P-2:** the emulator's actual per-namespace entity cap and per-subscription rule cap.
* **P-3:** does the emulator support sessions, session state, scheduled enqueue, duplicate detection and the
  dead-letter sub-queue for pre-declared entities?
* **P-4:** does the Event Hubs emulator honour more than 2 partitions, and does it expire offsets?
* **P-5:** does LocalStack 3.0.2 enforce the 10-attribute and 256 KB limits, and does it distinguish long polling?

None of these probes was run here: the cohort brief forbids starting a container or contacting a cloud service.

## 6. Deviations from the plan's assumptions

* Lead plan §5 assigns three profile variants per owner "as needed". For **Event Hubs** the `UnitArchitecture`
  variant currently has **zero** inherited rows — all 18 are integration-shaped. §5's "no empty placeholder
  projects" rule therefore means either the hermetic project is populated purely from gap analysis (the
  `EventHubConsumeContext` and `EventHubEndpointAddress` rows are the natural seeds) or it is not created.
  Reported, not decided.
* Lead plan §5 gives `src/Persistence/ViciOne.ServiceBus.AmazonS3` its own three test projects and states it has
  no inherited tests. That is **not quite right**: `tests/.../AmazonSqsTransport.Tests/Persistence/Storage_Specs.cs`
  is an S3 message-data test living in the SQS assembly. It is one obligation, and it belongs to the S3 owner, not
  to this one. Ledger row `OBL-R0-CLOUD-0228` is filed under the SQS source project because that is where the file
  is; the owner correction is a Lead disposition, and cohort R0-PER should be told.
* The same applies to `MessageData_Specs.cs` in the Azure cohort: four obligations that depend on
  `src/Persistence/ViciOne.ServiceBus.Azure.Storage`, which `SOURCE_OWNER_MAP.json` records as having no inherited
  tests at all.

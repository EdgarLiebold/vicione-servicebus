# Deferred engineering work

This file contains bounded work that was deliberately kept out of the active implementation slice.
It is not a second feature catalog, architecture, changelog, or license record. An item is removed
only after its acceptance evidence is committed.

## Verify startup validation across retained capabilities

The historical V5 API review identified delayed validation of correctness-critical options
(`API-005`). Its specific payload-admission example is fixed in the current source, and the
RabbitMQ, Azure Service Bus and Amazon SQS registrations now call `ValidateOnStart()`.
Those examples do not establish that every retained provider, persistence, scheduler and
consumer registration fails at host startup for invalid configuration.

Inventory the current options registrations and their validation paths, including named
multi-bus options. For each correctness-critical option, prove that an invalid selected
configuration fails before a receive endpoint starts, with a useful bus and feature name
in the error. Include negative tests for missing, malformed and conflicting settings and
verify that an unselected optional capability does not fail startup. Use the current source
and public API as the authority; the V5 counts and proposed API shapes in the historical
review are not current requirements. The original review is preserved in Git branch
`archive/review-v5-handover-20261001`.

## Complete Azure Service Bus validation against the real cloud service

The local Azure Service Bus emulator executes queue, topic, subscription, rule, forwarding,
session-state, scheduling, duplicate-detection and dead-letter contracts. It does not provide a
truthful substitute for Azure-owned identity, tier, availability, partition, transport and clock
semantics. The 46 open obligation IDs are:

`OBL-R0-CLOUD-0005`, `OBL-R0-CLOUD-0019`, `OBL-R0-CLOUD-0020`, `OBL-R0-CLOUD-0021`,
`OBL-R0-CLOUD-0025`, `OBL-R0-CLOUD-0026`, `OBL-R0-CLOUD-0027`, `OBL-R0-CLOUD-0028`,
`OBL-R0-CLOUD-0044`, `OBL-R0-CLOUD-0057`, `OBL-R0-CLOUD-0058`, `OBL-R0-CLOUD-0059`,
`OBL-R0-CLOUD-0066`, `OBL-R0-CLOUD-0067`, `OBL-R0-CLOUD-0068`, `OBL-R0-CLOUD-0069`,
`OBL-R0-CLOUD-0072`, `OBL-R0-CLOUD-0079`, `OBL-R0-CLOUD-0080`, `OBL-R0-CLOUD-0086`,
`OBL-R0-CLOUD-0088`, `OBL-R0-CLOUD-0089`, `OBL-R0-CLOUD-0095`, `OBL-R0-CLOUD-0096`,
`OBL-R0-CLOUD-0098`, `OBL-R0-CLOUD-0099`, `OBL-R0-CLOUD-0105`, `OBL-R0-CLOUD-0108`,
`OBL-R0-CLOUD-0109`, `OBL-R0-CLOUD-0117`, `OBL-R0-CLOUD-0122`, `OBL-R0-CLOUD-0124`,
`OBL-R0-CLOUD-0125`, `OBL-R0-CLOUD-0130`, `OBL-R0-CLOUD-0131`, `OBL-R0-CLOUD-0132`,
`OBL-R0-CLOUD-0137`, `OBL-R0-CLOUD-0138`, `OBL-R0-CLOUD-0247`, `OBL-R0-CLOUD-0248`,
`OBL-R0-CLOUD-0250`, `OBL-R0-CLOUD-0251`, `OBL-R0-CLOUD-0254`, `OBL-R0-CLOUD-0255`,
`OBL-R0-CLOUD-0256`, `OBL-R0-CLOUD-0257`.

Keep them open until a short-lived real namespace runs them. Their historical
transition mapping is available in Git commit `42a028a7fa8ed6facf941da3d72064cbf438f196`.

The external profile must use run-scoped entities and credentials, bind the exact Azure resource and
SDK versions, and prove both positive and negative outcomes for Entra/RBAC and SAS validity,
partitioned queue and subscription delivery, Premium versus Standard message-size limits,
AMQP-over-WebSockets, broker-clock AutoDeleteOnIdle, real retry backoff, entity deletion and outage
recovery, lock/session-lock renewal and loss, and TTL/lock-expiry exception classification. It must
use provider state or service acknowledgements as terminal barriers, never sleeps, polling success,
shared resources or a local-emulator result presented as cloud evidence.

The same profile owns the remaining management-plane and namespace-boundary contracts: topology-only
deployment, dynamic and multi-bus subscription endpoints, unchanged-subscription idempotence,
high-entity-count restart, cross-scope routing, temporary endpoint lifetime, scheduled publish and
send-context enqueue, raw-JSON response correlation, prefetched-message shutdown, shutdown-grace
publishing, complete broker-assigned message context values, and the combined Azure Blob Storage plus
Service Bus message-data matrix. These rows stay pending until the real service produces the named
positive and negative outcomes; their retired inherited tests are not reported as executing evidence.

## Separate cache index projection from external observation

`GreenCache<T>` currently uses the same observer fan-out for correctness-critical index projection
and optional external observers. Concurrent operations may publish those callbacks concurrently and
without a global order; the public contract now states that honestly. Do not add an unbounded
single-drainer queue: a blocked observer would let committed values and closures accumulate without
backpressure, and catch-all isolation would also hide a failed index key projection.

Normalize this as a dedicated API slice on top of the consolidated cache engine.
Give internal index projection its own fail-closed correctness path and define a separate bounded,
backpressure-aware external notification contract with an explicit failure channel, reentrancy rule
and ordering guarantee. Preserve complete observer fan-out and the current index generation guards.
Acceptance requires hostile key providers, blocking/throwing/reentrant observers, concurrent
add/remove/clear, bounded memory under backpressure, exact index consistency, and one-cause
mutations for ordering, failure isolation and queue bounds.

## Continue deterministic time normalization beyond envelope materialization

Envelope materialization is complete: System.Text.Json and MessagePack now consume one internal
serializer-independent projection for identifiers, addresses, headers, message types, host data and
times. A standard .NET `TimeProvider` is attached once to the pipeline context, each projection uses
at most one UTC snapshot, and zero or negative TTL values are materialized exactly without a
serializer-owned grace period. Payload encoding remains owned by each serializer. Target-contract
tests, cross-format comparison and four effective clock/clamp/drift mutations protect the rule.

The separate send-context projection in `ForwardMessagePipe<T>` remains intentional: transports,
observers, middleware and caller pipes need that state before body serialization. Do not merge these
causally different stages merely because they carry some of the same values.

The generic-forwarding policy is already resolved and implemented: after the caller pipe, only a
positive TTL can revive an expired inherited envelope; otherwise the forward is logged as
`FORWARD-EXPIRED` and discarded before transport dispatch, persistent-outbox storage or mediator
dispatch. This is intentionally different from the one-second response/fault grace. This TODO must
not reopen that policy or move it back into individual serializers or transports.

Testing observation primitives are also complete. `AsyncElementList<T>` and
`AsyncInactivityObserver` now use one injected standard `TimeProvider`, existing constructors retain
their `TimeProvider.System` defaults, synchronous callbacks execute outside the list monitor, and
source-query failures remain observable. Their native fake-time, cancellation, concurrency and
mutation proofs must be replayed but not redesigned by this TODO.

Recorded-message timestamps are complete as well. Sent, published and received observations, their
lists, bus observers, consumer/saga/handler registrations and mediator harness now derive their
immutable snapshots from the harness `TimeProvider`. Exact metadata, typed and untyped success,
consume faults and independent process-clock regressions are covered by native tests.

The remaining work is deliberately a separate path-complete product slice. Inventory and normalize
the wall-clock reads used by `AsyncTestHarness`/`ContainerTestHarness` budgets,
`RollingTimer`/`InactivityTestObserver`, saga polling, scheduling,
job-service heartbeat intervals, delayed redelivery, copy-context TTL, request state,
outbox/transport deadlines, health waits and retained adapters. Preserve domain-owned timestamp
providers where they are actual public behavior; replace accidental process-clock reads with the
same standard context/provider model only after every caller and persisted/wire consequence is
understood. Acceptance requires deterministic boundary tests, RabbitMQ and every retained affected
adapter, public-API/package comparison, and targeted one-cause time mutations. It also replays the
completed envelope and forwarding contracts without redefining them.

## Complete external benchmark scenarios

The transport- and SQL-Server-backed benchmark scenarios remain tracked in
[`benchmarks/ToDo.md`](benchmarks/ToDo.md). Complete them only with their real infrastructure and do
not replace them with inventory-only or skipped green results.

## Make Activity telemetry observationally no-throw

Meter recording is already isolated through no-throw helpers in the current product, but inherited
`ActivitySource` creation and `StartedActivity` notification still occur directly at multiple call
sites. A throwing or otherwise hostile listener must never change message delivery, persistence,
retry, scheduling, outbox or transport semantics. Do not patch only the Entity Framework outbox
path; that would create inconsistent telemetry behavior and a second policy.

Run one path-complete observability slice that inventories every activity source, listener callback,
`StartedActivity` notification and activity-lifetime owner. Introduce one explicit no-throw
observation boundary while preserving the current OpenTelemetry source names, tags, status,
parentage and timing. Product exceptions and cancellation must retain their original identity, and
listener failures must not be converted into retries, faults or delivery failures.

Acceptance requires deterministic tests with throwing activity listeners at every distinct
emission owner, one-cause mutations that remove the isolation, exact OpenTelemetry schema checks,
all affected unfiltered native profiles, and zero behavior or public-API loss.

## Complete MessageJournal external provider validation

The current provider contract is covered hermetically and against ephemeral PostgreSQL and Azurite
instances. Before release, run the same bounded append, retention, concurrency, failure-isolation
and public-composition contract against short-lived real SQL Server, Azure SQL and Azure Table
resources in the `External` profile. Do not represent an emulator result as proof of a cloud
service's transaction, concurrency or storage-limit behavior, and do not weaken the provider set to
avoid external infrastructure.

Acceptance requires unfiltered native xUnit/MTP execution, zero skips, isolated per-run resources,
secret-free durable evidence, exact resource cleanup and a documented disposition of every semantic
difference from the local PostgreSQL/Azurite results.

## Complete Azure Table persistence validation against the real cloud service

The native Azure Table cohort executes the full repository path locally against a run-scoped
Azurite table: entity conversion, validated keys, saga insertion/read-only/update, real ETag
conflicts and retries, durable futures (including routing slips), all three job-service sagas, and
the bounded optional MessageJournal provider. This is the required local integration proof, but it
does not prove Azure account authentication, cloud networking, production throttling, service-side
limits or every behavioral difference between Azurite and Azure Table Storage.

Before release, add one source-mirrored xUnit 4 / Microsoft Testing Platform 2 `External` project
that runs the same provider-crossing contract against a short-lived real Azure Table resource. The
selected Azure group must fail before discovery when configuration is incomplete; it must never
skip or silently fall back to Azurite. Use the existing typed configuration pipeline and the
`Azure.Identity` credential chain, create a unique table per test run, avoid logging secrets or
tokens, and delete only the exact run-owned tables during cleanup.

The External matrix owns three distinct, non-interchangeable service contracts from the frozen R0
ledger. `OBL-R0-PER-0455` exercises Cosmos DB for Table separately, including its consistency model,
property differences and causal 429/retry-after response. `OBL-R0-PER-0456` proves Entra ID success,
insufficient-scope failure and token refresh during an in-flight operation. `OBL-R0-PER-0457`
verifies the real service response at the 1-MiB entity, 255-property and maximum-property-size
boundaries. Azure Table Storage success must not be reported as Cosmos compatibility, and neither
service may fall back to Azurite.

Acceptance requires locked Release restore/build, unfiltered External execution with zero failures
and skips, resource-cleanup evidence, exact Azure SDK/service error identities for authentication,
throttling and optimistic-concurrency failures, and an explicit disposition of every observed
Azurite-versus-Azure difference. Local Azurite results must remain labelled `LocalIntegration` and
must never be promoted into this cloud verdict.

## Complete Entity Framework SQL Server and Azure SQL external variant validation

The inherited Entity Framework R0 set contains 90 obligation contracts but 156 execution variants.
Current native coverage executes or provider-neutrally consolidates 116 of those identities: 48
unparameterized, 39 provider-neutral and 29 against real PostgreSQL. The remaining 40 identities are
provider-specific: 29 SQL Server cases and 11 SQL Server resiliency cases. They remain
`EXTERNAL_PENDING`; neither their former execution nor a local PostgreSQL result is evidence that
SQL Server or Azure SQL behaved correctly.

Build one source-mirrored xUnit 4 / Microsoft Testing Platform 2 External cohort for these 40
identities. Reuse one behavioral contract where SQL Server and Azure SQL are semantically identical,
but execute it against both short-lived real SQL Server and Azure SQL resources wherever deployment
or retry behavior can differ. The profile must fail before discovery when its selected provider is
not completely configured; it must never skip, inventory, or silently downgrade a missing resource.
Resource coordinates come from the one typed native-test configuration pipeline. Credentials stay
in the shared User Secrets store or the provider credential chain and never enter source, logs or
durable evidence.

Acceptance requires an exact 156-row variant-disposition matrix derived from the frozen R0 set,
locked Release restore/build, unfiltered External execution with zero failures and skips, run-scoped
database names, cleanup evidence, one-cause retry/transaction/provider mutations and independent
product and test/evidence PASS reviews. Only then may the 40 rows move from `EXTERNAL_PENDING` to an
executing terminal disposition.

## AWS real-service closure

The native AWS cohort uses one run-scoped LocalStack fixture to prove actual AWS SDK request,
serialization, persistence, concurrency and lifecycle-composition paths for SQS/SNS, DynamoDB and
S3. It does not claim IAM/OIDC, quota, throttling or service-controlled delayed behavior from an
emulator. The following frozen obligations therefore remain release-blocking `EXTERNAL_PENDING`:

- `OBL-R0-CLOUD-0216` and `OBL-R0-CLOUD-0217`: real long-running SQS visibility renewal and
  isolation between slow consumers;
- `OBL-R0-CLOUD-0265`: the default AWS SDK credential chain, short-lived OIDC credentials and
  in-flight refresh;
- `OBL-R0-CLOUD-0266`, `OBL-R0-CLOUD-0268` and `OBL-R0-CLOUD-0272`: real SQS/SNS payload and
  attribute quotas, long polling/cancellation and partial batch-failure semantics;
- `OBL-R0-PER-0523` and `OBL-R0-PER-0524`: service-controlled DynamoDB TTL deletion and provisioned
  throughput throttling, distinct from optimistic concurrency;
- `OBL-R0-PER-0603`: service-controlled S3 lifecycle deletion after the configured retention.

Run these in source-mirrored xUnit 4 / Microsoft Testing Platform 2 External projects against a
short-lived real AWS account boundary. Configuration must fail before discovery when the selected
role, region or resource owner is incomplete; tests must never skip or fall back to LocalStack.
Use OIDC and the normal AWS SDK provider chain, create only run-owned resources, persist no access
key, secret, session token or signed URL, and delete the exact run-owned resources even after a
failed test. Acceptance requires zero failure/skip, exact provider error identities, bounded causal
barriers instead of wall-clock sleeps, resource-cleanup evidence and an explicit LocalStack-versus-
AWS disposition for every observed semantic difference.

S3 arbitrary per-message retention is a separate release-blocking product decision. The current
repository deliberately accepts either no TTL or a positive whole-day TTL exactly equal to its
immutable bucket lifecycle policy; it rejects every value that would otherwise promise unsupported
per-object deletion. Before exposing arbitrary TTL, choose and implement one complete ownership
model (for example object tags plus owned lifecycle rules with bounded policy cardinality, or a
separate expiration index and deletion worker). Prove concurrent policy updates, restart recovery,
partial failure, cancellation, exact object retention, bounded resource growth and real AWS
eventual deletion. Do not silently round durations, create one lifecycle rule per object, or accept
a TTL that the product cannot enforce.

## Complete Event Hubs validation against the real cloud service

The pinned local Event Hubs emulator and Azurite own deterministic transport, checkpoint, retry,
metadata, saga and lifecycle verification. They do not issue Microsoft Entra tokens, evaluate Azure
RBAC, reproduce service-controlled partition rebalancing under real namespace pressure or enforce the
real service's retention lifecycle. Those differences remain visible instead of being counted green:

- `OBL-R0-CLOUD-0258`: workload identity, authorization denial and token refresh against Azure;
- `OBL-R0-CLOUD-0261`: service-owned partition rebalance with exact checkpoint continuity;
- `OBL-R0-CLOUD-0262`: service-enforced retention at the configured boundary.

Run these in a disposable Azure namespace with OIDC/workload identity and no stored client secret.
Acceptance requires bounded provider-owned state and delivery barriers, exact partition/checkpoint
continuity, explicit cleanup of the namespace resources, zero skip/failure and evidence that names the
Azure endpoint and identity mode without recording tokens or connection strings.

## Complete RabbitMQ validation against Amazon MQ

The pinned run-scoped RabbitMQ fixture owns every deterministic protocol, topology, retry, lifecycle,
stream, scheduling and management assertion. `OBL-R0-BRK-0004` remains external because it requires
Amazon MQ's managed TLS endpoint, AWS-owned certificate chain, account policy and service lifecycle;
counting a local container as that proof would be false.

Run the retained connection/start/stop case against a disposable Amazon MQ RabbitMQ broker through
short-lived AWS identity, without storing credentials or connection strings. Acceptance requires an
exact successful endpoint ready/stop lifecycle, bounded provider state, explicit broker/resource
cleanup, zero skip/failure and evidence that identifies the AWS region and authentication mode without
recording secrets.

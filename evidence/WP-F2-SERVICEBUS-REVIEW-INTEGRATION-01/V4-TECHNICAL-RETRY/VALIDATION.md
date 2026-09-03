# V4 technical retry and send-failure classification validation

Date: 2026-09-03

## Bound inputs and scope

- Package: reviewer integration 10/12, V4 checkpoint 009.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-09` at local architecture
  commit `389162b4`.
- Product baseline: `0af45942509b6391a8265cc2eb366b422714e375`, tree
  `87adffec5b3dcb59d2d134cdfee5da0dfbc47263`.
- Protected review aggregate recorded for the immutable handoff:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V4 bundle SHA-256: `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`.
- Semantic donor: `8e98f4273420a5c72d674c50af7b24eb257ddc48`; reconciled through final V4
  head `f8050928715e536b60c42d800d1cbb81c085818f`.
- `review/**` remained read-only and untracked throughout.

The current native EF outbox, RabbitMQ reliability and TimeProvider owners postdate the donor and remain
authoritative. The donor was therefore treated as architectural evidence rather than applied as a patch.

## Integrated behavior

Consumer infrastructure now has a public, conservative `ITechnicalFailureClassifier` taxonomy and one
canonical bounded policy. Only explicitly transient failures receive immediate retries at 100 ms, 500 ms
and 2 s or delayed redelivery at 15 s, 1 minute and 5 minutes. Unknown failures do not silently enter the
standard retry loop; structural, programming, cancellation, serialization and security failures are
terminal. Invalid values supplied through the exception-side classification interface are normalized to
unclassified. Nested and aggregate traversal is order-independent: terminal evidence dominates, and an
unknown member prevents an all-transient aggregate verdict.

ActiveMQ, Amazon SQS, Azure Service Bus, PostgreSQL and SQL Server now register typed singleton adapters
for the existing persisted outbox send-failure contract. Each adapter walks the complete exception chain,
uses only provider types, status codes, failure reasons or numeric error codes, and gives permanent evidence
precedence over an outer transient wrapper. Message text, exception type names and `ToString()` output are
not policy inputs. The already proven RabbitMQ adapter remains unchanged; its new convenience extension
binds the canonical delayed cadence and transient taxonomy to the finite native TTL/DLX queue-redelivery
plan.

Built-in Future, Job and Quartz infrastructure defaults use this shared policy. User-configured business
retry policies are untouched.

## Native corrections beyond the donor

Native reconciliation found and corrected four material weaknesses:

1. The donor aggregate loop returned at the first unknown member, so a later terminal member could be
   ignored and classification depended on aggregate order.
2. Provider adapters returned at the first recognized outer exception, allowing permanent inner evidence
   such as authorization or configuration failure to be hidden by a transient wrapper.
3. Arbitrary numeric values returned by `IRetryFailureClassification` escaped the public taxonomy.
4. Applying the conservative policy literally to Future infrastructure exposed missing provider-neutral
   concurrency normalization. Azure Table surfaced stale ETags as a generic `SagaException`, while EF Core
   could wrap transient database evidence in `InvalidOperationException`.

The fourth issue was diagnosed causally rather than masked with broad retries. Azure Table now maps only
HTTP 412 update/delete failures to the existing `ConcurrencyException`, preserving the exact Azure
exception as the inner cause; 400 and 500 remain exact non-concurrency `SagaException` failures. EF Core
maps only `DbUpdateConcurrencyException` at save/update/delete boundaries to the same provider-neutral
exception and leaves other `DbUpdateException` instances unchanged. The default classifier treats that
concurrency contract and `DbException.IsTransient` as transient, including EF's exact
`InvalidOperationException`/transient-`DbException` wrapper shape. Terminal inner evidence still dominates.

This restores the intended technical retry behavior without classifying all saga, EF, Azure or database
failures as transient.

## Native test ownership and assertion quality

Twenty-five new source-owner methods plus one strengthened Azure boundary method cover:

- every owned central classification branch, custom classification, invalid enum values, nested and
  aggregate precedence in both orders, exact null boundaries and typed database evidence;
- immutable schedules, exact retry/redelivery intervals, actual built-policy filtering and custom
  classifier forwarding;
- transient, permanent and unknown paths for all five provider adapters, full-chain precedence,
  text-independence and singleton/idempotent registration through every configuration overload;
- RabbitMQ's exact canonical queue plan and filter composition;
- exact internal Future, Job and Quartz composition call sites and absence of parallel bespoke defaults;
- Azure Table 412 normalization versus 400/500 controls, and EF save/update/delete concurrency
  normalization versus unchanged non-concurrency controls.

The assertions bind exact enum values, exact exception types, exact inner-exception identity, exact saga
type/identity properties, interval sequence and order, policy retry counts, DI implementation type and
lifetime, and source-owner call counts. No new test is assertion-free, no oracle derives its expected value
from the implementation under test, and no provider verdict depends on message text. The one initial
mutation survivor led to a stronger independent precedence/registration oracle before the mutation was
accepted.

## Positive execution evidence

The final analyzer-active Release Unit-solution build completed with zero warnings and zero errors. All
final test commands used the repository's pinned/offline dependency graph and MTP's serialized module mode.

| Scope | Result |
|---|---:|
| Core classifier focus | 3/3 passed |
| Azure Table repository boundary | 21/21 passed |
| EF repository boundary | 10/10 passed |
| Azure Table Future against fresh Azurite | 6/6 passed (`vicione-fa6142f31d7f`) |
| EF Future against fresh PostgreSQL | 6/6 passed (`vicione-2ad1cc09cf77`) |
| ActiveMQ outage recovery focus | 2/2 passed (`vicione-4260d7563d1b`) |
| SQS/Quartz focus | 1/1 passed (`vicione-b415aaa800bf`) |
| Complete SQS LocalStack owner | 48/48 passed (`vicione-fb695ba55764`) |
| Canonical Unit/Architecture | 3,237/3,237 passed, 0 skipped |
| General six-provider LocalIntegration | 342/343 passed, 0 skipped (`vicione-48e3690cf4b0`) |
| SQL Server/PostgreSQL provider profile | 62/62 passed, 0 skipped (`vicione-4ef7eca04e50`) |
| Azure Service Bus emulator profile | 24/24 passed, 0 skipped (`vicione-9e4386243726`) |
| RabbitMQ profile | 24/24 passed, 0 skipped (`vicione-04778dbdaefa`) |

The sole broad-run failure is the pre-package-6
`InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet` race. It
was previously reproduced against clean baseline commit `0df0a5ed`; in this package it again varied between
database `ReceiveCount` values 1 and 2 instead of 3. No package-10 source participates in that inbox/outbox
path. It remains a separately recorded inherited defect and is not hidden as a green result.

The first broad invocation omitted the explicit ActiveMQ outage-control switch and therefore failed the two
tests that correctly refuse unmanaged broker interruption; the corrected complete ActiveMQ assembly passed
inside the final broad run. A one-off SQS/Quartz trigger-cleanup observation under the initial overloaded
run passed both in isolation and in the subsequent complete 48-case SQS owner. SQL validation also rejected
two incomplete invocations before product execution was accepted: one used a solution name as an invalid
profile enum, and one started SQL Server without the PostgreSQL fixture required by the cross-provider
credential case. The canonical two-fixture run is the 62/62 result above.

## Independent mutation evidence

Fifty-nine one-cause production mutations were applied independently. Every changed target built, its
named native owner went causally red, and the target was restored before the next mutation and final
positive execution.

| Range | Mutated mechanisms | Killing observations |
|---|---|---|
| M01-M12 | central explicit, built-in, nested and aggregate classification; invalid values and precedence | exact kind/order rows failed |
| M13-M19 | immutable immediate/redelivery cadence, transient-only filters, null guards and extension forwarding | exact intervals, retry counts or source-owner call counts failed |
| M20-M39 | ActiveMQ, SQS, Azure Service Bus, PostgreSQL and SQL Server typed transient/permanent/unknown branches and full-chain precedence | provider-specific verdict rows failed |
| M40-M44 | idempotent classifier registration for all five transports | exact descriptor type/count/lifetime rows failed |
| M45-M49 | Future, Job, Quartz and RabbitMQ canonical composition | architecture or exact queue-plan rows failed |
| M50 | `ConcurrencyException` no longer transient | central taxonomy row failed |
| M51 | direct transient `DbException` ignored | typed database row failed |
| M52 | exact EF `InvalidOperationException`/transient-DB wrapper rule removed | wrapped database row failed |
| M53-M54 | Azure Table update/delete 412 status changed to 411 | stale ETag rows retained generic failures |
| M55-M56 | Azure Table update/delete mapping broadened to all 4xx/5xx | 400/500 control rows became concurrency failures |
| M57-M59 | EF save/update/delete concurrency normalization removed | operation-specific exact exception rows failed |

The restored intended implementation has these SHA-256 values:

```text
5f7b2062c5319f4a943c8ff4191c02c779c80fe1f89af6f23e05d4b78bc2b415  DefaultTechnicalFailureClassifier.cs
3b74351dc91a8edc9ce627bbf5855bbe0a4acd9d293e19c49441362cb036817b  TechnicalRetryPolicy.cs
7429b22c21fa8ad8b0be46f15f8bd7af619f57cc3c588492499c39833685e642  ActiveMqSendFailureClassifier.cs
032e17b83726852ac004bbfbb22c6c1a5aef26f5f04e8909c518f58de4e20b98  AmazonSqsSendFailureClassifier.cs
1d0cc3054960459aed78b58089217edd6126dc9c1bffb777edb81435f748070b  ServiceBusSendFailureClassifier.cs
5ca81a9b6b82067c6fd6715ea95eba2a1a7cda66790e4a81e4e5964edfbf4138  PostgresSendFailureClassifier.cs
5656f6a24ae12223f44159a35710a4ee7bde42ddf905f59fd8df9ccedcb7c8a8  SqlServerSendFailureClassifier.cs
eb5a5a83231c868d3c60b6a3b9f8087edaeb5d6eac2e8439b24f10e6a00def76  AzureTableSagaRepositoryContext.cs
68d005a5e1c1fe99e67473fbc2a0aec869829aaf8f408998db70c23613774c5f  DbContextSagaRepositoryContext.cs
ce083f26a67ab2f0065dd3d4ade84e1703a9c579233654a5895ec34b14bb4cf3  RabbitMqQueueRedeliveryExtensions.cs
```

## Static and hygiene gates

- Every file listed by the protected review manifest verifies; the V4 bundle hash is unchanged.
- Scoped `dotnet format --verify-no-changes` passes for every changed/new C# file.
- All changed requirements JSON files parse and project through the complete native test run.
- Classifier sources contain no message-text, type-name-string or `ToString()` policy logic.
- `git diff --check`, stale-symbol, duplicate-registration and empty-directory gates pass.
- No protected review file is staged or modified.

Package status: locally complete as reviewer package 10/12 (83.3%). No remote publication is authorized or
implied by this evidence.

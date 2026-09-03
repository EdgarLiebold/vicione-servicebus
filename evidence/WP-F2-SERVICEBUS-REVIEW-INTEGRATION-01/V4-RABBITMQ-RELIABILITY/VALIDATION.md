# V4 RabbitMQ reliability, recovery, and fault-redrive validation

## Bound inputs

- Integration baseline commit: `cf2e9ebfbc193573fd376df549469a956bc7de80`
- Integration baseline tree: `0babdea5cf9e910e849d57ce55fd9725d0ea0f13`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head: `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commits: `8bf6d3493032add0cb4495ff65d75590e3310673` and
  `6ab374c3cfcf45c0bdb9bd6800ea6099a0f035eb`
- The PO-owned `review/**` tree remained unchanged and untracked.

## Integrated behavior

RabbitMQ endpoints can now use a finite, immutable set of predeclared TTL/DLX delay queues for
technical redelivery without the delayed-message plugin. Endpoint startup declares and inspects the
complete classic or quorum topology. Runtime delays outside that finite set fail closed. Unnamed,
exchange-only, stream, empty, nonpositive, fractional-millisecond, over-TTL-limit, and over-name-limit
configurations are rejected before provider work.

One connection-scoped cache owns durable non-auto-delete topology knowledge. Stable declarations and
bindings are single-flight, while transient topology remains channel-owned. Definitions are immutable
structural snapshots with framed nested values and structured entity keys. Conflicting definitions fail
before provider work. Caller cancellation cancels only that wait; provider faults evict the shared entry;
generation checks prevent an invalidation race from resurrecting stale declarations. Channel,
connection, topology, send, and unsolicited consumer-cancel paths invalidate the shared knowledge.

RabbitMQ send failures are classified only from typed provider exceptions and AMQP reply codes. Nested
authentication remains permanent even inside a transient-looking broker-unreachable wrapper, reply text
never controls policy, and unknown exception types remain unclassified.

Default-bus and typed-bus administrative operations perform bounded redrive from the topology-derived
error queue. Both source and target names are validated and passively verified. A dedicated channel uses
mandatory publish, publisher confirms, and return tracking. Exact MessageId, CorrelationId, and fault-type
filters compose by conjunction. Unmatched deliveries remain unacknowledged and are requeued when the
channel closes. A source delivery is acknowledged only after the target publish confirms; the documented
post-confirm/pre-ack crash window is therefore at-least-once rather than silent loss.

## Review quality and native corrections

The reviewer direction is high value and correctly identifies the missing reliability capabilities, but
the donor is not safe as a direct patch. Native integration corrected these defects and ambiguities:

1. A canceled waiter evicted a still-running connection-owned declaration and could start a duplicate.
2. A transient outer `BrokerUnreachableException` returned before a nested authentication failure was
   inspected.
3. Concatenated topology keys could alias valid names containing delimiters.
4. Unframed canonical strings could alias nested structures and scalar types.
5. A declaration invalidated while completing could publish stale success after the invalidation.
6. The donor lifetime rewrite was not compatible with the current lease-owned channel/connection
   disposal path; the already proven asynchronous shutdown handoff remains authoritative.
7. The first real mandatory-return run exposed an `async` lambda passed to an `Action<T>` pipe. Its
   `PublishReturnException` escaped as `async void` and terminated the test host with exit 134. The
   operation now uses the task-aware pipe and returns the exact failure to its caller.
8. The topology-derived source queue name was not validated after the custom error formatter expanded it.
9. Real RabbitMQ inspection established that the current source settings explicitly project
   `x-queue-type=classic`; the delay queue must preserve that semantic argument just as it preserves
   quorum type and initial group size.

This supports an A-/B+ assessment: strong architecture and risk selection, with material concurrency,
exception-ordering, key-canonicalization, and task-boundary defects still requiring native correction.

## Executing evidence

- Final analyzer-active, non-incremental Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: zero
  warnings and zero errors.
- Final canonical serialized Unit/Architecture profile: 3,141/3,141 passed, zero failed, zero skipped.
- Final RabbitMQ unit owner: 148/148 passed, zero failed, zero skipped.
- Final analyzer-active RabbitMQ LocalIntegration build: zero warnings and zero errors.
- Final real RabbitMQ profile: 24/24 passed, zero failed, zero skipped, canonical fixture run
  `vicione-1fe54599dc04`.
- The real profile inspects classic and quorum TTL/DLX arguments and bindings before publishing, deletes
  a durable endpoint queue externally and proves complete route recovery, and exercises structured and
  unfiltered redrive, exact bounds, mandatory rejection, passive topology checks, source retention, body,
  routing, IDs, content type, type, persistence, and fault-header preservation.
- The architecture wall-clock rule rejected an intermediate 25 ms polling delay. The delay was removed;
  the focused rule then passed and the complete 3,141-case profile passed afterward.
- Both changed requirements JSON files parse and contain 80 unit and 21 local dispositions. Compiled
  requirement projection passes in the complete profile.
- Scoped `dotnet format --verify-no-changes`, `git diff --check`, forbidden-wait/async-void scans, and
  empty-directory scans pass. The canonical fixture runner left no Docker test container running.
- No inherited RabbitMQ test was removed; the new owners extend rather than replace the existing suite.

The first direct focused provider command was deliberately rejected by test configuration because it did
not use the canonical fixture runner. Subsequent provider evidence uses only the runner-projected
run-scoped credentials. Intermediate test-oracle corrections are retained in this record: classic queue
type is explicit, complete topology recovery is observed rather than the earlier queue-only intermediate
state, the minimal DI host includes logging while retaining `ValidateOnBuild`, and RabbitMQ's exact
mandatory-return type is `PublishReturnException`.

## Independent one-cause mutations

Twenty-four buildable one-cause product mutations were killed. Every target was restored before the final
positive builds and executions.

| ID | Single changed cause | Causal native owner |
| --- | --- | --- |
| M01 | Treat stable exchanges as uncached | 32 provider calls violate stable single-flight |
| M02 | Evict shared work when only one waiter cancels | later waiter starts a second declaration |
| M03 | Retain a faulted cache entry | healthy retry receives the first failure again |
| M04 | Return after post-await generation invalidation | racing declaration is not repeated |
| M05 | Erase scalar types from topology fingerprints | `int` and `long` definitions alias |
| M06 | Collapse binding keys into delimiter text | valid delimiter-bearing names collide |
| M07 | Generate a routing key for an undeclared delay | finite-delay owner observes no rejection |
| M08 | Truncate fractional milliseconds | interval-boundary owner observes no rejection |
| M09 | Accept stream queues for TTL/DLX redelivery | endpoint-shape owner observes no rejection |
| M10 | Skip generated return-exchange and delay-queue name checks | long generated topology is accepted |
| M11 | Return transient at the outer broker-unreachable wrapper | nested authentication becomes transient |
| M12 | Omit AMQP 403 from permanent reply codes | code and reply-text independence rows fail |
| M13 | Invert the MessageId predicate | exact conjunction and bounded loop results fail |
| M14 | Scan one message beyond `MaxScanCount` | exact scan-limit owner consumes the fourth row |
| M15 | Acknowledge the source before target publish | order, publish-failure, and ack-failure owners fail |
| M16 | Disable publisher confirmations | dedicated-channel option owner fails |
| M17 | Do not inherit the source queue type | real quorum topology cannot start |
| M18 | Dead-letter to a different routing key | both real classic and quorum topology oracles fail |
| M19 | Make cache invalidation a no-op | external deletion never restores the complete binding |
| M20 | Actively declare rather than passively verify queues | missing topology is silently created |
| M21 | Pass the redrive task through an `Action<T>` pipe | mandatory return escapes and terminates the host |
| M22 | Remove typed-bus queue-operations registration | exact DI ownership row fails |
| M23 | Skip validation of the formatted source queue | over-limit derived name is accepted |
| M24 | Publish a fresh empty properties object | real redrive loses IDs and the complete property oracle fails |

Removing only the eager invalidation call in the unsolicited consumer-cancel callback produced the same
observable recovery because the topology-filter/channel-recycle paths independently invalidate the same
connection cache. It is recorded as an equivalent defense-in-depth mutation, not counted as killed. The
stronger behavior mutation M19 disables invalidation itself and is causally red. The eager callback is
retained because it closes the short interval before channel recycle for other connection users.

Restored SHA-256 values:

- topology cache: `dd477d0ba14baab101ea7de9110f7aca54674c4c050aee9b5efbc2466d4a4bd5`
- queue-redelivery plan: `258dce124a34b3820261d2b0d632ccb735d33de64ef6e9abb71c90d075aa39a6`
- queue operations: `e9fe49a23c1ddd0f489d5e74b8ee6139f36ba257f17c537d143431dd2a07b95f`
- topology-cache tests: `bce02cbc37c76607940222f755204844ef9a4a5c9858d093d2309bb23870a3bf`
- real fault-redrive tests: `fae777bf54b15c4540076419218d58d758fa1a49ffe39c521eab87c81443a2d1`

This local package is the seventh of twelve semantic V4 reviewer packages. It raises V4 integration
progress to 7/12 (58.3%). Remote publication is not included or implied.

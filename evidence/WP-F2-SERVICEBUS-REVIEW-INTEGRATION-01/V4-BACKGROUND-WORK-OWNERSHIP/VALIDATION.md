# V4 bounded background work and failure ownership validation

Date: 2026-09-03

## Bound inputs and scope

- Package: reviewer integration 11/12, V4 checkpoint 010.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-10` at local architecture
  commit `7d03116aacbdbdf4bcfacaca31221ac9ef8041ba`.
- Product baseline: `f693b737de33a00d2acafe8a8b0a0397c53fd498`, tree
  `de56a8bca7ae66ced847e45d74df4beb0ceafc12`.
- Protected review aggregate recorded for the immutable handoff:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V4 bundle SHA-256: `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`.
- Semantic donor: `633cf9726d852826240ee4fd3a9faa679ffe2a39`; reconciled through final V4
  head `f8050928715e536b60c42d800d1cbb81c085818f`.
- `review/**` remained read-only and untracked throughout.

The current native tests and the already integrated concurrency, resource-cache, RabbitMQ and
TimeProvider owners postdate portions of the donor. The donor was therefore used as semantic evidence,
not applied as a blind patch.

## Integrated behavior

`OneTimeContextPayload` now has a true single-flight attempt owner. Every caller concurrent with one
attempt awaits the same task and observes the same success, cancellation or exact failure. A failed or
canceled attempt is removed only after its cohort has observed it, so only a later caller retries. Explicit
eviction remains unavailable while an attempt is active and creates a new generation afterwards.

`BufferedBus` owns an explicit positive capacity. Admission is asynchronous, cancellation-preserving and
backpressured; a flush releases reservations only for the snapshot it owns. Default and typed DI entry
points validate the capacity and reject conflicting repeated registrations instead of silently retaining
the first value.

The in-memory fabric has separate positive bounds for ready delivery and accepted scheduled work. The
ready queue is a bounded FIFO channel with waiting writers. Scheduled admission is bounded before a delay
is accepted, and queue shutdown cancels and awaits every accepted delayed delivery before returning. The
public host configurator and provider pass one immutable capacity to each queue.

`Gauge.Remove` is asynchronous. Its final transition invokes zero-activity observers in registration order,
awaits each observer and preserves the first failure instead of detaching observer work.

Agent creation now has one `StartAgent` bridge for call sites that own the supplied async context rather
than the mirror task returned by `CreateAgent`. The mirror is observed, while the exact factory outcome and
created context remain owned by `IAsyncPipeContextAgent`. Long-running receive, batch, checkpoint, job and
scheduled-delivery loops start directly and retain their task in their lifecycle owner. ActiveMQ, Azure
Service Bus and RabbitMQ callbacks likewise retain or transfer the stop outcome rather than suppressing it
through warning pragmas or empty continuations.

## Native correction beyond the donor

The literal V4 ActiveMQ connection callback was not correct under real recovery. It used `Task.Yield`
before retiring a faulted connection so that the returned stop task could be stored before NMS re-entered
the callback. In the ordered OpenWire-then-AMQP outage scenario, that delay let recovery advance ahead of
connection retirement. All messages were delivered, but AMQP bus shutdown then hung until the operation
timeout.

The failure reproduced repeatedly with the donor-shaped package code. The unchanged package-10 product
baseline passed the identical two-case test against the current isolated fixture (2/2,
`vicione-2160d95f81bd`), proving a package regression rather than a fixture or historical defect. Reverting
only `ConnectionContextFactory` made the package pass immediately while all other package changes remained
active (2/2, `vicione-63cefd7305ee`).

The final correction claims the stop transition with an incomplete completion owner under the callback
lock before invoking `Stop`. NMS re-entry therefore cannot start a duplicate transition, while the actual
stop begins synchronously at the original causal boundary. The bridge catches and logs its failure and
completes the stored owner in `finally`; no `Task.Yield`, recursive disposal or lost exception remains.
The exact two-case reproducer then passed (2/2, `vicione-54be60d580f8`) and the complete ActiveMQ provider
assembly passed 95/95 (`vicione-622bb58931c7`).

The recovery test was also corrected so a cleanup timeout can no longer replace the primary failure. It
retains and rethrows the original exception and records a subsequent cleanup failure as diagnostic data.

## Native test ownership and assertion quality

New and strengthened source-owner cases bind:

- shared success/failure/cancellation generations and eviction for one-time setup;
- exact buffered capacity, stable blocked admission, cancellation identity, reservation release and DI
  conflict semantics;
- immediate FIFO capacity, scheduled capacity/cancellation, positive configuration and stop/drain
  ownership for the in-memory fabric;
- ordered, awaited Gauge observers and exact first-failure identity;
- exact agent factory failure and created-context identity under supervisor ownership;
- repository-wide source boundaries for agent creation, retained long-running tasks and callback-driven
  transitions;
- the real ActiveMQ OpenWire-to-AMQP outage, recovery, post-recovery delivery and bounded shutdown path.

The initially surviving capacity and shutdown mutations caused the relevant tests to add an independently
observable 250 ms quiet window after the first delivery had entered a blocking receiver. This proves the
second producer/stop task remains pending for the intended reason instead of merely winning a scheduler
race. The RabbitMQ callback mutation likewise strengthened the architecture oracle from a broad text scan
to the exact returned stop expression. No timeout is treated as success.

Three architecture requirements and fourteen Core requirement variants were added; the existing
Abstractions requirement was rebound to the stronger shared-failure-generation case. All three JSON files
parse and project successfully in the complete native run.

## Positive execution evidence

The final analyzer-active Release Unit-solution build completed with zero warnings and zero errors. All
test commands used the locked local dependency graph and MTP serialized module mode.

| Scope | Result |
|---|---:|
| Supervisor ownership focus | 2/2 passed |
| Background-work architecture focus | 3/3 passed |
| ActiveMQ ordered outage reproducer | 2/2 passed (`vicione-54be60d580f8`) |
| Complete ActiveMQ/Artemis owner | 95/95 passed (`vicione-622bb58931c7`) |
| Canonical Unit/Architecture | 3,255/3,255 passed, 0 skipped |
| General six-provider LocalIntegration | 342/343 passed, 0 skipped (`vicione-d3bde6199baa`) |
| SQL Server/PostgreSQL provider profile | 62/62 passed, 0 skipped (`vicione-2bfc3a81bc13`) |
| Azure Service Bus emulator profile | 24/24 passed, 0 skipped (`vicione-f884ec2dbe98`) |
| RabbitMQ profile | 24/24 passed, 0 skipped (`vicione-e4a44a612b59`) |

The sole broad-profile failure is the pre-package-6
`InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet` race:
database `ReceiveCount` was 1 rather than the asserted 3. It was previously reproduced against clean
baseline commit `0df0a5ed5974e7030986eae16699d6bc16f91ec2`; no package-11 source participates in that path. Azure
Table, DynamoDB, S3, Event Hubs, SQS, PostgreSQL transport and ActiveMQ assemblies were all green in the
same 343-case carrier. The inherited EF defect is recorded honestly rather than relabeled as a pass.

## Independent mutation evidence

Fourteen independent one-cause production mutations were executed. Every mutant either built and made
its named owner causally red or, where the first oracle survived, caused that oracle to be strengthened
before the mutation was accepted. Every target was restored before final positive validation.

| Mutation | Mutated mechanism | Killing observation |
|---|---|---|
| M01 | remove one-time concurrent single-flight guard | concurrent cohort lost the exact shared failure |
| M02 | invert failed-attempt reset/retry condition | later healthy retry could not complete |
| M03 | increase buffered capacity by one | capacity/admission owner completed too early |
| M04 | invert repeated-DI capacity conflict | exact conflicting registration no longer failed |
| M05 | increase immediate queue capacity by one | strengthened blocking-receiver quiet-window owner failed |
| M06 | increase scheduled-work capacity by one | second scheduled admission completed too early |
| M07 | omit delayed deliveries from queue stop | strengthened stop-pending/drain owner failed |
| M08 | detach Gauge zero-activity observation | ordered completion and exact failure owners failed |
| M09 | accept zero fabric capacity | exact public validation owner failed |
| M10 | accept zero default buffered capacity | exact DI validation owner failed |
| M11 | remove the owned agent-start bridge | repository-wide architecture owner failed |
| M12 | wrap the SQS receive loop in `Task.Run` | direct lifecycle-task architecture owner failed |
| M13 | discard RabbitMQ callback stop result | exact returned-transition architecture owner failed |
| M14 | delay ActiveMQ connection retirement with `Task.Yield` | AMQP shutdown timed out after ordered outage recovery |

The restored intended implementation has these representative SHA-256 values:

```text
caebe726b6bd2459d4f61ac1cb3c13d788ac3a4e17c5211f5fb1febfafba1c6f  OneTimeContextPayload.cs
4fbd7fb3827ec4e03b782afb515e9273acd9bc4b8752776ad12a9582a54f40de  BufferedBus.cs
5551b92d424fed847b6c3d0eeea7ba7242469e758a721a46400fb26ca99e01ff  MessageFabric.cs
fbb7ac6c2ab95973e4898504f92f72229cbd42203b5c8bbd3893f44321385e8c  MessageQueue.cs
836b27e7e8f3b4fd7a305adddb9c4e14aaeed1c8c976b0d7c6198b181cb126c8  Gauge.cs
fa7192d05b2b846adbd5f0358f08ee1ffc78310e2cd2fef5f86e8b1443969a73  SupervisorExtensions.cs
7d926c6377eb46d4aee1efd6c0a46b085ade72213a51a37a09823df6fb9ec535  ActiveMQ ConnectionContextFactory.cs
```

## Static and hygiene gates

- Every file listed by the protected review manifest verifies; the V4 bundle hash is unchanged.
- Scoped `dotnet format --verify-no-changes` passes for every changed/new C# file.
- All changed requirements JSON files parse and project through the complete native test run.
- No newly added `Task.Run`, `ContinueWith`, unbounded channel, CS4014 suppression, `async void`,
  `Thread.Sleep`, TODO or FIXME remains.
- `git diff --check`, empty-directory and repository-wide ownership gates pass.
- No protected review file is staged or modified.

Package validation is complete and ready for its local product and architecture freeze as reviewer package
11/12 (91.7%). No remote publication is authorized or implied by this evidence.

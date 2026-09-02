# V4 bounded ResourceCache validation

## Bound inputs

- Integration baseline commit: `0df0a5ed5974e7030986eae16699d6bc16f91ec2`
- Integration baseline tree: `b2ab0314d1c583e3731c77e5d83c7912327f6cd6`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head: `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commit: `d9d804d7ba7912e3b41aa9a790bb3233d4c8b1d5`
- The PO-owned `review/**` tree remained unchanged and untracked.

## Integrated behavior

One `ResourceCache<TValue>` state machine now owns pending creation, committed visibility, secondary
index publication, expiration, capacity eviction, observer delivery, cancellation and final resource
release. `KeyedResourceCache<TKey, TValue>` is only a one-index facade over that owner. The two former
cache engines and their duplicate internal machinery are removed together with the fully superseded
tests; no empty compatibility directory remains.

Creation is single-flight per requested key, a caller cancellation cancels only that waiter, factory
failure removes the pending entry, and all index keys are prepared before atomic publication. Capacity
is a hard bound over committed plus pending ownership. Exact TTL boundaries use the configured
`TimeProvider`; sliding retention observes both cache lookup and resource usage. Eviction, clear and
disposal release each owned resource exactly once, preferring asynchronous disposal where both disposal
contracts exist.

Observer callbacks execute outside the state lock, are serialized, and cannot convert a committed add
into a failed cache operation. Clock callbacks, lifetime cancellation and usage subscription also execute
outside the state lock. A concurrent removal during usage subscription is detected and detached, so a
released resource retains no cache callback.

The same owner is integrated into core send endpoints, ActiveMQ producer ownership and Event Hubs
producer ownership. AWS queue and topic caches use the bounded owner for ordinary cached resources and a
separate `DurableResourceStore` for resources whose lifetime must be tied to the connection rather than
TTL. The durable store is single-flight, retryable after a failed factory, disposes late values after a
pending removal, and contains disposal and cancellation-callback faults.

## Review quality and semantic corrections

The reviewer direction is architecturally strong: it found the real duplicate-owner problem and correctly
identified single-flight, capacity, TTL, usage and disposal as one state machine. The donor was not safe to
copy without verification. Native tests and static review found and corrected these additional defects:

1. Cache clear and disposal invoked caller-controlled cancellation callbacks while holding the state lock.
2. Timed cleanup obtained the caller-provided `TimeProvider` timestamp while holding the state lock.
3. Usage subscription could race removal and leave a callback attached to an already released resource.
4. A cache caller could complete before its added-observer callback, violating the committed observation
   boundary.
5. Pending creation needed a cache-owned operation lease so disposal could not finish before the factory
   owner released its result.
6. The donor accepted an invalid zero-capacity boundary in the AWS host test and named a nonexistent Event
   Hubs test path; the native integration uses the existing LocalIntegration project.
7. Final static review found that `DurableResourceStore` also canceled pending factories under its lock and
   propagated exceptions thrown by cancellation callbacks. Four new materialized cases reproduced both
   remove and dispose failures before the product correction.
8. Concurrent durable-store disposal returned a completed second `DisposeAsync` call while the first call
   still owned a pending resource. All dispose callers now share one final completion barrier.

This supports an A-/B+ assessment of the review: high-value architecture and defect discovery, but still
requiring native integration, race analysis and mutation-backed correction.

## Test and build evidence

- Final analyzer-active, non-incremental Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: zero
  warnings and zero errors.
- Final canonical serialized Unit/Architecture profile: 3,109/3,109 passed, zero failed, zero skipped.
- Complete core test assembly: 1,543/1,543 passed.
- Complete ActiveMQ unit assembly: 136/136 passed.
- Complete AWS unit assembly after the final cancellation and disposal hardening: 77/77 passed.
- General LocalIntegration execution started PostgreSQL, Azurite, LocalStack, ActiveMQ, Artemis and the
  Event Hubs emulator. Every directly affected provider assembly passed, including Event Hubs, ActiveMQ
  and Amazon SQS; the run identity was `vicione-3ac508c7e053`.
- The final complete Amazon SQS LocalIntegration assembly passed 48/48 against a fresh LocalStack fixture,
  run `vicione-b4f8bb961c44`.
- Scoped whitespace verification passed all 37 added or modified C# files. All four changed requirements
  JSON files parse, their compiled projections pass, `git diff --check` passes, and active old-cache symbol
  and empty-directory scans are empty.
- Docker teardown was verified after the provider runs; no test container remained.

An intermediate final-candidate Unit/Architecture attempt was 3,107/3,108 because the unchanged
`ObservationBoundaryTests.RaisedBudgetCancellationIsNotQuiescence` timing carrier failed once. It passed
immediately in isolation and in the byte-identical complete 3,108/3,108 rerun. An earlier byte-identical
complete run similarly observed one existing Futures harness snapshot race and then passed 3,104/3,104.
Both are recorded rather than attributed to the cache delta.

The broad LocalIntegration run was 340/341 because the pre-existing EF
`InboxOutboxConcurrencyTests.ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet` persisted
`ReceiveCount == 1` instead of 3. It failed the same way in isolation against a fresh PostgreSQL fixture
(`vicione-36e876abef00`). A clean temporary archive of the unmodified baseline commit was restored, built
and executed against another fresh PostgreSQL fixture; it produced the identical failure
(`vicione-27dbb09326b3`). This proves the EF defect predates and is independent of ResourceCache. The
temporary archive was removed, and the out-of-scope EF source and test were not changed in this package.

## Independent one-cause mutations

Twenty-six buildable one-cause product mutations were killed. Every target was restored before the
final build and positive runs.

| ID | Single changed cause | Causal native owner |
| --- | --- | --- |
| M01 | Accept zero cache capacity | invalid capacity and time-bound options |
| M02 | Ignore an existing pending creation | concurrent single-flight factory count and identity |
| M03 | Retain a faulted pending creation | healthy retry after failed creation |
| M04 | Accept a factory result whose projected key differs | projected-key mismatch rejection |
| M05 | Reverse least-recently-used eviction | exact capacity victim identity |
| M06 | Expire at rather than beyond the TTL boundary | exact expiration boundary |
| M07 | Reverse absolute and sliding reference selection | absolute expiration ignores hits and usage |
| M08 | Remove resource-usage subscription | usage refreshes sliding retention |
| M09 | Prefer synchronous disposal | dual-contract resource uses only async disposal |
| M10 | Complete the caller before added observers | caller waits for added observation |
| M11 | Let caller cancellation cancel shared creation | cancellation isolates one waiter |
| M12 | Invoke cache lifetime cancellation under lock | reentrant cache-state callback completes |
| M13 | Keep usage callback after concurrent removal | released resource has no attached cache callback |
| M14 | Obtain timer timestamp under lock | reentrant `TimeProvider` callback completes |
| M15 | Propagate an observer failure | observer failure is isolated after atomic commit |
| M16 | Bypass the core send-endpoint cache | cold and warm lookups preserve endpoint identity |
| M17 | Bypass the ActiveMQ producer cache | concurrent requests share one producer and stop releases once |
| M18 | Remove ActiveMQ usage notification | producer operations refresh usage and preserve delegation |
| M19 | Retain a faulted AWS durable entry | next factory attempt recovers |
| M20 | Do not dispose a late AWS value after removal | pending removal disposes the late value exactly once |
| M21 | Bypass the Event Hubs producer cache | concurrent requests share one producer |
| M22 | Remove Event Hubs usage notification | producer operations refresh usage and preserve payload/token |
| M23 | Cancel AWS removal under the state lock | remove reentrancy row fails; dispose control stays green |
| M24 | Cancel AWS disposal under the state lock | dispose reentrancy row fails; remove control stays green |
| M25 | Propagate AWS cancellation-callback faults | both remove and dispose fault-isolation rows fail |
| M26 | Return early from a concurrent AWS disposal | second caller completes before pending ownership releases |

The final `DurableResourceStore.cs` SHA-256 after M23-M26 restoration is
`d64410ad7a4ae6c9950ed1aec4e2d25a059dca9f999367fa05e651e6f64d4523`; its test-owner SHA-256 is
`985c2c4687630f323845be16f54654c68f82284b75a2dc123e484ea3cba2c9fb`.

This local package is the sixth of twelve semantic V4 reviewer packages. It raises V4 integration
progress to 6/12 (50%). Remote publication is not included or implied.

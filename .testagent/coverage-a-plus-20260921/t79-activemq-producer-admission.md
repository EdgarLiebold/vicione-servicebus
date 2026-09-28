# T79 ActiveMQ cached producer admission and send ownership

The frozen T74 profile identified a large uncovered direct-forwarding surface
in `CachedMessageProducer`. The adjacent cache tests already prove single
flight, waiter cancellation, independent destinations and retry after a
faulted factory. Manual source review found a concrete failure: the public
wrapper constructor accepted a null destination or producer. When a native
producer factory completed with null, the cache stored a wrapper that failed
only when used and prevented healthy creation for the same key.

Two red-first tests failed before the product fix: constructor admission
threw no exception for missing dependencies; a null-producing factory
returned a cached wrapper instead of rejecting it. The constructor now
rejects `destination` and `producer` with exact argument names. The cache
retains its existing retry-on-fault behavior; a later healthy factory for the
same destination is proven to send and to be disposed exactly once on stop.

The third test checks a different public contract: explicit destination,
message, delivery mode, priority and TTL reach the native NMS producer
unchanged through synchronous and asynchronous sends. A pending async send
emits exactly one usage signal before completion; its failure retains the
original exception and triggers neither retry nor another usage signal.
The Red Team found a separate synchronous-throw path in the native async
API. A fourth test now proves the original synchronous exception propagates
and usage has already been reported when the native producer is invoked.

The affected xUnit v3/MTP class passes 9/9. Assertion-quality
review found exact identity, argument, state, lifecycle and exception oracles;
none of the four new tests is assertion-free or trivial-only. Static
pseudo-mutation review indicates dropping either constructor guard, caching a
null producer, changing send settings or duplicating usage signals would fail
the affected tests. Independent read-only Red Team final re-review is PASS
with no remaining concrete P1/P2 finding. Exact-commit provider verification
is pending. The T74 33-profile report remains the
latest complete Line/Branch/CRAP measurement; the next aggregate follows
the agreed multi-packet interval unless a broad contract change requires it.

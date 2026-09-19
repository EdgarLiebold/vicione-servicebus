# Iteration 223 — active research and source-only checkpoint

The ServiceBus A+ goal remains active. This checkpoint is not an A+ admission:
the lead's personal full-owning-Core-test-project read gate (§4.3) is only
86/702 tracked files at the hash-bound baseline (see
`../core-test-project-full-read/partial-ledger.md`). The historical ordering
deviation remains open; the source inventory's previously admitted total
remains 831/4,118. No new tests were designed, edited, or admitted here.

Three disjoint Sol 5.6 xhigh read-only audits examined saga connector,
ResourceCache and request-client source. Their findings are candidate defects,
not independently mutation-qualified fixes:

- ResourceCache capacity counts indexed entries and pending factories, not
  evicted resources awaiting asynchronous disposal. Repeated `AddAsync` with
  blocked disposal may leave arbitrarily many live cache-owned resources with
  `Capacity=1`. A correct bound needs accounting for retiring ownership and
  capacity-change signaling outside the index and no lock held across await.
- A direct `AddAsync` for a key reserved by a pending `GetOrAddAsync` factory
  can commit the same key, block `RemoveAsync` while the factory remains
  pending, then fault the factory's single-flight callers on duplicate commit.
- Request deadlines/timers are initialized after endpoint acquisition and send
  pipeline execution. An endpoint that never becomes ready can exceed its
  absolute deadline indefinitely; a response received before a hung send task
  completes also suppresses the timeout while response completion awaits send.
  Terminal timeout may not cancel the send if `_sendContext` already completed.
- Reusing a saga specification for a second direct `ConnectSaga` accumulates
  terminal filters in persistent configurators and can duplicate saga
  consumption. Existing tests intentionally assert repeated-build accumulation,
  so a connection-specific snapshot or explicit single-use contract needs
  careful design rather than changing general Build semantics.

One smaller request-client error-precedence defect has a source-only correction:
the constructor starts the send before the caller can register typed response
handlers. If endpoint acquisition faults immediately, `Fail` used to set the
terminal bit and later `ResponseAsync` synthesized cancellation. `Fail` now
stores the exact response exception under the handler lock; a subsequent
registration returns that exception instead of false cancellation. Genuine
cancel/dispose paths still return canceled tasks. This was inspected against
the existing request-client lifecycle tests but has **no new causal regression
or mutation evidence** until the §4.3 gate is dispositioned.

Verification: strict Core Release build, 0 warnings and 0 errors; first full
MTP run 6,216/6,217, one 30-second timeout in
`StateMachineNestedRequestIntegrationTests.NestedRequestCompletion_ResumesTheOriginalRequestWithTheExactResponseAsync`.
The exact test passed alone 1/1 on retry; the following unfiltered full Core
run passed 6,217/6,217, 0 skipped. The one-off failure is retained as an
observed instability, not declared fixed. `git diff --check` passed.

No remote push is made at this checkpoint. The remote-specific private-source
exfiltration confirmation is still absent; this does **not** pause or block
the active local goal and personal read/verification work continues.

Later read-only continuation: the personally complete Core test-project ledger
advanced from 86/702 to 100/702, adding Courier activity/host contracts,
subscription capture, argument integration and InMemory conversation inheritance.
The additional reading does not retroactively qualify the source-only fix or
resolve the historical §4.3 ordering deviation.

Next read-only checkpoint: 110/702 complete, including the shared Courier
fixture, convention/JSON registry, executor and routing-slip request bridge,
success/fault integration and unavailable consume-context overload boundaries.
No source/test files changed since the successful full-suite checkpoint.

Further personal read: 115/702, adding consumed-context outgoing metadata,
deserializer completion/payload ownership, context scope/proxy forwarding and
request/response envelope flow. Only the read ledger/status changed since the
last full-suite verification. The goal stays active after this checkpoint.

Continuation checkpoint: 120/702, adding activity-context adapters, saga
consume-context proxy, outgoing wrapper, response lifetime and endpoint/fault
resolution tests. The effective Core test-project gate remains incomplete;
provider parity and causal mutation evidence remain outstanding. No product or
test file changed since the last full-suite verification.

Continuation checkpoint: 133/702 personally read Core test-project files,
adding the 11-route endpoint matrix, message consume forwarding, consumer
metadata/configuration, batch context/factory/collector lifecycle, instance
registration and disposal contracts. This is a read-ledger checkpoint only;
the effective gate, provider parity and causal mutation evidence remain open.

Continuation checkpoint: 160/702 personally read Core test-project files.
The newest set covers consumer concurrency and dynamic connections, batch
timing/cleanup, initializer converters/providers, job API fixtures and the
embedded requirement-projection assertion. Driver/fixture closure and all
remaining project files are still required before test-design clearance.

Continuation checkpoint: 170/702 personally read, with assembly-wide topology
and correlation bootstrap, timer/key/job fixtures, nested pipe composition,
JSON defaults, topology naming and state-machine ownership/status contracts.
The aggregate test read gate remains incomplete and the goal remains active.

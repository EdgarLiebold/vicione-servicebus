# Message and host retry — product path analysis

## Scope and decision boundary

The complete sources for the three inherited fixtures, all 17 frozen obligations, retry filters,
policies and contexts, message-retry configuration observers, bus lifecycle, host send retry,
receive-transport retry and the relevant retained transport configurations were read before the
replacement was designed. The inherited tests are behavior evidence, not an API-shape contract.
ViciOne.ServiceBus preserves useful capabilities through an A+ API; source and binary compatibility
with the inherited API are not requirements.

## A+ contract

- A handled failure has one retry-budget owner. Inner endpoint/consumer policies do not multiply an
  outer bus policy, while disjoint policies retain independent exception ownership.
- Consumer and bus budgets are exact. A successful retry stops immediately; explicit `None` and an
  absent policy each execute once.
- A concrete message remains retryable when its topology contains an abstract ancestor and an
  interface. Concrete-base and interface consumers receive their own exact retry lifecycles.
- Bus stop cancels a pending message retry before another attempt starts. Create/start/stop failures
  terminate the same retry lifetime, cancellation precedes disposal, and cleanup is idempotent.
- Host send retry has one executor and one configured delay. It accepts an explicit standard
  `TimeProvider`, preserves the exact caller token, gives stopping precedence when both sources
  cancel, retains the last transport failure as the stopping cause and rethrows the exact terminal
  failure when the policy is exhausted.
- Public null collaborators, missing policies, null policy results and missing observers fail at
  their owning boundary with stable diagnostics.

## Product corrections

1. `HostConfigurationRetryExtensions` now delegates to the shared iterative retry executor instead
   of owning a second loop. Its historical unconditional one-second pause was removed because every
   retained host configuration already supplies an explicit retry policy and the extra pause made
   the actual delay hidden and non-deterministic.
2. Host retry gained an explicit `TimeProvider` overload; the existing overload remains only as the
   `TimeProvider.System` convenience boundary.
3. Source cancellation is classified from the two source tokens rather than the linked token carried
   by the delay exception. Stopping is the stronger state when both sources are cancelled.
4. A bus-level `UseMessageRetry` call now binds retry lifetime to the same bus configurator instead
   of silently using `CancellationToken.None`.
5. `RetryBusObserver` now keeps a stable token, cancels on every failed/stop terminal path and
   atomically cancels before releasing its source.
6. Consume retry registers lifecycle cancellation once when the policy context is created instead of
   replacing one registration after every failure.
7. Message-retry entry points, observers and pipe specifications reject invalid collaborators and
   null policy results immediately instead of failing later through reflection or null dereference.

## Intentional boundaries

System.Text.Json cannot materialize an abstract root contract without explicit polymorphism metadata.
The inherited TypeCast fixture did not consume its abstract ancestor; it consumed the concrete
message and merely placed the ancestor and interface in the topology. The replacement preserves that
topology regression and adds real retry through a concrete base and a public interface. It does not
invent an unsupported abstract-root wire contract.

`ReceiveTransport<TContext>` has a separate supervisor/readiness/fault lifecycle and its own
historical reconnect loop. It was analyzed but not changed from host-send evidence. Its duplicated
delay, process clock and swallowed cancellation are recorded as a dedicated path-complete item in
`TODO.md`; this cohort does not claim receive reconnection is normalized.

## Upstream history

The host send retry loop entered upstream for retrying send and publish during broker outages. Later
changes corrected cancellation, separated the send policy from receive policy for Azure Service Bus
and used a distinct stopping token. The fixed one-second breather existed from the original loop,
even though retained host configurations now define their own exponential minimum delay. The A+
implementation keeps the outage and cancellation capability while removing duplicate delay policy.

# Mutation validation — middleware coordination and resilience

Each effective mutation changed one product behavior, rebuilt the owning Release test project and
ran the exact owning test through Microsoft Testing Platform. Every mutation was restored before
the next one. The final unmutated profiles ran only after all mutations had been removed.

| Mutation | Intended rejection | Observed result |
| --- | --- | --- |
| Stop observing the source task after cancellation wins | A source failure arriving after cancellation must not become an unobserved task failure | The marked product failure was published through `TaskScheduler.UnobservedTaskException`; the exact fact failed while its deliberately unobserved control failure proved the runtime observation boundary was active |
| Keep a failed one-time setup as the cached terminal state | A later healthy caller must be able to retry after failure | The strengthened fact observed three executions instead of the required two and failed |
| Permit every competing circuit-breaker transition to install its own open state | One transition owns the successor state and its timer | The concurrency fact observed 16 active timers instead of exactly one and failed |
| Convert a null runtime string partition key to an empty key | Invalid routing input must be rejected, not silently redirected | The exact null-boundary fact received no exception and failed |
| Create the rate timer from the system clock instead of the configured `TimeProvider` | Virtual time must own replenishment deterministically | The exact timer-ownership and replenishment fact failed |
| Publish only the first context from `LatestFilter` | The observable value must always be the most recently entered context | The exact fact observed context 1 instead of context 100 and failed |
| Change the null rescue-context factory contract | A null custom rescue context is an invalid factory result | The exact null-factory fact failed |

The first version of the abandoned-fault test also required a `WeakReference` to die after five
immediate collections. A complete profile exposed that timing assumption. The final test instead
creates a marked, deliberately unobserved control failure in the same isolated helper. It waits
until the runtime publishes that control failure, performs additional complete finalization cycles
and then requires the product failure to remain absent. Removing the production observation call
makes this final form fail for the intended reason. Ten consecutive clean focused repetitions and
the final unfiltered profile were green.

No ineffective experiment is counted as mutation evidence, no mutant remains in the working tree,
and no test was weakened to accept a product defect.

Verdict: **PASS**.

# Mutation validation — middleware routing, limits and scope

Each effective mutation changed one product rule, rebuilt the owning Release test project and ran
the exact owning test through Microsoft Testing Platform. Every mutation was restored before the
next one.

| Mutation | Intended rejection | Observed result |
| --- | --- | --- |
| Store a scope-local payload in the parent context | Scope writes must never leak upward | The exact isolation fact found the local payload on the parent and failed |
| Replace the converted route's original continuation with an empty pipe | Dispatch must continue the input pipeline exactly once | The trace lacked `next:a`; the exact dispatch fact failed |
| Evaluate only the first output filter when multiple types are connected | Dynamic routing is compatible-type fan-out, not first-match selection | The second route count remained zero; the exact fan-out fact failed |
| Route a keyed context to the first connected pipe regardless of key | Both type and key must match | The west context was routed and the exact keyed fact observed two calls instead of one |
| Accept converter success with a null output | Successful conversion must yield a valid context | No boundary exception was raised and the exact invalid-collaborator fact failed |
| Remove the explicit output/input context compatibility check | Invalid route types must fail with the product contract, not a reflection diagnostic | The generic runtime message replaced the required `must implement` boundary and the exact fact failed |
| Initialize the concurrency semaphore with one extra permit | The maximum must equal the configured limit | The exact fact observed four concurrent executions instead of three |
| Activate the circuit at `attempts >= ActiveThreshold` | The default activation boundary is strict | The exact default-threshold fact observed five inner attempts instead of six |

Every mutant compiled before its intended failure. No ineffective experiment is counted and no
mutant remains in the working tree.

Verdict: **PASS**.

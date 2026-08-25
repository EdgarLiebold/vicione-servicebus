# Mutation validation — middleware retry

Each effective mutation changed one production rule, rebuilt the owning Release test project and
ran the exact owning tests through Microsoft Testing Platform. Every mutation was restored before
the next one; an initial analyzer failure was rejected as invalid mutation evidence and corrected
before the mutation sequence restarted.

| Mutation | Intended rejection | Observed result |
| --- | --- | --- |
| Disable nested retry-payload propagation | A downstream retry must remain the sole budget owner | Four composition/dispatch cases observed five attempts instead of one or five replacement contexts instead of one |
| Remove policy-context disposal from `RetryFilter` | A policy context must be disposed exactly once | The disposal fact observed zero disposals instead of one |
| Rethrow the linked-delay cancellation without source-token normalization | Source cancellation must preserve the exact caller token | The exact-token fact observed the linked retry token instead of the source token |
| Restore the historical cancellation-controlled loop exit | Policy cancellation must never report success | The cancellation fact observed successful completion instead of `OperationCanceledException` |
| Make `BaseRetryPolicyContext.Cancel` a no-op | Explicit cancellation before the first failure must prevent retry | The policy-context fact observed an allowed retry |
| Expose a read-only view of the caller's interval array without copying it | A retry schedule must be an immutable snapshot | The interval fact observed the caller's later mutation |
| Change the exponential terminal boundary from `<` to `<=` | The configured retry budget must be exact | The budget fact observed twelve attempts instead of eleven |
| Rethrow the retry context's first failure on exhaustion | Exhaustion must rethrow the exact final failure | The terminal-failure fact observed the initial exception instance |
| Bypass `PreRetry` in the task executor | Pre-retry and terminal callbacks must retain exact order | The callback trace lost `before:1` |

All nine effective mutants compiled before their intended behavioral failure. No ineffective
experiment is counted and no mutant remains in the working tree.

Verdict: **PASS**.

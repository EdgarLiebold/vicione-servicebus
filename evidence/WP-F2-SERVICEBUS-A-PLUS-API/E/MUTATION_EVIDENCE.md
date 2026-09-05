# Work package E verified mutations

This is targeted pseudo-mutation evidence, not a repository-wide mutation score. Every mutation changed
one production condition or assignment, used a narrow executing owner test, and was immediately restored.
The final canonical source was then rebuilt and passed the complete acceptance profile.

| # | One-cause mutation | Initial result | Closure and final result |
|---:|---|---|---|
| 1 | In-memory admission capacity `>` to `>=` | killed | Exact record/byte boundaries and concurrent admission failed. |
| 2 | Remove `DueAt` from in-memory immutable-intent equality | survived | Added changed-`DueAt` to the exhaustive identity-conflict matrix; repetition failed exactly that assertion. |
| 3 | Remove `DueAt` from EF immutable-intent equality | survived | Added changed-`DueAt` to the persisted lifecycle/reused-identity case; repetition failed exactly that assertion. |
| 4 | In-memory due comparison `<= now` to `< now` | killed | The exact-due-time schedule case became unclaimable. |
| 5 | Emit abandoned observations for every non-missing result | killed | The operations case observed two logs/metrics instead of the single applied transition. |
| 6 | Transient retry budget `< MaximumDeliveryAttempts` to `<=` | killed | Retry exhaustion exceeded the declared attempt limit. |
| 7 | EF capacity admission `< MaximumStoredCount` to `<=` | killed | Exact capacity admitted one excess row and the concurrent ledger result changed from five to six. |
| 8 | In-memory leased-schedule cancellation guard `||` to `&&` | survived | Added cancellation after a successful claim and retained-count assertions; repetition returned `Applied` instead of `InvalidState`. |
| 9 | EF leased-schedule cancellation guard `||` to `&&` | survived | Added the same post-claim fence to the restart/persistence test; repetition returned `Applied` instead of `InvalidState`. |
| 10 | RabbitMQ publisher confirmation `AwaitAck = true` to `false` | killed | The source-bound durable-acceptance architecture test failed while the other five class tests passed. |
| 11 | Retention boundary `<= TimeSpan.Zero` to `< TimeSpan.Zero` | killed | The new zero-retention startup case no longer failed and was the sole red case in its 26-case class. |

Final score for the bounded campaign: 11/11 killed after test hardening; four initial survivors, four
closed. No production mutant remains in the working tree.

The narrow executions used separately built Release assemblies and class filters. They are diagnostic
causality evidence only; package acceptance uses the three later unfiltered 3,696-test runs.

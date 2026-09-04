# V5 API usability mutation validation

Date: 2026-09-04

Each mutation changed one production behavior in an isolated copy at
`/private/tmp/vicione-v54-mutation.lbGe24`, compiled the owning project, and caused its designated native owner to fail
for the intended reason. The mutation was restored before the next experiment. A recursive comparison of the isolated
source and test scopes against the main worktree passed after restoration; the main worktree was never mutated.

| ID | One-cause production mutation | Causal killing observation |
|---|---|---|
| M01 | derive the wrong message-contract identity | typed durable send persisted a contract identity different from the application catalog |
| M02 | replace the caller's message id | the durable receipt and persisted envelope no longer retained the supplied id |
| M03 | route to the wrong destination | the stored destination differed from the resolved send endpoint |
| M04 | bypass payload admission | an over-limit typed payload reached the durable store instead of leaving it empty |
| M05 | omit durable envelope metadata | persisted metadata lost required transport-independent send-envelope evidence |
| M06 | invert the receipt's `IsNew` state | first admission was reported as idempotent or replay was reported as new |
| M07 | materialize only the first contract-catalog contribution | a later independently registered message contract could not be resolved |
| M08 | allow two durable composition owners | startup validation accepted an ambiguous provider owner |
| M09 | change the default persistence identity | outbox and durable sender no longer selected the canonical identity |
| M10 | drift the persistence-identity maximum length | exact-boundary and one-over validation diverged between API and EF mapping |
| M11 | reject the exact maximum with `>=` | the valid 128-character identity failed instead of only the 129-character identity |
| M12 | route `UsingInMemory<TBus>` through the untyped overload | the wrong bus received the durable provider registration |
| M13 | accept an unsupported continuation-token version | invalid forward-version input reached query execution |
| M14 | reverse equal-time quarantine id ordering | deterministic pagination returned equal-time records in the wrong order |
| M15 | infer `HasMore` from `count >= pageSize` | an exact-size terminal page incorrectly emitted a continuation token |
| M16 | report `Discarded` from requeue | the typed operation result misrepresented the performed transition |
| M17 | report `NotFound` for a present non-quarantined discard target | missing and invalid-state outcomes became indistinguishable |
| M18 | omit the id tie-breaker from the EF seek predicate | equal-time rows were skipped or repeated across page boundaries |
| M19 | ignore the shared identity in the EF durable store | the store accessed a different model identity from the EF outbox owner |
| M20 | remove `EditorBrowsable(Never)` from `IDurableSendStore<TBus>` | packed/public surface verification exposed provider SPI as ordinary developer API |

Result: **20/20 selected behavioral mutations killed; zero substantive survivor; all production sources restored.**

# V5 durable sender mutation validation

Date: 2026-09-03

Each counted behavioral mutation changed one production mechanism, compiled the complete owning test project, and made
the named native owner fail for the intended reason. Every mutation was removed before the next mutation and before the
final positive build. One additional production-source ordering sabotage was rejected by the architecture owner; it is
reported separately because the real pipeline already made the reordered awaits behaviorally equivalent.

| ID | One-cause production mutation | Causal killing observation |
|---|---|---|
| M01 | omit metadata from logical retained-byte accounting | exact body-plus-metadata ledger expected 20 but observed 13 |
| M02 | reject the exact maximum content-type length | exact-boundary valid intent threw at 256 characters |
| M03 | invert the pre-store catalog lookup | unknown contract reached the store instead of throwing |
| M04 | stop detecting the existing application catalog owner | second catalog registration no longer failed |
| M05 | disable in-memory count-capacity enforcement | 100 concurrent admissions crossed a limit of 10 |
| M06 | disable in-memory byte-capacity enforcement | exact body-plus-metadata over-limit admission succeeded |
| M07 | omit metadata from immutable intent comparison | same id with changed metadata was accepted as idempotent |
| M08 | retain the caller's body buffer by reference | post-admission caller mutation changed claimed bytes |
| M09 | make lease expiry exclusive rather than inclusive | takeover at the exact expiry instant returned no claim |
| M10 | remove in-memory generation fencing | stale completion retired a later re-admission |
| M11 | invert the retry due-time predicate | retry was claimable one tick before its due instant |
| M12 | remove the record while requeueing quarantine | retained count fell to zero before terminal discard/delivery |
| M13 | report a successful awaiting transition after early completion | early-completion race returned the wrong transition result |
| M14 | permit one extra consumer-completion timeout dispatch | three sends occurred under a two-attempt budget |
| M15 | classify explicit unknown/invalid provider results as permanent | unknown evidence became `NonRetryable` instead of `Unclassified` |
| M16 | make the transient retry bound inclusive | exhaustion scheduled another retry and produced no quarantine |
| M17 | persist an invalid completion mode as an unclassified failure | invariant-quarantine evidence had the wrong failure kind |
| M18 | swallow post-dispatch state-persistence failure | the exact persistence exception no longer escaped |
| M19 | bypass configured jitter when jitter is nonzero | ceiling delays lost cross-id decorrelation |
| M20 | claim one item beyond immediate worker concurrency | three dispatches started under a limit of two |
| M21 | retire an unrelated id after transport acceptance | the admitted intent remained capacity-owned |
| M22 | reorder explicit receive-owned completion after callback | source-order architecture owner failed; runtime control stayed green |
| M23 | invoke the process-local callback with a canceled token | released consumer work never retired the retained intent |
| M24 | claim InMemory transport acceptance instead of consumer completion | intent retired while receive-owned work was still blocked |
| M25 | accept SQL Server `DELAYED_DURABILITY=FORCED` | unsafe mode stopped throwing |
| M26 | ignore PostgreSQL `fsync=off` | unsafe WAL policy stopped throwing |
| M27 | classify SQLite `MEMORY` journal as persistent | unsafe journal mode stopped throwing |
| M28 | query EF state before commit-durability validation | raw missing-table provider exception escaped before preflight |
| M29 | reconstruct retained bytes from row count | recovered ledger reported 2 bytes instead of 10 |
| M30 | remove EF byte-capacity predicate | 20 byte admissions crossed the independent byte limit of 5 |
| M31 | remove EF count-capacity predicate | 20 records crossed the independent count limit of 5 |
| M32 | remove lease token from EF owned-update predicate | stale worker mutation succeeded instead of being fenced |
| M33 | remove generation token from EF completion predicate | stale completion retired a later incarnation |
| M34 | omit metadata from EF idempotent-intent comparison | changed persisted intent was accepted under the same id |

The initial M23 candidate looked up a value-type payload through a reference-type-only API and failed compilation. It was
rejected and is not counted. The corrected M23 is the buildable canceled-capability mutation above. M22 is not included in
the 33/33 behavioral score; it is an additional structural safety guard. No substantive high-risk mutation survived.

Two pseudo-mutation findings were resolved before the final count. The original EF concurrency case used count and byte
limits of five with one-byte records, allowing either predicate to mask a defect in the other. The final owner runs
independent count-limited and byte-limited concurrent cohorts. The same provider lifecycle owner now also proves metadata
participates in idempotence and an expired worker cannot mutate through a later lease.

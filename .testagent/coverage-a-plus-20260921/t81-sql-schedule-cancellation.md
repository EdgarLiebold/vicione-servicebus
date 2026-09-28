# T81 SQL scheduled-message cancellation

The frozen T74 complete profile left the SQL scheduler cancellation's host
connection, client pipe, and consume-context paths partly uncovered. The
existing SQL Server and PostgreSQL local integration tests prove successful
database cancellation; this packet tests the decisions and error boundaries
around those operations without a database.

Nine new behavior tests prove exact scheduled-message ID and caller-token
forwarding, active consume-client selection, cancellation before client lookup,
host supervisor and client creation, exact failure propagation under
`Retry.None`, a complete two-attempt recovery under `Retry.Immediate(1)`,
no retry when the scheduled row is already absent, caller cancellation after
supervisor entry but before client creation, and stopping-host rejection.
None of the tests merely asserts that a call completes: they observe which
client received the operation, attempt counts, exact tokens, and forbidden
side effects on rejected paths.

The read-only Red Team initially found three P2 survivors: bypassing host
retry, removing the client-pipe cancellation check, and treating `false` as
an error or retry. Each received a direct counterexample. The final re-review
was PASS with no concrete remaining P1/P2 gap. All nine new requirement
metadata entries match the JSON projection.

The SQL xUnit v3/MTP project passes 242/242 with no failures or skips on the
exact test commit `938c1b2f1`. This is an affected-project validation, not a
new product-wide Line/Branch/CRAP measurement; T74 remains the last complete
33-profile baseline. The next full measurement is reserved for the agreed
multi-packet checkpoint unless a cross-project change requires it earlier.

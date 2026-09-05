# A+ remediation test status

## Iteration 1

Iteration 1 is complete and ready for Git capture. The tests use xUnit 4 on Microsoft Testing Platform v2 and introduce no sleeps, ignored/skipped cases, swallowed exceptions, or assertion-free test bodies.

## Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Entity Framework abandonment timestamp | 1 failed | 1 passed |
| Receiver configuration forwarding | 1 failed | 1 passed |
| In-memory `AutoStart` | 1 failed | 1 passed |
| Endpoint registration inclusion | 1 failed | 1 passed |
| Composite filter shape and semantics | 1 failed | 1 passed |
| Azure message-session query | 3 failed with `NotImplementedException` | 3 passed |
| Azure message-session state-write cancellation | 2 of 3 failed | 3 passed |
| Job lifecycle cancellation | 6 of 7 failed | 7 passed |
| Static `NewId` façade | 1 of 2 failed | 2 passed |

## Test quality

- Persistence is asserted after reopening the store with a fresh Entity Framework context and compares the exact supplied timestamp.
- Configuration tests observe the authoritative downstream owner instead of relying only on setter round trips.
- Azure query tests cover matching and non-matching predicates, identity, count, and pre-cancellation.
- Cancellation tests compare exact token identity at each relevant provider, transport-send, and progress-buffer boundary.
- Reflection assertions constrain the intended public API shape and are paired with behavioral tests.
- `DispatchProxy` is limited to protocol-boundary doubles where a full broker connection would obscure the unit contract.

Nine one-cause mutation groups were executed and restored byte-for-byte: Entity Framework timestamp persistence, receiver forwarding, in-memory auto-start, endpoint inclusion, composite exclusion semantics, Azure query predicate evaluation, Azure state-write cancellation, job notification cancellation, and static `NewId` mutability. Every mutation was killed by its owning test project. The Azure mutation run also established that the owning test project must be rebuilt because rebuilding only a referenced product project can leave a stale copied assembly beside the Microsoft Testing Platform executable.

## Full validation

- Release unit-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,728 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering-solution whitespace verification: passed after correcting one indentation finding in the new Azure test.
- Engineering-solution style verification at warning severity: passed.

The previously measured whole-product baseline remains 70.1% line coverage and 55.6% branch coverage. Coverage will be recollected after the remaining remediation iterations so the final report represents the final code rather than an intermediate snapshot.

## Iteration 2

Iteration 2 resolves the SQL URI materialization and topology-name collision findings.

### Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| PostgreSQL and SQL Server URI materialization | 10 failed, 4 passed | 14 passed |
| Core bounded temporary names | 3 failed | 3 passed |
| Azure subscription naming | 4 failed, 6 passed | 11 passed |

The SQL contract now round-trips relative and absolute `Uri` instances accepted by the write path and rejects language null, `DBNull`, blank text, malformed text, and non-string values explicitly. Topology shortening is owned by one internal implementation using SHA-256 and a 13-character Base32 suffix, providing 65 suffix bits while retaining a readable prefix and each provider's exact maximum length.

Five isolated mutations were killed and restored: PostgreSQL relative-value rejection, SQL Server relative-value rejection, reduction of the shared hash suffix from 13 to six characters, removal of the Azure public parameter guard, and removal of the Core minimum-length guard.

### Full validation

- Release unit/architecture solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Complete Unit/Architecture profile: 3,751 passed, 0 failed, 0 skipped across 21 assemblies.
- Release engineering-solution build with warnings as errors: passed, 0 warnings and 0 errors.
- Engineering whitespace verification: passed.
- Engineering style verification at warning severity: passed.

A repeat full-profile run exposed an existing observation-test race: the handler completion signal could precede publication into the consumed-message observer list. The test now awaits the public observation signal before taking a deliberately non-waiting snapshot. The formerly failing test passed ten isolated repetitions and the final complete profile. The private async iterator in the same file was also renamed from `Empty` to `EmptyAsync`, closing the previously recorded bidirectional async-naming exception.

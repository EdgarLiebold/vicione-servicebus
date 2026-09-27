# T56 — durable payload admission and transaction ownership

Status: focused implementation and adversarial controls pass. Frozen full-product
measurement and publication remain. Baseline T55 is pushed at `45e250bf0`.

## Contract and existing evidence

This is one connected EF persistence packet, with the existing in-memory
admission/recovery integration test retained as parity evidence. The Microsoft
code-testing-agent workflow was applied inline: Research, Plan, Implement;
find-untested-sources ran one bounded Roslyn pairing on 26 byte-identical inputs
(`artifacts/t56-pairing*`); test-gap-analysis and assertion-quality reviewed
behavioral assertions. Static pairing found 18 source files, four test files,
three pairs and 15 unpaired sources. It does not measure runtime coverage.

| Path | Failure and recovery proof |
| --- | --- |
| EF reliable inbox, SQLite | A first business record and outgoing intent are explicitly saved inside the serializable transaction. A second oversized publish raises body admission and the outer retry signal. Fresh database reads show no business or outgoing row, zero capacity if the background service initialized its ledger, and a due retry with one attempt. A later invocation commits only a distinct replacement ID and exact serialized text; consumed attempts become two. |
| Direct EF scoped outbox, SQLite | A later oversized `AddSend` rejects without implicitly deciding ownership of an earlier accepted intent. Explicit `AbortAsync` removes it; deliberate `CommitAsync` retains it. Fresh database reads check exact IDs and capacity before and after another valid batch. |
| EF reliable inbox, PostgreSQL | The same saved-partial-attempt rollback, persisted retry and replacement-content assertions run against a fresh real PostgreSQL database and the public `UseReliableMessaging(...UseEntityFramework<AdmissionDbContext>())` registration. The scoped factory is invoked directly. The delivery host is not started because its concurrent polling races the test's deliberate partial save under serializable isolation. This is provider persistence evidence, not broker-dispatch evidence. |

Requirement tuples are registered in the two EF JSON projections. Both projection
tests pass. The new case counts are one SQLite inbox fact, two scoped theory
instances and one PostgreSQL fact.

## Adversarial review and causal controls

Read-only review found two initial gaps in both inbox tests: ID-only final-state
checks could accept an old payload stored under the replacement ID, and checking
only the inner admission exception could miss the retry-wrapper contract. Both
tests now assert the serialized `message.text` and correlation ID, and the exact
outer retry type name. The internal type is not exposed to these test assemblies.

One isolated counterchange omitted `_dbContext.ChangeTracker.Clear()` in the
EF factory's generic failure path. The SQLite test failed on added business and
capacity entries (`artifacts/t56-mutant-tracker-test.log`), then source was
restored and SHA256 verified.

The first rollback-to-commit counterchange survived because the initial test
had only staged the first effects in the tracker. The test was strengthened to
call `SaveChangesAsync` before the second publish. The repeated counterchange
then failed on the leaked persisted business record
(`artifacts/t56-mutant-commit2-test.log`). This is a meaningful test correction,
not a product change. The product file was restored from MAIN with matching
SHA256 `a498485c40fc71eb0012f027005d5eb446e43a4977949b69d2cc6327e622e7d0`.

## Focused results and open gates

- EF SQLite inbox: one pass, `artifacts/t56-restored-AdmissionRejectedAfterFirstIntent.log`.
- Direct scoped EF outbox: two passes, `artifacts/t56-restored-RejectedLaterAdmission.log`.
- PostgreSQL inbox: one pass, `artifacts/t56-pg-resave2-test.log`, canonical fixture with clean teardown.
- Both requirement projection tests pass. Both affected test projects build with zero errors/warnings. Verify-only whitespace formatting passes.
- Initial PostgreSQL partial-save run with a started delivery host failed on PostgreSQL `40001` concurrent serializable transaction. The final test isolates the scoped persistence contract and passes. No product defect is inferred from that deliberately competing setup.

One frozen 33-profile run, independent receipt/coverage audit, changelog,
CHANGELIST and authorized push are pending. Global A+ and the all-repository
Roslyn API/XML-comment review remain open.

The first full run stopped at profile13 (EF unit):342/343 passed. The new test
assumed a zero-valued capacity row always existed after rollback, but the
delivery service may or may not have initialized it beforehand. The assertion
now accepts either no row or one row with exact zero count/bytes. No product
code changed. Earlier profiles belong to the old test tree and cannot be
combined with receipts from the corrected commit.

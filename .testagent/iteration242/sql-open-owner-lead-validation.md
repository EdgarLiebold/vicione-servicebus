# SQL connection-open ownership — Lead validation, 2026-09-19

This is a bounded correctness packet, not an A+ approval of either provider or the whole fork. The two disjoint pre-fix static packets are [PostgreSQL](postgresql-api-static-packet.md) and [SQL Server](sqlserver-api-static-packet.md). Both identified an owned connection created before `OpenAsync` but released by the caller only after a successful return. Their pre-fix source hashes are historical; the current three-file source admission/disposition pair is [here](sql-open-owner-source-admission.tsv) and [here](sql-open-owner-source-dispositions.tsv). The Lead manually read all three current files in full.

## Correction and requirements

`SqlTransportConnectionOpen.OpenOwnedAsync` is internal to the generic SQL transport and already visible to the two provider assemblies through existing signed friend declarations. Both provider connection factories call it. Successful opening transfers ownership unchanged; a failing or canceled open disposes the connection; a secondary cleanup exception does not mask the original opening exception. No public API member, parameter, or package dependency was added.

| Requirement variant | Direct test |
| --- | --- |
| successful-open-transfers-ownership | `SqlTransportConnectionOpenTests.OpenOwnedAsync_SuccessTransfersTheConnectionWithoutDisposingItAsync` |
| failed-open-releases-connection-and-preserves-failure | `SqlTransportConnectionOpenTests.OpenOwnedAsync_FailedOpenDisposesAndPreservesTheOriginalFailureAsync` |
| canceled-open-releases-connection-and-preserves-token | `SqlTransportConnectionOpenTests.OpenOwnedAsync_CanceledOpenDisposesAndPreservesTheCallerTokenAsync` |
| cleanup-failure-does-not-mask-open-failure | `SqlTransportConnectionOpenTests.OpenOwnedAsync_FailedCleanupDoesNotMaskTheOriginalOpenFailureAsync` |

The four variants are in `SqlTransportRequirements.json` and the executable requirement-projection test passed as part of the unfiltered module run. These tests directly prove helper behavior and the Lead inspected both provider call sites; they do not independently exercise a real failed Npgsql or SqlClient connection open end-to-end.

## Dynamic evidence and immutable-snapshot limits

- Current SQL Transport Release project build: 0 warnings, 0 errors. Its unfiltered test project: **138/138 passed, 0 skipped**.
- A separate temporary worktree at `/private/tmp/vicione-servicebus-open-mutant.fGt8if`, based on frozen source/test commit `527e5efb03eed784ff31bb06b1053eccedb4295c`, replaced the helper's `DisposeAsync` call with a completed task. It built with 0 warnings/errors, and the unfiltered module returned exit 2: **135 passed, 3 failed, 0 skipped**. The failed tests were the failed-open, canceled-open, and cleanup-failure cases, each observing disposal count 0 instead of 1. The original working tree was not mutated.
- The first independent snapshot (`3205266d0ba80feed6f4d51e2c78c90192d82666`, `src/tests` digest `99cd775739d6992cf571033e01c813dda7eb2d39d26cdecb156aafdfdbe7bf18`) built the Unit/Architecture profile with 0 warnings/errors and ran **9,592/9,593**, 0 skips. Its only failure was two new EF Outbox metadata tests absent from the snapshot's embedded requirement projection; the later current EF project, including the projection entries, passed **271/271** at stable EF-scope digest `7f7e7fbcc24c448ba74a267d45b864780f85736d35d5a3ff104655c902ea1b37`.
- The second independent snapshot (`527e5efb03eed784ff31bb06b1053eccedb4295c`, `src/tests` digest `7ad8da15fb9e1fa113d9337d210f4aa730fbdc69ffd605d31460c5828610b01c`) includes the SQL ownership fix and built the Unit/Architecture profile with 0 warnings/errors. Its unfiltered result was **9,601/9,603**, 0 skips; the two failures were EF JSON conversion tests against the snapshot's old comparer. A concurrent, later main-worktree commit `36594e8e6` corrected the comparer without weakening the tests. The current EF Release project then built with 0 warnings/errors and passed **274/274**, 0 skips, at stable EF-scope digest `9c32fe687f9c4de455f4eb410b3ebde7d0d7e6fd43f0b1a0df188b21df81ecee`.

Do not combine the successful SQL and EF module runs into a claim that the entire current Unit/Architecture profile is green: that requires one newer hash-stable whole-profile run. Nor are the two snapshots current whole-fork coverage or a real-provider result. The last frozen 32-assembly unit-derived coverage/CRAP measurement remains the older [coverage handoff](coverage-refresh-lead-handoff.md): 66,870/89,583 lines (74.6459%), branch union bounded at 67.1748–71.2250%, 465/21,281 methods above CRAP 30. Whole-provider coverage, API acceptance and full source A+ remain open.

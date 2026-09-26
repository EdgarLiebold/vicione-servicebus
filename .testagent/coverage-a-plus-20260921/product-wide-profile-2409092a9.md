# Complete product-wide measurement at 2409092a9

Measured commit: `2409092a95fa34501ff4ebe88f91783376117ebb`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests tree: `6547a97e48c15dc9125266357b4c900577f5b6d6`.
This report is a documentation successor, not another measured commit.

## Validated results

All 33 profiles were freshly measured at one commit: 20 unit/CPU and 13 local
integration profiles. All four canonical fixture groups exited zero, including
cleanup. Core passed 6,593/6,593, including the 78 new lifecycle/provider cases.
SQL Server passed 75/75 in 5m35s; no live process was restarted. The strict
aggregate is complete. Independent read-only accounting review confirmed 487
file hashes, commit/profile/CPU/provider/assembly completeness, the inventories,
all counts and the delta without a concrete accounting blocker.

| Measure | Result |
| --- | --- |
| Passing test executions, including CPU repetitions | 12,693 |
| Product assemblies | 32 |
| Lines | 85,271 / 93,762 = 90.9440925% |
| Conservative branches | 30,749 / 36,841 = 83.4640754% |
| Method identities, including compiler-generated identities | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Line-gap identities | 4,465 |
| Zero / partial line coverage | 2,783 / 1,682 |
| Additional branch-only candidates | 1,506 |
| Union of line-gap and branch-only candidates | 5,971 |

No failed or skipped run contributes. Conservative branch counts use maximum
covered/valid counts per line and identity, not a union of individual branch IDs.
Method identities are not independent product contracts. A+ remains unproven.

## Target and comparison with T32

All 30 ScheduleSend/Publish async bodies in InMemoryOutboxMessageSchedulerContext
now have full line coverage. Across all 52 identities in that source file,
method-line observations rise from 167/294 to 289/294 and conservative branches
from 28/34 to 30/34. These method-line sums can repeat a physical source line.
Line-gap identities in the file decrease from 32 to five.

[Focused behavioral evidence](t33-outbox-scheduling.md) covers exact message
contracts, initializer/pipe metadata, destination/due-time/token preservation,
real checkpoint rollback and commit, deferred cancellation and provider failure
recovery. Three targeted mutations are detected; restored controls pass 78/78.
The recording boundary does not prove transport serialization or broker storage.

Remaining target line gaps are the invalid-checkpoint guard (line 62), clock
accessor (46), two generated Checkpoint setters (48), and the generic publish
cancellation callback (533). Remaining branch observations include the checkpoint
path (8/10), AddCancelMessageAsync (5/6) and lazy factory callback (1/2).
No exclusions or A+ acceptance are inferred from this list.

Product-wide, T33 adds 149 covered lines and two conservative branches versus
791e29af4. Thirty-seven line-gap identities close and one newly appears; line
gaps decrease by 36. Branch-only candidates increase by one, so the union falls
from 6,006 to 5,971. Product source is unchanged. Do not attribute every observed
change outside the focused context to the new tests.

SagaInstance.MarkInUseAsync again lacks lines 109,110,112 (13/16 lines,3/4
branches). EF Saga RollbackAsync still lacks lines 269,272 (3/5 lines). The
Future AddSubscription callback is now 2/2 without new focused assertions.
These observation changes do not establish either regression or behavioral
closure; their causes remain unestablished.

The next prepared risk is preservation of the original EF Saga operation error
when best-effort rollback also fails. Its read-only reviewed plan uses public
load/query operations, real SQLite, supported EF interceptors, exact exception
identity, uncancelable rollback, context release and healthy continuation.
Broader coverage/CRAP acceptance remains open. Roslyn API/comment auditing
follows actual A+ coverage completion.

## Local evidence fingerprints

Raw artifacts are ignored local files; hashes do not publish them.

| artifacts/ file | SHA-256 |
| --- | --- |
| t33-aggregate.json | 0000b5f9f998fad27f0c1225be59796fd01047e7bd791957a7017c923cc8ca0a |
| t33-all-methods.json | 1059a60638706deef05342fe900e49640bfb2d9ecc008c8d8854d1c71b149436 |
| t33-method-gaps.json | 3c070c9325f4218651bc713eafa72927f131097059ee275bf5e483ac6d59ff67 |
| t33-branch-only-gaps.json | edbdd7450e3f1e32402d917141e760951bcda0a6d96e947bd8bd9a9c3b72937e |
| t33-profile-progress.json | f056715f8c6c7a32350dc53d2afebf068955cd2dc74f1a6ecfd670d85af99e17 |
| t33-gap-delta.json | 1f4361b30706ee0e815a2b4dfb1dbd1d9c2a892e5fe7d8b201b99911a07dc348 |

Orchestration: artifacts/t33-measure-all.py. Analysis:
artifacts/t33-analyze-complete.py, then artifacts/t33-compare-gaps.py.
All measurement and analysis processes are terminal.

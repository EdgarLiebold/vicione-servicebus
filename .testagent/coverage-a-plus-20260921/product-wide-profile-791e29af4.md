# Complete product-wide measurement at 791e29af4

Measured commit: `791e29af42f85e4b08462f9d08b84f128d6403b6`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests tree: `040997f1d69879329399cdc24a63b382ad085e3c`.
This report is a documentation successor, not another measured commit.

## Validated results

All 33 profiles were freshly measured at the same commit: 20 unit/CPU and 13
local integration profiles. All four canonical fixture groups exited zero,
including cleanup. SQL Server passed 75/75 in 5m50s; its live process was not
restarted. EF passed 314/314 and SQS 302/302. The strict aggregate is complete.
Independent read-only accounting review confirmed 487 file hashes, exact commit,
profile/CPU/provider/assembly completeness, all counts and the gap delta, with
no concrete accounting blocker. This is not product-wide A+ acceptance.

| Measure | Result |
| --- | --- |
| Passing test executions, including CPU repetitions | 12,615 |
| Product assemblies | 32 |
| Lines | 85,122 / 93,762 = 90.7851795% |
| Conservative branches | 30,747 / 36,841 = 83.4586466% |
| Method identities, including compiler-generated identities | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Line-gap identities | 4,501 |
| Zero / partial line coverage | 2,795 / 1,706 |
| Additional branch-only candidates | 1,505 |
| Union of line-gap and branch-only candidates | 6,006 |

No failed or skipped run contributes. Branch counts take maximum covered/valid
counts per line and identity, not a union of individual branch IDs. Method
identities are not independent product contracts. These results do not prove A+.

## Targets and comparison with T31

- SQS Batcher.ApplyResponse: 20/20 lines, 18/18 conservative branches,
  complexity 18 and CRAP 18. Both optional-collection branches now execute.
- EF AwaitConsumerCompletionAsync body: 14/14 lines, 4/4 conservative branches,
  complexity 4 and CRAP 4. Prior coverage was 14/14 lines and 3/4 branches.
- [Focused evidence](t32-optional-results-and-leases.md) proves caller outcomes,
  exact persisted state/capacity and lease ownership with four detected mutations,
  followed by restored passing controls. These strengthen behavioral assertions
  beyond merely executing the target methods.

Compared with 6a0aca709, covered lines increase by six and conservative branches
by eleven. Five line-gap identities close and two newly appear. The line-gap
count decreases by three; branch-only candidates decrease by five; the union
decreases from 6,014 to 6,006. Product source is unchanged. Do not attribute all
observation differences to the new focused tests.

The newly observed gaps are EF Saga transaction RollbackAsync (3/5 lines,
missing 269 and 272) and FutureExtensions.AddSubscription callback (0/2 lines,
missing 38 and 39). Both had gaps in earlier measurements; their causes remain
unestablished. ClientRequestHandle.SendAsync still lacks line 155 (18/19).
Agent.SetCompleted and SagaInstance.MarkInUseAsync now have full line coverage
without new focused assertions for those paths. Three DurableSendRecord getters
also became covered. None of these observations alone proves behavioral closure.

Broader work remains: risk inventory, meaningful tests across product areas and
unresolved coverage/CRAP acceptance. The next prepared candidate is outbox
scheduling lifecycle and transformation, with real commit/checkpoint rollback
oracles reviewed read-only. Roslyn API/comment auditing follows actual A+
coverage completion.

## Local evidence fingerprints

Raw artifacts remain ignored local files; hashes do not publish them.

| artifacts/ file | SHA-256 |
| --- | --- |
| t32-aggregate.json | 64dd017ce75a0a151ad156e43614bb9c48f021e3a2a0cc049b3940f3c1565703 |
| t32-all-methods.json | 067b1703da74fbefaf80c60897a35208e45793ee4f73886f5516be136fc31a05 |
| t32-method-gaps.json | 2bbabfb5eadec27e04043da3f8b7af17e39593aa85c8567d2f482cd73b0a6c1b |
| t32-branch-only-gaps.json | ac3661d386efb3057d04749930fa251d3ad9f6c9a8e7c0ecc8cad93afd5a3d3e |
| t32-profile-progress.json | fbe2aa15f1278ee888a611686514b07c0b7257ae9c24cf85e36761c9af569c20 |
| t32-gap-delta.json | 2ef98a3cdf53684644dcede70e287f1b11d817fa0a188309347e8ef4568aec0a |

Orchestration: artifacts/t32-measure-all.py. Analysis:
artifacts/t32-analyze-complete.py, then artifacts/t32-compare-gaps.py.
All measurement and analysis processes are terminal.

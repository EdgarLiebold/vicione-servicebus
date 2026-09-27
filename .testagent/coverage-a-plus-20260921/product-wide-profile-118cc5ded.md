# Product-wide profile: 118cc5ded

Measured commit: `118cc5dede6f0de75d28e826c079453cc37ab41d`.
Source tree: `547d2ffa55d09c19670952bbc3f7f920de389c07`.
Test tree: `451582bc81511b6d9c4720eee0f81213d01651a5`.
Microsoft coverage-analysis workflow; canonical exact-commit receipts and full
provider measurement. All 33 profiles verified, 32 expected assemblies, 12,908
passing executions. All four fixture groups exit 0, including cleanup; SQL Server
passes 75/75 without skips. No retry replaced a failed T41 profile.

| Metric | T40 | T41 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,407/93,762 | 85,401/93,751 |
| Line coverage | 91.08914% | 91.09343% |
| Covered/valid conservative branches | 30,814/36,841 | 30,808/36,823 |
| Conservative branch coverage | 83.64051% | 83.66510% |
| Method identities | 26,061 | 26,060 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,446 | 4,445 |
| Zero / partial line coverage | 2,771 / 1,675 | 2,769 / 1,676 |
| Additional branch-only gaps | 1,511 | 1,510 |
| Union of gap candidates | 5,957 | 5,955 |

## Target evidence and limits

See [T41 behavior tests and counterprobes](t41-cron-boundaries.md).

| Method | Lines | Conservative branches | Complexity / CRAP |
| --- | ---: | ---: | ---: |
| StoreExpressionValues | 13/13 | 17/20 | 20 / 20 |
| ProgressNextFireTimeDay | 15/15 | 17/18 | 18 / 18 |

Their prior CRAP values were 29.12963 and 28.66103. Simplification removes
unreachable guards and preserves existing behavior; baseline tests already passed.
Four target branches remain unobserved. This packet does not establish every cron
expression or timezone/DST contract. Zero CRAP>30 does not establish global A+.

## Reconciled comparison

The old private StoreExpressionValues signature maps explicitly to the new one.
Only that old signature and SkipWhiteSpace disappear; only the new signature is
added. Three comparable line-gap identities close and three open. The removed
uncovered helper accounts separately for the net line-gap decrease of one.
The other closure is generic OrCanceledAsync. Newly observed gaps are
ClientRequestHandle.SendAsync (155), Agent.SetCompleted (240/242) and RearmTimer
(225); no causal attribution to the Cron changes is made.

Physical unchanged-text mapping finds 7 newly observed and 8 no-longer-observed
lines. There are 22 unmatched old lines (16 covered) and 11 unmatched new lines
(all covered): covered total changes by -6 and denominator by -11. Cron contributes
-2 covered physical lines through structural changes; other files contribute -4.
Cron line 155 and old 1300/1301→new 1280/1281 are newly observed in the text mapping.
The latter pair changed reachability: equal text does not prove a previously
missing test. Modified/deleted lines are not counted as coverage gains.

Outside Cron, gains are TaskExtensions 52/53, PublishEndpoint 235 and
ClientRequestHandle.Responses 33. Losses are Agent 240/242, ResourceCache.Creation
193, ClientRequestHandle.Responses 24/26/28, ClientRequestHandle 155 and
InMemoryDelayProvider 225. These observations remain in the worklist.

Conservative branches in Cron change from 521/585 to 514/567; other files have
net +1 covered branch with unchanged denominator. Non-target changes: +2 in
PublishEndpoint; +1 each in TaskExtensions, CompensateActivityScopeProvider,
ExecuteActivityScopeProvider and ClientRequestHandle.Responses; -1 each in
EFTransactionalOutboxSource, Agent, ResourceCache.Creation, ClientRequestHandle
and InMemoryDelayProvider. These are observations, not attributed test gains.

## Evidence integrity

Independent read-only review confirms 487 file hashes, 66 runner/settings bindings,
all profile/assembly identities, commit/tree bindings, and physical line OR counts
from all 66 old/new XML reports. Independent conservative branch totals and
method signature/gap reconciliation also pass. Final audit has no accounting blocker.
Conservative branch counts retain the existing limitation: observations are not
a branch-ID union. Method inventories include compiler-generated identities.

Local artifacts are ignored; SHA-256 evidence:

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t41-aggregate.json | 85f7829408defecdd16fa412ae17946b08eb87db4c49a5c609c8c6a29963bba3 |
| t41-all-methods.json | 4669137e8d81f86b353c69e559329d95d49947fe6363bbbf1172c03bdc008858 |
| t41-method-gaps.json | c92fc281b290426d8b686f721a2ebc0bd08132e727d0ce7eaff4b5117da5354b |
| t41-branch-only-gaps.json | e60574431d83abb4091aa3d2a47f1c57edf346a134753435da7c7e661c1fdc03 |
| t41-profile-progress.json | b4cd693a55634b42a3b7342ceeac732fee1cbbe8a2ab0838e89ade16fc691470 |
| t41-gap-delta.json | cb3bb1c5f535345a70ab86522ddc28d4a37275a0d003737e3b80fd72180075e9 |

Global A+ remains open. Roslyn API/comment audit follows completion of the coverage
and CRAP objective, as requested.

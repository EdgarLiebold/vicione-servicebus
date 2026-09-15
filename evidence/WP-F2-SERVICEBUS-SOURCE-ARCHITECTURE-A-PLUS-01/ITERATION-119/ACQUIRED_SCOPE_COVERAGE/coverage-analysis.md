# Coverage Analysis - Retry, Redelivery and Consume-Policy Kernel

| Metric | Value |
|--------|-------|
| **Date** | 2026-09-15 |
| **Line Coverage** | 96.4365% |
| **Branch Coverage** | 82.4786% |
| **Risk Hotspots** | 0 (CRAP >30) |
| **Tests** | 3,820 passed · 0 failed · 0 skipped |

## Summary

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| **Line Coverage** | 96.4365% | 80% | ✅ |
| **Branch Coverage** | 82.4786% | 70% | ✅ |
| **Methods Analyzed** | 100 | — | — |
| **Risk Hotspots** | 0 | 0 | ✅ |
| **Test Result** | Passed | — | ✅ |

> Coverage collected from **1 of 1 selected Core test project(s)**.
> Outputs saved to this owned directory; raw Cobertura remains at the actual path below.
> Protected TestResults/review are neither read nor changed; optional reports are not generated.

This is seven-file kernel risk analysis, not entire-product/provider coverage or
acceptance. Loaded graph49,343/60,934 lines (80.9778%),16,854/23,082 branches (73.0179%).
Selected kernel433/449 distinct file/line points and193/234 instrumented class/line
branch outcomes. Diagnostic80/70/30 thresholds do not replace the A+ goal or establish
100% correctness. Full Core3,820 pass, zero skip, current explicit profile includes
debugger-attributed source and auto-properties and excludes test source.

PowerShell is absent. The author fully reads the bundled CRAP/coverage algorithms
and applies them with read-only Ruby/REXML PullParser. Per-method lines are distinct;
file lines union hits by path/line, branches retain class/line identity in the one XML.
All18,130 emitted methods have complexity attributes;100 selected kernel methods
receive the reported CRAP analysis. No multi-host union or whole-product inference.

Compared with the previous same-seven-file packet, actual structure reduces Attempt
complexity32→22 and CRAP32.8438→22.3636. Line coverage426/446→433/449; branch outcomes
201/242→193/234. Branch percentage is lower after a changed instrumented denominator;
do not represent that as test improvement or infer correctness from percentages.

---

## 🔥 Risk Hotspots (Top 10 by CRAP Score)

| Rank | Method | Class | File | Complexity | Coverage | CRAP Score |
|------|--------|-------|------|-----------|---------|-----------|
| 1 | AttemptAsync / MoveNext | RetryFilter | RetryFilter.cs:88 | 22 | 90.9091% | **22.3636** |
| 2 | SendPolicyAsync / MoveNext | RedeliveryRetryExecution | RedeliveryRetryExecution.cs:34 | 18 | 100% | 18.0000 |
| 3 | Propagate | RetryOperationState | RetryOperationState.cs:114 | 16 | 93.3333% | 16.0759 |
| 4 | SendPolicyAsync / MoveNext | RetryFilter | RetryFilter.cs:52 | 14 | 100% | 14.0000 |
| 5 | ExecuteCancelableAsync / MoveNext | RetryPolicyExecution | RetryPolicyExecution.cs:130 | 12 | 100% | 12.0000 |
| 6 | TryGetTerminal | RetryOperationState | RetryOperationState.cs:142 | 12 | 100% | 12.0000 |
| 7 | CreatePolicyContext | ConsumeContextRetryPolicy<TFilter,TContext> | ConsumeContextRetryPolicy.cs:109 | 12 | 100% | 12.0000 |
| 8 | CreatePolicyContext | ConsumeContextRetryPolicy | ConsumeContextRetryPolicy.cs:34 | 10 | 100% | 10.0000 |
| 9 | ShouldPropagate | RetryPolicyExecution | RetryPolicyExecution.cs:98 | 8 | 83.3333% | 8.2963 |
| 10 | IsOwned | RetryOperationState | RetryOperationState.cs:54 | 8 | 85.7143% | 8.1866 |

> **CRAP Score** = `Complexity² × (1 − Coverage)³ + Complexity`.
> Scores above30 are flagged. A score≤5 is considered safe. Heuristic, not proof.

Spot check Attempt:22²×(1−20/22)³+22=22.363636. The genuine shared terminal
responsibility removes duplicate/manual status branches. No risk threshold changes,
source exclusions, dummy line-touch tests or artificially reduced complexity input.

---

## 📋 Coverage Gaps by File

| File | Line Coverage | Branch Coverage | Uncovered Lines | Priority |
|------|--------------|----------------|----------------|---------|
| ActivityRedeliveryRetryFilter.cs | 68.7500% | 66.6667% | 5 | 🟢 LOW |
| RedeliveryRetryFilter.cs | 100% | 66.6667% | 0 | 🟢 LOW |

Other totals: RetryOperationState131/133 lines,58/74 branches; RetryPolicyExecution85/88,
28/32; RetryFilter82/87,49/62; RedeliveryRetryExecution49/50,23/26;
ConsumeContextRetryPolicy59/59,27/28. Uncovered lines2+3+5+1+0+5+0=16=449−433.
File averages do not erase method-level gaps. The complete13 below-threshold member
set follows; overlapping method lines are not summed to manufacture a file denominator.

| Member | Source line | Lines covered/total | Branches covered/total |
| --- | ---: | ---: | ---: |
| ActivityRedeliveryRetryFilter.IProbeSite.Probe | 29 | 0/5 | 0/0 |
| RetryFilter.SendPolicyAsync token getter b__1 | 62 | 0/1 | 0/0 |
| RetryFilter.AttemptAsync fault delegate b__6 | 105 | 0/1 | 0/0 |
| RedeliveryRetryExecution.SendPolicyAsync token getter b__1 | 44 | 0/1 | 0/0 |
| RetryOperationState.IsOwned | 54 | 6/7 | 4/8 |
| ActivityRedeliveryRetryFilter..ctor | 21 | 4/4 | 2/4 |
| RedeliveryRetryFilter..ctor | 23 | 4/4 | 2/4 |
| RetryFilter.PropagateNestedRetryFailureAsync fault delegate b__1 | 157 | 2/2 | 1/2 |
| RetryFilter.NotifyTerminalAsync fault delegate b__1 | 173 | 2/2 | 1/2 |
| RetryOperationState.Enter | 33 | 4/4 | 2/4 |
| RetryOperationState.GetCurrent | 198 | 3/3 | 2/4 |
| RetryOperationState.GetRequiredState | 193 | 2/2 | 2/4 |
| RetryOperationState.ExecuteAsync / MoveNext | 168 | 8/8 | 1/2 |

---

## 💡 Recommendations

1. **Exercise real null-fault-task and ownership boundary contracts** — source lines105,
   157,173 and IsOwned54 deserve exact infrastructure identity/no replay/once-only
   release assertions. Do not merely force internal invalid state to hit guard branches.
2. **Retain real attempted-business and nested cancellation feature oracles** — Attempt
   has20/22 measured lines and16/22 branches. At100% line coverage its CRAP would be22,
   not0; responsibility and behavior correctness remain separate from raw percentages.
3. **Complete thin redelivery validation/probe paths and actual providers afterward** —
   Activity five unexecuted probe lines are low-complexity, not the whole coverage gap.
   Provider publishing/topology cancellation requires provider evidence, not Core stubs.

---

## 📁 Reports

| Report | Path |
|--------|------|
| Markdown summary (this file) | evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/ACQUIRED_SCOPE_COVERAGE/coverage-analysis.md |
| Raw Cobertura XML | /private/tmp/vsb-iteration119-acquired-scope-profile/acquired-scope-profile.cobertura.xml |
| HTML (browsable) | Not generated (optional — request HTML reports to enable) |
| Text summary | Not generated (optional — request HTML reports to enable) |
| GitHub markdown | Not generated (optional — request HTML reports to enable) |
| CSV data | Not generated (optional — request HTML reports to enable) |

Raw XML SHA-256 `e069bad445c2997c2b65676ba697bd74956c3d6c9fdeb5f781216ec5ff466b2c`.
Raw Core CTRF SHA-256 `9ed01233c619a3bdc1cda44bf712bc41d62249c3d236a888c23d6b10f3fb8a7a`.
Profile tools/ci/coverage.settings.xml SHA-256
`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.
No optional installation, protected-directory cleanup, dependencies or ignore-file edits.

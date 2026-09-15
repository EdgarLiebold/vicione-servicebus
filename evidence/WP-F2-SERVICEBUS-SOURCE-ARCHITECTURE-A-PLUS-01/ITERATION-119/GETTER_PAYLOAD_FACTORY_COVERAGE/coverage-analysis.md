# Coverage Analysis - Retry, Redelivery and Consume-Policy Kernel

| Metric | Value |
|--------|-------|
| **Date** | 2026-09-15 |
| **Line Coverage** | 95.5157% |
| **Branch Coverage** | 83.0579% |
| **Risk Hotspots** | 1 (CRAP >30) |
| **Tests** |3,805 passed ·0 failed ·0 skipped |

## Summary

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| **Line Coverage** |95.5157% |80% |✅ |
| **Branch Coverage** |83.0579% |70% |✅ |
| **Methods Analyzed** |99 |— |— |
| **Risk Hotspots** |1 |0 |⚠️ |
| **Test Result** |Passed |— |✅ |

> Coverage collected from **1 of1 selected Core test project(s)**. The summary is
> saved in this owned directory; raw XML remains at the actual temporary path below.
> Protected TestResults/review are neither read nor modified. No optional tools installed.

This is a seven-file changed-kernel analysis, not whole-product/provider coverage or
acceptance. Loaded Core graph:49,332/60,934 lines (80.9597%),16,858/23,090 branches
(73.0100%). Selected kernel:426/446 distinct file/line points,201/242 instrumented
class/line branch outcomes. Source includes debugger-attributed code and auto-properties,
excludes test source, and uses the explicit current profile. The previous report selects
six files at de97; percentages are not presented as directly comparable denominators.

The fully read bundled complexity/coverage/CRAP algorithms are applied with read-only
Ruby/REXML PullParser because PowerShell is absent. Per-method lines are distinct;
file lines union hits by path/line, while branch counters retain class/line identity
within the one XML. All18,129 emitted method entries have a complexity attribute;
only99 selected kernel entries receive the reported risk analysis. No multi-host
union, provider acceptance or overall100% API-test assurance is inferred.
Diagnostic defaults80/70/30 do not replace the original A+ acceptance goal.

---

## 🔥 Risk Hotspots (Top10 by CRAP Score)

| Rank | Method | Class | File | Complexity | Coverage | CRAP Score |
|------|--------|-------|------|-----------|---------|-----------|
|1 |AttemptAsync /MoveNext |RetryFilter |RetryFilter.cs:109 |32 |90.6250% |**32.8438** |
|2 |SendPolicyAsync /MoveNext |RetryFilter |RetryFilter.cs:52 |24 |96.2963% |24.0293 |
|3 |SendPolicyAsync /MoveNext |RedeliveryRetryExecution |RedeliveryRetryExecution.cs:34 |18 |100.0000% |18.0000 |
|4 |Propagate |RetryOperationState |RetryOperationState.cs:112 |14 |92.8571% |14.0714 |
|5 |CreatePolicyContext |ConsumeContextRetryPolicy<TFilter,TContext> |ConsumeContextRetryPolicy.cs:109 |12 |100.0000% |12.0000 |
|6 |ExecuteCancelableAsync /MoveNext |RetryPolicyExecution |RetryPolicyExecution.cs:130 |12 |100.0000% |12.0000 |
|7 |PropagateNestedRetryFailureAsync /MoveNext |RetryFilter |RetryFilter.cs:190 |10 |86.6667% |10.2370 |
|8 |TryGetTerminal |RetryOperationState |RetryOperationState.cs:139 |10 |100.0000% |10.0000 |
|9 |CreatePolicyContext |ConsumeContextRetryPolicy |ConsumeContextRetryPolicy.cs:34 |10 |100.0000% |10.0000 |
|10 |ShouldPropagate |RetryPolicyExecution |RetryPolicyExecution.cs:98 |8 |83.3333% |8.2963 |

> **CRAP** =`Complexity² ×(1−Coverage)³ +Complexity`; scores>30 are flagged,
> scores≤5 are considered safe. This is a risk heuristic, not correctness proof.

Async operation names explicitly map to emitted MoveNext entries. AttemptAsync has
29/32 measured lines and25/32 branches. Even100% line coverage at fixed complexity32
would leave CRAP32>30. The next architecture packet must simplify real control-flow
responsibilities rather than add dummy line-touch tests or change metric exclusions.
In particular, duplicate generic terminal notification paths and manual completed-task
status branches should be assessed together with safe acquired ownership references.

---

## 📋 Coverage Gaps by File

| File | Line Coverage | Branch Coverage | Uncovered Lines | Priority |
|------|--------------|----------------|----------------|---------|
|ActivityRedeliveryRetryFilter.cs |68.7500% |66.6667% |5 |🟢 LOW |
|RedeliveryRetryFilter.cs |100.0000% |66.6667% |0 |🟢 LOW |

All remaining file totals: ConsumeContextRetryPolicy59/59 lines,27/28 branches;
RedeliveryRetryExecution49/50,23/26; RetryOperationState106/109,46/58;
RetryPolicyExecution85/88,28/32; RetryFilter100/108,69/86. Uncovered lines sum20,
reconciling446−426. File averages do not hide the AttemptAsync risk.

Every16 below-threshold emitted member follows. Source lines may overlap other
members, so these method counters are not summed to invent a file denominator.

| Member | Source line | Lines covered/total | Branches covered/total |
|---|---:|---:|---:|
|RetryOperationState.Enter |33 |4/4 |2/4 |
|RetryOperationState.IsOwned |53 |5/6 |3/6 |
|RetryOperationState.GetCurrent |191 |3/3 |2/4 |
|ActivityRedeliveryRetryFilter..ctor |21 |4/4 |2/4 |
|ActivityRedeliveryRetryFilter.IProbeSite.Probe |29 |0/5 |0/0 |
|RedeliveryRetryFilter..ctor |23 |4/4 |2/4 |
|RedeliveryRetryExecution.SendPolicyAsync closure b__1 |44 |0/1 |0/0 |
|RetryOperationState.Lease.Dispose |226 |5/6 |1/2 |
|RetryOperationState.Mark factory b__8_0 |65 |0/1 |0/0 |
|RetryOperationState.PublishTerminal factory b__9_0 |78 |0/1 |0/0 |
|RetryOperationState.ExecuteAsync /MoveNext |164 |8/8 |1/2 |
|RetryFilter.SendPolicyAsync closure b__1 |62 |0/1 |0/0 |
|RetryFilter.SendPolicyAsync closure b__4 |76 |2/2 |1/2 |
|RetryFilter.AttemptAsync closure b__5 |126 |0/1 |0/0 |
|RetryFilter.AttemptAsync closure b__6 |136 |2/2 |1/2 |
|RetryFilter.PropagateNestedRetryFailureAsync closure b__2 |201 |2/2 |1/2 |

Factory fallbacks/guards must be judged against valid contracts and the known callback
architecture findings. No deliberately invalid internal dictionary, dummy production
feature or test-only public runtime API is added to force unreachable line coverage.

---

## 💡 Recommendations

1. Address the three source-established admission/Mark/Propagate payload callback
   failure classes with acquired state/caller references, causal regressions and
   independent mutations. Preserve exact contexts and sibling/parent isolation.
2. Separate/simplify AttemptAsync's genuine terminal and retry responsibilities.
   Retain every business/observer/cleanup/cancellation oracle; remeasure after repair.
3. Close actual delayed/RabbitMq provider publish/topology cancellation and the
   BasePipeContext cache-constructor policy inconsistency. Finish current-source
   all-host/API/package/journey gates and real whole-product coverage separately.

---

## 📁 Reports

| Report | Path |
|--------|------|
| Markdown summary |This owned coverage-analysis.md |
| Raw Cobertura |/private/tmp/vsb-iteration119-getter-payload-factory-complete-profile/getter-payload-factory-complete-profile.cobertura.xml |
| HTML |Not generated (optional) |
| Text summary |Not generated (optional) |
| GitHub markdown |Not generated (optional) |
| CSV |Not generated (optional) |

Raw XML SHA-256:`d40e429708a70e460a64d84c7c487d34c9bf69abe1ab684ff928f91d023fa3fa`.
Raw CTRF SHA-256:`efe7ef7c929350d4ccb719a7ee7ec72408af5ce900e9a2691bcb5c34f8736612`.
Explicit profile SHA-256:`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.
User-facing measured scopes, hotspot and real next architecture work are delivered
immediately after analysis, before optional work. No source/test/comment generator.

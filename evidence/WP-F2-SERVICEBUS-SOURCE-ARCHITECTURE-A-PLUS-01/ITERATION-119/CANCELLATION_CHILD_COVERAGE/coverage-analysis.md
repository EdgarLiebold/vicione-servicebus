# Coverage Analysis - Retry and Redelivery Kernel

| Metric | Value |
|--------|-------|
| **Date** | 2026-09-15 |
| **Line Coverage** | 93.3162% |
| **Branch Coverage** | 81.1224% |
| **Risk Hotspots** | 1 (CRAP > 30) |
| **Tests** | 3794 passed · 0 failed |

## Summary

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| **Line Coverage** | 93.3162% | 80% | ✅ |
| **Branch Coverage** | 81.1224% | 70% | ✅ |
| **Methods Analyzed** | 82 | — | — |
| **Risk Hotspots** | 1 | 0 | ⚠️ |
| **Test Result** | Passed | — | ✅ |

> Coverage collected from **1 of 1 selected Core test project(s)**.
> Outputs saved to this owned evidence directory (markdown summary); raw Cobertura
> XML remains at the actual private temporary path below. Protected TestResults is
> deliberately not read, modified or used. No optional tool/report installation.

This is a six-file kernel analysis, not entire-product/provider coverage or A+ acceptance.
The explicit profile includes all src code, auto-properties and debugger-attributed
operations. Loaded Core graph:49,330/60,935 executable lines (80.9551%) and
16,845/23,072 branches (73.0106%). Kernel:363/389 distinct file/line points and
159/196 instrumented class/line branch outcomes. No test source is included.
File line counters merge hits by filename/line across generated classes; branch
counters retain the instrumented class/line identity within this one XML. No
multi-host union or independent branch-outcome merge is claimed.

PowerShell is absent; the fully read bundled Compute-CrapScores and
Extract-MethodCoverage algorithms are applied through read-only Ruby/REXML with
the same per-method complexity attribute, distinct lines, exact branch counters
and CRAP formula. No source/test/comment generator or project edit is used.
Diagnostic thresholds80/70/30 are skill defaults, not replacement acceptance gates.

---

## 🔥 Risk Hotspots (Top 6 by CRAP Score)

Methods flagged as high-risk: complex code with low test coverage that is dangerous to change.

| Rank | Method | Class | File | Complexity | Coverage | CRAP Score |
|------|--------|-------|------|-----------|---------|-----------|
| 1 | `AttemptAsync` / `MoveNext` | `RetryFilter<TContext>` | `Middleware/RetryFilter.cs:123` | 28 | 82.5000% | **32.2018** |
| 2 | `SendPolicyAsync` / `MoveNext` | `RetryFilter<TContext>` | `Middleware/RetryFilter.cs:52` | 22 | 88.5714% | **22.7225** |
| 3 | `SendPolicyAsync` / `MoveNext` | `RedeliveryRetryExecution` | `RetryPolicies/RedeliveryRetryExecution.cs:34` | 16 | 90.0000% | **16.2560** |
| 4 | `Propagate` | `RetryOperationState` | `RetryPolicies/RetryOperationState.cs:112` | 14 | 92.8571% | **14.0714** |
| 5 | `ExecuteCancelableAsync` / `MoveNext` | `RetryPolicyExecution` | `RetryPolicies/RetryPolicyExecution.cs:85` | 12 | 100.0000% | **12.0000** |
| 6 | `PropagateNestedRetryFailureAsync` / `MoveNext` | `RetryFilter<TContext>` | `Middleware/RetryFilter.cs:218` | 10 | 85.7143% | **10.2915** |

> **CRAP Score** = `Complexity² × (1 − Coverage)³ + Complexity`.
> Scores above 30 are flagged. A score ≤ 5 is considered safe.

Async methods use the emitted state-machine MoveNext coverage, explicitly mapped to
the personally read source operation. CRAP is a heuristic, not correctness proof.
At fixed complexity28, covering all40 currently measured Attempt lines would reduce
CRAP32.2018 to28; merely hitting more lines would not prove its error/cancellation
semantics. The internal counterreview finds an actual token-getter exception-filter
hole there; causal regression and architecture-correct repair take priority.

---

## 📋 Coverage Gaps by File

Files below the line or branch coverage threshold, ordered by uncovered lines descending:

| File | Line Coverage | Branch Coverage | Uncovered Lines | Priority |
|------|--------------|----------------|----------------|---------|
| `Middleware/ActivityRedeliveryRetryFilter.cs` | 68.7500% | 66.6667% | 5 | 🟢 LOW |
| `Middleware/RedeliveryRetryFilter.cs` | 100.0000% | 66.6667% | 0 | 🟢 LOW |

Other kernel file totals are not hidden: RetryFilter106/120 lines,63/78 branches;
RetryOperationState106/109,46/58; RetryPolicyExecution70/71,22/24;
RedeliveryRetryExecution54/57,20/24. Uncovered lines sum26, reconciling389−363.
RetryFilter is still the changed critical operation with the highest method risk,
despite its file averages exceeding the diagnostic thresholds.

Every one of the14 below-threshold instrumented members follows; none is silently
called fine merely because the file average passes. Compiler closure names refer
to the exact raw XML/source shape at this frozen snapshot.

| Member | Source line | Lines covered/total | Branches covered/total |
|---|---:|---:|---:|
| `ActivityRedeliveryRetryFilter.IProbeSite.Probe` | 29 | 0/5 | 0/0 |
| `RetryOperationState.<>c.<PublishTerminal>b__9_0` | 78 | 0/1 | 0/0 |
| `RetryFilter.<>c__DisplayClass6_1.<AttemptAsync>b__4` | 154 | 0/1 | 0/0 |
| `RetryOperationState.<>c.<Mark>b__8_0` | 65 | 0/1 | 0/0 |
| `RetryOperationState.IsOwned` | 53 | 5/6 | 3/6 |
| `RetryOperationState.Lease.Dispose` | 226 | 5/6 | 1/2 |
| `RetryFilter.<>c__DisplayClass6_2.<AttemptAsync>b__5` | 164 | 2/2 | 1/2 |
| `RetryOperationState.Enter(PipeContext)` | 33 | 4/4 | 2/4 |
| `RetryFilter.<>c__DisplayClass8_0.<PropagateNestedRetryFailureAsync>b__1` | 227 | 2/2 | 1/2 |
| `RetryOperationState.GetCurrent` | 191 | 3/3 | 2/4 |
| `ActivityRedeliveryRetryFilter..ctor` | 21 | 4/4 | 2/4 |
| `RedeliveryRetryFilter..ctor` | 23 | 4/4 | 2/4 |
| `RetryOperationState.ExecuteAsync.MoveNext` | 164 | 8/8 | 1/2 |
| `RetryFilter.<>c__DisplayClass5_2.<SendPolicyAsync>b__3` | 90 | 2/2 | 1/2 |

Counts are per member and may overlap other members' source lines; they are not
summed to invent a file denominator. Ownership factory fallbacks/guard paths must
be assessed against valid contracts, not activated by dummy line-touching tests.

---

## 💡 Recommendations

1. Causally prove and repair the RetryFilter token-getter exception-filter finding:
   exact infrastructure identity, no extra classifier/business completion, exactly-once
   cleanup and unchanged outer lifecycle. Then remeasure the changed risk path.
2. Exercise meaningful pending nested fault notification during an existing retry
   attempt and genuine invalid custom lifecycle tasks. Assert effects/failure identity;
   do not replace behavior evidence with higher average coverage.
3. Close actual delayed/RabbitMQ publish-token continuations and adapter-factory
   primary/cleanup preservation with their causal provider-boundary tests. Core graph
   coverage cannot substitute for these provider semantics or a later full-host union.

---

## 📁 Reports

| Report | Path |
|--------|------|
| Markdown summary (this file) | `evidence/WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01/ITERATION-119/CANCELLATION_CHILD_COVERAGE/coverage-analysis.md` |
| Raw Cobertura XML | `/private/tmp/vsb-iteration119-cancellation-child-complete-profile/cancellation-child-complete-profile.cobertura.xml` |
| HTML (browsable) | `Not generated (optional — request HTML reports to enable)` |
| Text summary | `Not generated (optional — request HTML reports to enable)` |
| GitHub markdown | `Not generated (optional — request HTML reports to enable)` |
| CSV data | `Not generated (optional — request HTML reports to enable)` |

The immediately delivered user-facing summary includes both measured scopes, top
risk operations and the actual counterreview finding before further optional work.
Optional report generation is intentionally skipped. No TestResults ignore mutation
or provider package addition is needed or made for this analysis.

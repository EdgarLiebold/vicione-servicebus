# Frozen a91b25 Unit-derived coverage and CRAP · 19.09.2026

This is a **hash-bound, unit-derived** measurement, not whole-test or real-provider coverage. All runs used the immutable snapshot `/private/tmp/vicione-servicebus-green-freeze.H2NAR1/repositories/vicione-servicebus`, local commit `a2373f95efaa801b7f2c6905d7d2f06dc8e696fb`. The sorted tracked/untracked `src`+`tests` digest (excluding protected `review/**` and `TestResults/**`) was `a91b25cd499715630a606aabb24cde8603ba41231f427ccc553140bd12334295` before and after collection/aggregation. The later live worktree diverged; this report is not a metric for its changed bytes.

The separate Release coverage build with the measurement-only assembly loader finished with **0 warnings and 0 errors**. Its first unfiltered Unit/Architecture run was **9,604 passed, 1 failed** because the loader expected EventHubs and EventHubs.Testing outputs absent from the Unit solution. The Lead built those two source assemblies into the same isolated `sdk/` output (0 warnings/errors), the loader then passed 1/1, and the complete unfiltered instrumented solution rerun passed **9,605/9,605, 0 failed, 0 skipped**. The extra test is the measurement-only loader. All 14 project-specific unfiltered coverage runs passed, as did the unfiltered Core coverage run (**6,243/6,243**) and targeted loader coverage run (1/1). These produced exactly **16 fresh Cobertura XML reports**; no old raw reports were mixed in.

| Deduplicated product-source measure | Current unit-derived value |
| --- | ---: |
| Source projects / loadable assemblies observed | 33 / 32 of 32 |
| Covered / valid lines | **67,005 / 89,704 = 74.6957%** |
| Covered / valid branches, conservative lower bound | **24,365 / 36,214 = 67.2806%** |
| Covered / valid branches, capped upper bound | **25,831 / 36,214 = 71.3288%** |
| Methods / methods with CRAP > 30 | **21,301 / 463** |
| CRAP median / p95 / p99 / maximum | **2 / 14 / 72 / 9,312** |

The existing `aggregate.py` deduplicates lines by source path/line and methods by path/class/name/signature. Cobertura lacks taken-arc identity across reports, so the branch union is an interval, **not an exact overall branch percentage**. CRAP is `CC² × (1 − method covered-line ratio)³ + CC` with Cobertura complexity; `>30` is a risk-worklist threshold, not an A+ verdict. `source_assemblies_missing` and `unexpected_packages` are both empty.

Highest unit-only CRAP candidates include RabbitMQ send async state-machine `MoveNext` (**9,312**), Azure Service Bus connection `MoveNext` (**3,422**) and receiver `MoveNext` (**2,550**), Amazon SQS receive endpoint `MoveNext`/RabbitMQ header setter (both **2,162**), Amazon SQS notification parser (**1,806**), ActiveMQ header setter (**1,640**) and PostgreSQL host parser (**1,482**). Those methods have no covered lines in this unit-only profile. ActiveMQ, Amazon SQS, EventHubs, generic SQL Transport, PostgreSQL and SQL Server source projects also have 0 covered lines in this profile; their local-integration tests must be instrumented before interpreting them as final untested code. The [provider plan](provider-coverage-static-plan.md) identifies 13 local test projects and the remaining fixture/overlay risks.

Portable ignored artifacts are `artifacts/coverage-iteration242-a91-unit/summary.json` (SHA-256 `d52a3dfbbef25d4732668f64add5baa6984f6425798eb2569deccf5da5d8b54b`), `methods.json` (`69c46d9e7dbac831261fbed6c5bda021285888dde2c02187ed49966004cb696d`) and `cobertura-raw.tgz` (`c8cadf8ae1d3a67e14959c71077c30bfb1dd474aac13c5d466f61929e350aecf`). The aggregator SHA-256 is `567fa01ddfada8a081dc525ee684345058563e137530ca54888dcfca1256922d`; the raw report manifest and per-project values are in `summary.json`. The artifact bundle is local, not pushed; this committed record carries its hashes and limits.

The same-snapshot local-provider union was subsequently measured in [coverage-a91-combined-evidence.md](coverage-a91-combined-evidence.md). Real-cloud/provider evidence, mutation/assertion-quality review, bidirectional public-API mapping and complete source A+ dispositions remain open. No live-byte A+ acceptance is claimed here.

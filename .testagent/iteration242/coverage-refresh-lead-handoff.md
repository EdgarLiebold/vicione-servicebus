# Coverage Refresh — Lead handoff (not a measurement)

Status: preparation only. The isolated build started by the assisting agent was
stopped with Ctrl-C after repository governance was reconciled: for A-0071 only
the Lead runs .NET build/test processes (PO-2026-08-22-01 Nr. 2 and
PO-2026-08-21-03 Nr. 5). Exit code 1 means interrupted build, not failed tests.
No test results or fresh coverage metrics are claimed here. The build reported
zero warnings and zero errors before interruption. No product, test, project,
package, lock, or existing coverage-report file was edited by this preparation.

Prepared isolated path:
`artifacts/coverage-iteration242-refresh-20260919-01/`. Only its generated
assembly-loader test, MSBuild import target, and coverage-run helper were
created. An interrupted `sdk/` build tree also exists under that path. The
prior `artifacts/coverage-iteration242-postfix/` remains unchanged.

At preparation start, HEAD was
`9e9d22cd9520e624b3adb6d6d1e28ccf485c28b4`. The source/test worktree
content digest before the build and again after interruption was
`469a2cbea2379b33317ba793db0090dbcce42afa48dc1731c2c00c8d29710769`.
It covers tracked and untracked, non-ignored files under `src` and `tests`:

```sh
git ls-files -c -o --exclude-standard -z -- src tests | sort -z | xargs -0 shasum -a 256 | shasum -a 256
```

The Lead must capture a new pre-run digest immediately before resuming, and a
post-run digest after every collection/aggregation step. A mismatch makes the
measurements stale for a single source/test snapshot. Concurrent unrelated
HEAD changes do not by themselves invalidate a stable content digest.

From the ServiceBus repository root, the intended commands are:

```sh
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --artifacts-path artifacts/coverage-iteration242-refresh-20260919-01/sdk \
  -p:CustomAfterMicrosoftCommonTargets="$PWD/artifacts/coverage-iteration242-refresh-20260919-01/coverage-universe.targets" \
  --verbosity minimal

VICIONE_COVERAGE_REPO="$PWD" dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  --configuration Release --no-build --no-restore \
  --artifacts-path artifacts/coverage-iteration242-refresh-20260919-01/sdk \
  --verbosity minimal

bash artifacts/coverage-iteration242-refresh-20260919-01/run-unit-coverage.sh

VICIONE_COVERAGE_REPO="$PWD" dotnet test \
  --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --artifacts-path artifacts/coverage-iteration242-refresh-20260919-01/sdk \
  --coverage --coverage-output-format cobertura \
  --coverage-output "$PWD/artifacts/coverage-iteration242-refresh-20260919-01/raw/core/coverage.cobertura.xml" \
  --verbosity minimal

VICIONE_COVERAGE_REPO="$PWD" dotnet test \
  --project tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --artifacts-path artifacts/coverage-iteration242-refresh-20260919-01/sdk \
  --filter-method '*LoadEverySourceAssemblyForCoverageDenominator*' \
  --coverage --coverage-output-format cobertura \
  --coverage-output "$PWD/artifacts/coverage-iteration242-refresh-20260919-01/raw/universe/coverage.cobertura.xml" \
  --verbosity minimal

python3 artifacts/coverage-iteration242-wholefork/aggregate.py \
  --raw artifacts/coverage-iteration242-refresh-20260919-01/raw \
  --output artifacts/coverage-iteration242-refresh-20260919-01 \
  --scope 'fresh unfiltered UnitArchitecture profile; 32 loadable source assemblies; 15 coverage-enabled unit modules'
```

The full unfiltered UnitArchitecture profile supplies the test pass/fail/skip
count. The 14 project-specific coverage runs, the unfiltered core run, and the
generated-only universe run should produce **16 fresh Cobertura files**. The
EventHubs local-integration project is intentionally not run. The universe
test is a measurement-only assembly loader and does not count as product/test
source for the source/test digest. It should verify 32 source assemblies, with
`ViciOne.ServiceBus.Analyzers.Package` excluded because it has no loadable
product output. No real-provider, local-integration, external, or all-test
claim follows from this procedure.

Aggregation must use `aggregate.py` unchanged: lines deduplicate by source
path and line number; methods by source path, class, method name and signature;
branches by source path, class and line number. For each branch key, the
largest covered count in a single report is the conservative lower bound;
the capped sum is the upper bound. Cobertura has no taken-arc identity, so no
exact cross-report branch union may be asserted. Verify `source_assemblies_total`
and `source_assemblies_observed` both equal 32, no missing/unexpected packages,
and reconcile CRAP totals, hotspot count and top entries with the generated
`summary.json`/`methods.json`. CRAP uses Cobertura method complexity and the
deduplicated method-line ratio: `CC² × (1 − covered/valid)³ + CC`.

Report results only after all fresh runs and the post-run digest. If a run
fails or source/test content drifts, record the partial result and its limits
without presenting it as current coverage.

## Lead attempt on 2026-09-19

The Lead captured pre-run `src/tests` digest
`469a2cbea2379b33317ba793db0090dbcce42afa48dc1731c2c00c8d29710769`.
The specified isolated Release build gave no compilation/test output for over
five minutes and was terminated after an attempted console cancellation. A
second Lead build with `--no-restore` likewise produced no completion/output
for over four minutes and was terminated by its verified exact PID. Neither
attempt produced a fresh Cobertura file or a completed test result; a terminal
exit status from process cancellation is not a successful build. No coverage
or CRAP aggregation was run from this attempt.

The post-attempt `src/tests` digest was
`1320cc6c859b72f1f92d7af268aeecb2f12e7dabb50c4951dff2213d245976a4`,
different from the pre-run digest because the shared worktree changed during
the attempt. No fresh measurement can be attached to the pre-run tree. The
previous unit-derived and Unit+Rabbit reports remain explicitly older,
partial snapshots. Resume only with a new stable content digest and a
separately diagnosed build/restore path; do not infer fresh coverage from
this failed collection.

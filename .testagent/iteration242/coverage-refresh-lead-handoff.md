# Coverage Refresh — preparation and frozen Lead measurement

Historical preparation status: the isolated build started by the assisting agent was
stopped with Ctrl-C after repository governance was reconciled: for A-0071 only
the Lead runs .NET build/test processes (PO-2026-08-22-01 Nr. 2 and
PO-2026-08-21-03 Nr. 5). Exit code 1 means interrupted build, not failed tests.
No test results or fresh coverage metrics came from that preparation. The build reported
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
The specified isolated Release build gave no visible compilation/test output at
minimal verbosity for over five minutes and was terminated after an attempted
console cancellation. A second Lead build with `--no-restore` likewise produced
no visible completion/output at minimal verbosity for over four minutes and
was terminated by its verified exact PID. **Neither attempt proves that the
build was actually hung.** A later single-project Lead build with normal
verbosity and disabled build servers completed in 1:36 with zero warnings and
errors; that result shows the earlier silence was not sufficient to diagnose a
hang. Neither canceled full-solution attempt produced a fresh Cobertura file or
a completed test result; a terminal
exit status from process cancellation is not a successful build. No coverage
or CRAP aggregation was run from this attempt.

The post-attempt `src/tests` digest was
`1320cc6c859b72f1f92d7af268aeecb2f12e7dabb50c4951dff2213d245976a4`,
different from the pre-run digest because the shared worktree changed during
the attempt. No fresh measurement can be attached to the pre-run tree. The
previous unit-derived and Unit+Rabbit reports remain explicitly older,
partial snapshots. Resume only with a new stable content digest and an actual
completed isolated build; do not infer fresh coverage from the canceled runs.

## Lead frozen-snapshot measurement on 2026-09-19

The moving shared worktree could not provide one unchanged `src/tests` digest
through a multi-minute collection. The Lead therefore copied only permitted
tracked files and untracked `src/tests` files into
`/private/tmp/vicione-servicebus-coverage.Yh443A/repositories/vicione-servicebus`.
Neither `review/**` nor `TestResults/**` was copied. The sorted `src/tests`
content digest was `0f1e46f52094c24439bffe761ade9b2fe5d66df2fe588ec9ef2c2c5db8f77403`
before the copy, after the copy, and after aggregation. This is a **frozen
historical snapshot**, not the later current worktree.

The Lead restored and built `ViciOne.ServiceBus.Tests.Unit.slnx` in Release:
zero build warnings and errors. The generated assembly-loader test passed and
found all 32 loadable `src` assemblies. Fourteen instrumented unit-project
runs passed; the instrumented Core run produced Cobertura after 6,243 tests,
6,242 passed and one failed. That failure was
`OutboundNetworkBoundaryTests.ProductAssemblyBytes_ContainNoVendorAddressAndDoContainAddressLikeStrings`:
its ASCII-only byte scanner did not see UTF-16LE CLR literals. The later
current-worktree test fix is **not** part of this coverage snapshot. A separate
unfiltered Unit/Architecture solution attempt in this snapshot had two
additional setup-sensitive failures (the generated loader before its path was
corrected, and an executable-project MSBuild-evaluation assertion before all
test projects had been restored); it is not reported as a green suite.

The 16 Cobertura reports were deduplicated with
`artifacts/coverage-iteration242-wholefork/aggregate.py` by source path/line,
class/line branch count, and source/class/method/signature. Results:

| Frozen unit-derived measure | Value |
| --- | ---: |
| Source assemblies observed / expected | 32 / 32 |
| Covered / valid lines | 66,870 / 89,583 = 74.6459% |
| Covered / valid branches, conservative lower | 24,281 / 36,146 = 67.1748% |
| Covered / valid branches, capped upper | 25,745 / 36,146 = 71.2250% |
| Methods and CRAP > 30 | 21,281 / 465 |
| CRAP median / p95 / p99 / maximum | 2 / 14 / 72 / 9,312 |

The branch interval is required because Cobertura reports counts rather than
taken-arc identities; an exact cross-report union is not recoverable. The
largest CRAP scores include RabbitMQ send (`9,312`), Azure Service Bus
connection handling (`3,422`), receiver exception handling (`2,550`), and
Amazon SQS validation (`2,162`). Several provider projects have 0% in this
**unit-only** profile, so those numbers are uninstrumented-provider gaps, not
proof their local-integration code is never tested. The scores are method-risk
triage, not A+ acceptances.

Portable measurement artifacts are
`artifacts/coverage-iteration242-refresh-20260919-01/summary.json`
(SHA-256 `abaf45f3ad77537efb047201064c5f29d1143e59341ae49b8fbf41988437d1d0`),
`methods.json` (SHA-256 `8e41784d81aec798ad685f9da6ad60992cda167079fa7aed82c0ad563c2bed6a`),
and `cobertura-raw.tgz` (SHA-256
`4fb86cfa1ed22e45fad112c4b75b06551d605d4f95374cbacb82f333337b26ef`).
They are ignored build artifacts, not committed reports. The frozen source tree
and raw extracted reports remain at the temporary path above.

After repairing the architecture test in the **current** worktree, the Lead
built its Core test project with zero warnings/errors; the two affected tests
passed 2/2 and the unfiltered Core project passed 6,242/6,242, zero skips.
Its `src/tests` digest stayed
`700984c1e27cfb05b9df8f702eca65bbea8f2bd36253c8ebde993b4400052688`
before and after the unfiltered run. That green result does not retroactively
green the frozen coverage run, the full Unit/Architecture solution, all
providers, or the global A+ gate. A new current-byte whole-fork coverage and
provider-profile merge remain open.

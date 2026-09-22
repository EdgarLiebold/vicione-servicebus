# Product-wide coverage profile at source/test commit 44d9e3254

## Scope and provenance

- Exact source/test commit: `44d9e32546ccf1ffe60bc49e49cfc81c3aa348d6`.
  The tracked `src`/`tests` diff was empty when the aggregate was captured.
  Its 22 Unit reports were produced immediately before this commit; its 13
  provider reports and supplementary Abstractions report were produced after
  it. The commit changed only the two SignalR lockfiles and the changelog,
  not C# source or tests. The earlier SignalR assets already resolved the
  corrected project graph. A post-commit locked restore, zero-warning Release
  build, and Unit/Architecture gate verified the committed lockfiles separately.
- Microsoft CodeCoverage ran with `tools/ci/coverage.settings.xml`. The final
  aggregate contains 36 fresh Cobertura reports: 22 Unit/Infrastructure
  projects, 13 local-provider projects, and the same Abstractions suite under
  `DOTNET_EnableAVX2=0`. All 32 loadable source assemblies appear; none are
  missing or unexpected.
- The raw XML, report SHA-256 values, method ranking, and aggregate JSON are
  under `artifacts/coverage-a-plus-20260922-8abfe1e8a/analysis-36-noavx2/` and
  `artifacts/coverage-a-plus-20260922-8abfe1e8a/raw/`.
- The 22 Unit/Infrastructure modules passed 9,536 tests. The ten shared
  provider modules passed 405. SQL Server passed 69, RabbitMQ 31, and Azure
  Service Bus 25. All passed with zero failures or skips. The four fixture
  runs `vicione-b017a81a566e`, `vicione-752e3db15dea`,
  `vicione-4ae66036c429`, and `vicione-a68772b783fa` have empty findings.
  The separate Unit/Architecture gate passed 9,981/9,981 after the SignalR
  lockfile-only correction, with zero failures and skips. Its 445 Architecture
  tests are excluded from the coverage aggregate because CodeCoverage
  injection invalidates an assembly-ownership assertion.
- The supplementary run passed all 751 existing Abstractions tests with AVX2
  disabled and zero failures or skips. The reference-corpus formatter test
  asserts exact values for case and bracket variants; the existing API-shape
  test rejects both 15- and 17-byte input with the exact `bytes` parameter.

## Product-wide result

| Measure | Result |
| --- | ---: |
| Line coverage | 82,927 / 93,153 = 89.0224% |
| Branch coverage, conservative lower bound | 29,470 / 36,629 = 80.4554% |
| Branch coverage, capped upper bound | 32,018 / 36,629 = 87.4116% |
| Methods with CRAP > 30 | 105 / 25,880 |

Cobertura gives branch counts but no branch identities. The aggregate merges
unique source class/line counts across reports; the lower bound takes the
maximum observed covered count and the upper bound caps the sum. It does not
claim an exact global branch rate.

The previous complete profile at `e1a965290` used different coverage
instrumentation and counted 90,376 valid lines. The current explicit settings
count 93,153 valid lines, with differences across nearly every assembly.
Comparing the two percentages as a code improvement would be misleading.

The same-byte 35-report control, before the no-AVX2 run, measured
82,851/93,153 lines, a branch interval of 29,446–31,925/36,629, and 106
methods above CRAP 30. Its aggregate remains in `analysis-35/`. The existing
strong test suite on a non-AVX2 runtime added 76 covered source lines and
removed one CRAP hotspot without any source or test edits.

## Highest current CRAP risks

| CRAP | Method | Product gap to investigate |
| ---: | --- | --- |
| 218 | `ServiceBusConnectionContext.CreateTopicSubscriptionAsync` | Subscription and rule reconciliation has 44/69 lines covered. |
| 156 | `ServiceBusTopicConfigurator` async `MoveNext` | 0/5 lines covered. |
| 156 | `ServiceBusReceiveContext.GetTransportProperties` | 0/10 lines covered. |
| 156 | `DictionaryExtensions.SetValue` | 0/11 lines covered. |
| 128.99 | `PayloadAdmissionTransportBoundary.Admit` | 17/43 lines covered. |
| 124.37 | `AmazonSqsReceiveEndpointConfiguration` async `MoveNext` | 24/36 lines covered. |

`DashedHexFormatter.Format` moved from 7/38 to 38/38 covered lines and from
CRAP 237.17 to 20. Its conservative branch count is 18/20. The remaining
reported branch alternatives are within `Avx2.IsSupported &&
BitConverter.IsLittleEndian`; the existing malformed-input test covers both
sides of the length boundary. No new test was added solely for coverage.

The most uncovered product lines are in Core (3,906), Abstractions (1,134),
Azure Service Bus (919), and RabbitMQ (713). The next focused slice should
investigate Azure Service Bus subscription and rule reconciliation at CRAP 218
against actual client behavior and its existing tests. Any source correction
and new test must pass focused and complete gates plus adversarial read-only
review.

Global A+ is open. No claim is made that all code paths or all branch arcs
are covered.

The adversarial read-only review checked all 36 report paths and hashes,
32/32 assembly membership, the CRAP arithmetic, the exact corpus assertions,
the malformed-input boundaries, and the four fixture finding files. It found
one provenance wording issue, corrected above, and no remaining concrete
measurement or test-quality defect.

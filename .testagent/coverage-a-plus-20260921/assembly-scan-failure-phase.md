# Assembly scan failure phase

## Product behavior tested

`FailedAssemblyScan_IsCachedAndReportedWithoutHidingHealthyTypes` exercises
the public `AssemblyTypeCache` path with two independent assemblies whose
exported-type discovery fails. It verifies that both original exception
objects are retained, both failures are reported without assuming dictionary
order, and a healthy assembly remains scannable. It then makes both assemblies
recover, verifies that the failed snapshots remain cached until `Clear()`, and
checks that the next scan succeeds exactly once per assembly. The test cleans
the global cache in `finally`.

The test is bound to `REQ-VSB-ASSEMBLY-SCAN-FAILURE` variant
`multiple-exported-type-failures-cache-and-retry` in the Core requirement
projection. No product source or comments changed.

## Verification

- The corrected complete Core test module passed 6,372/6,372 tests with zero
  failures and skips. Its focused Microsoft CodeCoverage report at
  `artifacts/coverage-a-plus-20260923-8d6621c48/assembly-scan-core-final.cobertura.xml`
  measures `AssemblyTypeCache.ThrowIfAnyTypeScanFailures` at 6/6 lines,
  complexity 6 and therefore CRAP 6. The previous exact-commit global profile
  measured 0/6 lines and CRAP 42. This is a focused result, not a new
  product-wide profile.
- A preceding focused test invocation selected zero tests because its xUnit v3
  method filter lacked the fully qualified name. The corrected invocation
  passed 1/1; the later complete Core run passed with the final test content.
- The first adversarial review found a missing requirement projection entry
  and a first-error-only aggregation gap. Both were corrected. The read-only
  follow-up review returned PASS with no remaining concrete findings.
- The final complete serial Unit/Architecture gate passed 10,203/10,203
  tests, zero failures and skips. Its log is
  `artifacts/coverage-a-plus-20260923-8d6621c48/logs/assembly-scan-full-gate-final.log`.

The next exact-commit 36-report product-wide profile must be collected before
the focused coverage improvement can be attributed to global metrics.

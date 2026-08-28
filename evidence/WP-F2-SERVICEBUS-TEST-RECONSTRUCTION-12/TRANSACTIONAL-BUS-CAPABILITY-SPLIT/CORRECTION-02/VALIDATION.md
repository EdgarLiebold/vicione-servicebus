# Transactional bus stack-provenance correction

## Frozen subject

- Technical commit: `f1f64a385a8e1f0f3bf5d647be6cbeff8bdd532e`
- Technical tree: `a49611eb84e1e680b04c63f0641f6d42574e7563`
- Technical parent and preceding evidence: `8b02142d50a7e085c60aac40a1d53bcef4ad70cb`
- Preceding product correction: `3c1bfbafeb326047a86a2ec6573f6f516e2bd230`
- Architecture scope: `17a0791b563a99e81c3f589a5d63214e59330d99`, tree
  `151861503f841e55e2dfd1f4a93dc3c95e879161`

`TECHNICAL_CORRECTION.patch.gz` is the byte-exact one-file delta. The existing reflected-boundary Fact now
proves not only exact exception type and object identity but also the original callback frame
`ThrowingBusInstanceCallback.GetResult`. This directly binds the selected `ExceptionDispatchInfo` contract.
No new test case was added, so the UnitArchitecture floor remains 2271.

## Positive execution

All commands recorded in `FINAL_RESULTS.json` ran at the exact technical bytes:

| Boundary | Result |
|---|---:|
| Engineering locked restore | exit 0 |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| UnitArchitecture | 2271/2271; 19 CTRFs; 0 failed/skipped |
| Transaction namespace | 39/39; 0 failed/skipped |
| LocalIntegration | 244/244; 7 CTRFs; 0 failed/skipped |

LocalIntegration used fresh PostgreSQL, Azurite, LocalStack, ActiveMQ Classic and Artemis resources under
`vicione-0fda85f6124b`. Dynamic loopback endpoints, broker logs, two controlled ActiveMQ outage/restore
cycles and empty fixture findings are retained. The run ownership token and generated credentials are not.

## M24 causal proof

M24 changes only the EDI rethrow to `throw exception.InnerException;`. Its Release build succeeds with zero
warnings and errors. The one-test MTP run exits 2 with exactly one failure and no skips: the same exception
instance is still observed, but the required callback frame is absent. The target then restores to the exact
technical SHA-256. The compressed patch, baseline/mutant/restore hashes, exact working directories and argv,
binlog, raw logs and CTRF make the attack independently reconstructible. Cumulative closure is 24/24 killed
mutants.

## Verdict

This additive package supersedes only the earlier stack-diagnostic MINOR. The previous technical and evidence
commits remain immutable. A new independent read-only review of this exact technical/evidence chain remains
mandatory before remote publication and architecture acceptance.

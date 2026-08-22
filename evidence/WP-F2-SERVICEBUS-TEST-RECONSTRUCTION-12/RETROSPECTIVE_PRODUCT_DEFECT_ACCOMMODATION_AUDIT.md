# Retrospective Product-Defect Accommodation Audit

Date: 2026-08-22
Verdict: **PASS for the reconstructed scope; not a claim that the whole product is defect-free**

## Bound subject

- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`
- Commit: `adc38fe37b9a13ca4e2f847b8c2db6b3d2223fc6`
- Tree: `75673dbe1bb005fd56cf07bc69a49c2845a6d45b`
- Remote branch at audit start: the same commit
- Accepted pre-test R0 baseline: `a6b205c9`

The audit covers every tracked C# file under `tests2/`, every reconstructed behavior cohort, its inherited-behavior disposition, the available targeted mutation evidence, and every product-source change between the accepted R0 baseline and the bound subject. It does not claim that product areas whose inherited tests have not yet been reconstructed are defect-free.

## Question answered

No known test in the reconstructed scope was weakened, skipped, filtered, given an environment-dependent escape, or rewritten to accept a product defect. No known product defect discovered by the reconstructed tests remains hidden or falsely green.

That statement is evidence-bounded, not metaphysical: tests can reveal defects but cannot prove the absence of every latent defect. Known product risks outside the reconstructed scope remain explicit findings and are listed below.

## Evidence and method

1. Read all 92 tracked C# files under `tests2/`, including behavior tests, architecture tests, configuration, and shared test infrastructure.
2. Reviewed assertions, expected values, exception boundaries, synchronization, timeouts, environment access, cleanup paths, and every catch block in context.
3. Compared the complete post-R0 Git history and product-source diff with the test changes.
4. Reconciled all six completed inherited-behavior dispositions with their native owners.
5. Reviewed the seven targeted mutation reports. A surviving MessagePack transport mutant caused stronger assertions; it did not cause weaker product expectations.
6. Ran static anti-bypass scans:
   - 309 `[Fact]`/`[Theory]` method declarations;
   - zero skip markers;
   - zero `TODO`, `FIXME`, known-bug, or workaround markers in C# test code;
   - zero random-number calls;
   - zero conditional test exclusions.
7. Performed fresh locked restores, clean Release builds, and unfiltered native Microsoft Testing Platform runs for both materialized profiles.

## Completed cohort disposition

| Cohort | Inherited disposition | Result |
|---|---:|---|
| Abstractions | 78 obligations: 70 executable replacements, 8 explicitly non-executing/non-product rows | No open inherited behavior |
| Analyzers and code fixes | 115/115 replaced | No open inherited behavior |
| SignalR | 28 rows: 26 executing replacements, 2 inherited commented/non-executing methods disposed with active behavior coverage | No open inherited behavior |
| MessagePack | 67/67 replaced | No open inherited behavior |
| State-machine visualizer | 9/9 replaced | No open inherited behavior |
| Cron expression | 58 rows: 57 replaced, 1 replaced and hardened | No open inherited behavior |

## Product-defect decisions found in history

### Message-body defects before R0

Commit `b5fc515a` corrected the bounded message-body defects in product code: decoded Base64 length, string length and whitespace preservation, read-only streams, and a non-growable empty stream. The native reconstruction retains those product contracts; it does not encode the former defective behavior.

### Cron defect after R0

Commit `3c628228` is the only post-R0 semantic C# product change. `CronExpression.BuildExpression` incorrectly treated empty spans between consecutive separators as cron fields. The product parser was corrected to ignore empty spans. The regression tests retain the correct padded-expression contract, and targeted mutation evidence proves that removing the product guard is detected. The remaining diff in that file is formatter-only.

### MessagePack hardening

Commit `9eb0c245` strengthened the transport-selection assertions after a substitution mutant survived. It added an exact MessagePack content-type assertion. No product behavior was relaxed or special-cased.

## Native execution evidence

| Gate | Result | Binlog SHA-256 |
|---|---|---|
| Unit locked restore | Exit 0; lock files unchanged | `880edd2bfe51620f5eaf6be7f5e0bf8a9374185669f2a1f6aa545dcb6169ca34` |
| LocalIntegration locked restore | Exit 0; lock files unchanged | `f7e2b8c453586012c0d93666198f2c72706635be76abed0367e443fd9ee764ea` |
| Unit/Architecture Release build | Exit 0; 0 warnings; 0 errors | `860464565fda5fc19394110ab5233c6d9111eaf0bd772ebdf5212ea2d70d2af3` |
| LocalIntegration Release build | Exit 0; 0 warnings; 0 errors | `05449f0e01d6bb4f8501ebad3ae33613769b18e3e38fbeadab861ee06dd59a06` |
| Unit/Architecture unfiltered run | 618 total; 618 passed; 0 failed; 0 skipped; Exit 0 | `53f05c22c52f7bd98b5dd08a4d15a6b694af8a36ba01199aa4e2a6eb4447ec70` |
| LocalIntegration unfiltered run | 3 total; 3 passed; 0 failed; 0 skipped; Exit 0 | `6434998ff5306000b8860bf8c83ec9219917bdf694552ab0eef0fd49e126c6af` |

The first audit run correctly failed one repository architecture test because a stale two-day-old VS Code workspace restore had recreated an untracked 129-byte project placeholder at a retired test path. The test was not changed. The unversioned tool artifact was removed, the repository returned to the bound clean tree, and the same unfiltered rule then passed. This is evidence that the architecture gate detects repository contamination rather than accommodating it.

## Explicitly open, not hidden

The following architecture findings are outside the reconstructed behavior scope and remain release-gating work rather than falsely green tests:

- `F-SB-01`: replace inherited encryption with the approved modern AES-GCM design;
- `F-SB-02`: complete the public API and obsolete-path cleanup without losing useful capabilities;
- `F-SB-03`: resolve or explicitly exclude the EF Core bus-outbox poison-message/head-of-line blocking path before A+ release.

The Persistence cohort containing `F-SB-03` has not yet been reconstructed. Its absence from the current 621 executed tests is visible and must not be represented as coverage.

## Non-defect hardening opportunities

Two small assertion-quality improvements remain useful but are not evidence of accommodated product defects:

- use a fixed timestamp in the MessagePack foreign-envelope fixture instead of `DateTime.UtcNow`;
- extend the MessageData external-reference round-trip test from address preservation to dereferenced-content preservation when that behavior's owning cohort is reconstructed.

Neither item changes an accepted product contract or masks a current failure.

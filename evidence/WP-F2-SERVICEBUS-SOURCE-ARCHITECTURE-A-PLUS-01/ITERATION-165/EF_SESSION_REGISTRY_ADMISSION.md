# Iteration 165 — EF session registry admission

## Result

This packet personally reads the complete EF bus-outbox session registry source (53 lines) and the
complete reliable-registration owning test file (269 lines). The product review found the registry's
locked create-or-reuse implementation correct. Its cache key separates DbContext type and reliable
mode, and failed creation cannot publish a partial session.

The admission closes a proof gap rather than changing product code: the transactional branch already
had a concurrent-resolution test, while the reliable branch had only sequential reuse coverage. The
new reliable case releases sixteen callers together and proves one distinct session and identical
references for every caller.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 53 | `d69fa540dd9a3a2e4fa3cb9b3f1b092a7cb2b51537fdfe5e97b7f2853134a69e` | `d0bd454ec7422157b5ab2c525fd588baeb8c738742d25c4b51ea1ce019f772e5` |
| Tests | 1 / 269 | `6379663e614077ece1799fade0a49f6535608144cd5427def36386e9bfc50f4a` | `26da1644690e63722ac15bdf9cd4d1e429359fcbaa0b68ff5918f07230565ef1` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 164 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkBusOutboxSessionRegistry.cs` | `528fa56cfb60b60dd91ec7794635100efcf93f07c912525731c04313b1dd0aad` |
| Test | `EntityFrameworkReliableMessagingRegistrationTests.cs` | `e895ba42c0f4ab304bc52ffa338573e41fc19eb37e0c4fa1aaab5293a8eb3c19` |

Cumulative personal source admission is 292/4,116 current C# files.

## Proof

The combined owning run contains eighteen discovered cases: six reliable-registration cases and
twelve selector/transactional cases. It covers both registry modes, first creation, sequential cache
hits, concurrent cache hits, ambient consume-context handling and deterministic selector behavior.
The requirement manifest binds the new case to
`concurrent-reliable-session-resolution-has-one-winner`.

Four successfully compiled single-cause mutants were killed and restored: bypass the reliable
cache hit, omit reliable cache publication, bypass the transactional cache hit and omit
transactional cache publication. Two earlier mutation attempts rejected by nullable warning-as-error
compilation are not counted. The registry source was restored byte-for-byte before final gates.

Final combined Cobertura is `/private/tmp/vicione-servicebus-iteration-165-final.cobertura.xml`,
SHA-256 `39e940c1292c948173a8f9e9cfc2d32a937c66d39b15aac85716d6216f534156`.
The registry reports 100% line and branch coverage; maximum complexity and CRAP are both 4. Unit
sorted-display-name SHA-256 is
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.

| Gate | Result |
| --- | --- |
| Combined registry owning tests | 18/18 passed |
| Full EF unit | 248/248 passed |
| Strict Release product/EF/unit/local builds | 0 warnings, 0 errors |
| Product/test format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 4/4 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-165-ef-session-registry-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

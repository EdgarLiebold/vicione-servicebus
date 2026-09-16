# Iteration 162 — EF outbox delivery HA admission

## Result

This packet personally reads the complete EF transactional outbox delivery source (548 lines) and
all three concrete owning reliability, source and telemetry test files (1,113 lines). The review
found one operational defect: claim-concurrency losses and fail-closed foreign-owner rows entered
the general 1,000-attempt exponential retry policy before the existing outer handlers could return
no progress. A single contention or lock-provider invariant failure could therefore occupy the
delivery worker in repeated 3–30 second backoffs.

The retained correction gives foreign-owner rows a private, typed invariant exception and excludes
that exception together with `DbUpdateConcurrencyException` from operational retry. Both paths now
roll back, return `false` within one second and preserve the pending row, empty lock identifier and
message. Other operational exceptions retain the bounded retry policy. Secondary rollback failure
remains suppressed so it cannot mask the primary claim or ownership failure.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 548 | `23356deceb8f286198af78412ff640289c8df691c509843184c7d86e53e3d10d` | `d64af25cc129c980c2d3460428780603b9d3cb81ef1b3041625282f6dd083303` |
| Tests | 3 / 1,113 | `25edce15ee557c81197673d4f1f1a84e8d56429cb9e305c92694ac2811bb0d04` | `24fabd424b9d1cdc7696d8a61f66e9280fcfe06c9b26c979aa89b1c18b85666d` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 161 source/test chains with the corresponding manifest
hash. The manifests contain:

| Kind | File | Content SHA-256 |
| --- | --- | --- |
| Source | `EntityFrameworkTransactionalOutboxSource.cs` | `d5755969e02d070451aa237b6a5b83967a61e9c026838727ac75ffea836e1acf` |
| Test | `BusOutboxDeliveryTelemetryTests.cs` | `2c746bb1071327ae4af830f20d3008204af0889fd8366bc6f1d208f240857f01` |
| Test | `BusOutboxReliabilityStateTests.cs` | `40e8991c9ee2d7e4d7f11928980bb3eade1b8c78ddd50b8ade891ab32d8accf2` |
| Test | `EntityFrameworkTransactionalOutboxSourceTests.cs` | `4408783191dfc8cd8399ea0a8e6fe32368d92cf4d0f0472d659584d473da23a2` |

Cumulative personal source admission is 289/4,116 current C# files.

## Proof

Three new focused cases inject an EF concurrency loss, a deliberately incorrect foreign-row lock
statement and a failing transaction rollback. Assertions prove prompt no-progress results,
unchanged owner/status/lock/message persistence and suppression of the secondary rollback fault.
The existing no-work case also gains the same one-second completion boundary. Three new
requirement-projection entries bind the added methods. The full owning suite additionally proves
constructor validation, typed-bus control, caller cancellation, paging, progress, cleanup,
classification, bounded backoff, quarantine, timeout propagation, send failure and telemetry.

Five compiled single-cause mutants were killed and restored: retry EF concurrency loss, retry an
owner-invariant failure, invert the foreign-owner guard, report concurrency loss as progress and
rethrow a secondary rollback failure. The first two were killed by the one-second deadline; the
remaining three were killed by exact outcome assertions.

Final combined Cobertura is `/private/tmp/vicione-servicebus-iteration-162-final.cobertura.xml`,
SHA-256 `6c74bd247d169b691eb6f2b5631816439582b9fe2281a9ff0b0ec104577ae3a9`.
Every executable source line and every delivery/rollback state-machine line is covered; the sole
zero-hit sequence point is compiler-generated closing brace line 181. The source class reports 100%
line and 97.30% branch coverage. Maximum method/state-machine complexity is 18; with complete line
coverage, maximum method CRAP is 18 and none exceeds 30. Unit sorted-display-name SHA-256 is
`2eaa219ca3139e16588db40f75ff445ce33c3b8adcd3faad4e0c503c1716b905`.

| Gate | Result |
| --- | --- |
| Focused delivery source | 8/8 passed |
| Full EF unit | 242/242 passed |
| Strict Release product/unit/local builds | 0 warnings, 0 errors |
| Product/unit format | Exit 0 |
| Core Release | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 5/5 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-162-ef-outbox-delivery-ha-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

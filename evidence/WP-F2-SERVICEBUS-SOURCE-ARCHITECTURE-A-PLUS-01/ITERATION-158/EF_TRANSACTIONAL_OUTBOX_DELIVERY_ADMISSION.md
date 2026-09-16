# Iteration 158 — EF transactional-outbox delivery admission

## Result

This packet personally reads the EF transactional-outbox delivery source (543 lines) and its three
owning test files (1,017 lines). The source now rejects missing lock providers and undefined
isolation levels at construction, keeps caller cancellation observable, persists bounded retry or
quarantine state, resolves the correct control for default and typed buses, advances paged delivery
durably and removes only completed outboxes. The former delivery loop was decomposed into named
preparation, send, failure and completion steps without changing its transaction boundary.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 1 / 543 | `065cf37a1f09ef8c51244860c8fbf371f1383fb9966ffde37d69221e5a67e372` | `cb795dc5169b7aba55ab073d3f3628eca12e7d3cd262f3929771cedb2c9710c6` |
| Tests | 3 / 1,017 | `5473f9a42b43914491f935fc75a2664faae8635db0eb3649ca8f4e41bf2277fa` | `7e9cc892a10a2271fe7742aab3e7e870d12658d2deb46612a65a1e3f07a5e589` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 157 source/test chains with the corresponding manifest
hash. The source manifest contains
`src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/Outbox/EntityFrameworkTransactionalOutboxSource.cs`
at content hash `cf65e1ae4038752ed1df5f5a9f8bb5a45ce9a34b33fdeecaa90a8cde520d7269`.
The test manifest contains `BusOutboxDeliveryTelemetryTests.cs`,
`BusOutboxReliabilityStateTests.cs` and `EntityFrameworkTransactionalOutboxSourceTests.cs` at
content hashes `2c746bb1071327ae4af830f20d3008204af0889fd8366bc6f1d208f240857f01`,
`40e8991c9ee2d7e4d7f11928980bb3eade1b8c78ddd50b8ade891ab32d8accf2` and
`358a821e648f54dae43696f102e185fa879feb0e1a9288ab4e4794a80bb3df67`.
Cumulative personal source admission is 277/4,659 current C# files.

## Proof

The 27 focused cases exercise all constructor inputs, default- and typed-bus resolution, exact wait
token delegation, pre-cancelled and active-send cancellation, real SQLite selection and paging,
progress persistence, delivered-state cleanup, empty delivery, endpoint timeout tokens, successful
send cleanup, transient retry persistence, permanent and exhausted quarantine, corrupt attempt
counters, corrupt metadata, missing destinations, classifier success/decline/fault/null isolation,
built-in failure classification and capped exponential retry delay. Requirement-projection entries
bind every new test method. Assertions observe persisted rows, message retention/removal, status,
attempts, failure metadata, sequence progress, timestamps and exact cancellation tokens.

Six compiled single-cause mutants were killed and restored:

1. accept a missing lock-statement provider;
2. accept an undefined transaction isolation level;
3. select outboxes with a different bus key;
4. discard the delivered sequence checkpoint;
5. suppress persisted send-failure state; and
6. swallow caller cancellation during an active send.

The final focused Cobertura artifact is
`/private/tmp/vicione-servicebus-iteration-158-final.cobertura.xml`, SHA-256
`6b30f37235f17d8e6f861a6d8d3a63a007326be0ced346675c0a9dd04e022658`.
The hand-written source class has 100% line coverage and 96.55% branch coverage. Its constructor,
classifier, failure transition, retry calculation and both default/typed bus-control branches have
100% line and branch coverage. The generated delivery-loop state machine has 100% line and 94.44%
branch coverage at complexity 18. Residual branches are optional debug/activity/metrics null paths,
the defensive exception-type fallback, transaction rollback failure preservation and unexpected
database/concurrency faults; they do not encode an untested success or state-transition contract.
Maximum owner-method CRAP is 18 and none exceeds 30. Unit sorted-name SHA-256 is
`d2016df8bed8230635ae77fe26372cad21a8b5e5d3ecc9a5840d91dc0babe506`.

| Gate | Result |
| --- | --- |
| Focused owners | 27/27 passed |
| Full EF unit | 225/225 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0 |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race in
one batching integration test; the complete serialized gate is green. Protected `review/**`,
`TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or modified. Intended tag:
`servicebus-a-plus-iteration-158-ef-transactional-outbox-delivery-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

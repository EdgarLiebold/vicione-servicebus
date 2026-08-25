# RabbitMQ address validation

## Frozen subject

- Technical commit: `a3ce3c697634234242473b6428d5e292901985eb`
- Technical tree: `9d02b5144c9ad246e641f7c81e41393f00124ad1`
- Parent: `a0b18ca62578d01a2bce19d7dffd3cd77cfa13d2`
- Branch: `test/servicebus-xunit4-mtp2-a-plus-v2`

The technical commit includes the complete product, test, build, documentation, disposition and
TelemetryMonitor barrier delta. Final execution evidence is a child only; it does not change the
reviewed technical subject.

## Inherited closure

- Retired fixture SHA-256 before deletion: `9264bfe2e2e763ba683b7bd1afc6473a4d3d18a9090105454c7f21751843ba48`
- Frozen R0 ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`
- `INHERITED_BEHAVIOR_DISPOSITION.json` contains exactly 46 unique obligations,
  `OBL-R0-BRK-0215` through `OBL-R0-BRK-0260`, with no gap, duplicate or unrelated ID.
- The old fixture remains byte-recoverable from Git history.

## Stationary positive gates

All commands ran from the repository root with .NET SDK `10.0.302`, locked package graphs, build
servers disabled and MTP's native command form.

| Gate | Result | Raw result |
|---|---:|---|
| Unit solution locked restore | exit 0; graph already current | `final-locked-restore.txt` |
| Serial Engineering Release build | exit 0; 0 warnings; 0 errors | `final-engineering-build.txt` |
| RabbitMQ executable, unfiltered | 94/94 passed; 0 failed; 0 skipped | `final-rabbitmq-test.txt` |
| UnitArchitecture, unfiltered | 1681/1681 passed; 0 failed; 0 skipped | `final-unit-test.txt` |
| LocalIntegration with run-scoped PostgreSQL and Azurite | 17/17 passed; 0 failed; 0 skipped | `final-local-integration-test.txt` |
| Changed-file `dotnet format whitespace --verify-no-changes` | exit 0 | command verdict |
| `git diff --check` | exit 0 | command verdict |
| generated Apache 2.0 section 4(b) change list | 7617/7617 entries matched | command verdict |

The RabbitMQ cohort itself is hermetic and opens no broker, socket or container. The LocalIntegration
run is the repository-wide required profile and uses the canonical fixture runner; it does not stand
in for the address tests.

## Causal full-profile correction

The first complete Unit run found one pre-existing `TelemetryMonitorTests` race, unrelated to the
RabbitMQ product delta. `PostReceive` occurs before `ReceivePipeDispatcher` stops the receive span;
that later stop rearms the one-minute idle timer. The test advanced virtual time between those two
events and then waited for a new minute it never advanced. `ObservableTimeProvider` now exposes a
change-count waiter, and the receive observer registers that waiter inside `PostReceive` before the
span can stop. Tests wait for the exact rearm before advancing time. No product timeout, retry,
sleep, wall clock or acceptance criterion was relaxed. The focused telemetry class passes 4/4 and
the final complete Unit profile passes 1681/1681.

## Negative proof

`MUTATION_MANIFEST.md` binds six one-cause product mutations and one requirement-omission sabotage.
Every run exited `2` for its intended assertion, no run skipped a test, and every modified target was
restored to its baseline SHA-256 before the final build. `SHA256SUMS` binds this report, the manifest,
all raw results and both inherited-analysis artifacts.

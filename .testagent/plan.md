# Execution plan — ViciOne.ServiceBus test reconstruction

Lead plan SHA-256 `6a4dd0237d574d264548d8a9d03c7a9d6f5ff6a11249354b58a8ff2d02e43776` — the sole target-architecture contract. This file is the team's
execution plan only: ownership, sequence, commands, mappings, blockers. It changes no architecture.

Frozen research result: `evidence/WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11/R0/R0_FROZEN_RESULT.md`.

## 1. Exact commands

Measured, not quoted — see `MTP_COMMAND_FORM_PROBE.md`.

| Purpose | Command |
|---|---|
| Profile run | `dotnet test --solution ViciOne.ServiceBus.Tests.<Profile>.slnx --minimum-expected-tests <N>` |
| Single project | `dotnet test --project <path>.csproj <MTP arguments>` |
| Machine-readable result | `--report-trx` (never `--logger trx`) |
| Filters | `--filter-class` / `--filter-method` / `--filter-trait` (never `--filter "ClassName=…"`) |
| Crash / hang | `--blame-crash`, `--blame-hang-timeout` |

MTP arguments are passed **directly**, with no `--` separator and no positional path. This requires
`"test": { "runner": "Microsoft.Testing.Platform" }` in `global.json` — the open blocker in §5.

## 2. Wave sequence and ownership

Product Owner priority: everything locally provable is finished to A+ first; cloud follows.

| Wave | Content | Profile | Entries |
|---|---|---|---:|
| F1 | central packages and MSBuild, `ViciOne.ServiceBus.Testing`, `…Testing.Xunit`, sentinel, profile solutions, architecture test project | — | — |
| C1 | Abstractions, Core/InMemory, MessagePack, Analyzers, Visualizer, SignalR, benchmark correctness, 44 diagnostics obligations | UnitArchitecture | 2766 |
| C2 | EF Core, Quartz, PostgreSQL, SQL Server | LocalIntegration | part of 778 |
| C3 | RabbitMQ, ActiveMQ, local emulators | LocalIntegration | rest of 778 |
| C4a | Azure Service Bus, Event Hubs, Storage, Table, SQS, S3, DynamoDB — written and emulator-proven | External | 128 |
| C4b | the runs against real short-lived resources | External | waits for access |
| F2 | TestFramework removal after terminal disposition of all 147 files | — | — |
| P1 | promotion of `tests2` to `tests` | — | requires C4b |
| G1 | freeze | — | requires P1 |
| G2 | independent review | — | — |

Each wave: complete owner reading → ledger projection bound by the Lead → new test code → focused
MTP run → assertion and gap review → two independent internal read-only reviews → only then the old
cohort is removed.

## 3. Internal agents

Up to eight concurrent, each with a closed collision-free task; one integrator owns every shared
file; writers get disjoint target projects; at least two agents review read-only — one for semantic
loss and false green, one for structure, dependencies, determinism and test quality. R0 used twelve
sequentially-scheduled read-only cohorts under this rule with zero id collisions and zero tracked
modifications.

## 4. Design decisions already fixed by measurement

- The sentinel is an assembly fixture failing in cleanup; the obligation marker is read at
  invocation through `BeforeAfterTestAttribute`. Extension project references
  `xunit.v3.extensibility.core` only.
- `RequireCompleteObligationSet` provenance follows the `RestoreLockedModeFromCommandLine` shape;
  a project cannot grant itself the flag.
- The nested `Directory.Build.*` import the parent unconditionally; an `Exists` guard is fail-open
  and forbidden.
- The new support projects do not take the product namespace `ViciOne.ServiceBus.Testing`, which 61
  files in five shipped assemblies already populate with 62 type names.
- Broker fixtures own one broker per owning fixture; inherited assembly-level serialization is not
  carried over as the isolation mechanism.

## 5. Blockers

| # | Blocker | Effect |
|---|---|---|
| B-1 | `global.json` is outside the write scope | no release-relevant profile run is possible; blocks F1 |
| B-2 | no disposition token for structurally unprovable service behaviour | two obligations cannot reach a terminal state |
| B-3 | a retained Python engineering tool may keep neither its Python self-tests nor gain a native owner | leaves it unproven either way |
| B-4 | real cloud access absent | C4b, P1, G1 wait by design |

## 6. Requirement to evidence

The final `Requirement | Evidence` table quoting every bound Product Owner requirement verbatim is
maintained in `.testagent/status.md` and completed before the freeze.

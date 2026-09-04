# plan — directive 0111 correction round

Each checklist item from `research.md` maps to a concrete carrier or an explicit blocker. Phases run in
parallel across disjoint path ownership; only the integrator touches shared files and commits.

## Ownership (published before any writer started)

| Owner | Exclusive write scope |
|---|---|
| integrator | `Directory.Packages.props`, every `*.csproj`, `*.slnx`, `Directory.Build.*`, every `packages.lock.json`, `evidence/**`, git |
| writer A | `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests/**/*.cs` |
| writer B | `tests2/Testing/ViciOne.ServiceBus.Testing.Containers{,.Tests}/**/*.cs` |
| writer C | `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests/**/*.cs` |
| writer D | `tests2/Core/ViciOne.ServiceBus.Tests/**/*.cs`, `tests2/Testing/ViciOne.ServiceBus.Testing.Contracts/**/*.cs` |
| writer E | `tests2/Serialization/**/*.cs`, `tests2/Testing/ViciOne.ServiceBus.Testing/**/*.cs` |
| reviewers | read-only, no repository writes |

No two writers own a file. Every writer reports required foreign-file changes to the integrator
verbatim instead of editing them.

## Item → carrier

| # | Checklist item | Carrier | Status |
|---|---|---|---|
| 1 | §2.1 ArchUnitNET core API | A, plus integrator for the package | package done; code with A |
| 2 | §2.2 one entry package per application | integrator | **done and measured** |
| 3 | §2.3 test logger out of the product graph + boundary rule | integrator (package) + A (rule) | package done; rule with A |
| 4 | §2.4 isolated fixture for the evaluated-reading control | A | with A |
| 5 | F-0111-01 shell probe deleted, small C# case instead | integrator (deletion) + B (case) | with B |
| 6 | F-0111-02 explicit enable, no module initializer, zero warnings | B | with B |
| 7 | F-0111-03 `Lazy<T>` directly, wrapper and its tests deleted | D | with D |
| 8 | F-0111-04 release support internal, eight properties proven | B | with B |
| 9 | F-0111-05 internal creation seams, five branch proofs | B (seams and Docker-free proofs), C (wiring unchanged) | with B, C |
| 10 | F-0111-06 tautological assertion removed | C | with C |
| 11 | §5.1–5.5 architecture gates | A (rules, helper, concurrency, pack, fixtures), B (delete the second graph reader) | with A, B |
| 12 | §6 assertion and test-code quality over every file | A, B, C, D, E each for their own scope | with all |
| 13 | §7 two reviewers, one commit, binlogs, final run | integrator | after integration |

## What item 2 actually measured, and the correction it forced

The previous record claimed, as a measurement, that the central `xunit.v3.extensibility.core` pin had
to stay because removing it failed a locked restore with NU1004. **That measurement was wrong, and the
Lead named the reason.** The locked restore failed against a lock file that still recorded the old
`CentralTransitive` entry — a stale state, not a live consumer.

Measured correctly, in the order the directive prescribes: remove the pin → ordinary restore that
regenerates the locks → prove locked restore. Both steps report zero errors. The pin is gone.

## Deliberate non-goals

- No product behaviour under `src/**`, no tracked change under `tests/**`, no baseline tag, no remote
  branch. Product lock-file changes caused solely by removing test tooling from the product restore
  graph are authorized and are the only `src/`-adjacent change.
- No second verification mechanism, no permanent mutation infrastructure, no coverage tooling.
- No new external dependency.
- Counts are not a goal. F-0111-03 removes three cases on purpose; retaining them to keep 319 as a
  number is explicitly forbidden.

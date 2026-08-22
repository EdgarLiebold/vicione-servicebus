# C11 TaskExecutor mutation and acceptance evidence

Date: 2026-08-23

Technical commit: `e87b7ac25945609895c54347583a6265764278fa`
Technical tree: `3681da9c3b2d8990bc3886e202ab64d200848b4f`

## Scope

This source-owner cohort replaces the two inherited assertion-free `TaskExecutor_Specs` cases and
closes the constructor gap recorded as `OBL-R0-CORE-D-0474`. The replacement is ordinary xUnit 4
code executed by Microsoft Testing Platform 2. It does not use the inherited TestFramework, delays,
polling, random input, skips, environment resources, or an auxiliary policy runner.

The accepted source hashes before the technical commit are:

| Path | SHA-256 |
| --- | --- |
| `src/ViciOne.ServiceBus/Util/TaskExecutor.cs` | `111716b8a4b32932212b713204827121f0d10b299eaf4037d74afb31d63d9912` |
| `tests2/ViciOne.ServiceBus.Tests/Util/TaskExecutorTests.cs` | `a7115277ab092701439b692e78759d1a7d26e0c694fc93c15c848009ad3a1588` |
| `tests2/ViciOne.ServiceBus.Tests/Requirements/CoreRequirements.json` | `6c0a08145f3cfad57105414ed7bb886bff2a9cf5cce344efed1c488d55a7ce6f` |
| `evidence/native-tests/task-executor/INHERITED_BEHAVIOR_DISPOSITION.json` | `423adcb6a9652aa509243b41a5943489308642e089c2e6b880c940f19cff9974` |

## Behavior and product corrections

- Action, Task, ValueTask, synchronous-result, Task-result, and ValueTask-result delegates complete
  with their observable effect or exact result.
- ValueTask delegates have explicit, non-ambiguous entry points. The inherited call bound to the
  generic synchronous overload and awaited only `Task<ValueTask>`, so it did not prove completion of
  the ValueTask body.
- Concurrency, bounded backpressure, queue-admission cancellation, queued-work cancellation, and
  delegate cancellation have deterministic coordination and bounded cleanup.
- Delegate faults remain unwrapped. Push returns after admission, while disposal drains accepted
  work. Disposal is idempotent and later submissions receive a stable `ObjectDisposedException`.
- Both constructor limits and every public delegate parameter have explicit validation.

## False-green attacks

Each accepted mutation changed one behavior, built successfully, failed the responsible test, and
was then removed before the next mutation.

| Mutation | Observed verdict |
| --- | --- |
| Complete non-generic ValueTask work before awaiting its delegate | 2 failed, 13 passed; both ValueTask completion contracts rejected it |
| Remove the positive `prefetchCount` guard | 1 failed, 14 passed; the exact constructor boundary rejected it |
| Remove an executing item from a bounded single-reader channel before its work completes | 1 failed, 14 passed; bounded backpressure rejected it |
| Deliver queued cancellation as an ordinary `InvalidOperationException` | 1 failed, 14 passed; the cancellation contract rejected it |
| Leak `ChannelClosedException` after disposal | 1 failed, 14 passed; the stable disposal boundary rejected it |
| Remove the Action requirement attribute | 1 failed, 195 passed; the compiled requirement projection rejected it |

One proposed mutation was deliberately excluded from the evidence: setting an internal completion
source to a faulted `OperationCanceledException` is normalized by the outer C# async Task to the same
public canceled state. It is observably equivalent at this API boundary and therefore is not claimed
as a killed mutant.

## Final acceptance

| Gate | Result |
| --- | --- |
| Bounded `dotnet format --verify-no-changes` for product and test files | exit 0 |
| Core-owner Release build | exit 0; 0 warnings; 0 errors |
| Focused `TaskExecutorTests` run | 17 total; 17 passed; 0 failed; 0 skipped |
| Complete core-owner run | 198 total; 198 passed; 0 failed; 0 skipped |
| UnitArchitecture Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered UnitArchitecture run | 706 total; 706 passed; 0 failed; 0 skipped |
| Engineering Release build | exit 0; 0 warnings; 0 errors |
| JSON parsing, diff whitespace, forbidden delay/random/skip scan | pass |

The inherited file is removed only in this accepted state, after all three owned ledger obligations
have terminal dispositions. The separate `ChannelExecutor` consolidation remains an explicit product
task in `TODO.md`; no capability is silently removed in this cohort.

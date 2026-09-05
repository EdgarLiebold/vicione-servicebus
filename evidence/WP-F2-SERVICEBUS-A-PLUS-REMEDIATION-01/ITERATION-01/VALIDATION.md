# A+ remediation iteration 1 validation

Date: 2026-09-06

## Scope

This iteration removes confirmed runtime defects and silent public contracts while preserving supported features:

- exact Entity Framework durable-send abandonment timestamps;
- complete receiver-configuration forwarding;
- effective in-memory auto-start and endpoint-registration inclusion;
- immutable composite-filter collections with corrected exclusion naming;
- working Azure Service Bus message-session saga queries and cancellable state writes;
- end-to-end job lifecycle cancellation propagation; and
- an immutable, thread-safe static `NewId` façade while retaining explicit custom-generator construction.

## Behavioral evidence

Each correction was introduced from a failing test against the secured baseline. Focused owner tests then passed after implementation. The tests assert persisted state after a fresh Entity Framework context, downstream configuration effects, include/exclude truth tables, Azure saga predicate and identity semantics, exact cancellation-token identity, and parallel identifier uniqueness.

Nine isolated mutation groups were killed and restored byte-for-byte. The mutations removed or inverted one corrected behavior at a time: timestamp persistence, receiver forwarding, auto-start propagation, endpoint inclusion, composite exclusion, Azure query filtering, Azure state-write cancellation, job notification cancellation, and the prohibition on public global `NewId` mutation.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,728 passed, 0 failed, 0 skipped across 21 assemblies |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |

Microsoft Testing Platform and Roslyn BuildHost require local inter-process sockets. In the workspace sandbox they fail with `SocketException: Permission denied`; the validated procedure therefore uses an isolated `DOTNET_CLI_HOME`, the existing local NuGet cache, serial MSBuild without shared compilation, and runs socket-dependent test/format hosts outside the sandbox. A naive solution-wide parallel test invocation was stopped after it retained numerous MSBuild nodes; the structured solution/profile execution above is the reproducible path.

## Review status

This is internal engineering evidence, not an independent external or Red Team acceptance. Iteration 1 is complete; remaining A+ work is tracked in the deep-review report and subsequent iteration plans.

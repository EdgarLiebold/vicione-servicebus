# C32 validation — kill switch

Date: 2026-08-25

## Accepted build gates

- bounded Core project Release build, serial, no restore, no incremental build, build servers
  disabled, binlog captured: exit 0, 0 warnings, 0 errors, 25.33 seconds;
- Architecture project Release build under the same constraints: exit 0, 0 warnings, 0 errors,
  26.31 seconds;
- complete `ViciOne.ServiceBus.Engineering.slnx` Release build under the same constraints: exit 0,
  0 warnings, 0 errors, 1 minute 31.88 seconds.

## Accepted native MTP gates

| Surface | Passed | Failed | Skipped |
|---|---:|---:|---:|
| C32 configuration | 7 | 0 | 0 |
| C32 lifecycle and InMemory integration | 19 | 0 | 0 |
| Core owner | 828 | 0 | 0 |
| Architecture | 84 | 0 | 0 |
| Tests.Infrastructure | 67 | 0 | 0 |
| Roslyn test infrastructure | 4 | 0 | 0 |
| Abstractions | 235 | 0 | 0 |
| Analyzers | 114 | 0 | 0 |
| Analyzer code fixes | 29 | 0 | 0 |
| MessagePack | 54 | 0 | 0 |
| SignalR | 32 | 0 | 0 |
| StateMachineVisualizer | 8 | 0 | 0 |
| **UnitArchitecture total** | **1455** | **0** | **0** |
| LocalIntegration | 3 | 0 | 0 |

All native processes used the canonical profile, the predeclared minimum count and an unfiltered
process verdict for their assembly. The architecture process needed 35.599 seconds; an exploratory
30-second process timeout therefore aborted it after 74 already-passing cases and was not counted as
an accepted gate. The repeated run with a 120-second process budget passed 84/84.

One exploratory default-parallel no-incremental Unit solution build terminated after 15:29 with
exit 1 while reporting 0 warnings and 0 errors. It had no binlog and is not acceptance evidence.
The bounded builds and the complete serial Engineering build above isolate this as a local MSBuild
orchestration failure rather than a source failure.

Static closure is also green: 26 distinct kill-switch requirement rows, no stale public runtime
state names or obsolete threshold/delay APIs in active product/test code, all three replaced Core
fixtures absent, both real-broker fixtures retained, and `git diff --check` clean.

Result: PASS.

# C37 InMemory Outbox Fault — Validation

## Completed before the final profile

- Focused restored test: 1 total, 1 passed, 0 failed, 0 skipped.
- Forced non-incremental Release build after mutation restoration: 0 warnings, 0 errors.
- Requirement projection JSON parses successfully.
- The native test contains no `Task.Delay`, `Thread.Sleep`, `Stopwatch`, process-time oracle or
  detached `Task.Run`.
- `git diff --check` passes.
- The inherited `tests/ViciOne.ServiceBus.Tests/Outbox_Specs.cs` is removed only after the restored
  focused test passed.

## Final profile

- Serial Unit Release build: exit 0, 0 warnings, 0 errors.
- Complete serial Engineering Release build: exit 0, 0 warnings, 0 errors.
- Unfiltered UnitArchitecture profile: 1471 total, 1471 passed, 0 failed, 0 skipped.
- Unfiltered LocalIntegration profile: 3 total, 3 passed, 0 failed, 0 skipped.
- The enforced floor is 1471 in `README.md`, `docs/build.md`, the native workflow and the active
  execution plan.

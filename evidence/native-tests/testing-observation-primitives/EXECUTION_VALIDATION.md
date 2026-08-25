# Testing Observation Primitives — Execution Validation

Date: 2026-08-24

## Stationary product state

Both temporary process-time mutants were removed before the final build. The final source uses the
injected provider for the asynchronous list deadline, synchronous list deadline and inactivity
interval. Existing constructors delegate to provider-aware overloads with `TimeProvider.System`.

## Final commands

```text
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore --disable-build-servers --no-incremental -v:minimal
```

Exit 0: 0 warnings and 0 errors.

```text
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/unit-architecture --minimum-expected-tests 954 --max-parallel-test-modules 1
```

Exit 0: 954 total, 954 passed, 0 failed, 0 skipped.

```text
dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-build --no-restore --results-directory artifacts/test-results/core --minimum-expected-tests 429
```

Exit 0: 429 total, 429 passed, 0 failed, 0 skipped.

```text
dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-restore --disable-build-servers --no-incremental -v:minimal
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/local-integration --minimum-expected-tests 3 --max-parallel-test-modules 1
```

Both exit 0: build 0 warnings and 0 errors; tests 3 total, 3 passed, 0 failed, 0
skipped.

Two bounded project-specific `dotnet format ... whitespace --verify-no-changes` runs exited 0 for all
nine changed product and native test C# files; they formatted 0 of 1,977 product files and 0 of 93
test files. An earlier solution-level sandboxed attempt could not create Roslyn's local named pipe
and therefore did not inspect formatting. The two authoritative reruns used the same checked-out
bytes outside that sandbox and loaded their exact owning projects without workspace warnings. This
was an execution-environment restriction, not a code or test failure.

## Structural checks

- `git diff --check`: exit 0.
- Both disposition JSON files and `CoreRequirements.json`: valid JSON.
- No `Thread.Sleep`, real `Task.Delay`, stopwatch threshold, polling loop, skipped test or custom
  product clock exists in the cohort.
- `tests/ViciOne.ServiceBus.Tests/Testing/AsyncMessageList_Specs.cs` and
  `tests/ViciOne.ServiceBus.Tests/Testing/InactivityObserver_Specs.cs` are absent.
- No empty inherited directory was left behind.

Verdict: **PASS**.

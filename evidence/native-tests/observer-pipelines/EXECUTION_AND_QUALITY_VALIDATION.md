# Observer and message-flow execution and quality validation

## Release verification

All commands ran from the repository root with .NET SDK 10.0.302. No test was filtered in either
final profile.

```text
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore --results-directory artifacts/test-results/unit-observer-pipelines \
  --minimum-expected-tests 1063 --max-parallel-test-modules 1
exit 0; total 1063; passed 1063; failed 0; skipped 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-restore --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore \
  --results-directory artifacts/test-results/local-integration-observer-pipelines \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
exit 0; total 3; passed 3; failed 0; skipped 0
```

The complete affected executable projects also pass independently:

```text
ViciOne.ServiceBus.Tests: total 533; passed 533; failed 0; skipped 0
ViciOne.ServiceBus.Abstractions.Tests: total 139; passed 139; failed 0; skipped 0
```

## Test-quality review

- The full changed test code was read line by line after implementation and after the parallel-run
  correction.
- Bounded Roslyn whitespace verification passes for every changed test file.
- `git diff --check` passes.
- Both changed requirement projections parse as JSON and the native architecture gate verifies
  their compiled passive metadata.
- The cohort contains no skip, sleep, real delay, process-clock oracle, random input, unbounded
  polling, inherited fixture, shared running harness or assertion-free fact.
- Assertions bind external message values, addresses, callback stages, identities, type names,
  exceptions, counts and negative boundaries rather than restating the implementation.
- The receive tests select the target delivery by exact `ReceiveContext`, preventing unrelated
  parallel bus callbacks from creating either false failures or false passes.
- Eight one-cause product mutations were rejected and their complete restoration was hash-checked.

The inherited tests remain available in Git history. Their live files were deleted only after the
terminal 27/27 disposition, focused verification, full profiles, static review and mutations passed.

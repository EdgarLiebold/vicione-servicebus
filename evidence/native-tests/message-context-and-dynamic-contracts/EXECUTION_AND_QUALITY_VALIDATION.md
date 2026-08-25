# Message contexts and dynamic contracts execution and quality validation

## Release verification

All commands used .NET SDK 10.0.302, locked dependencies, Release output, native xUnit 4/MTP 2
execution, and zero skips.

```text
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore \
  --results-directory artifacts/test-results/unit-message-context-dynamic-contracts-final-timeprovider \
  --minimum-expected-tests 1104 --max-parallel-test-modules 1
exit 0; total 1104; passed 1104; failed 0; skipped 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-restore --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore \
  --results-directory artifacts/test-results/local-integration-message-context-dynamic-contracts-final \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
exit 0; total 3; passed 3; failed 0; skipped 0

dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  --configuration Release --no-restore --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0
```

The affected Core executable passes independently with 574/574 tests and no skips. The bounded
Roslyn format/analyzer checks pass for both product files and all three new test files.

## Test-quality review

- All four inherited fixtures, all 30 ledger rows, every replacement file, the connected product
  paths, and relevant source history were read before closure.
- Inherited tests are a minimum only. The cohort adds source-derived positive, negative, boundary,
  concurrency, causation, cancellation, metadata, and invalid-contract behavior.
- Tests use exact external oracles and real in-memory transport/serializer paths. They contain no
  sleep, blocking wait, wall-clock tolerance, random input, inherited fixture, or assertion-free body.
- Eight one-cause product mutations were rejected, and every mutated file was restored after each
  probe; the recorded final hashes include the later bounded formatting and time-source cleanup.
- The inherited-ledger comparison reports exactly 30 expected and 30 disposed unique ids, with no
  missing or unexpected id. Both requirement JSON parsing and the full projection tests pass.
- `git diff --check` is clean for the complete cohort. No cohort test contains a skip, sleep, delay,
  blocking wait, wall-clock dependency, random oracle, or inherited fixture.
- The remaining inherited NUnit project still builds with zero warnings/errors after removal. No
  empty directory remains under `tests`.

The inherited source bytes remain recoverable from Git history. Their four live files are removed
only after terminal 30/30 disposition, full profile execution, static quality review, and mutation
review.

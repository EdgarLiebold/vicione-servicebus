# Message identity, conversation, time, and header execution and quality validation

## Release verification

All commands ran from the repository root with .NET SDK 10.0.302. Final profile runs were unfiltered.

```text
dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore \
  --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release \
  --no-build --no-restore \
  --results-directory artifacts/test-results/unit-message-identity-headers \
  --minimum-expected-tests 1080 --max-parallel-test-modules 1
exit 0; total 1080; passed 1080; failed 0; skipped 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet build ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-restore --no-incremental --disable-build-servers -m:1
exit 0; warnings 0; errors 0

VICIONE_TESTS__Profile=LocalIntegration \
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release \
  --no-build --no-restore \
  --results-directory artifacts/test-results/local-integration-message-identity-headers \
  --minimum-expected-tests 3 --max-parallel-test-modules 1
exit 0; total 3; passed 3; failed 0; skipped 0
```

The affected Core executable passes independently with 550/550 tests and no skips.

## Test-quality review

- All five inherited files, all replacement files, the complete connected product paths, and the
  relevant source history were read before closure.
- The cohort adds 17 source-derived facts for 15 inherited obligations; inherited tests are only the
  minimum behavior evidence.
- Bounded Roslyn format/analyzer verification passes for all changed C# files.
- `git diff --check` and both JSON parses pass.
- No cohort test contains a skip, sleep, delay, wall-clock tolerance, random input, inherited fixture,
  shared running harness, assertion-free body, or wait-based negative assertion.
- Assertions bind exact values, identities, priorities, headers, addresses, timestamps, casing,
  mutation boundaries, and negative states.
- Eleven one-cause behavior mutations were rejected. Every product file was restored and hash-checked.

The inherited source bytes remain recoverable from Git history. Their live files are removed only
after terminal 15/15 disposition, full profile execution, static quality review, and mutation review.

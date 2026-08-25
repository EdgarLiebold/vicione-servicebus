# Testing Harnesses — Execution Validation

Date: 2026-08-24

## Stationary product state

The temporary `SendQuery` bypass mutant was removed before the final build. The restored SHA-256 of
`src/ViciOne.ServiceBus/Testing/TestSagaRepositoryDecorator.cs` is
`48d5b7c6a551e15c99852541e1bb4d2edb03008d8951fe88c1044bab2fac319a`, exactly the value captured
before the mutation.

The four DI-utility mutants were removed before the final build. Final SHA-256 values are
`1704d5d2aee174ac1c75cdc541eb351eae24f1feab997ae35ca994aa537dd70f` for
`TestingServiceProviderExtensions.cs`,
`855fb6ff04cedaf2a71600419a349b621e5c7010df4f2762f23757efe12a0eef` for
`ServiceProviderTestExtensions.cs`, and
`7f474c8dcde569f762a6f0922faaaaae10904d3d154000d71cf0cfa020b7a78b` for
`TransportBusInstance.cs`.

## Final commands

```text
env MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore --no-incremental --disable-build-servers -m:1 -v:minimal
```

Exit 0: 0 warnings and 0 errors.

```text
env DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/unit-final-1046 --minimum-expected-tests 1046 --max-parallel-test-modules 1
```

Exit 0: 1,046 total, 1,046 passed, 0 failed, 0 skipped.

```text
env VICIONE_TESTS__Profile=LocalIntegration DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/local-integration-final-1046 --minimum-expected-tests 3 --max-parallel-test-modules 1
```

Exit 0: 3 total, 3 passed, 0 failed, 0 skipped.

The query-correlated saga class was also run directly from its MTP executable after the final
restore: 5 total, 5 passed, 0 failed, 0 skipped. The System.Text.Json `DateOnly`/`TimeOnly` cohort was
run directly: 4 total, 4 passed, 0 failed, 0 skipped. The consume-observer and recorded-message
classes each passed 3/3, while the expanded message-filter class passed 6/6.

## Structural checks

- `git diff --check`: exit 0.
- `CoreRequirements.json` and all cohort disposition files parse as JSON.
- The new query test and its passive requirement row form one exact identity pair.
- No skipped or tautological test, `Thread.Sleep`, ambient product-clock read, or real delay exists
  in the new harness and temporal serializer scope.
- The four replaced DI/container fixtures, the temporal serializer fixture, the inherited consume-
observer fixture, and all nine replaced inherited `Testing` fixtures are absent.
- No empty inherited test directory remains.
- The message-observation-list class passes 10/10 and covers every public query shape, exact observer
  success/fault records, shared-list identity boundaries, extension semantics, constructor
  validation and endpoint-local publication observation from a real dynamically connected receive
  endpoint. Six independent product mutations are rejected for their intended reasons.
- The five DI-utility and dynamic-endpoint facts pass together with the complete 105-test Testing
  namespace. They cover exact filtered publication, required arguments, virtual readiness timeout,
  ordered task registrations, both dynamic connector overloads, both global endpoint-callback forms,
  real registration-context services, separate delivery and causation. Two consecutive namespace
  runs passed 105/105 after correcting the independently discovered retry-observer race.

Verdict: **PASS**.

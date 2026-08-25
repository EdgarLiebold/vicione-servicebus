# Circuit Breaker completion evidence

## Subject

- Technical commit: `a9cb55ea8c991850ac0725cd3d4fd12061fe9958`
- Technical tree: `0c40d7b0d4fcd04ceb735e2001312812250ada84`
- Configuration: `Release`
- Runtime: pinned repository .NET SDK and native Microsoft Testing Platform
- Result: PASS

The mutations below were applied one at a time. Each mutated test command exited with code `2`, for the named behavioral reason. The mutation was then reverted before the next case. `MUTATION_MANIFEST.md` binds the exact target, baseline and post-restore hash, mutant hash and fully expanded command for every case. The final SHA-256 values of all touched source or test files equal their values at the technical commit.

## Mutation command

Unless a row says `class`, every case used this exact command shape, with the fully qualified method from the `Filter` column:

```text
dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method <Filter>
```

Standard output and standard error were captured together in the listed raw result file.

## Product mutations

| ID | Single mutation | Filter | Expected and observed cause | Raw result |
|---|---|---|---|---|
| M01 | Change inclusive trip boundary from `attempts < MinimumThroughput` to `attempts <= MinimumThroughput`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ExactMinimumThroughputAndFailureRatio_OpenTheCircuitInclusively` | The exact threshold no longer opens; the expected rejection is absent. | `mutations/M01-throughput-boundary.txt` |
| M02 | Change open expiry from `elapsed < duration` to `elapsed <= duration`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.OpenCircuit_RejectsBeforeTheProtectedPipeWithADistinctFailure` | The exact-duration recovery admission remains rejected. | `mutations/M02-open-time-boundary.txt` |
| M03 | Ignore the Open-to-HalfOpen compare-exchange result. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.HalfOpen_AdmitsExactlyOneProbeAndRejectsEveryConcurrentCompetitor` | More than one contender reaches the protected pipe. | `mutations/M03-half-open-cas-ownership.txt` |
| M04 | Stop advancing the break-duration index after a failed probe. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ClassifiedProbeFailure_EscalatesTheBreakDurationAndSuccessResetsIt` | The expected escalated duration differs. | `mutations/M04-backoff-progression.txt` |
| M05 | Treat every `OperationCanceledException` as caller cancellation without requiring an actually cancelled context token. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.MatchingButNotCanceledContextToken_IsAClassifiedDependencyFailure` | A dependency cancellation is incorrectly released without reopening. | `mutations/M05-cancellation-causality.txt` |
| M06 | Close Half-open on an unclassified exception instead of releasing it without a verdict. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.FailureOutsideTheConfiguredFilter_ReleasesTheProbeWithoutClaimingRecovery` | The next call is admitted instead of proving Half-open remains. | `mutations/M06-no-verdict-release.txt` |
| M07 | Remove the product version from the circuit-breaker `Meter`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals` | All measured meter-version assertions fail. | `mutations/M07-otel-source-version.txt` |
| M08 | Omit Half-open release when the user exception classifier throws. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ExceptionClassifierFailure_ReleasesTheProbeAndPropagatesTheExactFailure` | The next attempt is rejected as `probe_in_progress`. | `mutations/M08-classifier-failure-release.txt` |
| M09 | Re-throw a rejection telemetry-observer exception. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ThrowingOpenTelemetryObservers_CannotChangeCircuitStateOrFailures` | The observer exception replaces the circuit rejection. | `mutations/M09-observer-isolation.txt` |
| M10 | Publish the live exception filter instead of a detached snapshot. | `ViciOne.ServiceBus.Tests.Configuration.CircuitBreakerOptionsTests.RetainedExceptionConfigurator_CannotMutateThePublishedFilterSnapshot` | Later builder mutation changes the published runtime filter. | `mutations/M10-retained-builder-snapshot.txt` |
| M11 | Retain the caller-owned exception-type array instead of cloning it. | `ViciOne.ServiceBus.Tests.Configuration.CircuitBreakerOptionsTests.CallerOwnedExceptionTypeArray_CannotMutateThePublishedFilterSnapshot` | Later array mutation changes the published runtime filter. | `mutations/M11-caller-owned-type-array.txt` |
| M12 | Replace the `open` rejection reason with `invalid`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals` | The exact OTel metric/activity tag multiset differs. | `mutations/M12-open-rejection-reason.txt` |
| M13 | Replace the `probe_in_progress` rejection reason with `invalid`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals` | The exact OTel metric/activity tag multiset differs. | `mutations/M13-probe-in-progress-rejection-reason.txt` |

## Test-setup sabotage

| ID | Single sabotage | Filter | Expected and observed cause | Raw result |
|---|---|---|---|---|
| M14 | Change the composed test pipeline from `UseConcurrencyLimit(2)` to `UseConcurrencyLimit(3)`. | `ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.RetryCircuitBreakerAndConcurrencyLimit_ComposeWithoutChangingTheirOwnership` | Observed maximum concurrency becomes three instead of two. | `mutations/M14-concurrency-test-setup.txt` |

M14 is intentionally classified as a test-setup sabotage, not as a product mutation.

## Restored source hashes

```text
27e33680886d8b795f5c3250cdb30e89495b7cb7e735003b9b5394788d231f37  src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerStateMachine.cs
a1f15fc30226a754eef66368cd7f36270ce96da3512b3b609d4624e79fb4fb35  src/ViciOne.ServiceBus/Middleware/CircuitBreakerFilter.cs
9ed9f1d9474468f5bf3fbbe4e7eed6f55d3ca48e5d4adee3366b1c421c62775a  src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerTelemetry.cs
b004f5e95280b66efc68bf9f2597f82034c3f008d7a39fbe6c0525119cd5d86d  src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ExceptionSpecification.cs
fe84d0267f88aba3a0a894bd860e2d81a09e382538405a9fa8664292d0ce81e6  src/ViciOne.ServiceBus.Abstractions/Middleware/ExceptionFilters/CompositeFilter.cs
ea0f04e5b10c3b2b5c5e746860009ac602c8e5b6777f12e979e10c28fbfbcb97  src/ViciOne.ServiceBus.Abstractions/Middleware/ExceptionFilters/CompositePredicate.cs
18ea1afdbbc50f677b3a91419b669c687e3f49c4b8960acaa2864e4e0e928be1  tests2/ViciOne.ServiceBus.Tests/Middleware/CircuitBreaker/CircuitBreakerFilterTests.cs
```

## Final green commands

```text
env MSBUILDDISABLENODEREUSE=1 dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers -m:1 /bl:/private/tmp/vsb-circuit-final-engineering.binlog
dotnet test /bl:/private/tmp/vsb-circuit-final-unit.binlog --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/unit --minimum-expected-tests 1494 --max-parallel-test-modules 1
env VICIONE_TESTS__Profile=LocalIntegration dotnet test /bl:/private/tmp/vsb-circuit-final-local.binlog --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx --configuration Release --no-build --no-restore --results-directory artifacts/test-results/local-integration --minimum-expected-tests 3 --max-parallel-test-modules 1
```

| Gate | Result | Raw result |
|---|---|---|
| Entire Engineering solution Release build | Exit 0; 0 warnings; 0 errors | `final-engineering-build-serial.txt` |
| Unfiltered UnitArchitecture profile | Exit 0; 1494 passed; 0 failed; 0 skipped | `final-unit-test.txt` |
| Unfiltered LocalIntegration profile | Exit 0; 3 passed; 0 failed; 0 skipped | `final-local-integration-test.txt` |

The first parallel full-graph diagnostic stalled in MSBuild project-reference resolution for the benchmark project and was terminated after roughly ten minutes with zero reported warnings and errors. Replaying its binlog isolated `_GetProjectReferenceTargetFrameworkProperties`; the same complete graph then passed serially in 1m18s. This is an engineering-build parallelism issue, not a circuit-breaker product or test failure.

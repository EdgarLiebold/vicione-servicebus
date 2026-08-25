# Circuit Breaker mutation manifest

## Binding

- Technical commit: `a9cb55ea8c991850ac0725cd3d4fd12061fe9958`
- Technical tree: `0c40d7b0d4fcd04ceb735e2001312812250ada84`
- Every command below was executed from the repository root.
- Standard output and standard error were captured together in the named raw result.
- Every command exited with code `2` because the named test rejected the single mutation.
- Every target was restored immediately after its command; its post-restore hash equals its baseline hash.

## M01 — inclusive throughput boundary

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerStateMachine.cs`
- Baseline/post-restore SHA-256: `27e33680886d8b795f5c3250cdb30e89495b7cb7e735003b9b5394788d231f37`
- Mutation: `attempts < _settings.MinimumThroughput` → `attempts <= _settings.MinimumThroughput`
- Mutant SHA-256: `933dfb4ae0e1c27059d7a3ff8c3a5a03d40716c204341c5d9d6e804f63c34b74`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m01.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ExactMinimumThroughputAndFailureRatio_OpenTheCircuitInclusively`
- Raw result: `mutations/M01-throughput-boundary.txt`

## M02 — exact open-time expiry

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerStateMachine.cs`
- Baseline/post-restore SHA-256: `27e33680886d8b795f5c3250cdb30e89495b7cb7e735003b9b5394788d231f37`
- Mutation: `elapsed < open.Duration` → `elapsed <= open.Duration`
- Mutant SHA-256: `0db1047c5edb84d5ddfdf5f1f3d75dc4d8edbc3686dd9e8ae10b65234095a2f1`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m02.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.OpenCircuit_RejectsBeforeTheProtectedPipeWithADistinctFailure`
- Raw result: `mutations/M02-open-time-boundary.txt`

## M03 — exclusive Open-to-Half-open CAS owner

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerStateMachine.cs`
- Baseline/post-restore SHA-256: `27e33680886d8b795f5c3250cdb30e89495b7cb7e735003b9b5394788d231f37`
- Mutation: ignore the return value of `Interlocked.CompareExchange(ref _state, recovery, open)` and never retry a losing contender.
- Mutant SHA-256: `53cad1c26895c3885e62bf6bf01a2579e13aec09142174e4913f785deec32137`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m03.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.HalfOpen_AdmitsExactlyOneProbeAndRejectsEveryConcurrentCompetitor`
- Raw result: `mutations/M03-half-open-cas-ownership.txt`

## M04 — backoff progression

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerStateMachine.cs`
- Baseline/post-restore SHA-256: `27e33680886d8b795f5c3250cdb30e89495b7cb7e735003b9b5394788d231f37`
- Mutation: `halfOpen.DurationIndex + 1` → `halfOpen.DurationIndex`
- Mutant SHA-256: `2cfcd28fe9fdf702342ea594d54a128aead485b131886a7dbd736719e0f82529`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m04.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ClassifiedProbeFailure_EscalatesTheBreakDurationAndSuccessResetsIt`
- Raw result: `mutations/M04-backoff-progression.txt`

## M05 — cancellation causality

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreakerFilter.cs`
- Baseline/post-restore SHA-256: `a1f15fc30226a754eef66368cd7f36270ce96da3512b3b609d4624e79fb4fb35`
- Mutation: remove the `context.CancellationToken.IsCancellationRequested` requirement.
- Mutant SHA-256: `9fa10075bf1f56d9da44806225fe32426a03a33d805f250c9cdbe93795222703`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m05.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.MatchingButNotCanceledContextToken_IsAClassifiedDependencyFailure`
- Raw result: `mutations/M05-cancellation-causality.txt`

## M06 — no-verdict release

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreakerFilter.cs`
- Baseline/post-restore SHA-256: `a1f15fc30226a754eef66368cd7f36270ce96da3512b3b609d4624e79fb4fb35`
- Mutation: `_stateMachine.ReleaseWithoutVerdict(lease)` → `_stateMachine.RecordSuccess(lease)` for an unclassified exception.
- Mutant SHA-256: `ce3ea1e9e63d89145eeaa689404a81d4f9b9e7fc97ee363623a16128cb53b484`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m06.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.FailureOutsideTheConfiguredFilter_ReleasesTheProbeWithoutClaimingRecovery`
- Raw result: `mutations/M06-no-verdict-release.txt`

## M07 — OTel meter version

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerTelemetry.cs`
- Baseline/post-restore SHA-256: `9ed9f1d9474468f5bf3fbbe4e7eed6f55d3ca48e5d4adee3366b1c421c62775a`
- Mutation: construct the `Meter` without the product version.
- Mutant SHA-256: `1c30d621c0190947d7a7c16de1d8f46d3c7bae4d9c529a16c11187dc939b7a9c`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m07.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals`
- Raw result: `mutations/M07-otel-source-version.txt`

## M08 — classifier-failure release

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreakerFilter.cs`
- Baseline/post-restore SHA-256: `a1f15fc30226a754eef66368cd7f36270ce96da3512b3b609d4624e79fb4fb35`
- Mutation: remove `_stateMachine.ReleaseWithoutVerdict(lease)` from the classifier-exception path.
- Mutant SHA-256: `54141774a549d220712f4afc9a821bab028073c9f3b9cd2fe2aa042491e70d85`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m08.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ExceptionClassifierFailure_ReleasesTheProbeAndPropagatesTheExactFailure`
- Raw result: `mutations/M08-classifier-failure-release.txt`

## M09 — telemetry-observer isolation

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerTelemetry.cs`
- Baseline/post-restore SHA-256: `9ed9f1d9474468f5bf3fbbe4e7eed6f55d3ca48e5d4adee3366b1c421c62775a`
- Mutation: rethrow from the rejection telemetry no-throw boundary.
- Mutant SHA-256: `06842c7a9077ebb31b966269a52ca8d49dd41431a2c01bb947f75f2c3deefaee`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m09.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.ThrowingOpenTelemetryObservers_CannotChangeCircuitStateOrFailures`
- Raw result: `mutations/M09-observer-isolation.txt`

## M10 — retained-builder isolation

- Target: `src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ExceptionSpecification.cs`
- Baseline/post-restore SHA-256: `b004f5e95280b66efc68bf9f2597f82034c3f008d7a39fbe6c0525119cd5d86d`
- Mutation: return the live `Filter` instead of a detached `CreateSnapshot()` result.
- Mutant SHA-256: `396324b01a5a1b7d3cd8fb9aac48f8850e0ec01dbd34c0a935e42d943e44ceaa`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m10.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Configuration.CircuitBreakerOptionsTests.RetainedExceptionConfigurator_CannotMutateThePublishedFilterSnapshot`
- Raw result: `mutations/M10-retained-builder-snapshot.txt`

## M11 — caller-owned type-array isolation

- Target: `src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ExceptionSpecification.cs`
- Baseline/post-restore SHA-256: `b004f5e95280b66efc68bf9f2597f82034c3f008d7a39fbe6c0525119cd5d86d`
- Mutation: retain `exceptionTypes` instead of cloning it.
- Mutant SHA-256: `9874423f8189352408b633bb1f57ff949f13ebfa2d67ea19a9f4047f695918df`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m11.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Configuration.CircuitBreakerOptionsTests.CallerOwnedExceptionTypeArray_CannotMutateThePublishedFilterSnapshot`
- Raw result: `mutations/M11-caller-owned-type-array.txt`

## M12 — `open` rejection reason

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerTelemetry.cs`
- Baseline/post-restore SHA-256: `9ed9f1d9474468f5bf3fbbe4e7eed6f55d3ca48e5d4adee3366b1c421c62775a`
- Mutation: false branch reason `open` → `invalid`; the `probe_in_progress` branch remains unchanged.
- Mutant SHA-256: `7cdbafa05f9d2c61437296f5a49a0c1f8a2267d01eaa4c231f2d0c1633b6f5dc`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m12.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals`
- Raw result: `mutations/M12-open-rejection-reason.txt`

## M13 — `probe_in_progress` rejection reason

- Target: `src/ViciOne.ServiceBus/Middleware/CircuitBreaker/CircuitBreakerTelemetry.cs`
- Baseline/post-restore SHA-256: `9ed9f1d9474468f5bf3fbbe4e7eed6f55d3ca48e5d4adee3366b1c421c62775a`
- Mutation: true branch reason `probe_in_progress` → `invalid`; the `open` branch remains unchanged.
- Mutant SHA-256: `5cca4f39a9e7379cd57a05313f78798e85f6f5f017d70729a231b47a298ca2b2`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m13.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals`
- Raw result: `mutations/M13-probe-in-progress-rejection-reason.txt`

## M14 — concurrency test-setup sabotage

- Target: `tests2/ViciOne.ServiceBus.Tests/Middleware/CircuitBreaker/CircuitBreakerFilterTests.cs`
- Baseline/post-restore SHA-256: `18ea1afdbbc50f677b3a91419b669c687e3f49c4b8960acaa2864e4e0e928be1`
- Mutation: `UseConcurrencyLimit(2)` → `UseConcurrencyLimit(3)`.
- Mutant SHA-256: `55d607f169d0244364d42c33a06e22d6fa319669dd8be5e3a137cd046cf40fbf`
- Command: `dotnet test /bl:/private/tmp/vsb-circuit-m14.binlog --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --filter-method ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker.CircuitBreakerFilterTests.RetryCircuitBreakerAndConcurrencyLimit_ComposeWithoutChangingTheirOwnership`
- Raw result: `mutations/M14-concurrency-test-setup.txt`

M14 is a test-setup sabotage, not a product mutation.

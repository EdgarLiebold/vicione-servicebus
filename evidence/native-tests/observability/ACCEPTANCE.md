# Observability acceptance

## Subject

- Technical commit: `ec53a17d6df91f8512770dc55498a8c6e6bcb071`
- Dependency-lock correction: `5de73998`
- Final tree before this evidence: `596b9926ed0a964534a0eec858f8b07c12a3b0d4`
- Runtime: .NET 10, xUnit 4, Microsoft Testing Platform 2

The slice replaces inherited StatsD, Windows performance-counter and mutable custom-tag paths with
one bounded `System.Diagnostics.Metrics`/OpenTelemetry surface. The application remains the owner of
listeners, exporters and sampling.

## Positive verification

| Check | Result |
|---|---|
| `dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode` | exit 0 from a fresh detached worktree |
| `dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore --no-incremental` | exit 0; 0 warnings; 0 errors |
| complete core test project | 918 passed; 0 failed; 0 skipped |
| complete Unit/Architecture profile with floor 1584 | 1584 passed; 0 failed; 0 skipped |
| complete LocalIntegration profile with PostgreSQL and Azurite | 17 passed; 0 failed; 0 skipped |
| generated Apache 2.0 section 4(b) change list | 7606 entries; byte-for-byte verifier PASS |

The fresh locked restore initially rejected two stale test lockfiles. The only regenerated delta adds
the already declared `ViciOne.ServiceBus.Tests.InternalAccess` project reference to the Azure Table
and EF Core test lockfiles. A second fresh locked restore passed.

## False-green attacks

Each mutation was applied alone to a detached worktree of the final subject, built successfully, and
then exercised through one named xUnit test. Exit code 2 is the expected killed-mutant verdict.

| ID | Single mutation | Patch SHA-256 | Required test failure | Exit |
|---|---|---|---|---:|
| M01 | count sent messages only on success | `adac919fc83ac3d045487af73430fb5f247940be1257f4b488e36201715e788b` | failed send: expected one attempt, observed zero | 2 |
| M02 | omit completion of successful in-memory outbox enqueue | `d70efb73d163efa23fcdf494bc501be895fd586aef2a646194cea3045781a2d2` | outbox: expected enqueue and delivery, observed one phase | 2 |
| M03 | create a process-scoped meter instead of using the provider factory | `0a5c6495c23133abf2ffef600e6831a4adeaeb1f8b4d03b61106da4bd918796f` | provider contract: expected eight instruments, observed none | 2 |
| M04 | classify every consumer adapter as an ordinary consumer | `ad65da7564f4a041669744053bac5bf6644aee12f4ae575dfa7ca0bc51de5b09` | handler classification: expected `handle/handler`, observed `consume/consumer` | 2 |
| M05 | let the last exception overwrite the first exception | `d5731788bc6bd880e08fbbcb1d2aa39d8994434505f995582508e4ce9ad4b663` | metric operation: expected handler exception, observed retry exception | 2 |
| M06 | rethrow a custom `IMeterFactory` activation failure | `df91f80cda071edba39dcd802a007c6aa5d9869b48b0e5a84c230c4938ddeb41` | application factory exception escaped into bus startup | 2 |
| M07 | remove histogram advice from `messaging.process.duration` | `02bdbd31e42c62f19c6b93f158996e9cdb0c8c2cf1bedb86da2660f569c3db09` | expected OTel bucket boundaries, observed null | 2 |

The common build command was:

```bash
dotnet build tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  -c Release --no-restore --no-incremental
```

The common test command was:

```bash
dotnet test --project tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj \
  -c Release --no-build --no-restore \
  --filter-method <fully-qualified-test-method> \
  --minimum-expected-tests 1 --max-parallel-test-modules 1
```

The selected methods were, in order:

1. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.FaultedSend_RecordsTheAttemptAndItsFullyQualifiedErrorType`
2. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.InMemoryOutbox_EmitsDistinctEnqueueAndDeliveryOutcomes`
3. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.BareContainer_PublishesTheExactInstrumentContractThroughItsOwnFactory`
4. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.ConsumerClassification_UsesTheExplicitAdapterContractInsteadOfTypeNames`
5. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.MetricOperation_CompletesOnceAndPreservesTheFirstObservedException`
6. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.ApplicationMeterFactory_IsPreservedAndItsFailureCannotPreventDelivery`
7. `ViciOne.ServiceBus.Tests.Monitoring.MessagePipelineMetricsTests.BareContainer_PublishesTheExactInstrumentContractThroughItsOwnFactory`

After reversing M07, `git diff --exit-code` and `git status --short` were empty. The four repeatedly
mutated product files had these restored SHA-256 values:

```text
57fd321ac4e129d7f4979b578285dadbc656684081769cfa359d6b263f9fc951  LogContextInstrumentationExtensions.cs
d7022e98737451b49b814f420b1e5aedc5a36088ce15a68f29bedfab1cc216b5  LogContextInstrumentationState.cs
bf21c6d232f9616a8cbcd4f795842ae8d4eb4edaa1e4d9dc4d254833ea8d3483  MetricOperation.cs
21e24ce7e2e1a5c4bf65e9e4fe404a2cd25cd8c002cd607a732a4405b1b1610e  OutboxSendEndpoint.cs
```

The restored worktree was rebuilt with 0 warnings and 0 errors and its complete core test project
finished with 918 passed, 0 failed and 0 skipped.

## Standards check

The standardized instrument names, required attributes, units and recommended duration boundaries
were checked against OpenTelemetry semantic conventions 1.44.0. The advice is published through the
.NET 10 `InstrumentAdvice<double>` API; no OpenTelemetry SDK or exporter dependency is introduced
into the product.

- <https://opentelemetry.io/docs/specs/semconv/messaging/messaging-metrics/>
- <https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics.instrumentadvice-1.histogrambucketboundaries?view=net-10.0>

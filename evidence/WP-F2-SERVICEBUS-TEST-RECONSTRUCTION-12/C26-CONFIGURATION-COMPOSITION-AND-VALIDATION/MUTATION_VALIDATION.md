# C26 mutation validation

Each mutation was applied alone to the working tree, built in `Release` with locked restored dependencies, executed through the native xUnit v4 / Microsoft Testing Platform executable, and immediately reverted. The final focused rerun used the restored product sources.

## Shared build commands

```text
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 dotnet build tests2/ViciOne.ServiceBus.Abstractions.Tests/ViciOne.ServiceBus.Abstractions.Tests.csproj --configuration Release --no-restore --disable-build-servers --no-incremental --maxcpucount:1 /p:UseSharedCompilation=false --verbosity minimal
env DOTNET_ROOT=/usr/local/share/dotnet DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 dotnet build tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj --configuration Release --no-restore --disable-build-servers --no-incremental --maxcpucount:1 /p:UseSharedCompilation=false --verbosity minimal
```

Every mutation build completed with 0 warnings and 0 errors. Every filtered run required its exact method and at least one discovered test; the final unfiltered profiles enforce zero skipped tests and the complete profile floor.

## One-cause results

| Mutated product invariant | Detecting test | Observed failure |
|---|---|---|
| Remove materialization from `SendPipeSpecification.Validate` | `SendPipeSpecificationTests.Validate_ReturnsAStableSnapshotAndReportsEachOwnedSpecificationOnce` | Exit 2; the earlier result grew from `base,message` to `base,message,later` after later configuration. |
| Remove the parent observer from `SendPipeConfiguration` | `SendPipeConfigurationTests.EndpointSpecification_ComposesTopologyBusAndEndpointLayersExactlyOnce` | Exit 2; the trace began with `endpoint-alpha` instead of `topology-alpha`, proving loss of topology and bus layers. |
| Remove ordinal sorting from saga message discovery | `SagaConnectorTests.MessageContracts_AreDiscoveredInSemanticRoleAndStableMessageOrder` | Exit 2; reflection order returned `ZuluInitiated` before `AlphaInitiated`. |
| Give `Orchestrates` precedence over duplicate `InitiatedBy` contracts | `SagaConnectorTests.DuplicateInitiatedAndOrchestratedRole_CreatesTheMissingSagaOnce` | Exit 2; no saga instance was created for the initiating message. |
| Retry a faulted observer notification instead of preserving its first failure | `ConfigurationObserverTests.ObserverNotification_PreservesTheFirstFailureAndRejectsReentrantValidation` | Exit 2; the second validation produced a different exception instance after rerunning the observer. |
| Snapshot handler validation before observer notification | `ConfigurationObserverTests.ObserverInjectedSpecifications_AreValidatedOnTheFirstAndEverySubsequentSnapshot` | Exit 2; the observer-injected `handler` validation result was absent from the first snapshot. |
| Stop propagating a late root send specification to existing message specifications | `SendPipeSpecificationTests.RootSpecificationAddedAfterMessageCreation_ReachesTheExistingMessageExactlyOnce` | Exit 2; the exact expected trace `late-root` was empty. |
| Keep a send message specification whose observer initialization failed | `SendPipeSpecificationTests.FailedObserverInitialization_IsDiscardedBeforeTheMessageCanBeRequestedAgain` | Exit 2; the observer ran once instead of twice because the incomplete cache entry was reused. |
| Stop propagating a late root publish specification to existing message specifications | `PublishPipeSpecificationTests.RootSpecificationAddedAfterMessageCreation_ReachesTheExistingMessageExactlyOnce` | Exit 2; the exact expected trace `late-root` was empty. |
| Keep a publish message specification whose observer initialization failed | `PublishPipeSpecificationTests.FailedObserverInitialization_IsDiscardedBeforeTheMessageCanBeRequestedAgain` | Exit 2; the observer ran once instead of twice because the incomplete cache entry was reused. |

## Restored-state proof

After all ten mutations were reverted, both affected test projects rebuilt with 0 warnings and 0 errors. The complete source-owner project runs and the unfiltered UnitArchitecture profile provide the final restored-state proof in `VALIDATION.md`.

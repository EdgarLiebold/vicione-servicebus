# EF-Saga test project read closure (iteration 242)

Baseline: `HEAD=194271bbd0062d99ea856435dec9390e69b20d11`, `HEAD^{tree}=444c215cea6ab9463c83a8a3b65f5708c7853dd9`. This ledger records the pre-edit working-tree content read by the EF-Saga agent. The two already-dirty Outbox test files belong to the lead and were read, not edited here. `git ls-files` returned exactly 34 files in the EF test project; all 34 were read fully. SHA-256 values below bind the actual pre-edit bytes.

| SHA-256 | Path under `tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests/` |
|---|---|
| `1db6c20c1e230044ab8cd65cc0987439f583c726a4f03504fe6ca5dabeb782b9` | `ApiSurfaceGlobalUsings.cs` |
| `399622ffb3d049e88279e2579a4053a650c33db2c5cfbc1f61b850c3889b6f55` | `Configuration/EntityFrameworkJobServiceConfigurationTests.cs` |
| `6eeb77d6adc60f95abb344aa271d462495fba602f823a56918da82693abe10e8` | `Configuration/EntityFrameworkOptionsStartupValidationTests.cs` |
| `9cb4eba43f4cbfdec92ffad8fbbf2f8c50d254bb83f98c5a9fa2b4bc2a529272` | `Configuration/EntityFrameworkProviderConfigurationTests.cs` |
| `065dce00e9fa5dac2d5e0470f0b80040dccf0acf3ddfbc4a8d1df4fb1a800e56` | `Configuration/EntityFrameworkPublicArgumentValidationTests.cs` |
| `8563ce6298e86f2bf2b2cf57b5db92e3dc1098748c5563ddd0fa1c6d328cdee2` | `Configuration/EntityFrameworkTestDatabaseNameTests.cs` |
| `bcddbd9dcc1f4cda31350ba2dd17783fa9609bf7b072ee74fc1d2ab2ff8feeec` | `Configuration/EntityFrameworkTimeProviderTests.cs` |
| `3bf352027da460cc6a4871ab4cfd9f1fe160fd81ee5c0aaa2c9cba89bd3ef6b8` | `DurableSend/EntityFrameworkDurableSendDurabilityTests.cs` |
| `a90f71e2779fd4038845d3e1957c5fe3c64db2b2f1ce1af75db4182f6f9e3a44` | `DurableSend/EntityFrameworkReliableStoreTests.cs` |
| `95dc9a0de506c40f7ca4a5fe0596227fa514785bcfa560029dd232a50011e303` | `DurableSend/ReliableMessagingMigrationTests.cs` |
| `31ccd19879c7ed675498412408d8c5ebde58e5790beb0645822e6e8f2b03ed46` | `EntityFrameworkCoreIntegration/EntityFrameworkExecutionStrategyTests.cs` |
| `a2ec9c00423bb8ef966ef7e338615e0b1cfd2492be2d2410525d2df2393d5782` | `MessageJournal/EntityFrameworkMessageJournalModelTests.cs` |
| `2c746bb1071327ae4af830f20d3008204af0889fd8366bc6f1d208f240857f01` | `Outbox/BusOutboxDeliveryTelemetryTests.cs` |
| `40e8991c9ee2d7e4d7f11928980bb3eade1b8c78ddd50b8ade891ab32d8accf2` | `Outbox/BusOutboxReliabilityStateTests.cs` |
| `ba9a54e488e4b624c8ab3f5cd8b812bfc1e242cb23bb316ef7b791d0f55b9eea` | `Outbox/EntityFrameworkOutboxOperationsTests.cs` |
| `bbfd96c7f31758763f774ec897cfea04d217bd61aec0288679c5b9613f8f5ee9` | `Outbox/EntityFrameworkOutboxWriteCoordinatorTests.cs` |
| `696ae699f76421bc26adcbe26d0e317661522c5c0e239015affeaf510dba40cc` | `Outbox/EntityFrameworkReliableInboxPipelineTests.cs` |
| `ec11dff5eeae15f3d43339037fd646e8c36b6fd1b186fe7d662f82049fc637a4` | `Outbox/EntityFrameworkReliableMessagingRegistrationTests.cs` |
| `eda9456ad244db864f04b8d5003853f810dbdb9523274fa742503c066fb11eac` | `Outbox/EntityFrameworkScopedBusContextProviderTests.cs` |
| `4408783191dfc8cd8399ea0a8e6fe32368d92cf4d0f0472d659584d473da23a2` | `Outbox/EntityFrameworkTransactionalOutboxSourceTests.cs` |
| `568f1adfe8467f0496b2db4daff65445afd80e9e2eefe70ded96015ca55a80bd` | `Outbox/EntityFrameworkTransactionalScopedBusContextTests.cs` |
| `0d11bd550d5a11ffb9c9e101bf7922fc3836fffd961134228a0cb41c9de35bc5` | `Outbox/InboxCleanupServiceTests.cs` |
| `b9e3ce1e0f4e788b5616ea7b2cdca8138b14dda87562e1cddb9e80dddbefe286` | `Outbox/MessagePackOutboxReplayTests.cs` |
| `98437298ef1ead328f1cd0c600e9754dbe05094add18dd22cbf9e9eb47f573ed` | `Outbox/OutboxMessageFactoryTests.cs` |
| `193cdf169998adc364fde101baf00aa129d7c8f9e972b226b48b19446627802c` | `Outbox/OutboxMessageTests.cs` |
| `2255a3600454328932518d25e31b46d2e638e68be7641481fafb5a89164f3e63` | `Requirements/EntityFrameworkRequirements.json` |
| `eecd0f7428421a48bf2c9f104820723764154af8ab61f18fb84240e95f1d6711` | `Requirements/RequirementCoverageProjectionTests.cs` |
| `c8276bc3644962990c62e92f61d616406c703aabd0c89cb77d2f4102104ebc92` | `Saga/DbContextSagaRepositoryContextTests.cs` |
| `45b7250c35d00d160e7aec84f588c00b763864ef8b426cf02880d2faf3781c51` | `Saga/EntityFrameworkSagaRepositoryFactoryTests.cs` |
| `6cbd213009f6d3c2e63a25e8c72ede245486adbb481ba482fe63e548e09c2bee` | `Saga/SagaClassMapTests.cs` |
| `c3690aea62dd525bf966aa0346dd9f54eeb31d8e5e7d9d757eb7d64f0bd5aec3` | `Saga/SqliteOptimisticSagaConcurrencyTests.cs` |
| `2a562f5604fce0c227c4c07548523cefb0cb28009dcbca607fe82c49cb006903` | `SqlLockStatementProviderTests.cs` |
| `6b1545738ab188c33163e5cd7ffdfee2fdc888d5032a4558509b17b802410720` | `ViciOne.ServiceBus.EntityFrameworkCore.Tests.csproj` |
| `f0a75551e91331583dea2d3e541366645678ac696bfe6235aa59cb8355df8aef` | `packages.lock.json` |

Effective inputs read in full before editing: root/test/source `Directory.Build.props`, root/test `Directory.Build.targets`, `Directory.Packages.props`, `signing.props`, `global.json`, `ViciOne.ServiceBus.Tests.Unit.slnx`, `.github/workflows/native-tests.yml`, `tests/testconfig.json`, `tests/testsettings.json`, the three embedded `docs/migrations/reliable-messaging-*.sql` resources, `RequirementCoverageAttribute.cs`, `RequirementCoverageProjectionVerifier.cs`, and `ProbeResultBuilderTestDriver.cs`. Their pre-edit SHA-256 values are captured in the agent terminal evidence; none is being changed in this packet. The `git ls-files` / read-set closure is `34 = 34`.

Research and test plan: `Probe` currently disposes the returned DbContext directly, violating factory ownership (`ContainerSagaDbContextFactory.ReleaseAsync` is intentionally a no-op). Assert that probing preserves the container-owned context, asynchronously releases delegate-owned context, and releases on model failure. `RepositorySagaDbContext` has instance-dependent mappings but EF Core's default model-cache key depends only on the context type; assert isolation between two repositories with different maps and rebuilding after a late map registration. Change only the internal repository/factory implementation and focused Saga tests plus their passive requirement projection, then run the scoped Release test project after the concurrent coverage pass finishes.

## Final EF-Saga packet evidence

The two changed source files were personally read in full before edits and their final diffs reviewed. The source changes are internal only: the synchronous `Probe` now calls the factory's `ReleaseAsync` through a synchronous bridge, and repository-created DbContexts use a mapping-generation-aware EF model-cache key that includes the `designTime` bit. The official EF Core dynamic-model guidance used for the cache-key decision is https://learn.microsoft.com/en-us/ef/core/modeling/dynamic-model.

Final changed source SHA-256:

| SHA-256 | Path under `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/` |
|---|---|
| `ed7c491bb0d428152d450ef03ef69684691117386d1559dffda93a847639119a` | `EntityFrameworkCoreIntegration/Configuration/EntityFrameworkSagaRepository.cs` |
| `befc6519928c8c7db1b6eb3bfb46938bbd4f902099f544da20d265c6cd30571a` | `EntityFrameworkCoreIntegration/Saga/EntityFrameworkSagaRepositoryContextFactory.cs` |

The eight added causal tests are `Probe_PreservesTheContainerOwnedContext`, `Probe_ReleasesTheDelegateOwnedContextThroughItsFactory`, `Probe_ReleasesTheContextWhenModelConstructionFails`, `Probe_PropagatesTheFactoryReleaseFailure`, `SharedRepository_DistinguishesTheMappingsOfSeparateRepositories`, `SharedRepository_BuildsANewModelAfterAddingAnotherSagaMap`, `SharedRepository_ReusesTheModelWhenItsMappingsHaveNotChanged`, and `SharedRepository_UsesAModelCacheKeyWithSeparateDesignTimeIdentity`. Their requirement-projection entries were added in `Requirements/EntityFrameworkRequirements.json`.

Verification: `dotnet test --project tests/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Tests/ViciOne.ServiceBus.EntityFrameworkCore.Tests.csproj -c Release --no-restore --results-directory /private/tmp/vicione-ef-saga-iteration242-testresults` passed unfiltered (257/257); `dotnet test --project tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj -c Release --no-restore --results-directory /private/tmp/vicione-ef-saga-iteration242-architecture-testresults` passed unfiltered (441/441). `git diff --check` passed. The two pre-existing dirty Outbox test files were not edited in this packet.

Error behavior: if the otherwise successful `Probe` encounters a `ReleaseAsync` failure, that exact exception propagates (covered by test). If both model construction and release fail, the release exception masks the model-construction exception due to the `finally` precedence; this matches the sibling asynchronous factory-release paths and was not silently changed here.

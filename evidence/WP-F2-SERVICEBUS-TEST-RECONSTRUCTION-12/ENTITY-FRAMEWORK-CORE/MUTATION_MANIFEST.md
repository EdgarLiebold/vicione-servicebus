# Entity Framework Core persistence mutation manifest

Every mutation starts from technical commit `8ea356043316861f0ac983de5fa8470410e2f9ca`, is applied in
an isolated detached worktree, runs only its named native owner, and is then restored. The common
runner settings are Release, locked restored assets, one test module, strict zero-test policy,
fail-on-skip and a bounded two-minute MTP process timeout.

## M01 — discard a delivery signal before waiter registration

- Target: `src/ViciOne.ServiceBus/Middleware/Outbox/BusOutboxNotification.cs`
- Baseline SHA-256: `9106850a3ad62e00d319a320a45a2423b3069bbf57d24c1a56857f01679441c9`
- Exact mutation: delete only `_deliveryPending = true;` from `Delivered()`.
- Mutant SHA-256: `43fc8d026d9103169744fba0b2dd814fff981b72a42ee8e053710f7e355c2b43`
- Owner: `BusOutboxNotificationTests.DeliveredBeforeWait_IsConsumedWithoutAdvancingTheConfiguredClock`
- Result: 1 total, 1 failed, 0 skipped, exit code 2.
- Causal failure: `TimeoutException` at the bounded wait because the pre-arrival signal was lost.

## M02 — emit an unquoted model-resolved SQL Server sort column

- Target: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/EntityFrameworkCoreIntegration/SqlServerLockStatementFormatter.cs`
- Baseline SHA-256: `4307eeb565af5bda5ce96d2e95d3eff3b508b219c4fc5211856cb39f6e289dc4`
- Exact mutation: replace `QuoteIdentifier(columnName)` with `columnName` only in
  `CreateOutboxStatement`.
- Mutant SHA-256: `8bf2868764b47511947ef6dadbfea94941e81d2c04d2b95dde441a4f34c99ee2`
- Owner: `SqlLockStatementProviderTests.OutboxStatement_UsesTheExactModelAndQuotesEveryIdentifier`
- Result: 3 total, 2 passed, 1 failed, 0 skipped, exit code 2.
- Causal failure: the SQL Server row expected `[Created]]At]` but received the raw `Created]At`.

## M03 — classify an unrelated EF entry failure as a saga insert race

- Target: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/EntityFrameworkCoreIntegration/Saga/DbContextSagaRepositoryContext.cs`
- Baseline SHA-256: `d2974c0bc249bfae93cdd48bf22a73e1ab8cb7879712035ab0d9c06ce7dc20d9`
- Exact mutation: delete only the guard that requires `exception.Entries` to contain the exact saga
  candidate instance before conflict classification proceeds.
- Mutant SHA-256: `d229488cef3c75d018dc2c4fc2f8d1539967af95dfb80bb07a2232a02123ddc0`
- Owner: `DbContextSagaRepositoryContextTests.Insert_DoesNotSwallowAnUnrelatedEntryFailureWhenTheSagaIdentityExists`
- Result: 1 total, 1 failed, 0 skipped, exit code 2.
- Causal failure: `Assert.Throws<DbUpdateException>` observed no exception because the mutant
  incorrectly converted the unrelated failure into a missing insert result.

## Restore closure

After M03 the worktree restored to the three baseline hashes above and `git status --short` was
empty. The isolated worktree was then removed and pruned. The main technical branch remained on
commit `8ea356043316861f0ac983de5fa8470410e2f9ca` and clean throughout all mutations.

## Second frozen subject

M04 through M07 start from technical commit
`f1096d531e00e64f2683d4e41542630df2af4550`, tree
`89ee8622c1bae270af661c3d098d4b98aaa7b17b`. They run in a second isolated detached worktree with
the same Release, locked-restore, strict-zero, fail-on-skip, one-module and bounded MTP settings.
LocalIntegration owners use a run-scoped PostgreSQL fixture.

## M04 — bypass the shared optimistic isolation policy

- Target: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/Configuration/Configuration/EntityFrameworkSagaRepositoryConfigurator.cs`
- Baseline SHA-256: `23f69d27bb35f8c4b657eb7ab62d16a4b54140fbe06a14f25f3fd756f8fd616b`
- Exact mutation: replace `SetConcurrencyMode(ConcurrencyMode.Optimistic);` in
  `SetOptimisticConcurrency` with direct assignment to `_concurrencyMode`.
- Mutant SHA-256: `c93ed430913c440f3c1f92fa586c4fbfc2e19b9d54e15945c1ef25d4cccbe93f`
- Owner: `EntityFrameworkProviderConfigurationTests.SetOptimisticConcurrency_UsesTheSameReadCommittedDefaultAsTheModeProperty`
- Result: 1 total, 1 failed, 0 skipped, exit code 2.
- Causal failure: the resolved strategy reported `Serializable` instead of `ReadCommitted`.

## M05 — turn a read-only saga event into a writable event

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/Saga/PostgreSqlReadOnlySagaTests.cs`
- Baseline SHA-256: `b6c676fa7c9c07ed680ade27c4b21efc222066d8d5768f2476358aec77cdb6e7`
- Exact sabotage: delete only `configuration.ReadOnly = true;` from the status event declaration.
- Mutant SHA-256: `5e2fc08c8b12811a76844b3e59cb15b996a91f1afd8f1417ba3eef87694c1dad`
- Owner: `PostgreSqlReadOnlySagaTests.ReadOnlyEvent_RespondsFromThePersistedStateWithoutSavingItsMutation`
- Result: 1 total, 1 failed, 0 skipped, exit code 2; run identity `vicione-d754eb5ae15e`.
- Causal failure: PostgreSQL contained `This mutation must not be persisted` instead of `Started`.

## M06 — omit pessimistic load-query customization

- Target: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/EntityFrameworkCoreIntegration/Saga/PessimisticLoadQueryExecutor.cs`
- Baseline SHA-256: `51459c62cd1e75d16edc4fe1762adca45fa5a79345bf48fae2f97725040995c4`
- Exact mutation: delete only the `SagaQueryCustomization.Apply` assignment from `Load`.
- Mutant SHA-256: `085e3f4f58fc8059b28df51f8e0986310136650f85ca59a470797bebe6a96229`
- Owner: `PostgreSqlPessimisticSagaQueryCustomizationTests.LockedLoad_AppliesTheConfiguredNavigationGraphInsideTheTransaction`
- Result: 1 total, 1 failed, 0 skipped, exit code 2; run identity `vicione-d3dfdf6ced4f`.
- Causal failure: the required first-level navigation was null at the exact graph assertion.

## M07 — omit PostgreSQL row locking

- Target: `src/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration/EntityFrameworkCoreIntegration/PostgresLockStatementFormatter.cs`
- Baseline SHA-256: `e648b4879a9b8089aeecf13c7c7ef8ac9f595cdb432edbec299f9b7fd5c7b30c`
- Exact mutation: delete only `sb.Append(" FOR UPDATE");` from `Complete`.
- Mutant SHA-256: `a56ceb5bceee3dbbf20e8e979c0ea160393bf35a5d8806a0b1339bfbfef293f3`
- Owner: `PostgreSqlSagaConcurrencyTests.SameCorrelation_EntersOneHandlerAtATimeAndPersistsBothUpdates`
- Result: 1 total, 1 failed, 0 skipped, exit code 2; run identity `vicione-c8aed1f296d6`.
- Causal failure: the bounded row-lock observer saw no second `FOR UPDATE` attempt.

## Second restore closure

After M07 all four files restored to the baseline hashes recorded above, `git status --short` was
empty, and `git diff --check` passed in the detached worktree. The main technical branch remained
stationary on `f1096d531e00e64f2683d4e41542630df2af4550` throughout the four probes.

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

## Third frozen subject: outbox reliability

M08 through M13 use the exact source bytes of technical commit
`b95faaeff0be853baa4747df1366f15049d3ec0b`, tree
`9598ce52248094920b1c59c646b50bb8f37d7288`. Every probe was applied in one isolated detached
worktree, built in Release from locked assets, and executed only through its named native xUnit 4 / MTP
2 owner with strict zero-test and fail-on-skip settings. LocalIntegration owners used a fresh
run-scoped PostgreSQL fixture.

## M08 — discard persisted routing keys during outbox delivery

- Target: `src/ViciOne.ServiceBus/Middleware/OutboxMessageSendPipe.cs`
- Baseline SHA-256: `501f4e342117980ae1d40fdefe0454da7255a552c569f29e10adbabd69cd962b`
- Exact mutation: delete only the `ReadPropertiesFrom` block that restores persisted transport
  properties onto the outgoing send context.
- Mutant SHA-256: `be489f382fb36a05c84d9cc5181e8a43e441e2f30fdd0fd2f5e060e7e9454c1c`
- Owner: `ReliableTransactionalOutboxTests.RoutingKeys_RoundTripThroughThePersistentOutbox`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-0c5f84b9b1cc`.
- Causal failure: the bounded receiver wait expired because the restored message no longer carried
  the direct-exchange routing key.

## M09 — bypass the Entity Framework outbox before Quartz scheduling

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/EntityFrameworkCoreIntegration/QuartzTransactionalOutboxTests.cs`
- Baseline SHA-256: `a5b221161356b72d1b1fdc2352c5994821ca3eea328596649ea29d6d86ed355c`
- Exact setup sabotage: delete only the `UseEntityFrameworkOutbox<QuartzOutboxDbContext>` endpoint
  configuration.
- Mutant SHA-256: `f3d8d0481685498ffaba18cbf3f51edeb09dd11d83fd6df06644ed4ba973760e`
- Owner: `QuartzTransactionalOutboxTests.ScheduledPublish_ReachesQuartzOnlyAfterTheEntityFrameworkTransactionCommits`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-dec4b99790c6`.
- Causal failure: the publish observer saw one scheduling command before the commit gate was
  released; the contract requires exactly zero.

## M10 — remove the real send-boundary failure

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/EntityFrameworkCoreIntegration/ReliableTransactionalOutboxTests.cs`
- Baseline SHA-256: `e45ff3984feb7b712fae6bfc0acc5ed6ad8b79f913f32355df9713b9626aab16`
- Exact setup sabotage: replace the first `ExpectedTransportSendFailure` thrown by the test-owned
  send observer with successful completion.
- Mutant SHA-256: `f363d2dfed933491c59eb6adce6546ecc3b863865d8a08d853e9cd4999ca7318`
- Owner: `ReliableTransactionalOutboxTests.TransportSendFailure_RetriesTheCommittedOutboxWithoutDuplicatingTheMessage`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-803a47eb92ad`.
- Causal failure: the exact attempt oracle expected two transport sends but observed one.

## M11 — lose the request identity on a delayed saga response

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/EntityFrameworkCoreIntegration/TransactionalOutboxRequestSagaTests.cs`
- Baseline SHA-256: `ff528f4d5de3da13ba608acea190a3f22d9aa2857978f810e080d3b8bdffb845`
- Exact mutation: delete only the assignment of the persisted saga `RequestId` to the delayed
  response send context.
- Mutant SHA-256: `817d2bf093bde5ac072ce6b9625117d39b04e5f3d7396887481c33f1736fc33e`
- Owner: `TransactionalOutboxRequestSagaTests.DelayedSagaResponse_PreservesTheRequestIdentityAcrossTheOutboxSchedule`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-7fede214c4c3`.
- Causal failure: the requester rejected the response without its correlation identity and reached
  the bounded request timeout.

## M12 — replace the originating publish scope with an unrelated identity

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/EntityFrameworkCoreIntegration/ScopedOutboxFilterTests.cs`
- Baseline SHA-256: `9e7fb6598bae45b329796c6819b5f7be2587414c903a16363007e50d4625d960`
- Exact setup sabotage: report `Guid.Empty` from the publish filter while still resolving the real
  scoped dependency.
- Mutant SHA-256: `4dd59dda378637496be194d05b2795d8b7dac86740deb28a738e0858d632c620`
- Owner: `ScopedOutboxFilterTests.ConsumeOutboxPublishFilter_UsesTheExactConsumerScope`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-50be6e95006d`.
- Causal failure: the exact identity assertion received only the zero identity instead of the
  consumer scope.

## M13 — suppress the first transactional failure

- Target: `tests2/Persistence/ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests/EntityFrameworkCoreIntegration/TransactionalOutboxFaultTests.cs`
- Baseline SHA-256: `eedc3eda63aa52d720057689dd7492c09fa11717ecbe2d4f745844cf03ac50fe`
- Exact setup sabotage: change the first-attempt failure predicate from `attempt == 1` to the
  unreachable `attempt == int.MaxValue`.
- Mutant SHA-256: `59f81c7b5d9bd50f65468d7034d46c111b8ea5e4aca35c8b6260abece2de1a20`
- Owner: `TransactionalOutboxFaultTests.FirstAttemptFailure_RollsBackItsOutboxAndTheRetryPublishesEachEffectOnce`
- Result: 1 total, 1 failed, 0 skipped; run identity `vicione-029a49b0179a`.
- Causal failure: the exact delivery-attempt oracle expected two attempts but observed one.

## Third restore closure

After M13 all six target files matched the baseline SHA-256 values above, `git status --short` and
`git diff --check` were clean, and the detached mutation worktree was removed normally. The main
product worktree was not changed by any probe. Its already prepared Quartz observer refinement was
validated after the probe series and then frozen as part of technical commit `b95faaef`.

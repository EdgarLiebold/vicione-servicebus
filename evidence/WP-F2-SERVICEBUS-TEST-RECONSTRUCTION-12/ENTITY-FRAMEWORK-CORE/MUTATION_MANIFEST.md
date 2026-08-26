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

# Entity Framework Core retry-state correction validation

## Frozen technical subject

- Commit: `6632dfa2017348f94a9f18e35bb2a08415dadc77`
- Tree: `e3a039081f9a7e96917e1f08a3b82dea21627096`
- Parent: `d259900bdedbbc49f339815ab5921002fcea3596`
- Baseline evidence before the correction: `923568bb`
- SDK: `/usr/local/share/dotnet/dotnet` 10.0.302

The correction centralizes EF retry-attempt cleanup, preserves state after successful attempts,
rolls back only the failed attempt's in-memory outbox tail, propagates the consume cancellation token
to inbox persistence, and prevents saga query code inside an existing transaction from adding a
second execution-strategy boundary. The outbox checkpoint owns its context and cannot be applied to
another outbox.

## Positive verification

All .NET/MSBuild commands ran outside the filesystem sandbox under the isolated environment recorded
in `FINAL_RESULTS.json`. The complete locked Engineering restore and Release build succeeded; the
build produced zero warnings and zero errors. The unfiltered native UnitArchitecture profile passed
1,882/1,882 with zero failure and zero skip. The focused EF UnitArchitecture project passed 53/53.

The canonical run-scoped PostgreSQL/Azurite LocalIntegration profile passed 70/70 under identity
`vicione-6adceb0711bd`; the focused real-PostgreSQL EF profile passed 59/59 under identity
`vicione-3f3f39c3fe96`. Raw console logs, build binlogs, focused CTRF reports and fixture diagnostics
are retained under `positive/`. The CI verification model passed, followed by 253/253 CI tool tests,
103/103 identity tool tests and the generated 7,848-entry root `CHANGELIST.md` consistency gate.

The deterministic retry owner creates a test-owned `PostgresException` with SQLSTATE `40001` and
passes it through the real PostgreSQL-backed EF outbox and Npgsql execution-strategy path. It does
**not** claim that the PostgreSQL server generated a concurrent-transaction serialization conflict.
This keeps the retry trigger deterministic while exercising real provider classification,
transaction rollback, ChangeTracker cleanup, re-execution and persistence.

## Provider-variant closure

The inherited 90 EF obligations expand to 156 execution identities. The mechanically validated
matrix in `R0_VARIANT_DISPOSITIONS.json` retains every identity exactly once:

- 77 identities have executing native owners;
- 39 identities are consolidated only where the owner is genuinely provider-neutral;
- 40 SQL Server/Azure SQL identities remain visible as `EXTERNAL_PENDING` contracts.

No pending identity is counted green. The SQL Server/Azure SQL External profile remains explicit in
`TODO.md`; it must run against short-lived real resources before those 40 identities can become
executing dispositions. The matrix validation is `PASS` and is independently recorded in
`R0_VARIANT_DISPOSITION_VALIDATION.json`.

## Negative verification

Six byte-exact one-cause probes protect the corrected boundaries. Every mutant built, its native
behavior owner failed with MTP exit code 2 for the intended reason, and no probe used skip or an
absence-until-timeout oracle. `MUTATION_EXECUTION.json` binds the exact replacement, baseline and
mutant SHA-256, command subject, result counts and raw artifacts; `MUTATION_MANIFEST.md` explains the
behavioral cause. The isolated worktree was restored to the frozen commit and is clean.

## Verdict

The technical correction and its local evidence are complete. Acceptance is deliberately withheld
until `SHA256SUMS`, the generated root `CHANGELIST.md`, the identity-tool/change-list gates and two
independent read-only Red-Team reviews have all passed against the final evidence commit.

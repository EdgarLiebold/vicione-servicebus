# Azure Table correction — final local validation

## Frozen technical subject

- Technical commit: `497a7cd5570969bee13f35abb46bb37a7372dbee`
- Technical tree: `8e25a9d98d39bd71e1afc05e73e6b18b462d932d`
- Parent: `1faa2710dfeb0b961110fdc67818c34bd1f04377`
- Authorizing architecture commit: `2c8efb961dc28b45906eaedd92853ecb4997c18b`
- SDK: `10.0.302` from the repository `global.json`

The technical worktree was clean at every final positive run. Mutation restoration was checked
against the technical-tree SHA-256 for every target before the complete locked restore, build and
positive test runs.

## Corrections closed

1. Update and delete now classify cancellation from the caller's requested token, without assuming
   that an SDK or linked pipeline must attach that same token instance to its
   `OperationCanceledException`. Six deterministic theory rows cover update/delete crossed with
   caller, default and linked exception tokens. A separate two-row owner keeps non-requested
   dependency cancellation classified as a saga failure.
2. The public API now uses `TableClientFactory` and `TableClient` vocabulary. The former
   `CloudTable` provider names are absent. The provider implementation is internal, while the public
   composition surface exposes only the Azure.Data.Tables type actually used by the product.
3. The default Microsoft DI path explicitly maps both public repository contracts to the internal
   provider-backed factory. The regression test resolves both contracts from a real
   `ServiceCollection` scope and proves that they share the intended concrete factory.
4. MessageJournal capacity concurrency is tested with eight real, synchronized Azurite transaction
   submissions after all writers have completed their lease read and query. Exactly one append
   succeeds, seven fail with HTTP 412, and the final partition contains exactly one entry.
5. Atomic retention/capacity repair has a separate hermetic boundary owner. It observes exactly one
   `SubmitTransactionAsync` call, zero individual writes, and one ordered action list: lease
   `UpdateReplace`, exact deletions, and add. The real Azurite test remains the provider proof.
6. The HTTP-500 mutation owner now supplies a fixed saga correlation ID through the real context
   helper. Its broad-catch mutant therefore fails at the intended assertion because no exception is
   thrown; no unrelated helper exception participates in the verdict.

Tests were not weakened to accommodate product defects. The cancellation and DI defects were fixed
in product code first, then protected by native xUnit 4/Microsoft Testing Platform tests.

## Positive results

| Surface | Result | Raw evidence |
|---|---:|---|
| Locked Engineering restore | exit 0 | `positive/final-engineering-restore.log`, `positive/final-engineering-restore.binlog` |
| Engineering Release build | 0 warnings, 0 errors | `positive/final-engineering-build.log`, `positive/final-engineering-build.binlog` |
| Full UnitArchitecture | 1953/1953, 0 failed, 0 skipped | `positive/final-unit-test.log`, `positive/unit/*.ctrf` |
| Full LocalIntegration | 87/87, 0 failed, 0 skipped | `positive/final-local-integration-test.log`, `positive/local/*.ctrf` |
| Focused Azure Table Unit | 50/50, 0 failed, 0 skipped | `positive/final-azure-table-unit-test.log`, `positive/azure-unit/*.ctrf` |
| CI tooling self-tests | 253/253 | `positive/final-ci-selftests.log` |
| Identity tooling self-tests | 103/103 | `positive/final-identity-selftests.log` |
| Verification model | exit 0 | `positive/final-verification-model.log` |
| Canonical CHANGELIST | 8126 entries, exit 0 | `positive/final-change-list.log` |

The LocalIntegration run used one fresh run-scoped PostgreSQL plus Azurite fixture
`vicione-0b216ddcfb45`. The runner allocated loopback ports atomically, projected credentials only to
the child process, collected broker diagnostics under its ignored raw-run directory, and removed the
fixture after the test process.

## Mutation closure

`MUTATION_MANIFEST.json` binds the exact target, baseline bytes, replacements, mutant bytes,
post-restore bytes and causal expected result for M01–M16. `MUTATION_EXECUTION.tsv` binds the
expanded build/test commands and exit codes. Each mutation:

- changed only its named production target;
- built in Release with exit 0 and no warning/error;
- ran only the named owning test with a fail-closed minimum count;
- failed with MTP exit 2 for the documented causal reason;
- skipped no case; and
- restored the target to the technical-tree SHA-256 before the next mutation.

M14 is the provider mutation: removing only the lease action allowed all eight synchronized Azurite
writers to commit and left eight entries instead of one. M15 independently proves the complete
ordered atomic action list. M16 independently proves the DI mapping. These are distinct contracts
and are not counted twice as one oracle.

## R0 and cloud boundary

The accepted R0 disposition is unchanged: 39 obligations comprise 26 native executing
replacements, 10 suite-audit rows superseded by the PO decision, and exactly 3 explicit
`EXTERNAL_PENDING` cloud obligations (`OBL-R0-PER-0455` through `0457`).

No real Azure account was used or claimed. Azurite proves the local Azure.Data.Tables entity,
partition, ETag, transaction, conflict, Future, saga, job-service and MessageJournal behavior.
Cosmos DB for Table compatibility and 429 handling, Entra ID authentication/token refresh, and real
Azure service limits remain External work in `TODO.md` and are not counted green.

## Repeatability notes

- Only the Lead/root process starts .NET, MSBuild, Docker or network-backed provider processes.
- Build/test processes sharing `artifacts/sdk` run serially.
- CI process-tree self-tests must run outside the filesystem sandbox because they deliberately invoke
  macOS `ps`; the sandbox denial is an environment diagnostic and is not accepted as a product run.
- Identity self-tests and the final CHANGELIST check run only after the complete evidence path set is
  staged, so the generated legal inventory cannot omit ignored raw artifacts.

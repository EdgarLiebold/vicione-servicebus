# Azure Table native persistence closure

## Frozen subjects

- Technical commit: `f0a18299aa849b261d948c92aef2c8dfaa408313`
- Technical tree: `545682603c03a16f6913caaab4665bfde463b248`
- Parent: `1d63a7adca71e9e3bac135240d3ff979f7121de1`
- Authorizing architecture commit: `2c8efb961dc28b45906eaedd92853ecb4997c18b`
- SDK: `10.0.302`

The technical tree is the only product/test subject of this evidence. Every mutation was applied to
that tree, built, run against its exact owning test, and restored to the technical bytes before the
accepted positive reruns.

## Result

The local Azure Table closure is green. Locked Engineering restore succeeds. The complete
Engineering Release build succeeds with zero warnings and zero errors. The unfiltered native
Microsoft Testing Platform profiles pass at 1946/1946 UnitArchitecture and 87/87 LocalIntegration,
with zero failure and zero skip. The LocalIntegration run used fresh runner-owned PostgreSQL and
Azurite containers, dynamic loopback ports, a run-scoped identity and guaranteed teardown. The
focused Azure Table UnitArchitecture module passes 43/43; the Azure Table LocalIntegration module
contributes 25/25 to the complete local profile.

The fourteen UnitArchitecture CTRF files sum exactly to 1946 passed tests. The three
LocalIntegration CTRF files sum exactly to 87 passed tests. Raw console output, compressed restore
and build binlogs, provider endpoint metadata and broker logs are bound under `positive/`. No
credential or fixed resource name is stored in the evidence.

The repository tooling remains green after the evidence set is materialized: 253/253 CI self-tests,
103/103 identity self-tests, the complete verification model and the generated 8055-entry
CHANGELIST all pass. These focused tool results do not reinterpret the separately known historical
full-identity baseline findings as part of this technical Azure Table gate.

## Product corrections proved before test acceptance

- one shared Azure Table key authority enforces non-empty values, the official 1024-character
  boundary, forbidden characters, control characters and non-empty correlation identifiers;
- built-in and custom saga-key formatters cross the same fail-fast boundary before network I/O;
- load and write operations preserve causal caller cancellation and the exact token/exception;
- insert treats only an actual HTTP 409 as the duplicate race; other storage failures retain their
  identity;
- update and delete use the exact persisted ETag, and a missing ETag fails before TableClient I/O;
- persisted values fail closed when conversion is impossible, including malformed TimeSpan text;
- public configuration/factory boundaries reject missing owners before deferred execution; and
- the stale Cosmos-named load context is now the internal `AzureTableLoadSagaRepositoryContext`.

The unfiltered first pass also exposed an inherited lost-wakeup race in the KillSwitch test driver.
Count and signal are now sampled atomically under their transition lock. Its focused class passes
17/17 and the complete 1946er rerun is green. This is a test-infrastructure fix, not a product
workaround.

## Mutation closure

`MUTATION_MANIFEST.json` contains exact baseline hashes, replacements, occurrence counts, mutant
hashes, complete build/test arguments, expected failures and post-restore hashes for M01 through
M12. Each mutant built successfully and exited 2 in its exact owner for the documented causal
reason. The machine-readable CTRF files report the exact predeclared case count and zero skips.
`SHA256SUMS` binds every raw mutation build log, test log and CTRF result.

## R0 and cloud boundary

`R0_TERMINAL_DISPOSITIONS.json` contains exactly 39 unique inherited/source-derived obligations:
26 `REPLACED_EXECUTING`, 10 `SUPERSEDED_BY_PO_DECISION` for the removed suite-audit design and 3
`EXTERNAL_PENDING`. The latter are intentionally not local green claims:

- `OBL-R0-PER-0455`: Cosmos DB for Table compatibility and service-side 429 behavior;
- `OBL-R0-PER-0456`: Entra ID authentication and token refresh; and
- `OBL-R0-PER-0457`: real Azure service limits and service-only response behavior.

Azurite proves the real local `Azure.Data.Tables` path: serialization, partitions, ETags,
transactions, conflict behavior, sagas, Futures, routing slips, job service and the optional
default-off MessageJournal. It does not prove Azure identity, WAN/network policy, Cosmos
compatibility or cloud-only service limits. Those three cases remain visibly bound in `TODO.md` and
are not represented by skips or inventory-only tests.

The obsolete inherited NUnit project was removed atomically after the 39-row disposition and the
focused/local gates were green. All 18 files are deleted and no empty directory remains.

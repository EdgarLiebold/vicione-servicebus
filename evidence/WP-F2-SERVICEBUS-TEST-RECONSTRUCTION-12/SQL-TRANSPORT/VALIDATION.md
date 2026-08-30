# SQL transport native closure validation

Technical subject: `99665084c190f2affc9540c383a9d11172824ce0`, tree
`765093f8acc43ca997e88f377c890293cebd985e`. The cumulative SQL range begins at
the previously accepted commit `0e31f083fefe22ab1c19b5c76c06d1f22bb7f0e6`.

## Terminal obligation and retirement closure

The frozen map contains exactly 135 unique obligations, `OBL-R0-SQL-0001..0135`.
Exactly 134 are `REPLACED_EXECUTING`; only `OBL-R0-SQL-0054`, the obsolete
cross-provider runtime dialect resolver, is explicitly
`OBSOLETE_RUNTIME_RESOLVER_RETIRED`. The executing mapping is split into 43
UnitArchitecture and 91 LocalIntegration obligations.

The complete inherited `tests/Transports/ViciOne.ServiceBus.SqlTransport.Tests`
project is absent: 37 C# files, its project, compose file and lock file were
deleted as one 40-file terminal retirement. Its directory and the provider
subdirectories are absent, and there are no empty directories below
`tests/Transports`. The inherited workflow category and expected-file owner are
also removed; the verification model now classifies SQL as native test estate.

## Positive verification

- locked Engineering restore: exit 0, with raw log and 3.0 MiB binary log;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors, with raw
  log and 4.4 MiB binary log;
- complete UnitArchitecture: 2,399 passed, 0 failed, 0 skipped across 21 CTRF
  files, above the independent accepted floor of 2,394;
- SQL hermetic module within that run: 45 passed, comprising 43 R0 carriers,
  one requirement-projection carrier and one independent run-scoped database
  identity carrier;
- LocalIntegration base: 244 passed across the exact seven pre-SQL modules;
- PostgreSQL: 71 passed against fresh run `vicione-5ab275b37bdf`;
- SQL Server: 60 passed against fresh run `vicione-0e1ddd33bf22`;
- LocalIntegration aggregate: exactly 375 passed, 0 failed, 0 skipped;
- Architecture: 137 passed; CI self-tests: 257 passed; identity self-tests:
  148 passed; verification model: PASS.
- identity evidence regeneration: PASS with 0 findings, 4,206 live Git-identity
  bindings, 24,987 current public declarations and 44,090 total declaration
  records.

The canonical x64 CI contract remains one 375-test LocalIntegration command.
On this ARM64 workstation, the exact same module set was run as 244 + 71 + 60
so the emulated amd64 SQL Server 2025 fixture did not share one long-lived
compose process with the other five brokers. This changes neither discovery nor
the accepted aggregate and avoids treating an emulation-process failure as a
product or test failure.

Every provider run used a fresh runner-owned identity, loopback-only projected
endpoints, guarded teardown and an empty fixture-findings set. Broker logs are
bound without the private run-root token. The database-name carrier independently
proves stability within one run and inequality across two different run roots.

## Mutation closure

M01-M05 independently weaken SQL Server transient selection, run-scoped database
identity, SQL address validation, unique concurrent schema provisioning and the
stored-procedure delivery identifier. Every mutant target was changed at exactly
one occurrence, built successfully with 0 warnings and 0 errors, then made its
named behavioral carrier red with MTP exit 2. M05 intentionally runs five
variants: the three delivery-id-dependent cases fail and two controls stay green.
M04 and M05 use fresh SQL Server fixtures with empty guarded-teardown findings.

All four target files were restored to their exact Technical-tree SHA-256 before
the final locked restore, Engineering build and positive runs. The compressed
patches, build logs/binlogs, CTRFs, fixture projections and broker logs are bound
by `SHA256SUMS`.

The full Engineering binlog necessarily contains historical assembly identifiers
from the compiled graph. Its exact blob SHA-256 is therefore added to the narrow
`HISTORICAL_EVIDENCE` policy; this authorizes only that immutable evidence blob
and does not create a product, source-tree or wildcard exception.

## Test quality

The new tests use xUnit 4 on Microsoft Testing Platform v2, source-mirrored
namespaces, passive requirement JSON and deterministic provider acknowledgements.
No new Skip, `Thread.Sleep`, wall-clock `Task.Delay`, absence-only oracle or
shared mutable database identity is present. Concurrent SQL declarations use a
positive start barrier and the same provider-classified transient retry policy as
the product; the retry is bounded and does not weaken the exact final schema and
row-count assertions.

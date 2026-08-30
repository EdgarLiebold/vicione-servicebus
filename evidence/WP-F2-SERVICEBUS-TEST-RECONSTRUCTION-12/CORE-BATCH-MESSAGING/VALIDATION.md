# Core batch and messaging validation

## Frozen subject

- Technical parent: `620cc00465bb691dcafe9df60003bc51b264c58b`
- Technical commit: `2007f55f2f5cdc668392da30b9ac9cf5727fa781`
- Technical tree: `b7acc14d56d50facdb91e8c1d664375485fff526`
- Technical delta: exactly the 27 paths in `TECHNICAL_SCOPE.json`; the deterministic gzip patch is
  `TECHNICAL.patch.gz`.

## Cohort result

- The frozen map contains exactly 56 unique obligations and all 56 are `REPLACED_EXECUTING` in
  `UnitArchitecture`.
- Thirty-five newly materialized cases plus strengthened existing carriers execute 39 focused cases.
- Ten fully replaced legacy files are deleted atomically. They contain 2,820 deleted legacy lines;
  no empty directory remains below `tests/ViciOne.ServiceBus.Tests`.
- The Technical commit adds 1,844 lines and deletes 2,832 lines, a net reduction of 988 lines.

## Positive execution

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode --disable-build-servers /bl:<unit-restore.binlog>
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore --disable-build-servers /bl:<unit-build.binlog>
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 2514 --max-parallel-test-modules 1 --report-xunit-ctrf --parallel none
```

- Focused: 39/39, zero failed/skipped.
- Complete UnitArchitecture: 2,519/2,519 across 21 CTRFs, zero failed/skipped/pending/other,
  above the fail-closed floor of 2,514.
- Locked restore and Release build: exit 0; build has zero warnings and zero errors.
- Scoped `dotnet format --verify-no-changes`: exit 0 with a bound binlog.
- After mutation restoration, the affected project builds with zero warnings/errors and the same
  39 focused cases pass again.

## Causal mutation closure

M01–M08 each replace exactly one product-code occurrence against the Technical bytes. Every mutant
builds and its intended carrier turns red. The axes are batch size closure, keyed grouping,
consumer/saga admission, failed-outbox discard, consume-context payload propagation, inherited
endpoint conventions, mandatory mediator publish and explicit runtime send-pipe propagation.
Baseline, mutant and restored SHA-256 values are recorded in `MUTATION_MANIFEST.json`; every restored
hash equals its baseline.

One exploratory mutation against `MessageConsumeContext.TryGetPayload` remained green because that
helper is not the owner of the tested send/publish projection. It was discarded and replaced by M05
at the actual `TransferConsumeContextHeaders` owner; it is not counted as a killed mutant.

## Repository gates and environment diagnosis

- Verification Model: PASS.
- CI tool tests: 257/257.
- Identity tool tests: 148/148.
- Generated CHANGELIST: PASS.
- Evidence inventory: 69 files including `SHA256SUMS`; all 68 nonmanifest files are hash-bound.
- Generated CHANGELIST: 10,143 entries (`4,493` added, `4,162` modified, `1,486` deleted,
  `2` renamed).
- The first diagnostic CI-tool invocation was accidentally sandboxed and failed only where macOS
  denied `ps`. The canonical acceptance invocation was repeated outside the sandbox and is the
  bound 257/257 raw log. This is the known environment boundary, not a product/build failure.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.

# Core serialization native closure validation

## Frozen subject

- Technical parent: `cdc9fb65c00298e68a20e1246ab1604ce1c56341`
- Technical commit: `564050280b901c93a59e883fab809616397512ed`
- Technical tree: `da8ca336f108d1eefc4ad1c62a7278fd80e779a8`
- Technical delta: exactly the 25 paths in `TECHNICAL_SCOPE.json`; `TECHNICAL.patch.gz`
  is its deterministic binary patch.

## Cohort result

- The frozen map contains exactly 58 unique obligations and all 58 are `REPLACED_EXECUTING` in
  `UnitArchitecture`.
- Nine newly materialized cases plus strengthened existing carriers execute 14 focused cases across
  the Core and MessagePack test assemblies.
- Eleven fully replaced legacy serialization files are deleted atomically. They contain 1,865 old
  test lines; the complete Technical package adds 814 lines and deletes 1,991, a net reduction of
  1,177 lines.
- The old physical `tests/ViciOne.ServiceBus.Tests/Serialization` directory no longer exists. The
  historical frozen identity baseline remains untouched.

## Positive execution

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers /bl:<technical-engineering-restore.binlog>
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers /bl:<technical-engineering-build.binlog>
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 2523 --max-parallel-test-modules 1 --fail-skips on --report-xunit-ctrf --parallel none /bl:<unit-test-dotnet-test.binlog>
```

- Focused serialization execution: 14/14, zero failed/skipped.
- Complete UnitArchitecture: 2,528/2,528 across 21 CTRFs, zero failed/skipped/pending/other,
  above the fail-closed floor of 2,523.
- Locked Engineering restore, Technical Release build and post-mutation Engineering Release build
  exit zero. Both builds report zero warnings and zero errors.
- Scoped `dotnet format --verify-no-changes` exits zero with a bound binlog.

## Causal mutation closure

M01–M06 each replace one exact product-code occurrence against the frozen Technical bytes. Every
mutant builds and its intended native carrier turns red. The six axes are System.Text.Json interface
admission, ReceiveFault exception materialization, external MessageData references, raw transport
type headers, redelivery MessageId replacement and endpoint-owned MessagePack response selection.
Baseline, mutant and restored SHA-256 values are bound in `MUTATION_MANIFEST.json`; every restored
hash equals its Technical baseline.

One exploratory removal of the explicit ReceiveFault converter registration stayed green because
the focused round trip starts from the concrete `ReceiveFaultEvent`. It was restored, discarded and
replaced by M02 at the actual exception-information owner; it is not counted as a killed mutant.

## Repository gates and environment

- Verification Model: PASS.
- CI tool self-tests: 257/257.
- Identity self-tests: 148/148.
- Generated CHANGELIST: 10,200 entries, PASS.
- Evidence inventory: 58 files including `SHA256SUMS`; all 57 nonmanifest files are hash-bound.
- All .NET/MSBuild acceptance invocations ran outside the macOS sandbox because its process-query
  denial is a known environment boundary. Every MSBuild invocation has a binlog.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.

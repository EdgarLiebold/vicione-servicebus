# Core Courier native closure validation

## Frozen subject

- Technical parent: `d2608691fa27139702684652dd0e889fe12b175e`
- Technical commit: `369f776e1fe53911062151e718efe11e6dae3cd0`
- Technical tree: `766ce3e9f1b99ac2725fe8fc7886005638e3b243`
- Technical delta: exactly the 41 paths in `TECHNICAL_SCOPE.json`; `TECHNICAL.patch.gz`
  is its deterministic binary patch.

## Cohort result

- The frozen map contains exactly 81 unique obligations, `OBL-R0-CORE-C-0018` through
  `OBL-R0-CORE-C-0098`, and all 81 are `REPLACED_EXECUTING` in `UnitArchitecture`.
- Thirty-six native xUnit 4/MTP 2 cases in ten source-mirrored Courier test files bind the selected
  routing-slip construction, argument, payload, lifecycle, request, retry, revision, subscription,
  host, partitioning and fault contracts.
- Twenty-three fully replaced legacy Courier files are deleted atomically. They contain 2,937 old
  test lines; the complete Technical package adds 2,436 lines and deletes 2,942, a net reduction of
  506 lines while the fail-closed executable floor rises from 2,523 to 2,559.
- The physical `tests/ViciOne.ServiceBus.Tests/Courier` directory no longer exists. The frozen
  historical identity baseline remains untouched.

## Positive execution

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers /bl:<technical-engineering-restore.binlog>
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers /bl:<technical-engineering-build.binlog>
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory <run-root> --minimum-expected-tests 2559 --max-parallel-test-modules 1 --fail-skips on --report-xunit-ctrf --parallel none /bl:<unit-test-dotnet-test.binlog>
```

- Focused Courier execution: 36/36, zero failed/skipped.
- Complete UnitArchitecture: 2,564/2,564 across 21 CTRFs, zero
  failed/skipped/pending/other, above the fail-closed floor of 2,559.
- Requirement projection: 1/1, proving the 81 obligation rows resolve to the declared executable
  carriers.
- Locked Engineering restore, Technical Release build and post-mutation Engineering Release build
  exit zero. Both builds report zero warnings and zero errors.
- Scoped `dotnet format --verify-no-changes` exits zero with a bound binlog.

The 21 complete CTRFs and the valid gzip MSBuild binlog independently bind the complete test
execution; every restore, build, test and format invocation has its own binlog.

## Causal mutation closure

M01–M12 each replace one exact product-code occurrence against the frozen Technical bytes. Every
counted mutant builds and its intended native carrier turns red. The axes bind argument fallback,
latest compensation selection, immediate revised-itinerary discard, subscription content filtering,
fault projection, global serializer callback ownership, execute/compensate retry attempts, both
string partitioner overloads, explicit subscription suppression and event argument fallback.
Baseline, mutant and restored SHA-256 values are bound in `MUTATION_MANIFEST.json`; every restored
hash equals its Technical baseline. The mutation executions aggregate to 20 cases: 17 causal
failures and three deliberately independent controls that remain green.

Two exploratory candidates stayed green at the wrong executed owners: D01 mutated the per-message
serializer callback while Courier uses the global callback, and D02 mutated the Guid partitioner
overload while Courier uses the string overload. Both were restored, retained transparently under
`discarded/`, replaced by M06/M08 and are not counted as killed mutants.

## Repository gates and environment

- Verification Model: PASS.
- CI tool self-tests: 257/257.
- Identity self-tests: 148/148.
- Generated CHANGELIST: 10,310 entries, PASS.
- Evidence inventory: 99 files including `SHA256SUMS`; all 98 nonmanifest files are hash-bound.
- The macOS sandbox denies the process enumeration used by eight CI self-tests. The canonical CI
  self-test run therefore executes outside the sandbox and is bound separately; this is an
  environment boundary, not a product or test bypass.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.

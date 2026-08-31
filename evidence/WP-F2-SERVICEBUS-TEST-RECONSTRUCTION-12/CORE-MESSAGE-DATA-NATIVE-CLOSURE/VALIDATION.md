# Core MessageData native closure validation

## Frozen subject

- Technical parent: `c3791969c38e957d7847a63593acfc20466f3d24`
- Technical commit: `06c5d2ede430a728759624d40b2b904431098284`
- Technical tree: `7d8d3d7fefac8297a24446fc030d657d2f4dd0ac`
- Technical range: four direct commits from the parent through the frozen Technical commit.
- Technical delta: exactly the 24 paths in `TECHNICAL_SCOPE.json`; `TECHNICAL.patch.gz`
  is the deterministic binary patch for the complete range.

## Cohort result

- The terminal map contains exactly 27 unique obligations, `OBL-R0-CORE-C-0103` through
  `OBL-R0-CORE-C-0129`, all `REPLACED_EXECUTING` in `UnitArchitecture`.
- Twenty-two new xUnit 4/MTP 2 cases in five source-mirrored MessageData files bind repository,
  inline/stored, filesystem, encryption, transform, initializer, nested collection, application
  object, large JSON, publish and both request-client paths. The focused namespace also contains
  one pre-existing property-provider carrier, so focused execution is 23 cases.
- Ten fully replaced inherited files and 1,643 legacy lines are deleted atomically. The complete
  Technical range adds 1,416 lines and deletes 1,684, a net reduction of 268 lines while the
  executable floor rises from 2,559 to 2,581.
- The physical `tests/ViciOne.ServiceBus.Tests/MessageData` directory is absent. The frozen
  `build/verification/expected/core.txt` historical list is unchanged.

## Product correction discovered by the native carrier

`EncryptedMessageDataRepository` previously returned a malformed query derived from
`NameValueCollection.ToString()` rather than the inner repository's exact address. A filesystem
repository ignored the query and concealed the defect; a key-addressed repository exposed that the
wrapper could not read the address it returned. The Technical correction now:

- returns and reuses the exact inner address;
- applies the configured crypto provider's default key consistently to encrypt and decrypt;
- rejects null dependencies, addresses and streams at the public boundary; and
- disposes the owned inner stream when decrypt-stream construction fails while preserving the
  original exception.

These are product behaviors, not test-only accommodations, and are covered by exact round-trip,
ciphertext-at-rest, address, exception-identity and ownership assertions.

## Positive execution

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode --disable-build-servers /p:NuGetAudit=false /bl:<technical-engineering-restore.binlog>
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Engineering.slnx --configuration Release --no-restore --disable-build-servers /m:1 /nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false /bl:<technical-engineering-build.binlog>
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --results-directory <run-root> --minimum-expected-tests 2581 --max-parallel-test-modules 1 --fail-skips on --report-xunit-ctrf --parallel none /bl:<unit-test-dotnet-test.binlog>
```

- Focused MessageData execution: 23/23, zero failed/skipped.
- Complete UnitArchitecture: 2,586/2,586 across 21 CTRFs, zero
  failed/skipped/pending/other, above the fail-closed floor of 2,581.
- Requirement projection: 1/1, proving all new requirement variants resolve to compiled xUnit
  owners; the separate 27-row map binds every inherited obligation to those carriers.
- Locked Engineering restore and Technical Release build exit zero. The build reports zero warnings
  and zero errors.
- After all mutations were restored, the detached worktree received its own complete locked
  Engineering restore, complete Engineering Release build, focused test-project build and 23/23
  positive control. All exit zero and all product hashes equal the frozen Technical bytes.
- Scoped `dotnet format --verify-no-changes` exits zero with a bound binlog.

## Causal mutation closure

M01-M10 each replace one exact product-code occurrence against the frozen Technical product bytes.
Every mutant builds and exactly one intended carrier turns red. The axes bind ciphertext at rest,
decrypt-on-read, owned-stream cleanup, default-key use, string and byte inline thresholds, mandatory
stream storage, synchronous stored-reference loading, application-object storage and System.Text.Json
object conversion.

Every mutation has a deterministic gzip-compressed patch, build binlog/log, valid single-test CTRF, causal failure and restored
SHA-256 in `MUTATION_MANIFEST.json`. M02 deliberately compares numeric character sequences rather
than printing arbitrary ciphertext as raw characters; this preserves exact content equality while
keeping the machine-readable CTRF valid for all byte values.

D01 is retained transparently as a green wrong-owner exploration and is not counted. D02 is the
pre-correction M08 run whose product mutant was killed only by the configured timeout; it is retained
as a superseded diagnostic. Final M08 surfaces the original `MessageDataException` immediately, so
no counted mutation uses a timeout as its causal oracle.

## Repository gates and environment

- CI tool self-tests: 257/257.
- Identity self-tests: 148/148 after the complete Evidence inventory and generated CHANGELIST are
  staged.
- Verification Model: PASS.
- Generated CHANGELIST: PASS after the Evidence inventory is complete.
- The macOS sandbox can stall the full restore and denies Roslyn/process named-pipe operations. The
  identical locked restore, full builds, MTP run and scoped format verification therefore execute
  outside only that sandbox boundary. The out-of-sandbox restore completed normally in about ten
  seconds; this confirms an environment boundary rather than a product workaround.
- A first detached post-mutation Engineering build correctly failed because only the focused test
  project had been restored there. The verification order was corrected to a complete locked
  Engineering restore followed by the same no-restore Engineering build, which passed with 0/0.

No cloud fixture was required. No remote push was performed. Independent read-only acceptance,
architecture binding and any later remote publication remain separate actions.

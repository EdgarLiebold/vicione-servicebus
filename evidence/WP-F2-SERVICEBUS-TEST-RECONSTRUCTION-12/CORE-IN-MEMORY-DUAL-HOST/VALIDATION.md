# In-memory dual-host retirement validation

Technical subject: `47f73aa331a58328b133ccf9d105cd1d824ffccd`, tree
`4e279274d7365b1285457738b4c64b5197baca78`, direct parent
`e3b449e291afb948ecdf53c13a482aebcba6cd6e`. The behavioral replacement is commit
`c124e4cda59838b6b38d7a2e44725385c2b2b839`; its first child adds independent fail-fast host
preconditions and its second child binds the generated CHANGELIST and exact historical-identity
line inventory.

## Stronger native replacement

`OBL-R0-CORE-D-0175` is bound exactly once to the xUnit 4/MTP v2 Fact
`InMemoryTransportIsolationTests.DistinctVirtualHosts_RequireOneExplicitRelayAndPreserveTheOriginalSource`.
The inherited NUnit case observed only final delivery and could pass with a shared transport. The
native carrier proves two distinct run-scoped loopback virtual hosts before traffic, one exact
external-to-internal forward, one exact loop-suppression decision, the original message id and source
address at every hop, distinct input addresses, and exactly one real delivery after both buses are
stopped and drained. It uses positive transport observations and the configured operation timeout;
there is no sleep or absence-only terminal verdict.

The fully replaced `tests/ViciOne.ServiceBus.Tests/InMemoryDuo_Specs.cs` is deleted. Its containing
legacy project remains because unrelated inherited files still exist. The repository-wide empty
directory scan below `tests` and `tests2` returns no result.

## Verification

- restored focused carrier: 1/1 passed, 0 failed, 0 skipped;
- passive Core requirement projection: 1/1 passed;
- complete final UnitArchitecture execution: 2,416/2,416, 0 failed or skipped;
- complete final Engineering Release build: exit 0, 0 warnings, 0 errors;
- focused Release build: exit 0, 0 warnings, 0 errors;
- scoped `dotnet format --verify-no-changes --no-restore`: exit 0;
- CI tool tests: 257/257 passed outside the restricted process sandbox;
- identity self-tests: 148/148 passed;
- verification model: PASS;
- generated CHANGELIST: PASS with 9,810 entries;
- final identity scan: PASS with 0 findings across 5,654 baseline paths, 4,196 live
  Git-bound targets, 24,987 current product declarations, 1,711 current-added declarations and
  44,090 public-declaration records.

M01 replaces the sole virtual-host projection with the root loopback address. The mutant builds with
zero warnings and errors and the carrier fails immediately at the exact internal BaseAddress
precondition. M02 replaces the copied original SourceAddress with the external input queue address.
That mutant also builds and completes the relay, but the carrier fails at the exact source-identity
assertion. Both product files are restored to the exact Technical-tree SHA-256 values before final
verification.

No remote push is part of this evidence operation.

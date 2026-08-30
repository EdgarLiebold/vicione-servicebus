# In-memory outbox filter retirement validation

Technical subject: `917f54f60a5c237b1d6be477c383d4c800d91c4e`, tree
`b68e12b805b89b8ae8a07ecb7bc8d1ccd622d1f6`, direct parent
`58bee41e3c82b08e42e88b605875040ef8849a55`.

## Native replacement

`OBL-R0-CORE-D-0176` through `OBL-R0-CORE-D-0181` are bound exactly once to six xUnit 4/MTP v2
Facts in `InMemoryOutboxFilterTests`. The carriers exercise the real generic filter with the real
`InMemoryOutboxConsumeContext` and independently bind: the service-scope payload read by the
filter; absence of a bus-bound setter; failure cleanup and original-exception identity; post-pipe
pending-action execution; and scoped-context restoration on both successful and failed delivery.

The tests are deterministic and hermetic. They use event sequences and exception/object identity,
not elapsed time, sleeps, external services or absence-only terminal verdicts. The fully replaced
`InMemoryOutboxDirectPath_Specs.cs` and `InMemoryOutboxLifecycle_Specs.cs` files are deleted. Their
containing legacy project remains because unrelated inherited files still exist. The repository-wide
empty-directory scan below `tests` and `tests2` returns no result.

## Verification

- restored focused carrier: 6/6 passed, 0 failed, 0 skipped;
- passive Core requirement projection: 1/1 passed;
- complete final UnitArchitecture execution: 2,422/2,422, 0 failed or skipped;
- complete final Engineering Release build: exit 0, 0 warnings, 0 errors;
- focused Release build: exit 0, 0 warnings, 0 errors;
- scoped `dotnet format --verify-no-changes --no-restore`: exit 0, 0 files formatted;
- CI tool tests: 257/257 passed outside the restricted process sandbox;
- identity self-tests: 148/148 passed;
- verification model: PASS.
- full identity Evidence scan: PASS with 0 findings across 5,654 baseline paths, 4,196 live
  Git-bound targets, 24,987 current product declarations, 1,711 current-added declarations and
  44,090 public-declaration records.
- generated CHANGELIST: PASS with 9,848 entries.

M01 removes the missing-setter guard and is killed by the direct-path carrier at the real product
dereference. M02 removes successful pending-action execution and is killed by the exact ordered
event assertion. M03 removes failure discard and is killed when a test-owned post-failure drain
observes the forbidden surviving action. M04 removes scoped-context restoration; the two success
and failure restoration carriers fail while the four independent control cases remain green. Every
mutant builds with zero warnings and errors, and the product file is restored after every run to the
Technical-tree SHA-256 `8c5ec3bdc809809357457ea6049b9e276bedda960e76cb12dd01d73fc0c24c33`.

No remote push is part of this evidence operation.

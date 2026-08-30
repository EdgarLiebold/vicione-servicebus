# Consumer request/response retirement validation

Technical subject: `1e86d84b46a0b6ca2febd3dec4002f82e1a59e30`, tree
`85587426fd17b1fd3e30abc19dfebcb24ee99244`, direct parent
`f88ff7a45a29660592b548e291a11f5f38236a5b`. The behavioral retirement is commit
`f88ff7a45a29660592b548e291a11f5f38236a5b`; its direct child binds the deterministic
CHANGELIST deletion and exact historical-identity policy digest.

## Terminal replacement without duplicate tests

`OBL-R0-CORE-D-0033` is bound once to the existing xUnit 4/MTP v2 Fact
`DependencyInjectionTestHarnessTests.ScopedRequestClient_RecordsTheExactRequestAndResponse`.
The inherited specification merely awaited an unasserted response. The native carrier is stronger:
it proves the exact request, response and sent correlation identities, preserves the request id and
requires both terminal observations with no consume or send exception. Adding a duplicate Fact would
inflate the suite without adding a behavioral boundary, so the native test count and 2,410 floor
truthfully remain unchanged.

The fully replaced `Consumer_Specs.cs` is deleted. The legacy project root remains because unrelated
inherited files still exist; a repository-wide check below `tests` and `tests2` returns no empty
directory.

## Verification

- focused native carrier: 1/1 passed, 0 failed, 0 skipped;
- passive Core requirement projection: 1/1 passed;
- current complete UnitArchitecture execution: 2,415/2,415, 0 failed or skipped;
- the unchanged 21-file CTRF closure for the identical product/test binaries remains hash-bound in
  the direct ancestor Evidence commit `d717536dc94b32d47fcbcb28f0e3cd691a0806cb`;
- locked Engineering restore: exit 0;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- CI tool tests: 257/257 passed outside the restricted process sandbox;
- verification model: PASS;
- identity self-tests: 148/148 passed;
- final generated CHANGELIST: PASS with 9,765 entries;
- final identity scan: PASS with 0 findings, 5,654 baseline paths, 4,197 live baseline targets with
  Git identity, 24,987 current public declarations, 1,711 current-added declarations and 44,090
  public-declaration records.

M01 removes the single product line attaching the configured scoped consumer to its receive
endpoint. The mutant builds with zero warnings and errors; the endpoint records zero consumers and
the real request deterministically ends in `RequestTimeoutException`, killing the carrier. The
product file is restored to its exact Technical-tree SHA-256 before final verification.

No remote push is part of this evidence operation.

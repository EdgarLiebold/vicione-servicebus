# C10 future-location acceptance evidence

## Accepted boundary

`Futures/FutureLocationTests.cs` mirrors `src/ViciOne.ServiceBus/Futures/FutureLocation.cs`. It
replaces the inherited randomized round trip with a fixed external identifier and closes the
ledger's known missing-ID branch. Greenfield hardening covers the rest of the same public parsing
boundary: empty, malformed, and duplicate IDs, null constructor inputs, and an address without an
endpoint name, and relative URIs.

The initial red run reproduced four product defects: null input leaked `NullReferenceException`,
empty and malformed IDs leaked parser `ArgumentException`, and duplicate IDs leaked LINQ
`InvalidOperationException`. The product was corrected rather than the tests weakened. Invalid
stored locations now expose one stable `FormatException`; invalid direct constructor arguments use
the appropriate `ArgumentNullException` or `ArgumentException` with the public parameter name.

## Inherited closure

- `OBL-R0-CORE-D-0168` is terminally mapped to the exact fixed-ID round trip and endpoint
  normalization case.
- `OBL-R0-CORE-D-0470` is terminally mapped to the invalid-ID-query Theory, including its original
  missing-ID boundary.
- The null and missing-endpoint cases are explicit greenfield hardening of the same source owner.

Only after the positive replacements and product correction passed was
`tests/ViciOne.ServiceBus.Tests/FutureLocation_Specs.cs` removed.

## Positive acceptance

All commands ran from the repository root against Release outputs:

1. Focused core project: 181 total, 181 passed, 0 failed, 0 skipped.
2. Complete UnitArchitecture profile with `--minimum-expected-tests 689`: 689 total, 689 passed,
   0 failed, 0 skipped.
3. `ViciOne.ServiceBus.Tests.Unit.slnx` Release build: 0 warnings, 0 errors.
4. `ViciOne.ServiceBus.Engineering.slnx` Release build: 0 warnings, 0 errors.
5. Bounded `dotnet format --verify-no-changes` for the product and test files: exit 0.
6. Requirement projections, JSON parsing, `git diff --check`, and the no-skip/no-filter review pass.

## One-cause false-green attacks

Each mutation was applied to the real product or requirement boundary, rebuilt, executed, and
restored before final acceptance:

1. Retaining the full source endpoint address instead of creating the short queue address made only
   the round-trip case fail on its literal scheme oracle.
2. Leaking parser and LINQ exceptions instead of translating them to the public format boundary made
   the malformed-ID and duplicate-ID rows fail on exact exception type.
3. Removing the direct-address null guard made only the null-input case fail on exact exception type.
4. Removing the passive round-trip requirement attribute made the ordinary projection case fail
   because the projected variant had no compiled owner.

## Final-state integrity

No mutation remains. The product change is limited to explicit validation and exception translation
inside `FutureLocation`; its valid round-trip representation is unchanged. Generated output remains
under ignored `artifacts/`, and no root `TestResults` directory or transient result is tracked.

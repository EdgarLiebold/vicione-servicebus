# C9 array-message-publication acceptance evidence

## Accepted boundary

The inherited method name `Should_not_allow_array_message_types_but_does` contradicts its own
assertion and is therefore not retained as a requirement name. Its observed compatibility behavior
is retained explicitly: a public one-dimensional array is a valid single message contract. It is
not the ServiceBus batch-consumer abstraction and the test does not present it as one.

`Serialization/ArrayMessageTypeTests.cs` uses the public in-memory harness and the default
System.Text.Json envelope pipeline. It publishes one three-element array to a handler for the exact
array type and verifies the received element count, order, sequence values, and text values. The
test owns its public message contract and has no dependency on the inherited TestFramework.

The other two methods in the mixed inherited file were already replaced by the accepted C3
MessageUrn source-owner cohort. C9 therefore closes the last open obligation before removing the
entire inherited file.

## Inherited closure

- `OBL-R0-CORE-D-0260` is terminally mapped to
  `ArrayMessageTypeTests.PublicOneDimensionalArray_IsDeliveredAsOneOrderedMessage`.
- `OBL-R0-CORE-D-0261` and `OBL-R0-CORE-D-0262` remain terminally mapped by
  `C3-MESSAGE-URN/INHERITED_BEHAVIOR_DISPOSITION.json`.

## Positive acceptance

All commands ran from the repository root against Release outputs:

1. Focused core project: 172 total, 172 passed, 0 failed, 0 skipped.
2. Complete UnitArchitecture profile with `--minimum-expected-tests 680`: 680 total, 680 passed,
   0 failed, 0 skipped.
3. `ViciOne.ServiceBus.Tests.Unit.slnx` Release build: 0 warnings, 0 errors.
4. `ViciOne.ServiceBus.Engineering.slnx` Release build: 0 warnings, 0 errors.
5. Bounded `dotnet format --verify-no-changes` for the new C# file: exit 0.
6. Requirement projections, JSON parsing, `git diff --check`, and the no-skip/no-filter review pass.

## One-cause false-green attacks

Each mutation was applied to the real boundary, rebuilt, executed, and restored before final
acceptance:

1. Rejecting arrays in `MessageTypeCache<T>.CheckIfValidMessageType` made exactly the new core case
   fail during real bus topology construction; the other 171 core cases remained green.
2. Removing the passive requirement attribute from the new test made the ordinary requirement
   projection case fail because the projected variant had no compiled owner; the other 171 core
   cases remained green.

The successful behavior test itself would fail if serialization changed the element count, order,
or either property value because it compares all three received records against literal external
oracles.

## Final-state integrity

No mutation remains. The only inherited source deletion is the fully replaced mixed fixture.
Generated test output remains under ignored `artifacts/`; no root `TestResults` directory or raw
transient output is part of the accepted change.

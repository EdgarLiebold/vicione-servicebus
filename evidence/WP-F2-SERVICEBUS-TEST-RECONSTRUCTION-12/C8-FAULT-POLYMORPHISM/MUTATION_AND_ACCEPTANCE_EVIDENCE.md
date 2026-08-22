# C8 polymorphic-fault acceptance evidence

## Accepted boundary

The inherited `tests/ViciOne.ServiceBus.Tests/FaultPoly_Specs.cs` mixed two source owners and five
semantic-ledger obligations. Its replacement is intentionally split:

- `ViciOne.ServiceBus.Abstractions.Tests/Metadata/FaultMessageTypeTests.cs` owns exact `Fault<T>`
  type and URN metadata for class and interface hierarchies;
- `ViciOne.ServiceBus.Tests/Contexts/ConsumeContextEndpointExtensionsTests.cs` owns real fault
  generation and polymorphic publication through the in-memory transport.

The core test uses the public temporary-receive-endpoint API directly. It does not derive from,
copy, or emulate the inherited NUnit TestFramework fixture. The operation timeout is read through
the one typed native-test configuration pipeline, whose default remains `tests2/testsettings.json`
and whose supported override is `VICIONE_TESTS__OperationTimeout`.

The base-fault consumer proves both message views deliberately exposed by the product contract:
the base message is available directly from `Fault<Base>.Message`, and the complete derived
message is available through `ConsumeContext.TryGetMessage<Fault<Derived>>`. It also verifies the
fault identity, host metadata, exception type, exact original supported-message-type set, and the
actual derived-fault publication.

## Inherited closure

The terminal disposition file maps all five inherited obligations:

- `OBL-R0-CORE-D-0159` and `OBL-R0-CORE-D-0161` to the exact class-hierarchy metadata case;
- `OBL-R0-CORE-D-0160` and `OBL-R0-CORE-D-0162` to the exact interface-hierarchy metadata case;
- `OBL-R0-CORE-D-0163` to the real derived-interface-to-base-fault publication case.

Only after these replacements passed was the inherited mixed file removed.

## Positive acceptance

All commands ran from the repository root against Release outputs:

1. Focused Abstractions project: 123 total, 123 passed, 0 failed, 0 skipped.
2. Focused core project: 171 total, 171 passed, 0 failed, 0 skipped.
3. Complete UnitArchitecture profile with `--minimum-expected-tests 679`: 679 total, 679 passed,
   0 failed, 0 skipped.
4. `ViciOne.ServiceBus.Tests.Unit.slnx` Release build: 0 warnings, 0 errors.
5. `ViciOne.ServiceBus.Engineering.slnx` Release build: 0 warnings, 0 errors.
6. Bounded `dotnet format --verify-no-changes` for both new C# files: exit 0.
7. Requirement projections, JSON parsing, `git diff --check`, and the no-skip/no-filter review pass.

## One-cause false-green attacks

Every completed mutation was applied to the real product or test boundary, rebuilt, executed, and
then restored before the final acceptance run:

1. Reversing the `Fault<T>` hierarchy projection condition in `MessageTypeCache<T>` made both new
   metadata cases fail on exact missing/duplicate types.
2. Replacing `FaultEvent<T>.Exceptions` with an empty set made the core case fail on its exception
   assertion.
3. Replacing `FaultEvent<T>.FaultMessageTypes` with an empty set made the core case fail on its exact
   supported-type assertion.
4. Removing the passive requirement attribute from the core test made the normal xUnit projection
   test fail because the projected variant had no compiled owner.

An additional topology-removal experiment was stopped before producing a result because its
deliberately mutated rebuild did not finish within the diagnostic limit. It is not counted as
evidence. The same topology edge already has completed one-cause coverage in the accepted C7
implemented-message-topology cohort; C8 independently proves the live route through its successful
base-fault endpoint.

## Final-state integrity

No product mutation remains in the accepted working tree. The only removed file is the fully
replaced inherited fixture. Generated results remain under ignored `artifacts/`; no repository-root
`TestResults` directory or raw transient test output is part of the change.

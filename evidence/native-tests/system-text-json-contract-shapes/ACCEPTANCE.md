# System.Text.Json contract-shape acceptance

## Scope

This cohort replaces eight frozen obligations from four inherited NUnit files:

- `ExtensionData_Specs.cs`;
- `JsonSerialization_Specs.cs`;
- `MisnamedProperty_Specs.cs`;
- `PolymorphicProperty_Specs.cs`.

`SOURCE_FILE_CLOSURE.json` maps every obligation exactly once to seven ordinary native xUnit cases.
No product source, project file, package declaration, lock file, or build policy changed.

## Accepted verification

- locked restore of `ViciOne.ServiceBus.Tests.Unit.slnx`: exit 0, dependency graph unchanged;
- non-incremental Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: exit 0, zero warnings,
  zero errors;
- focused Core source-owner profile: 365 passed, zero failed, zero skipped;
- unfiltered native MTP UnitArchitecture profile with `--minimum-expected-tests 882`: 882 passed,
  zero failed, zero skipped;
- remaining inherited `ViciOne.ServiceBus.Tests.csproj` after all four deletions: exit 0, zero
  warnings, zero errors;
- Release build and unfiltered native MTP LocalIntegration profile: 3 passed, zero failed, zero
  skipped;
- bounded formatter run followed by `--verify-no-changes`: exit 0;
- closure validation: eight listed, eight distinct, no missing, unexpected, or duplicate obligation
  IDs;
- five valid one-cause mutations: all rejected for their intended converter, contract, value, or
  projection reason; details are in `MUTATION_VALIDATION.md`.

Sandbox-only failures caused by denied named-pipe access are not counted as product or test results.
The unchanged commands were rerun in the permitted execution context and passed as listed above.

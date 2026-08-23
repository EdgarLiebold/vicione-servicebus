# MessageBody contract acceptance

## Scope

The cohort replaces `tests/ViciOne.ServiceBus.Tests/Serialization/MessageBodyLength_Specs.cs`.
`SOURCE_FILE_CLOSURE.json` maps all 87 frozen obligations exactly once: 51 to the accepted
Abstractions owner, six to the accepted MessagePack owner, and 30 to the Core or cross-owner
closure implemented by this cohort.

No product source, project file, package declaration, lock file, or build policy changed.

## Accepted verification

- locked restore of `ViciOne.ServiceBus.Tests.Unit.slnx`: exit 0;
- non-incremental Release build of `ViciOne.ServiceBus.Tests.Unit.slnx`: exit 0, zero warnings,
  zero errors;
- unfiltered native MTP UnitArchitecture profile with `--minimum-expected-tests 875`: 875 passed,
  zero failed, zero skipped;
- Release build of `tests2/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`: exit 0,
  zero warnings, zero errors;
- focused Core source-owner profile: 358 passed, zero failed, zero skipped;
- Release build of
  `tests2/ViciOne.ServiceBus.MessagePack.Tests/ViciOne.ServiceBus.MessagePack.Tests.csproj`:
  exit 0, zero warnings, zero errors;
- focused MessagePack source-owner profile: 50 passed, zero failed, zero skipped;
- remaining inherited project build before and after deleting the fixture: exit 0, zero warnings,
  zero errors;
- Release build and unfiltered native MTP LocalIntegration profile: 3 passed, zero failed, zero
  skipped;
- bounded formatter run followed by `--verify-no-changes`: exit 0;
- closure validation: 87 listed, 87 distinct, no missing, unexpected, or duplicate obligation IDs;
- ten valid one-cause mutations: all rejected for their intended behavior, census, or projection
  reason; details are in `MUTATION_VALIDATION.md`.

Sandbox-only failures caused by denied named-pipe access or unavailable restore networking are not
counted as product or test results. The unchanged commands were rerun in the permitted execution
context and passed as listed above.

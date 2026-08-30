# Core transform native closure validation

Technical subject: `901e6d6d854b1a8231224dc468a50e1816b2a9e8`, tree
`03b05696fefa122f3f746f11912c456437034c56`, direct parent
`ea8bf9cf07c5890be46819d5ec072ef1d54d21d0`.

## Terminal replacement

The exact nine obligations `OBL-R0-CORE-C-0441..0449` are each mapped once to an
executing xUnit 4 / Microsoft Testing Platform v2 carrier. The new source-mirrored
`Transformation/TransformPipelineTests.cs` proves separate send and publish pipelines,
endpoint replacement identity in both modes, handler-interface targeting with an
independently connected control endpoint, and class-based configuration of every owned
property.

All three fully replaced inherited files are deleted. Their now-empty
`tests/ViciOne.ServiceBus.Tests/Transforms` directory was removed, and a repository-wide
empty-directory check below the remaining inherited test project returned zero paths.

## Positive verification

- focused transform tests: 9/9 passed, 0 failed, 0 skipped;
- requirement projection: 1/1 passed;
- complete UnitArchitecture profile: 2,408/2,408 passed across 21 CTRF files,
  0 failed, 0 skipped, above the declared 2,403 floor;
- locked Engineering restore: exit 0;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- CI tool self-tests: 257/257 passed outside the restricted sandbox;
- identity self-tests: 148/148 passed;
- verification model and generated 9,602-entry CHANGELIST: PASS;
- regenerated identity evidence: PASS with 0 findings, 4,203 live baseline Git
  identities, 24,987 current product public declarations and 44,090 total records.

The CI tests were deliberately rerun outside the filesystem/process sandbox. The same
suite inside that sandbox reported eight infrastructure errors because `ps` is denied;
the exact unrestricted local rerun passed 257/257. This is the recorded solution for
this repository's recurring sandbox-only compile/tool-test symptom.

## Mutation closure

M01-M04 independently remove replacement identity, send-filter installation,
consume-filter installation and class-based specification construction. Every mutant
changed one exact occurrence, built with 0 warnings and 0 errors, and made only its
named behavioral carrier red with MTP exit 2. All four files were restored to their
exact Technical-tree SHA-256 before the final positive restore, build and full test run.

The tests contain no skip, sleep, wall-clock-delay, absence-only oracle or inherited
fixture dependency. The handler-specific negative control owns a separately connected
receive endpoint and waits for its positive ready signal before publication.

No remote push is part of this evidence operation.

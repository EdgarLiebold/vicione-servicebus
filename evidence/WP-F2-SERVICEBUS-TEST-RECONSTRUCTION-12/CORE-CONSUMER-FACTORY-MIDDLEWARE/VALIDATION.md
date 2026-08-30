# Consumer-factory middleware retirement validation

Technical subject: `5937ebeb0b86e38ca420a482fb5b152e68b701d8`, tree
`f43f69958d562f488056fea3ef159066cc793ca4`, direct parent
`880b23330a8a24466dffe21be54450cb1dad1056`.

## Terminal replacement without duplicate tests

`OBL-R0-CORE-D-0032` is bound once to the existing xUnit 4/MTP v2 theory
`ConsumerMessageConfigurationTests.Registration_ComposesConsumerMessageAndConsumerSpecificMessageLayersExactlyOnce`.
Its ConsumerFactory case is a stronger carrier than the inherited request-only oracle: it proves the
consumer pipe, message pipe, consumer-message pipe and consumer invocation occur exactly once, in
order, with the same exact message and consumer instance. The independent Instance theory case is a
control. Adding another Fact would duplicate already executing behavior, so the native test count
and the 2,410 floor truthfully remain unchanged.

The fully replaced `ConsumerFactoryMiddleware_Specs.cs` is deleted. The legacy project root remains
because unrelated inherited files still exist; a repository-wide check below `tests` returns no
empty directory.

## Verification

- focused native theory: 2/2 passed, 0 failed, 0 skipped;
- passive Core requirement projection: 1/1 passed;
- complete UnitArchitecture: 2,415/2,415 across 21 CTRF files, 0 failed or skipped;
- locked Engineering restore: exit 0;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- CI tool tests: 257/257 passed outside the restricted process sandbox;
- verification model: PASS;
- identity self-tests: 148/148 passed;
- generated CHANGELIST: PASS with 9,754 entries;
- final identity scan: PASS with 0 findings, 5,654 baseline paths, 4,198 live baseline targets with
  Git identity, 24,987 current public declarations, 1,711 current-added declarations and 44,090
  public-declaration records.

M01 removes the single product line forwarding consumer-level pipe specifications. It builds with
zero warnings and errors; only the ConsumerFactory theory case becomes red with three entries instead
of four while the Instance control remains green. The product file is restored to its exact
Technical-tree SHA-256 before the final positive build and tests.

No remote push is part of this evidence operation.

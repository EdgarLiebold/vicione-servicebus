# Core send-topology native closure validation

Technical subject: `a23e9f58bcb7b849a3f36116df16faab3c61d161`, tree
`8e7ccae6aa39493438d9e8ca2249630498637283`, direct parent
`70691a661be7b9384098a6aa9ba0ab7f1728bfdb`.

## Terminal replacement

The exact two obligations `OBL-R0-CORE-C-0428..0429` are each mapped once to an executing xUnit 4 /
Microsoft Testing Platform v2 carrier in the source-mirrored
`ViciOne.ServiceBus.Tests.Topology.Configuration` namespace. One carrier proves the interface,
property and global correlation-selector surfaces through exact envelope and message values. The
other proves that message-specific serializer topology reaches the actual in-memory receive boundary
with raw JSON payload and `application/json` content type.

Both fully replaced inherited Topology files are deleted. Their now-empty
`tests/ViciOne.ServiceBus.Tests/Topology` directory was removed, and a repository-wide empty-directory
check below `tests` returned zero paths.

## Positive verification

- focused send-topology tests: 2/2 passed, 0 failed, 0 skipped;
- requirement projection: 1/1 passed;
- complete UnitArchitecture profile: 2,415/2,415 passed across 21 CTRF files, 0 failed, 0 skipped,
  above the declared 2,410 floor;
- locked Engineering restore: exit 0;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- CI tool self-tests: 257/257 passed outside the restricted sandbox;
- identity self-tests: 148/148 passed;
- verification model: PASS;
- generated CHANGELIST: PASS with 9,709 entries;
- final identity scan: PASS with 0 findings, 5,654 baseline paths, 4,199 live baseline targets with
  Git identity, 24,987 current public declarations, 1,711 current-added declarations and 44,090
  public-declaration records.

The CI tool suite is intentionally run outside the restricted filesystem/process sandbox because
that sandbox denies `ps`. The unchanged outside-sandbox command passes 257/257; no product or test
rule is weakened to accommodate the sandbox.

## Mutation and test-quality closure

M01-M03 independently remove global correlation delegation, omit the configured interface selector
and omit the message-specific serializer. Every mutant changes one exact occurrence, builds with
zero warnings and errors, and makes only its named behavioral carrier red with MTP exit 2. All three
product files are restored to their exact Technical-tree SHA-256 before the final positive build.

The mandatory assertion-quality review found exact payload, envelope identifier and content-type
oracles rather than invocation-only assertions. The anti-pattern and smell reviews found no skip,
sleep, wall-clock-delay, absence-only completion or external-resource dependency. The pseudo-mutation
review is bound by the three independent product mutants above.

No remote push is part of this evidence operation.

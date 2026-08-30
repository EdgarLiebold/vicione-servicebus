# Core message-fabric and topic-routing native closure validation

Technical subject: `266ae0d4369c9ffa6860db4e5a8f2f8fded18be1`, tree
`5a28b0f2f52ab4a6addb4b7c458e31bd2a4b634c`, direct parent
`d2e719e0129784f6fff0ffb3450d7764dbe3be0e`.

## Terminal replacement

The exact five obligations `OBL-R0-CORE-C-0450..0454` are each mapped once to an executing xUnit 4 /
Microsoft Testing Platform v2 carrier in the source-mirrored
`ViciOne.ServiceBus.Tests.Transports.Fabric` namespace. The graph tests prove each declared edge by
object identity and prove that cycle rejection does not install a partial edge. The topic tests
exercise the product exchange and delivery context directly, proving exact received-message sets
for `#`, `car.*` and `car.*.large` without a transport harness or timing oracle.

Both fully replaced inherited Transports files are deleted. Their now-empty
`tests/ViciOne.ServiceBus.Tests/Transports` directory was removed, and a repository-wide empty
directory check below `tests` returned zero paths.

## Positive verification

- focused message-fabric/topic tests: 5/5 passed, 0 failed, 0 skipped;
- requirement projection: 1/1 passed;
- complete UnitArchitecture profile: 2,413/2,413 passed across 21 CTRF files, 0 failed, 0 skipped,
  above the declared 2,408 floor;
- locked Engineering restore: exit 0;
- complete Engineering Release build: exit 0, 0 warnings, 0 errors;
- CI tool self-tests: 257/257 passed outside the restricted sandbox;
- identity self-tests: 148/148 passed;
- verification model and generated 9,657-entry CHANGELIST: PASS;
- regenerated identity evidence: PASS with 0 findings, 4,201 live baseline Git identities,
  24,987 current product public declarations and 44,090 total records.

The CI tool suite is intentionally run outside the restricted filesystem/process sandbox because
that sandbox denies `ps` and previously produced eight infrastructure-only errors. The unchanged
outside-sandbox command passes 257/257; no product or test rule is weakened to accommodate the
sandbox.

## Mutation and test-quality closure

M01-M04 independently remove cycle validation, redirect hash routing, redirect the terminal
single-segment wildcard and redirect the intermediate wildcard. Every mutant changes one exact
occurrence, builds with 0 warnings and 0 errors, and makes only its named behavioral carrier red
with MTP exit 2. Both product files are restored to their exact Technical-tree SHA-256 before the
final positive build and test runs.

The mandatory assertion-quality review found exact object-identity and received-set oracles rather
than invocation-only assertions. The anti-pattern and smell reviews found no skip, sleep,
wall-clock-delay, absence-only completion, inherited fixture or external-resource dependency. The
pseudo-mutation review is bound by the four independent product mutants above.

No remote push is part of this evidence operation.

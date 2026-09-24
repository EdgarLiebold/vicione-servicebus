# Product-wide coverage profile at 162dab6d6

## Measured scope and validation

- Product/test HEAD: `162dab6d6a541035ca40e565d3a8650ebdf61c38`.
  The tracked `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- The Release Unit/Architecture build had zero warnings and errors. The
  unfiltered Unit/Architecture gate passed 10,339/10,339 tests with no skips.
  The Azure Service Bus Unit project passed 318/318, and its local emulator
  project passed 30/30.
- `artifacts/coverage-a-plus-20260924-162dab6d6/raw/` contains 36 parseable
  Microsoft CodeCoverage Cobertura reports for 32 product assemblies. The two
  Azure Service Bus reports (Unit and local emulator) were generated at this
  HEAD. The other 34 reports are individually SHA-256 identical to the
  `0ef230802` profile; none observes the changed Azure Service Bus assembly.
  Eleven broker fixture records are reused; the fresh Azure Service Bus
  fixture is `vicione-a521852b5754`. All 12 records have empty findings.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record
  merged counts, exact CRAP decisions, report hashes, source/binary origins,
  and fixture evidence. Their SHA-256 values are respectively
  `d9c0e7508c136b7baf40c3c8a20dbd7616fb733327bc4b82be54b7f657263e5b`,
  `93fb88c6305fccae49d6c8f9ba39b20e6c2f19724bbd2541710ca2e31417302d`,
  and `0ade25119ddb9571253a5d2498a29f1a94526a4d3d94c87d8914e3f865dc1df0`.

## Product-wide result

| Measure | `162dab6d6` | Previous `0ef230802` |
| --- | ---: | ---: |
| Line coverage | 83,954 / 93,472 = 89.8173% | 83,948 / 93,467 = 89.8157% |
| Branch observation, conservative merge | 30,104 / 36,660 = 82.1167% | 30,101 / 36,660 = 82.1086% |
| Branch observation, capped sum | 32,586 / 36,660 = 88.8871% | 32,583 / 36,660 = 88.8789% |
| Methods with CRAP > 30, exact arithmetic | 38 / 25,985 | 39 / 25,983 |

Azure Service Bus `ServiceBusHostConfigurator.ParseEndpoint` dropped from CRAP
42.19 (complexity 40, 40/45 covered lines) to 6.00 (complexity 6, 15/15).
Its extracted `ParseEndpointItem` and `NormalizeEndpoint` methods have CRAP
20.40 (18/20) and 14.46 (13/15). Five new parser cases check duplicate keys
with mixed casing, malformed segments on both sides of a valid endpoint,
repeated and trailing separators, and exact normalization of a custom-port
scoped endpoint. They assert the public parser's returned URI or exception.

Cobertura provides per-line branch counts without stable branch identities.
The conservative merge uses the largest covered count at each source location;
the capped sum adds observed counts up to the largest valid count. Neither is
an exact union of branch identities. CRAP is
`complexity² × (1 − method line coverage)³ + complexity`, with the `> 30`
threshold checked using exact integer arithmetic.

## Evidence limits and next code areas

- This is a source-equivalent incremental coverage comparison, not 36 fresh
  executions at `162dab6d6`. Only Azure Service Bus product/test source changed
  since the fresh `0ef230802` profile; reports that could observe that assembly
  were regenerated. The full unfiltered Unit/Architecture gate and Azure Service
  Bus emulator run were executed at this HEAD.
- Reused reports retain their earlier binary and fixture provenance. Fresh
  Azure Service Bus reports record the HEAD-built binaries and new fixture.
  Successful command exits were observed in this session, but test execution
  logs were not persisted. Report, analysis, binary, and fixture hashes are
  persisted under the ignored artifact directory.
- Existing user-owned untracked `TestResults/` and `review/` were left intact.
- Three `ConsumeObserverConverter<T>` callbacks in Abstractions are the largest
  remaining CRAP hotspots at 42 each with 0/9 covered lines. Their supported
  runtime path and behavior must be determined before tests or product changes.

The A+ line, branch, and CRAP objective remains open. The full method ranking
is in `analysis-36/methods.json`.

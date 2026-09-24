# Product-wide coverage profile at 0ef230802

## Measured scope and validation

- Product/test HEAD: `0ef23080241b6f13e6604bafa31356273ea70ee0`.
  The tracked `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- The Release Unit/Architecture build had zero warnings and errors. The
  unfiltered Unit/Architecture gate passed 10,334/10,334 tests with no skips.
  The Azure Service Bus Unit project passed 313/313; the two directed mixed-client
  emulator regressions passed within the 30/30 local-provider run; the compiled
  local requirement projection passed separately 1/1.
- `artifacts/coverage-a-plus-20260924-0ef230802/raw/` contains 36 fresh,
  parseable Microsoft CodeCoverage Cobertura reports: 22 Unit, 13 local-provider,
  and one Abstractions run with `DOTNET_EnableAVX2=0` (786/786 passed). All
  12 broker fixture records have empty findings. The reports observe 32 product
  assemblies.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` record
  merged counts, exact CRAP threshold decisions, report hashes, binary inventory,
  and fixture evidence. Their SHA-256 values are respectively
  `fa7b35c8680316d224fb1e4e5017e2a7eae7a63a936417817d7be13162ed2170`,
  `18044165673f99c18fbc5928671b697e5235c3d069ee725cabb98aedaf767599`,
  and `e3afb1242e1b0b3367a85e46c88bd885ec0a4a58e04d97806063ee70d71fb5d7`.

## Product-wide result

| Measure | `0ef230802` | Previous `a8c79a588` |
| --- | ---: | ---: |
| Line coverage | 83,948 / 93,467 = 89.8157% | 83,918 / 93,514 = 89.7384% |
| Branch observation, conservative merge | 30,101 / 36,660 = 82.1086% | 30,083 / 36,664 = 82.0505% |
| Branch observation, capped sum | 32,583 / 36,660 = 88.8789% | 32,566 / 36,664 = 88.8228% |
| Methods with CRAP > 30, exact arithmetic | 39 / 25,983 | 40 / 25,979 |

Azure Service Bus `ConnectionContextFactory.CreateConnection` dropped from CRAP
50.00 (complexity 44, 41/48 covered lines) to 2.00 (complexity 2, 9/10).
Its new helpers have CRAP 8.83–14.00 in the merged profile. The existing
custom-port test now includes named-key and SAS rejection. Two real emulator
tests prove that the factory fills in either missing client while preserving
the supplied one: queue administration, exact message ID and body, and
message completion succeed across separate management and AMQP ports.

Cobertura provides per-line branch counts without stable branch identities.
The conservative merge uses the largest covered count at each source location;
the capped sum adds observed counts up to the largest valid count. Neither is
an exact union of branch identities. CRAP is
`complexity² × (1 − method line coverage)³ + complexity`, with the `> 30`
threshold checked using exact integer arithmetic.

## Evidence limits and next code areas

- The two emulator cases establish connection-string routing and mixed-client
  behavior. They do not establish live Azure authentication with TokenCredential,
  AzureNamedKeyCredential, or AzureSasCredential. Those routes retain their
  previous branch order and constructor overloads by source review, but no
  live-namespace credential outcome is claimed.
- The additional Unit and provider SDK output roots were cloned from the prior
  profile, then the changed Azure Service Bus provider and test project were
  rebuilt at this commit. The four additional Unit reports do not observe Azure
  Service Bus; their isolated output root still contains an unused older Azure
  Service Bus DLL copy, which is excluded from the source-binary inventory.
  Unchanged binaries may carry older informational revisions. Successful
  command exits were observed in this session, but test
  execution logs were not persisted. Report, script, binary, and fixture hashes
  are persisted under the ignored artifact directory.
- Existing user-owned untracked `TestResults/` and `review/` were left intact.
- The largest remaining hotspot is Azure Service Bus
  `ServiceBusHostConfigurator.ParseEndpoint`: CRAP 42.19, complexity 40, 40/45
  covered lines. Its segment-validation and URI-normalization stages can be
  separated after adversarial review of the existing malformed-input contracts.
  Three `ConsumeObserverConverter<T>` callbacks in Abstractions follow at CRAP
  42 each with 0/9 covered lines; their supported runtime use must be determined
  before adding tests or changing this public type.

The A+ line, branch, and CRAP objective remains open. The full method ranking
is in `analysis-36/methods.json`.

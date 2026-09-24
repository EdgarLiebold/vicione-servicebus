# Product-wide coverage profile at 6352ef3e9

## Measured scope

- Product/test HEAD: `6352ef3e9648882078f5aa863eff0c119e835655`.
  The tracked `src`/`tests` diff was empty during final analysis (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- The 36 successful Microsoft CodeCoverage Cobertura reports in
  `artifacts/coverage-a-plus-20260924-6352ef3e9/raw/` observe all 32 product
  assemblies: 22 Unit, 13 local-provider, and one Abstractions portability run
  with `DOTNET_EnableAVX2=0`. The supplementary run passed 786/786 tests.
- The Azure Service Bus Unit and emulator product/test binaries were rebuilt
  at this HEAD after an adversarial review found precommit binaries in the
  first collection attempt. The replacement runs passed 311/311 and 28/28.
  Only their replacement reports are counted. The full Unit/Architecture
  solution passed 10,332/10,332 tests; the Release build had zero warnings
  and errors.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` contain
  report hashes, 75 product-binary hashes, the HEAD, and 12 fixture records.
  Their SHA-256 values are respectively
  `082c978d3b6943abd7cc0085efe8939261cf53597229c35a2e724b3caf31b6a4`,
  `7ffc8ca839405cd15037c1e41ff9df1df099fa861cea716334296af8b0029a26`,
  and `87185538d7b957fe2dda1f8fcda100f7ff45b1d6e61b662369f6049fe6236b1c`.
  All 12 fixture records have empty `findings` lists.

## Product-wide result

| Measure | `6352ef3e9` | Previous `b4d370ac5` |
| --- | ---: | ---: |
| Line coverage | 83,935 / 93,515 = 89.7557% | 83,851 / 93,466 = 89.7128% |
| Branch observation, conservative merge | 30,091 / 36,666 = 82.0679% | 30,008 / 36,616 = 81.9532% |
| Branch observation, capped sum | 32,570 / 36,666 = 88.8289% | 32,475 / 36,616 = 88.6907% |
| Methods with CRAP > 30 | 42 / 25,974 | 45 / 25,971 |

The changed source and instrumentation alter line and branch denominators.
Cobertura supplies per-line branch counts without stable branch identities.
The conservative merge takes the largest covered count at each source
location; the capped sum adds observations up to the largest valid count.
Neither is an exact union of branch identities. CRAP is
`complexity² × (1 − method line coverage)³ + complexity`.

## Excluded attempts and evidence limits

- Initial ActiveMQ coverage omitted Artemis; initial Event Hubs coverage
  omitted Azurite. Their incomplete reports are under `excluded/`. The counted
  runs used the complete fixtures and passed 100/100 and 61/61 respectively.
- Two Azure Service Bus emulator reports and one Unit report used binaries
  built before `6352ef3e9`; all three are under `excluded/`. The
  counted Unit and emulator reports were regenerated from rebuilt product and
  test binaries carrying revision `+6352ef3e9`.
- Other assemblies unchanged between `5ebda66d2` and `6352ef3e9` were
  executed after `6352ef3e9`, but some binaries had been built before that
  commit. The intervening tracked `src`/`tests` changes affect only Azure
  Service Bus. Report XML does not independently prove the binary loaded;
  the binary inventory is a post-run record. Execution output was observed
  in this session but not stored as persistent logs. The hash manifest binds
  the available reports, scripts, binaries, and fixture records.
- User-owned untracked `TestResults/` and `review/` were excluded and left
  untouched.

## Next code areas

- Azure Service Bus `ConnectionContextFactory.CreateConnection` now has CRAP
  50.00, complexity 44, and 41/48 covered lines. Its revised validation paths
  need behavior tests and a careful extraction.
- JobService endpoint configuration has CRAP 44 despite 28/28 covered lines.
  The existing partitioning regression test directly proves all 18
  registrations, shared coordination, same-job serialization across message
  types, and progress of another partition. It is an extraction candidate.
- Azure Service Bus `ServiceBusHostConfigurator.ParseEndpoint` has CRAP 42.19,
  complexity 40, and 40/45 covered lines. The host/auth/port behavior is now
  exercised by hard unit and emulator tests, so splitting its parsing stages
  is another source-level simplification candidate.
- The three Abstractions `ConsumeObserverConverter<T>` callbacks have CRAP
  42 each and 0/9 covered lines. Test their actual observer dispatch path if
  it is used, or remove them only after proving that path is dead.

The A+ line, branch, and CRAP objective remains open. The complete method
ranking is in `analysis-36/methods.json`.

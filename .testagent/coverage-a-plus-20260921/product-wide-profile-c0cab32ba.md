# Product-wide coverage profile at c0cab32ba

## Scope and evidence

- Exact product/test HEAD: `c0cab32ba6b6bffe5e1e412e6010ccd071edfa47`.
  The tracked `src`/`tests` diff during aggregation was empty (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
- All 36 fresh Cobertura reports under
  `artifacts/coverage-a-plus-20260923-c0cab32ba/raw/` are parseable and
  observe all 32 product assemblies: 22 Unit/Infrastructure, 13 local-provider,
  and one supplementary Abstractions run with `DOTNET_EnableAVX2=0`. The
  36 counted test commands passed 11,109 executions, zero failures and skips.
  The portability command repeats 768 Abstractions tests.
- The four successful provider fixtures were `vicione-f01c7e1319ba` (six
  brokers with ActiveMQ outage control), `vicione-e3cf09ca933b` (SQL Server),
  `vicione-ee014114d797` (RabbitMQ), and `vicione-79ecc01af672` (Azure Service
  Bus). Their findings lists are empty. The local-provider graph built with
  zero warnings and errors. Immediately before collection, the complete
  Engineering Release build and Unit/Architecture gate passed with 10,234 of
  10,234 tests, zero failures or skips; see `saga-instance-equality-phase.md`.
- The first broad-provider fixture attempt exited before a usable report
  because its isolated build had received a relative MSBuild overlay path and
  the Abstractions provider test assembly exposed zero tests. The runner
  cleaned up that fixture (`vicione-540508702560`). This setup failure was
  observed during execution; its build/test console output was not archived.
  Rebuilding with an
  absolute overlay path restored the three expected Abstractions provider
  tests; the successful fixture above supplied the counted reports.
- `analysis-36/summary.json`, `methods.json`, and `provenance.json` contain
  the aggregation, method list, exact HEAD, and SHA-256 hashes of all 36
  reports, seven execution logs, analysis files, collection scripts, 77
  product binaries, four fixture records, and their broker logs.

## Product-wide result

| Measure | Current `c0cab32ba` | Previous `b6ffcfbdf` |
| --- | ---: | ---: |
| Line coverage | 83,751 / 93,419 = 89.6509% | 83,703 / 93,411 = 89.6072% |
| Branch observation, conservative merge | 29,989 / 36,672 = 81.7763% | 29,947 / 36,668 = 81.6707% |
| Branch observation, capped sum | 32,463 / 36,672 = 88.5226% | 32,426 / 36,668 = 88.4313% |
| Methods with CRAP > 30 | 59 / 25,943 | 63 / 25,943 |

Four selected methods moved below CRAP 30 in the full profile:
`Bind<TKey,TValue>.Equals(object)`, `SagaInstance<TSaga>.Equals(object)`,
`AmazonSqsEndpointAddress.TopicAddress`, and
`ScheduleSendPipe<T>.ScheduledMessageId` each moved from CRAP 42 to CRAP 6.
The absolute covered-line increase is 48, while the valid-line count grew by
eight due to source changes. Run variability elsewhere can also affect the
line totals, so the net difference is not attributed wholly to these tests.

Cobertura supplies per-line branch counts without stable branch identities.
The conservative observation takes the largest hit count at each branch
location; the capped sum adds observations up to the largest valid count.
Neither is a formal lower or upper bound on true combined branch coverage.
CRAP is `complexity² × (1 − method line coverage)³ + complexity`, following
the Microsoft `coverage-analysis` skill. The no-AVX2 setting is recorded by
its invocation, not independently proven by its XML. Hashes preserve files
but do not alone prove that each report came from the recorded binary or
commit; attribution also relies on the ordered commands and unchanged
tracked source/test files.

The A+ goal remains open for line and branch coverage and for the 59 methods
above CRAP 30. The next bounded slice should prioritize a product contract
or safely reduce excessive complexity, with adversarial review before
claiming a method-level improvement.

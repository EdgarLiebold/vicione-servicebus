# Cumulative product coverage profile at e8976fc62

## Scope and validation

- Product/test HEAD: `e8976fc622467446bb4be08409f44138ae84b622`.
  No `src` file changed since the preceding `9307916a4` profile. The tracked
  `src`/`tests` diff was empty at analysis time (SHA-256
  `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`).
  The intervening changes add source-owned Azure Service Bus fault-shutdown
  tests and repair a RabbitMQ test synchronization race.
- A full Release Unit/Architecture build passed with zero warnings and errors.
  The serial full Unit/Architecture gate passed 10,350/10,350, with zero
  failures and zero skipped tests. Its persisted log is
  `artifacts/coverage-a-plus-20260924-e8976fc62/unit-architecture-gate.log`
  (SHA-256 `b4f8050cd7411a6514363d54553e511acf84072de96667f6d64230d3ba3ea682`).
  A preceding full gate at `2ee536129` failed one RabbitMQ test whose fake
  signaled before removing its shutdown handler; isolated 2/2 and project-wide
  395/395 runs passed. Both successful removal paths now wait for a signal
  after removal. The Red Team reviewed this correction and the Azure tests.
- The fresh Azure Service Bus project run under Microsoft CodeCoverage passed
  326/326 at this HEAD. The new tests themselves pass 8/8. Its Cobertura report
  is SHA-256 `0f33cc619c20e90a673bc355ab34e04904b4d3ea00d8e5a490cb7c40e8fd3e6b`.
- `artifacts/coverage-a-plus-20260924-e8976fc62/raw/` contains 39 parseable
  Cobertura reports for 32 product assemblies. The 38 inherited reports are
  individually hash-identical to the preceding profile; one Azure Service Bus
  report was added at this HEAD. These are cumulative project slots and repeat
  observations, not 39 executions at this HEAD. All 12 broker fixture records
  are inherited; no new broker run occurred in this iteration.

## Product-wide cumulative result

| Measure | `e8976fc62` | Previous `9307916a4` |
| --- | ---: | ---: |
| Line coverage | 84,015 / 93,480 = 89.8748% | 83,987 / 93,480 = 89.8449% |
| Conservative branch observation | 30,126 / 36,660 = 82.1768% | 30,112 / 36,660 = 82.1386% |
| Methods with CRAP > 30, exact arithmetic | 34 / 25,989 | 36 / 25,989 |

`QueueClientContext.NotifyFaultedAsync` and
`SubscriptionClientContext.NotifyFaultedAsync` each move from CRAP 42 with
zero of seven covered lines to CRAP 6 with seven of seven. Both asynchronous
`StopAfterCallbackAsync` state-machine bodies also have seven of seven lines
covered and CRAP 2. The tests distinguish callback deferral, concurrent and
overlapping first faults, pre-cancellation, stop-failure logging, and a later
retry. The sixteen-thread start barrier exercises the initial race in four
rounds per context; its precise interleaving remains scheduler-dependent.

## Merge method and limits

- The raw reports are retained unchanged. The existing JobSaga source overlay
  still excludes that changed file from exactly eleven old reports and takes
  its current classes only from the fresh Core report in the preceding profile.
  The new provider report contains no JobSaga source classes. No product source
  changed in this iteration, so the new provider observation can be merged
  with the inherited reports by source location and method identity.
- `analysis-39/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record counts, report and binary hashes, exact method
  threshold decisions, source continuity, and the fixture chain. Their SHA-256
  values are respectively
  `93f60614255018217d2539ed73f98d502f843d529aaa5af32dc774ac9396785e`,
  `acfd5f97b7858f0ded49e0f61abe1492710e85059cb22666d7a360f2e2cc4aab`,
  `d00b98ef94a48ccae181a90078f4f4c4ced4c427f2229a816830eb52fdc66b75`,
  and `8519e16e85359ba64959ffbcb372b1b95301527f7e34f5319dc73326ee1d9c8e`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. Its capped sum is
  inflated by repeated observations and is not used for comparison. CRAP is
  `complexity² × (1 − method line coverage)³ + complexity`, with the `> 30`
  threshold checked using exact integer arithmetic. The analyzer requires all
  four targeted Azure methods to have all seven lines covered in the fresh
  report.
- The successful full-gate log is persisted. The CodeCoverage command exit was
  observed but its console log was not persisted. Existing user-owned untracked
  `TestResults/` and `review/` were left intact. The next ranked uncovered
  methods include three `ConsumeObserverConverter<T>` callbacks with no static
  product call site, Amazon SQS client creation and probe methods, and Azure
  Service Bus stream/batching helpers. Functional use must guide further tests.

The A+ line, branch, and CRAP objective remains open. The full ranking is in
`analysis-39/methods.json`.

# Cumulative product coverage profile at d415d2cbf

## Scope and validation

- Product/test HEAD: `d415d2cbf9ab25fc667df4ae9d674ab54aefb511`.
  The only changed `src` file since `08d41e4b6` is
  `src/ViciOne.ServiceBus/RetryPolicies/PipeRetryExtensions.cs`.
  The commit also adds cancellation ownership tests, requirement mappings,
  and a changelog entry. The adversarial test and refactoring review returned
  PASS after three test weaknesses were corrected.
- The Release Unit/Architecture solution was rebuilt after the commit with
  zero warnings and errors. Its log SHA-256 is
  `5abe9a5a313c561b84917ecf5b060f04281deafcfd7e28aef023e8b52aeca013`.
  All ten manifest-listed fresh binaries embed `d415d2cbf` as their
  informational revision. The subsequent serial full gate passed
  **10,357/10,357** with zero failures and skipped tests. Its log SHA-256 is
  `a475c45adf74cbf7514291665dbef089c7138cd27cb2a6bdf6585fe4aa7547d1`.
- The fresh Core run with Microsoft CodeCoverage and
  `tools/ci/coverage.settings.xml` passed **6,392/6,392** with zero failures
  and skips. Its log SHA-256 is
  `f7038e9f4a5e9135b6d2017d223a6238e0440748f965f95f21661a73fb1b3ff3`;
  the Cobertura report SHA-256 is
  `958c085da275cd8e16c263115a0dbf0b77077ac5c8ebf6143c42c4fac2f33890`.
- `artifacts/coverage-a-plus-20260924-d415d2cbf/raw/` contains 42 parseable
  Cobertura reports for 32 product assemblies. Forty-one reports are
  individually hash-identical to the preceding profile; the Core report is
  new at this HEAD. These are cumulative observations, not 42 executions at
  this HEAD. All 12 broker fixture records are inherited; no new broker run
  occurred in this iteration.
- An initial same-source build still embedded the parent commit revision.
  The adversarial provenance audit rejected that candidate. The final build,
  gate, coverage report, analysis, and manifest above were regenerated after
  the `d415d2cbf` rebuild; the rejected logs were overwritten.

## Product-wide cumulative result

| Measure | `d415d2cbf` | Previous `08d41e4b6` |
| --- | ---: | ---: |
| Line coverage | 84,053 / 93,487 = 89.9088% | 84,051 / 93,486 = 89.9076% |
| Conservative branch observation | 30,143 / 36,664 = 82.2142% | 30,143 / 36,664 = 82.2142% |
| Methods with CRAP > 30, exact arithmetic | 31 / 25,992 | 32 / 25,991 |

The former `PipeRetryExtensions.ExecuteAsync<TResult>` iterator exceeded
CRAP 30. Retry preparation is now a separate method. The fresh Core report
shows `ExecuteAsync<TResult>` at **36/36** lines and CRAP **20**, and
`PrepareRetryAsync` at **24/24** lines and CRAP **18**. Tests check
operation-owned cancellation, caller cancellation during the operation,
independent caller or policy cancellation during pre-retry, and caller
precedence when both tokens cancel. They verify token identity, attempt and
decision counts, and callback order.

## Merge method and limits

- `PipeRetryExtensions.cs` changed, so exactly 35 inherited reports
  containing that source file are excluded for it. Only the new Core report
  contributes that file's classes, lines, branches, and methods. The older
  JobSaga overlay still excludes eleven stale reports. The Serialization
  overlay excludes 35 stale reports and retains its fresh report from the
  preceding profile. All other product source files are unchanged since
  `08d41e4b6`.
- `analysis-42/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record the exact merge, exclusions, report and binary
  hashes, fixture inheritance, and CRAP decisions. Their SHA-256 values are
  respectively `016a26cb67991ca708d45bd1684b2c80018ba3deb5887af3781200985421bb7f`,
  `ddaf7cef2a9737d928e7115b2fd4282f92b09aca41755491324274d638616046`,
  `081f6f3271b5157c3a965f9380ac75fb4e98b3284fd424ceab0bed3a85c4d05c`,
  and `2bdba242bdf921c55a273b8b300f3d63c44189df0d45d91fd0605594cc05023b`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. The capped sum can
  overstate repeated observations and is not used for the quality comparison.
  CRAP is `complexity + complexity² × (1 − method line coverage)³`.
- The Microsoft `code-testing-agent`, `run-tests`, and `coverage-analysis`
  skills guided test design, execution, and risk measurement. The
  `test-gap-analysis` and `assertion-quality` review closed three Red Team
  findings: caller cancellation normalization had a weak oracle, only one
  linked-token cancellation direction was tested, and blocked tasks lacked
  cleanup after early assertions.

Global A+ remains open: line and branch coverage are below A+, and 31 methods
remain above CRAP 30.

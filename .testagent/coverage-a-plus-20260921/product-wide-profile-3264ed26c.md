# Complete product-wide measurement at 3264ed26c

Measured commit: `3264ed26c3914a44fff2ef3291750b766efda870`.
Source tree: `6d9e72b9ec42bac96f35f0fed1d684227b23dca0`.
Tests tree: `b92bcfdbc51b69a7185aed3d2c6a42b9b472dc95`.
This report is a documentation successor, not another measured commit.

## Validated results

All 33 profiles were freshly measured: 20 unit/CPU and 13 local integration
profiles. All four canonical fixture groups exited zero including cleanup.
The strict aggregate validator returned `complete`; no failed or skipped run
contributes. SQL Server passed 75/75 in 5m49s without restarting its live run.

Read-only adversarial accounting review independently verified 487 file hashes,
all receipt commits, counts and the five-closed/six-new gap delta. No accounting
blocker was found. This does not determine the causes of the six new gaps.

| Measure | Result |
| --- | --- |
| Passing test executions, including CPU repetitions | 12,607 |
| Product assemblies | 32 |
| Lines | 85,101 / 93,762 = 90.7627824% |
| Conservative branches | 30,732 / 36,841 = 83.4179311% |
| Method identities, including compiler-generated identities | 26,061 |
| Methods with CRAP strictly above 30 | 0 |
| Line-gap identities | 4,507 |
| Zero / partial line coverage | 2,799 / 1,708 |
| Additional branch-only candidates | 1,504 |
| Union of line-gap and branch-only candidates | 6,011 |

Branch counts use maximum covered/valid counts per line and identity across
profiles, not a union of individual branch IDs. Method identities are not
independent product contracts. Neither this measurement nor CRAP <=30 proves A+.

## Cancellation results and honest delta

All four targeted methods now have full line and observed branch coverage:

| Method | Lines | Branches | CRAP |
| --- | --- | --- | --- |
| PendingConfirmationCollection.Canceled | 3/3 | 2/2 | 2 |
| PendingConfirmationCollection.Cancel | 3/3 | 2/2 | 2 |
| PendingConfirmation.Canceled | 2/2 | 0/0 | 1 |
| ClientRequestHandle.Cancel | 10/10 | 2/2 | 2 |

[Behavioral tests, adversarial review and deliberate mutations](t30-cancellation-followup.md)
establish meaningful terminal-state assertions for these paths. No product
source was changed in this packet.

Compared with 90239365c, covered lines decrease by six and conservative covered
branches increase by seven. Five previous line-gap identities close, while six
previously complete identities now show gaps. Line-gap count therefore increases
by one; branch-only candidates decrease by five. Do not portray this as uniform
coverage improvement or attribute all observed changes to the new tests.

The six newly observed gaps remain explicit investigation candidates:

- EF reliable store AwaitConsumerCompletionAsync: 9/14 lines; missing 235–239.
- SagaInstance.MarkInUseAsync: 13/16; missing 109, 110, 112.
- EventHubs producer wrapper ProduceAsync: 3/4; missing 92.
- EF Saga transaction RollbackAsync local function: 3/5; missing 269, 272.
- EventHubs producer ProduceAsync local function: 0/3; missing 88–90.
- FutureExtensions.AddSubscription callback: 0/2; missing 38–39.

These are missing execution observations, not proven source regressions. Their
causes have not been established; do not dismiss them as harmless timing noise.
The full identities and paths are in the hashed delta below.

| Partial-coverage hotspot | Complexity | Covered lines | CRAP |
| --- | ---: | ---: | ---: |
| DynamoDB repository configurator Validate body | 14 | 8/14 | 29.428571 |
| CronExpression.StoreExpressionValues | 26 | 15/18 | 29.129630 |
| CronExpression.ProgressNextFireTimeDay | 26 | 16/19 | 28.661029 |
| RabbitMqAddressExtensions.GetConnectionFactory | 28 | 43/46 | 28.217473 |
| OutboxMessagePipe.DeliverOutboxMessagesAsync body | 28 | 31/33 | 28.174528 |

Next bounded packet: SQS mixed-result mapping and atomic duplicate-ID rejection,
with independent per-caller outcomes. Its read-only plan review requires complete
ID accounting before adding duplicates, distinct caller/owner tokens and exact
malformed-response exceptions for every caller. The wider gap inventory and
the six newly observed gaps remain open. Roslyn API/comment audit follows actual
A+ coverage completion.

## Local evidence fingerprints

Raw artifacts remain ignored local files; these hashes do not publish them.

| artifacts/ file | SHA-256 |
| --- | --- |
| t30-aggregate.json | 18907c8743c2ec7febb13e9a772e7752f3ce382ab7db92bf17317874aeb8bb49 |
| t30-all-methods.json | b2111634927a6f409db1a12e82fbcf9d40f2ddb23190536613591808373d1851 |
| t30-method-gaps.json | 98c09dc7d720f6c6eaa2db02ce97409169af2323f85eb7a39fb1c9a53b6c75fc |
| t30-branch-only-gaps.json | d912d87256abd2db915c32a56b8119316d1abf787e71ba476bd3a2bf6716aeae |
| t30-profile-progress.json | 14a08b7f340167b8fb92628564a4326c5eb3b24e7eedd02ce59f9353f1e64ee7 |
| t30-gap-delta.json | cce2fc760a63f79f2a87c1cccb7988341fd9c3c132424b1cf3267cfe2b5d48ff |

Orchestration: artifacts/t30-measure-all.py. Analysis:
artifacts/t30-analyze-complete.py, then artifacts/t30-compare-gaps.py.
Measurement and analysis processes are terminal; no measurement remains live.

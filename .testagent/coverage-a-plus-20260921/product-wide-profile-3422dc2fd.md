# Product-wide profile: 3422dc2fd

Measured commit: `3422dc2fd46687c68af55fba4aa08b76d48536dd`.
Source tree: `6dc2700433374e9108821fd2a16e2f76404108a2` (unchanged from T47).
Test tree: `efbdeb7d1b24dab1d2fa0663a435d61f0f4d7af7`.
Microsoft coverage-analysis workflow with canonical exact-commit receipts.
All 33 profiles pass: 13,020 successful executions across 32 measured assemblies,
zero failures/skips. All four provider fixture groups exit 0. One complete
measurement, no profile retry. Independent integrity and numerical audit passes.

| Metric | T47 | T48 |
| --- | ---: | ---: |
| Covered/valid physical lines | 85,587/93,753 | 85,703/93,753 |
| Line coverage | 91.28988% | 91.41361% |
| Covered/valid conservative branches | 30,906/36,845 | 30,937/36,845 |
| Conservative branch coverage | 83.88112% | 83.96526% |
| Method identities | 26,071 | 26,071 |
| CRAP strictly above 30 | 0 | 0 |
| Methods with line gaps | 4,403 | 4,375 |
| Zero / partial line coverage | 2,749 / 1,654 | 2,720 / 1,655 |
| Additional branch-only gaps | 1,507 | 1,525 |
| Union of gap candidates | 5,910 | 5,900 |

## Behavioral evidence

[T48 tests and counterprobes](t48-multibus-host-ownership.md) bind seventeen new
cases to three requirement variants in the Core test project:

- `HostLifecycle_ReportsEachBusAndKeepsTheOtherBusOperationalAsync`: four
  dynamic/explicit typed-bus and default/custom health combinations. Named health
  entries, tags and failure floors stay with their owner. Each bus is stopped in
  turn while the surviving bus delivers the independently expected payload.
- `InvalidHealthOptions_RejectStartupWithoutContaminatingTheCompanionBusAsync`:
  ten default/typed invalid-name/status/tag cases. Startup must fail with the
  exact validation type, owning options type and diagnostic; companion options
  remain intact. A single reflection wrapper is allowed around the cause.
- `ScopedSchedulers_DeliverCommandsThroughOnlyTheirOwningBusAsync`: endpoint,
  publish and native-delayed registrations, each on both buses with the same
  payload contract and distinct scopes. Actual received commands retain their
  bus, source, scope headers, destination, token, payload and clock. Recurring
  control identities and canceled admission are checked; drained counts reject
  unintended deliveries.

The corrected baseline passes 18/18 including requirement projection. Wrong
health-option ownership fails 12/14 with two controls passing. Wrong typed
publish-provider ownership fails 1/3 on the actual receiver while endpoint and
delayed controls remain green. Both product sources are SHA-restored. Combined
restored checks pass 50/50 with zero build warnings/errors; 5,875 source/test
paths match the isolated checkout. Final full Core profile passes 6,761 cases.
Read-only behavioral review finds no concrete blocker.

## Reconciliation

Product sources, method identities and physical denominators are unchanged.
There are 122 physical line gains and six lost observations, net plus 116.
Ninety-seven gains occur in the six primary target files: delayed-scheduler
registration 8, hosting registration 24, health-option registration 13 and
message-scheduler registration 52. The two health projection targets have no
new physical line gains; their stronger ownership assertions remain behavioral
evidence rather than a coverage-only addition.

Another sixteen gains occur in scheduler support: IAdvancedMessageScheduler 13,
DefaultRecurringSchedule 2 and EndpointRecurringMessageScheduler 1. Other gains
are EntityFrameworkReliableStore 1, PostgreSqlDbConnectionContext 2,
FutureExtensions 2, BatchConsumer 1, ServiceBusInstrumentation 1 and reliable
delivery telemetry 2. Lost observations are TaskExtensions 2,
ResourceCache.Creation 1, InMemoryDelayProvider 1 and ArrayPropertyConverter 2.
No causal attribution of these other gains/losses to the new tests is made.

Thirty-one identities leave the line-gap list: 24 primary targets, four
scheduler-support identities and three other observations. Three line gaps
appear in InMemoryDelayProvider.RearmTimer, ArrayPropertyConverter and
TaskExtensions.OrCanceledAsync. Nineteen entries enter the branch-only list:
fourteen migrate from line gaps and five have newly observed branch gaps. One
previous branch-only entry leaves. Thus 28 fewer line-gap entries and 18 more
branch-only entries produce ten fewer candidates overall. The 31 line-gap
closures must not be described as 31 fully covered methods.

Conservative physical branches improve by 31: primary targets gain 29,
scheduler support gains three and other observations net minus one. They remain per-line observations,
not a stable branch-ID union. Branch-only migrations are explicitly retained.

## Remaining risks and proof limits

| Method with remaining line gaps | Lines | Complexity | CRAP |
| --- | ---: | ---: | ---: |
| OutboxMessagePipe.DeliverOutboxMessagesAsync | 31/33 | 28 | 28.17453 |
| EventHubProducer.BatchSendPipe.SendAsync | 32/34 | 28 | 28.15958 |
| InMemoryReliableInboxContextFactory.SendAsync | 68/70 | 28 | 28.01829 |

The complete inventories are `artifacts/t48-method-gaps.json` and
`artifacts/t48-branch-only-gaps.json`. Their 5,900-entry union includes
compiler-generated identities; it is not a count of proven defects. No numeric
A+ line/branch threshold has been agreed. Zero CRAP values above 30 and green
profiles do not establish global A+.

Native delayed routing uses zero delay and does not prove temporal withholding.
Received scheduler commands prove routing, not execution or trigger persistence
by an external scheduler. Explicit DI scopes are tested; existing consume-context
and Quartz integration tests retain their separate acceptance scope. Optional
callback, unset health-option and clock-fallback branches remain visible in the
gap inventory.

## Integrity and reproducibility

Canonical receipts validate the measured commit and artifact hashes. The new
Microsoft.Extensions.Hosting 10.0.12 dependency belongs only to the native test
dependency group and Core test project; it is not counted as a product assembly.
All 96 tracked dependency locks match MAIN/GATE after the test-only restore.
Verify-only formatting on the actual test solution passes without edits.
Initial fixture compile/exception-expectation failures and deliberately terminated
restricted restore/build attempts are retained as nonacceptance evidence.

The final independent read-only audit finds no discrepancy or blocking finding.
It verifies 487 file hashes, 66 runner/settings bindings and nine broker-log
hashes. All terminal counts, 32 expected product assemblies and four empty fixture
finding sets are confirmed. Independent reconstruction of all 66 T47/T48 XML
reports matches the physical delta and every T48 method's complexity, coverage,
CRAP and gap classification. This is packet acceptance, not global A+ approval.

| Artifact under artifacts/ | SHA-256 |
| --- | --- |
| t48-aggregate.json | 7ee5042ea311f489630e1df7754f8e2159363d3edacbd0e3380e5d205b5b2108 |
| t48-all-methods.json | a37d6bacefa948669a4d172f2a690b9baaf8e8589bd5c4fcbfb82f30ca2ad527 |
| t48-method-gaps.json | 7e29121576274f2d43fde8f3e2c87f447a5019aaf4f56c9e539050a73ad430e7 |
| t48-branch-only-gaps.json | f77e5310857a21b639826bf4104a5cd2d943a3b0b714c4eb6d5eaac1d38a229b |
| t48-profile-progress.json | 51662101d17a17416c53d6611a3a951bd00f70ee8cad51b46d069d02c755662b |
| t48-gap-delta.json | a60eb3fa87f6a8aa8c232251cfbc328327c6d1a6daf9cf73441a282559f8bfec |

## Continuation

Publish this verified packet before starting the next
larger behavior package. First source reading remains complete. The all-repository
Roslyn API/comment audit follows completion of coverage/CRAP; global A+ remains open.

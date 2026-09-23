# ServiceBus A+ coverage campaign — status

## Latest product-wide profile

The exact-HEAD profile at `c073a5e30` has 36 parseable reports across all 32
product assemblies and 11,123 passing executions with zero failures or skips.
Four provider fixtures have empty findings; the tracked `src`/`tests` diff is
empty. Aggregation measures 83,795/93,439 lines = 89.6788%, branch
observations of 30,015–32,487/36,672 = 81.8472–88.5880%, and 56/25,953
methods above CRAP 30. These branch observations are not formal bounds.
JobService registration and NewId operator gains from the previous two slices
are present in this full profile. Independent read-only adversarial review
returned PASS. See `product-wide-profile-c073a5e30.md`.
Global A+ remains open.

The exact-HEAD profile at `c0cab32ba` has 36 parseable reports across all 32
product assemblies: 22 Unit/Infrastructure, 13 local-provider, and one
supplementary Abstractions run without AVX2. Its successful logs show 36/36
runs passing, 11,109 test executions, zero failures and skips; the additional
Abstractions run repeats 768 tests. Four successful provider fixtures have
empty findings, and the tracked `src`/`tests` diff is empty. Aggregation
measures 83,751/93,419 lines = 89.6509%, branch observations of
29,989–32,463/36,672 = 81.7763–88.5226%, and 59/25,943 methods above CRAP
30. The branch observations are not formal bounds on combined coverage. The
four selected methods from the binding, scheduling, SNS, and saga slices each
moved from CRAP 42 to CRAP 6 in this complete profile. A first provider fixture
attempt had an incorrect relative build-overlay path and produced no counted
report; its console output was observed but not archived. It was rebuilt with
an absolute path before the successful run. See
`product-wide-profile-c0cab32ba.md`. Global A+ remains open.

The fresh exact-HEAD profile at `b6ffcfbdf` has 36 parseable reports across all 32 product
assemblies: 22 Unit/Infrastructure, 13 local-provider, and one supplementary Abstractions run
without AVX2. Its archived successful logs show 36/36 runs passing, 11,102 test executions,
zero failures and skips; the Abstractions supplementary run repeats 768 tests. The four successful
fixture findings lists are empty and the tracked `src`/`tests` diff is empty. Aggregation measures
83,703/93,411 lines = 89.6072%, branch observations of 29,947–32,426/36,668 =
81.6707–88.4313%, and 63/25,943 methods above CRAP 30. Branch observations are not formal
bounds on merged coverage. The one-line net increase versus `7e184ded0` reflects variable
execution paths elsewhere and is not attributable to the new S3 tests. The complete
Unit/Architecture gate passed 10,227/10,227; two earlier broker-load gates are retained as
failed evidence with their test races corrected. Independent read-only review returned PASS for
the new profile. See `product-wide-profile-b6ffcfbdf.md`. Global A+ remains open.

## Latest focused slice

Public `NewId` equality previously had an unexecuted `==` operator in the
complete product profile (CRAP 42). New tests distinguish each of its four
identity words and check equality across operators, typed and boxed overloads,
hashes, dictionary lookup, and boxed comparison boundaries. The full
Abstractions Microsoft CodeCoverage suite passed 774/774; `==`, `!=`,
`Equals(object)`, and `CompareTo(object)` each measure 100% lines and branches
with CRAP 6, 1, 4, and 6. Read-only adversarial review returned PASS for
identity and boxing. The full Release Unit/Architecture gate passed
10,242/10,242. Per-word signed/unsigned ordering direction is a separate
unproven contract. See `newid-identity-boxing-phase.md`. Global A+ remains open.

The JobService correlation and SQL partition-key registration methods were
split by contract family while preserving all 30 and 31 registrations in exact
order. New source-owned tests assert the 30 correlation identities with distinct
JobTypeId, JobId, and AttemptId values, selected-empty and null boundaries, and
warm parallel registration after topology freeze. The complete Core suite and
Microsoft CodeCoverage run each passed 6,378/6,378. Both classes measure 100%
lines and branches in that Core run; the largest resulting method complexity
and CRAP score is 22, versus 62 for each original registration method. The
Engineering Release build passed with zero warnings and errors. Read-only
adversarial review found no refactor regression. The complete Release
Unit/Architecture gate passed 10,236/10,236. Four published Attempt events
are consumed by both the Job and Attempt sagas with different identity keys;
their single outgoing SQL partition key remains an open ordering-contract risk,
not a demonstrated runtime failure. Global A+ remains open. See
`jobservice-coordination-registration-phase.md`.

The public `SagaInstance<TSaga>` wrapper had the same subclass equality
inconsistency previously found in keyed binding: typed equality accepted a
derived wrapper around equal state, while object equality rejected it. The
pre-fix regression failed at that comparison during development; its console
output was not archived. The typed overload now matches the object overload's
exact-runtime-type rule. The focused test passed 1/1 during development;
complete Core with Microsoft CodeCoverage passed 6,376/6,376, and both
`Equals` overloads reached 100% lines/branches and CRAP 6. The Engineering
Release build had zero warnings/errors; the full Unit/Architecture gate passed
10,234/10,234. Read-only Red Team returned PASS. See
`saga-instance-equality-phase.md` and `CHANGELOG.md`. Global A+ and a new
complete product profile remain open.

The public Amazon SNS relative-topic projection had a scoped-host roundtrip
defect: `topic:orders` under `production` became
`topic:production/production_orders`, which the transport parser could not
resolve to the same entity. A red regression led to a fix that strips the exact
scope prefix and rejects names with no valid relative representation. The
root/scoped roundtrips and queue/ambiguous-name boundaries passed. Complete
Amazon SQS Release coverage passed 211/211; the selected getter reached 100%
lines and reported branches, CRAP 6. The Engineering Release build passed with
zero warnings/errors. A first full gate timed out in a Quartz test while a
coverage run was concurrent; the cause is unproven. The final serial gate
passed 10,233/10,233, and read-only Red Team returned PASS. See
`amazon-sns-relative-topic-address-phase.md` and `CHANGELOG.md`. Global A+
and a fresh full product profile remain open.

The public keyed-binding equality test exposed a real subclass inconsistency:
typed equality accepted a derived binding that object equality rejected, so
dictionary lookup contradicted object comparison. The pre-fix regression
failed as expected. `Bind<TKey,TValue>` now requires matching runtime types
through both overloads; equal base bindings still compare and hash equally.
The focused test passed 1/1, complete Core with Microsoft CodeCoverage passed
6,375/6,375, and both `Equals` overloads measure 7/7 lines and CRAP 6. The
Engineering Release build had zero warnings/errors; the full Unit/Architecture
gate passed 10,229/10,229. Read-only adversarial review returned PASS. See
`bind-equality-identity-phase.md` and `CHANGELOG.md`. A complete product-wide
profile after this source change remains open, as does global A+.

The delayed-send accepted-identity test verifies configured, live-context,
and accepted-snapshot token semantics with distinct GUIDs, including stable
message identity after context mutation and rejection of pipe reuse. The
focused test passed 1/1; complete Core with Microsoft CodeCoverage passed
6,374/6,374 and measured the selected public getter at 5/5 lines and CRAP 6,
down from 0/5 and CRAP 42 in the last full profile. The Engineering Release
build had zero warnings/errors and the full Unit/Architecture gate passed
10,228/10,228. Read-only adversarial review returned PASS. See
`schedule-send-accepted-identity-phase.md`. The new test is not yet included
in a complete product-wide profile; global A+ remains open.

The Quartz suspect-attempt integration test now schedules its naturally due
status check five minutes ahead while continuing to trigger all three checks
manually. This separates Quartz's natural timer from the test's 30-second wait
without changing its fault, retry, stale-completion, or terminal-state
assertions. Both focused cases passed 2/2, the entire Quartz project 267/267,
and the complete Unit/Architecture gate 10,226/10,226 while six brokers were
still starting. That fixture failed on a RabbitMQ Erlang cookie permission
error; Quartz finished before any broker was ready. A second run began with six
brokers ready: Quartz passed alongside PostgreSQL provider tests (79/79), but
the Unit/Architecture gate failed one Amazon S3 test because the fixture set
`AWS_REGION`. A deterministic client-config proxy corrected that test and a
positive `RegionEndpoint`-only case now checks bucket creation. A second
broker-ready run passed S3 and Quartz but exposed a saga observation race;
the saga test now waits for actual removal before checking the repository.
Both failed gates remain archived. Read-only adversarial review passed the
Quartz change; the original timeout's exact cause remains
unproven. The complete Unit/Architecture gate with all three final test
changes passed 10,227/10,227. See `quartz-suspect-manual-schedule-phase.md` and
the new complete `product-wide-profile-b6ffcfbdf.md`.

The RabbitMQ durable-send destination slice adds seven real-broker rejection
variants for unsafe topology options. The test checks the specific
configuration failure, an empty existing quorum queue, and its complete
binding set before and after each attempt. RabbitMQ local integration and
focused Microsoft CodeCoverage each passed 38/38; the full Unit/Architecture
gate passed 10,226/10,226. `ValidateDestination` improved from 11/17 lines
and CRAP 43.28 in the previous full profile to 17/17 lines and CRAP 22 in
the focused report. Adversarial review found an assertion limit, which was
corrected and rerun. See `rabbitmq-durable-destination-phase.md`.

The SNS subscription reconciliation slice fixes two provider defects: a failed attribute read
could be accepted as a usable existing subscription, and a changed configured attribute such as
`RedrivePolicy` could be ignored. Regression tests cover failure identity, ordering, exact SNS
updates, and unchanged attributes. The full Release build has zero warnings/errors, the complete
Unit/Architecture gate passed 10,226/10,226, and focused coverage passed 207/207. The adversarial
review passed after independently finding the second defect. See
`sns-subscription-attribute-read-phase.md` and `sns-subscription-all-attributes-phase.md`.

The runtime scheduled-send slice adds six A-grade behavior methods with seven cases for all four
public runtime overloads: inferred versus explicit contract identity, payload, due time,
destination, nondefault cancellation, pipe execution on the provider context, mismatched types,
and twelve required-argument boundaries. Focused Microsoft CodeCoverage passed 7/7; the complete
current-byte Unit/Architecture gate passed 10,163/10,163, with no failures or skips. Five former
CRAP>30 runtime-dispatch methods now measure between CRAP 6 and 9.73 in the focused report; the
explicit-type pipe overload has 9/9 lines and 8/8 reported branches, down from CRAP 72 to 8.
Final read-only adversarial review returned PASS. See `message-scheduler-runtime-contract-phase.md`.
The last complete product-wide profile remains `ac363722c`; global A+ remains open.

The RabbitMQ registration-options slice fixes explicitly empty or mixed
credentials being discarded and replaced by RabbitMQ.Client defaults. Three
attributed methods execute eight hard DI, TLS, certificate-policy, credential,
and client-boundary cases. Three adversarial FAIL rounds closed EXTERNAL-auth,
TLS, trust-policy, paired-credential, and trimming mutants before final PASS.
RabbitMQ passes 386/386 with Microsoft CodeCoverage, the Release build has zero
warnings/errors, and the current-byte Unit/Architecture gate passes
10,156/10,156. At exact commit `22d0f5800`, locked restore, the same build, and
386/386 RabbitMQ tests and the exact complete 10,156/10,156 gate pass.
The former CRAP-72 registration closure is fully covered with maximum CRAP 8,
and `GetConnectionFactory` is at CRAP 28.22. See
`rabbitmq-registration-options-phase.md`. Global A+ remains open.

The RabbitMQ connection-context slice closes ownership and failure-preservation
defects across supervisor stop, settings refresh, both real client-adapter
routes, owner registration, shutdown subscription, publication, and cleanup.
Twenty-three A-grade facts execute 38 deterministic cases. Two adversarial FAIL
rounds exposed and closed five initial mutant families plus unexpected handler
removal and subscribe/check ordering; final review returned PASS. At exact
commit `c9926b8c4`, focused RabbitMQ passes 38/38, the complete RabbitMQ project
passes 378/378, SignalR passes 97/97, Core passes 6,343/6,343, and the focused
Saga regression passes 1/1. The complete Unit/Architecture gate is
10,148/10,148 with zero failures and skips. Every reported
`ConnectionContextFactory` method and compiler state has full line and branch
coverage with maximum CRAP 20; the changed `TransportLifetime` disposal path
is also fully covered. See
`rabbitmq-connection-context-factory-phase.md`. Global A+ remains open.

The Core circuit-breaker runtime-settings slice fixes defensive validation
that accepted decreasing recovery delays when an internal caller bypassed the
public options API. Eight A-grade methods execute fourteen hard boundary,
aggregation, and sequence cases; two adversarial FAIL rounds exposed and
closed scalar-boundary, diagnostic-order, and inner-inversion mutants before
the final PASS. Focused tests pass 14/14, Core passes 6,343/6,343, the complete
Unit/Architecture gate passes 10,110/10,110, and the relevant Release builds
have zero warnings/errors. The validator has 17/17 lines, full reported branch
coverage, and CRAP 26. A detached exact `c13ebf62e` checkout passed locked
restore, zero-warning build, the focused suite, and complete Core suite with
the same target measurement. See `circuit-breaker-settings-phase.md`. Global
A+ remains open.

The Job Service state-machine slice reduces its fully covered constructor from
CRAP 104 to 1 by preserving the exact registration sequence across eleven
private lifecycle configuration methods. No binder or behavior expression
changed, and no structure-only test was added. Constructor plus configuration
methods total 189/189 lines with full reported branch coverage and maximum CRAP
26; this claim excludes compiler-generated lambdas that retain prior branch
gaps. Core passes 6,329/6,329, the Release Unit/Architecture build has zero
warnings/errors, and the complete gate passes 10,096/10,096. A detached exact
`74d278654` checkout passed locked restore, zero-warning build, the complete
Core suite, and the same target measurement. Read-only adversarial review
returned PASS for registration order, initialization, API, reflection, and
serialization. See `job-state-machine-complexity-phase.md`. Global A+ remains
open.

The Abstractions dictionary-extension slice removed two internal `SetValue`
overloads and `SetValues<TValue>` after repository-wide analysis found no
current caller, reflection binding, generator reference, or public contract.
The prior complete profile measured them at 0/23 lines and CRAP 156, 42, and
20. Adding tests would have preserved dead implementation only for coverage.
The complete Friend Assembly graph builds with zero warnings/errors, the full
gate passes 10,096/10,096, and a detached exact `01e77bc68` checkout passes
locked restore, zero-warning build, and 759/759 Abstractions tests. Its
Cobertura method list confirms that the removed symbols no longer exist. The
two retained active helpers were already fully covered with CRAP 2 in the last
complete profile. Read-only adversarial review returned PASS. See
`dictionary-mutation-dead-code-phase.md`. Global A+ remains open.

The Core timeout-consumer fault slice found semantic defects even though the
old asynchronous path already reported 12/12 lines, 10/10 branches, and CRAP
10. The corrected implementation separates its own wrapper/scope chain from
foreign contexts, publishes owned timeout faults through the original delivery
context, preserves the passed context for receive notification, recognizes
only its exact canceled timeout token, and owns every asynchronous outcome.
Three fail-first rounds and a complete-gate regression corrected direct,
scoped, and publication behavior. The final 25 A-grade methods run as 26/26
cases; Core passes 6,329/6,329, the Release Unit/Architecture build has zero
warnings/errors, and the complete gate passes 10,096/10,096. The method family
has 42/42 lines, full reported branch coverage, and maximum CRAP 24. A clean
detached checkout of exact code/test commit `2c61c180a` passed locked restore,
zero-warning build, 26/26 focused coverage, and 6,329/6,329 Core tests. Final
read-only adversarial review returned PASS. See
`timeout-consume-fault-phase.md`. Global A+ remains open.

The Core timeout-activity fault slice corrects unrelated cancellation being
reported as a configured timeout, message-context cancellation being decided
from an owning activity token, pre-canceled caller side effects, deferred
argument validation, and incomplete ownership of generation/notification
tasks. Two fail-first rounds reproduced six distinct failures. The final 22
A-grade methods run as 23/23 focused cases; Core with Microsoft CodeCoverage
passes 6,303/6,303, the Release Unit/Architecture build has zero
warnings/errors, and the complete gate passes 10,070/10,070. The selected
method family has 30/30 lines, full reported branch coverage, and maximum CRAP
16. A clean detached checkout of exact code/test commit `5d94930e9` passed
locked restore, zero-warning build, 23/23 focused coverage, and 6,303/6,303
Core tests. Final read-only adversarial review returned PASS. See
`timeout-activity-fault-phase.md`. Global A+ remains open.

The RabbitMQ send slice fixes positive TTL/delay values that became zero or were shortened by
millisecond formatting. Eight fail-first cases reproduced the old defect. The focused class passed
42/42; the full RabbitMQ Unit project passed 340/340 with Microsoft CodeCoverage and canonical
settings. The Release Unit/Architecture build had zero warnings/errors and its full gate passed
10,047/10,047. The isolated RabbitMQ LocalIntegration build had zero warnings/errors; 31/31 tests
passed against a fresh broker with canonical coverage and empty fixture findings. The selected
send-path methods have full reported line coverage, complexity and CRAP at most 20, and explicit
remaining branch gaps. An additional clean detached checkout of exact code/test commit `edde4076f`
passed 340/340 Unit, 6/6 affected architecture, and 31/31 fresh-broker tests after locked restores
and zero-warning builds. Read-only adversarial review returned PASS. See `rabbitmq-send-phase.md`.
The 36-report aggregate below is still the latest product-wide profile; global A+ remains open.

The reliable-messaging retry-jitter slice fixes nonfinite option acceptance,
numeric jitter bounds and two-tick distribution, and due-date overflow on a
real transient delivery. Invalid-policy tests now prove each named setting
instead of failing on default store limits. The Release Unit solution build
has zero warnings/errors; the Unit/Architecture gate passed 10,031/10,031 and
the full Core Microsoft CodeCoverage run passed 6,280/6,280. The five selected
methods have full reported line coverage; four have every instrumented branch,
while `ValidateAndFreeze` has no branches. Their CRAP scores are 1, 6, 24, 12,
and 12. Independent read-only adversarial review returned PASS. See
`reliable-messaging-jitter-phase.md`. This focused result does not replace the
product-wide aggregate below; global A+ remains open.

## Latest product-wide measurement

At exact source/test commit `f7d924f9477bc95bfc46bda67e3d545b8721066b`, 36 fresh reports cover
32/32 product assemblies. Unit/Infrastructure passed 9,602/9,602, providers passed 545/545, and the
no-AVX2 Abstractions run passed 759/759, all without failures or skips. Four canonical fixtures have
empty findings. The aggregate is 83,286/93,309 lines (89.2583%), a conservative branch interval of
29,698–32,135/36,695 (80.9320–87.5732%), and 89/25,902 methods with CRAP above 30. See
`product-wide-profile-f7d924f94.md` and the artifact-local `analysis-36/summary.json`,
`methods.json`, and `provenance.json`. Independent read-only adversarial review reproduced the
totals and hashes and returned PASS. Global A+ remains open.

At exact source/test commit `a955052272b744ceb44333a51035ac9ce4f66e3d`,
36 fresh reports from 22 Unit/Infrastructure projects, 13 local-provider
projects, and one no-AVX2 Abstractions run cover 32/32 product assemblies.
The tests passed 9,577/9,577, 545/545, and 759/759 respectively, with zero
failures and skips; all four fixture runs have empty findings. The deduplicated
profile is 83,256/93,283 lines (89.2510%), a conservative branch interval of
29,674–32,209/36,695 (80.8666–87.7749%), and 91/25,892 methods with CRAP
above 30. The source/test diff was empty. Independent read-only adversarial
review reproduced the totals, report and binary hashes, test counts, and
fixture evidence after correcting one no-AVX2 output-path finding. See
`product-wide-profile-a95505227.md` and the artifact-local
`analysis-36/summary.json`, `methods.json`, and `provenance.json`. Global A+
remains open.

## Current focused work

Runtime-typed recurring scheduler dispatch passes 14/14 focused tests and
6,271/6,271 Core Unit tests with canonical Microsoft CodeCoverage; both
selected converter methods are below CRAP 30 and independent adversarial
review returned PASS. The full Unit/Architecture gate passed 10,022/10,022;
the isolated exact commit `2419d384c` passed locked restore, a zero-warning
Release build, and 6,271/6,271 Core tests with the same target coverage.
See `recurring-scheduler-dispatch-phase.md`.
This focused slice has not established A+ line/branch coverage for the
converter methods or all recurring scheduling variants.

The Amazon SQS receive-endpoint validation slice is complete on the current
source and test bytes: 200/200 SQS Unit tests with Microsoft CodeCoverage,
10,018/10,018 Unit/Architecture tests, zero-warning Release SQS build, and
independent adversarial PASS. Five generated validation methods are all below
CRAP 30. The isolated exact commit `f3b886656` passed locked restore, a
zero-warning Release solution build, and 200/200 SQS tests with the same target
coverage; see `amazon-sqs-receive-validation-phase.md`. The PostgreSQL
scheduled-maintenance behavior slice passed exact-commit verification; see
`postgresql-maintenance-phase.md`. The dynamic request-rate limit correction
passed 759/759 Abstractions tests and independent code review; see
`request-rate-dynamic-limit-phase.md`. These focused reports are not a new
product-wide A+ measurement.

The preceding complete quantitative baseline is the fresh 36-report profile at
`4488b29fedb456defa8fdcd1f5984f8e165ee72a`: 83,067/93,200 lines
(89.1277%), a conservative branch interval of 29,565–32,111/36,649
(80.6707–87.6177%), and 97 methods with CRAP above 30. It covered 32/32
source assemblies. See `product-wide-profile-4488b29fe.md`. The focused
PostgreSQL and request-rate tests were added after this baseline. Global A+
remains open.

## Earlier complete quantitative baseline

- At source/test commit `44d9e32546ccf1ffe60bc49e49cfc81c3aa348d6`, 36
  fresh, parseable reports from 22 passing Unit/Infrastructure modules, all
  13 passing local-provider modules, and one supplementary Abstractions run
  with AVX2 disabled cover 32/32 product assemblies. The source/test diff was
  empty at capture. Unit coverage runs passed 9,536/9,536;
  local-provider coverage runs passed 530/530; the portability run passed
  751/751. All had zero skips and empty fixture findings. A post-commit locked
  restore, zero-warning Release build, and full Unit/Architecture gate passed
  9,981/9,981 on this HEAD.
- Aggregate: 82,927/93,153 lines = 89.0224%; branch interval
  29,470–32,018/36,629 = 80.4554–87.4116%; 105 methods exceed CRAP 30.
  The raw reports, hashes, methods, and summary are in
  `artifacts/coverage-a-plus-20260922-8abfe1e8a/analysis-36-noavx2/`. See
  `product-wide-profile-44d9e3254.md`. Global A+ remains open.
- The 22 Unit reports were produced immediately before the lockfile-only
  `44d9e3254` commit; the 13 provider reports and supplementary Abstractions
  report were produced after it. The commit changed neither C# source nor test
  code, and the prior SignalR assets already resolved the corrected graph.
  A separate post-commit locked restore and current-HEAD Unit/Architecture
  gate verified the committed lockfiles: 9,981/9,981, zero failures/skips.
  Read-only adversarial review passed after the provenance correction.
- This profile explicitly passes `tools/ci/coverage.settings.xml` to Microsoft
  CodeCoverage. Its 93,153 valid lines differ from the older profile's 90,376
  valid lines across nearly every assembly. Therefore the two percentage
  series are not a controlled before/after comparison.

### Same-byte 35-report control

Before the supplementary no-AVX2 run, the same source/test bytes and coverage
settings yielded 82,851/93,153 lines = 88.9408%, a branch interval of
29,446–31,925/36,629 = 80.3899–87.1577%, and 106 methods above CRAP 30.
Running the existing 751 Abstractions tests with AVX2 disabled covered the
scalar `DashedHexFormatter.Format` path: 7/38 → 38/38 lines and CRAP 237.17 →
20. Its 18/20 conservative branch count retains the runtime and endianness
condition; it does not indicate an untested input-length boundary.

### Previous complete profile

- At commit `e1a965290fe532ec8ff86dc699305dd4086f0099`, 35 fresh,
  parseable reports from 22 passing Unit/Infrastructure modules and all 13
  passing local-provider modules cover 32/32 product assemblies. The 445
  Architecture tests pass separately without coverage instrumentation because
  the collector injects types that invalidate one assembly-ownership test.
  Architecture coverage is excluded from the aggregate for that reason.
- Aggregate: 80,159/90,376 lines = 88.6950%; branch interval
  29,184–31,649/36,380 = 80.2199–86.9956%; 111 methods exceed CRAP 30.
  Source/test diff was empty when the reports were captured. The complete
  provider profile passed 25 Azure Service Bus, 405 broad-matrix, 69 SQL
  Server, and 31 RabbitMQ tests, all without failures or skips and with empty
  fixture findings. The raw reports and calculated methods are in
  `artifacts/coverage-a-plus-20260922-e1a965290/`. The subsequent Core
  assembly-scan correction has changed source and tests, so these totals are
  the last complete comparison profile, not a current-byte global result.

### Earlier comparison profile

- The immediately preceding complete profile at `4965a8468` covered
  80,091/90,318 lines (88.6767%), a branch interval of 80.1061–86.8794%,
  and 114 methods above CRAP 30. The Azure Service Bus metadata phase added
  68 covered lines and lowered the hotspot count by three under the same
  35-report scope.
- 36 fresh, parseable Cobertura reports cover 32/32 loadable product assemblies at commit
  `e0cf987c845154fea81ec27b63592a910aceac37`.
- Aggregate: 79,569/90,165 lines = 88.2482%.
- Branch interval: 79.2182–86.0351%.
- 142 methods exceed CRAP 30.

This 36-report baseline is retained for comparison with a complete profile. The latest partial
19-report rerun (including fresh SQS coverage and one Event Hubs local integration project) observes
32/32 assemblies but omits other provider integration projects: 76,341/92,835 lines = 82.2330%,
27,701–29,505/36,564 branches = 75.7603–80.6941%, and 235 methods above CRAP 30. The two
profiles have different test scope and cannot be used as a before/after coverage comparison.

## Prior iteration closure

The inherited source-review and package-structure change set is closed on the
current worktree. The final Release build completed with zero warnings and zero
errors, and the complete Unit/Architecture profile passed 9,769/9,769 with zero
failures and zero skips.

Provider evidence retained for this closure:

- the six-fixture local matrix passed 402/402 with empty fixture findings, run
  `vicione-285cc34ba452`;
- the standalone SQL Server profile passed 69/69 with empty fixture findings,
  run `vicione-0a7a93d9e1d6`;
- the final RabbitMQ profile passed 31/31 with empty fixture findings, run
  `vicione-5e9fd08c2ca0`;
- RabbitMQ Unit passed 324/324 after the final pre/post quorum-proof and
  concurrent cleanup regressions;
- the package gate rebuilt 31 packages and passed all 18 Developer Journeys,
  four isolated package consumers, and the 30-assembly runtime API comparison.

The final RabbitMQ contract requires an existing durable quorum queue before
publish and rechecks it after a persistent, mandatory, publisher-confirmed
publish without changing routing. Concurrent privileged queue deletion or
redeclaration during or after broker acceptance is an explicit operational
boundary. Concurrent failed sends preserve their original broker causes while a
new topology generation is running. A failed post-confirm check can retain an
already delivered intent, so retry remains at-least-once and can duplicate.

## Active phase

The RabbitMQ phase is complete in the inherited closure. The ActiveMQ phase is complete in the
current change set: 9,788/9,788 Unit/Architecture tests, three targeted real-broker cases, focused
coverage/CRAP, Microsoft test-quality assessment, and final adversarial review are green. Details are
in `active-mq-phase.md`.

The first generic SQL topology slice is also complete: 147/147 SQL tests and 9,797/9,797 complete
Unit/Architecture tests pass, its six selected baseline hotspots are below CRAP 30, and the final
adversarial re-review returned PASS. Details are in `sql-topology-phase.md`.

The SQL host-configuration slice is complete: 190/190 SQL tests and 9,840/9,840 complete
Unit/Architecture tests pass. URI credentials, mutable validation, PostgreSQL host parsing, atomic
replacement, effective data-source projection, inline-port precedence, and multi-host overrides
have hard behavior regressions. Every selected host hotspot is below CRAP 30, and two final
adversarial reviews returned PASS. Details are in `sql-host-phase.md`.

The receive-validation and SQL Server taxonomy CRAP slice is complete: 191/191 SQL tests, 15/15
filtered provider taxonomy cases, and 9,841/9,841 complete Unit/Architecture tests pass. The two
baseline methods fell from CRAP 34 and 38 to at most 18 and 2. The final adversarial review returned
PASS. Details are in `sql-validation-taxonomy-phase.md`.

The next SQL slice covers the remaining receiver loop, PostgreSQL runtime, and SQL Server migration
hotspots from the exact 142-method global baseline. A fresh product-wide aggregate will follow after
coherent phases; the focused reports do not claim that the requested global A+ target is reached.

The inherited SQL receiver-loop and retention work is closed in commit `b90d5e744`. The current
Amazon SQS naming and scoped-topology slice passed 165/165 SQS tests and final read-only adversarial
review; its 18 new tests are graded A under the Microsoft rubric. The final complete
Unit/Architecture gate passed 9,881/9,881 with no failures or skips. See
`amazon-sqs-naming-phase.md`. The subsequent SQS topology-declaration slice has 14 focused tests
and passed 179/179 SQS tests with Microsoft CodeCoverage. Six selected comparer/diagnostic CRAP
hotspots are now below 30, and its final read-only adversarial review returned PASS. The complete
Unit/Architecture rerun passed 9,895/9,895 without failures or skips. See
`amazon-sqs-topology-phase.md`. Global A+ remains open.

The SQS subscription-identity slice has eight focused tests and passed 187/187 SQS tests with
Microsoft CodeCoverage. Its two selected comparers moved from CRAP 110 each to 12.7 and 11.38.
Final read-only adversarial review returned PASS. The complete Unit/Architecture rerun passed
9,903/9,903 without failures or skips. See `amazon-sqs-subscription-identity-phase.md`.

The Azure Service Bus header and persisted-routing-metadata slice has nine A-grade behavioral
tests. The broker-owned sent time is protected from application-header spoofing while exact
application identity semantics remain intact. The complete Unit/Architecture gate passed
9,912/9,912; Azure Service Bus Unit passed 151/151 with coverage and its local emulator profile
passed 25/25 with empty fixture findings. Three selected method CRAP scores moved from 110 each
to 10, 10, and 12. Final adversarial read-only re-review returned PASS. See
`azure-servicebus-metadata-phase.md`. The complete 35-report aggregate on
`e1a965290` is recorded above; global A+ remains open.

The Core assembly-scanning slice has eight A-grade file and caller behavior
tests. A previously selected file could resolve to an unrelated loaded
assembly by filename; the corrected finder uses the selected file's manifest
identity. The final Release build has zero warnings and errors, the complete
Unit/Architecture gate passed 9,920/9,920 without failures or skips, and the
focused Core Unit coverage run passed 6,262/6,262. The two selected CRAP
hotspots moved from 272 and 156 to 16.02 and 12 in the focused report. The
final read-only adversarial review returned PASS. See
`assembly-scan-phase.md`. A complete product-wide profile on these changed
bytes is still required, and global A+ remains open.

The Azure Service Bus retry-taxonomy slice has 16 new A-grade test methods
covering direct, wrapped, intermediate, and aggregate failure causes. The
final Release build has zero warnings and errors; Azure Unit with Microsoft
CodeCoverage passed 201/201, the complete Unit/Architecture gate passed
9,970/9,970, and the isolated Service Bus emulator passed 25/25 with empty
fixture findings. A detached checkout of exact source commit `a628ecc3c`
passed locked restore and 201/201 Azure Unit with Microsoft CodeCoverage.
The two baseline host retry lambdas at CRAP 240 and 210
were replaced by focused methods at CRAP 28 or lower. The final adversarial
review returned PASS. See `azure-servicebus-retry-phase.md`. A fresh complete
35-report aggregate is required before any current-byte global A+ claim.

The Azure Service Bus cross-transport classification slice prevents its
globally registered classifier from claiming failures marked by a foreign
provider connection type while preserving real Azure retry-stop wrappers.
The Azure Unit suite passed 212/212 with Microsoft CodeCoverage, the complete
Release Unit/Architecture gate passed 9,981/9,981, and the final adversarial
review returned PASS. The focused classifier methods are at CRAP 8, 14,
14.27, and 28. The isolated Azure Service Bus emulator passed 25/25 with
empty fixture findings. A clean detached checkout of exact source commit
`6d0ecbbae` passed locked restore and 212/212 Azure Unit tests with Microsoft
CodeCoverage. A fresh complete product profile is recorded above; global A+
is open. See
`azure-servicebus-cross-transport-phase.md`.

The Azure Service Bus subscription slice closes three silent-success paths:
missing configured rules on existing subscriptions, unidentifiable generated
filters, and concurrent creators whose winning subscription was not
reconciled. Red phases reproduced the latter two failures; real emulator
regressions verify persisted rules, and the SDK race test verifies both
settings and rule updates. Azure Unit passed 213/213, the full Release build
had zero warnings and errors, and the complete Unit/Architecture gate passed
9,982/9,982 without failures or skips. The final real emulator run passed
28/28 with empty fixture findings. The six focused subscription methods are
all below CRAP 30; `CreateTopicSubscriptionAsync` moved from 218 to 15.71.
Exact source/test commit `84b6f2df7` passed a clean, isolated locked restore,
zero-warning Release build, and 213/213 Azure Unit tests with Microsoft
CodeCoverage.
Final read-only adversarial review and the test-helper follow-up both returned
PASS. See `azure-servicebus-subscription-phase.md`. These focused results do
not replace the complete product-wide profile at `44d9e3254`; global A+ is
still open.

The Azure Service Bus publish-validation slice adds seven hard tests for invalid and composed
paths, idle-lifetime boundary, excluded topics, evaluated option freezing, public SDK-options
isolation, and exact broker/sender projection. Azure Unit passed 220/220 with Microsoft
CodeCoverage. The full Release build had zero warnings and errors; the Unit/Architecture gate
passed 9,989/9,989 on a bounded-parallelism rerun after a load-sensitive Quartz timeout in the
first run. The official Azure Service Bus emulator passed 28/28 with Microsoft CodeCoverage and
empty fixture findings. The emulator test project now explicitly references the coverage extension
and has a matching lockfile. Selected validation, projection, getter, and freeze methods have CRAP
12, 20, 2, 1, and 4 respectively. Final read-only adversarial review returned PASS. See
`azure-servicebus-publish-validation-phase.md`. The last complete product-wide profile remains
`44d9e3254`; global A+ is open.

The isolated exact source/test commit `2f3a4b6eb` also passed locked restores,
zero-warning Release builds, Azure Unit 220/220 and official-emulator 28/28,
both with Microsoft CodeCoverage. The emulator fixture had no findings; report
hashes and the `/private/tmp` Docker mount diagnostic are in the phase record.

The Azure Service Bus receive-metadata slice restores the broker's `ReplyTo` destination when a
received delivery is persisted or replayed. Three hard regressions prove the complete five-field
roundtrip, a reply-only message, and blank-value omission; the red phase exposed both lost-reply
cases. Azure Unit passed 223/223 with Microsoft CodeCoverage, and the targeted method moved from
CRAP 156 to 14 with full reported line and branch coverage. The zero-warning Release build and
complete Unit/Architecture gate passed 9,992/9,992. The official emulator passed 28/28 with
Microsoft CodeCoverage and empty fixture findings. Final read-only adversarial review returned
PASS. See `azure-servicebus-receive-metadata-phase.md`. The last complete product-wide profile
remains `44d9e3254`; global A+ is open.

The clean exact source/test commit `8cd224525` passed locked restores, zero-warning Release
builds, Azure Unit 223/223 with Microsoft CodeCoverage, and official emulator 28/28 without
coverage instrumentation. The emulator fixture had no findings. Two earlier exact-checkout
attempts stopped before test execution because of MSSQL fixture startup and MTP named-pipe
startup; details and the retained report hash are in the phase record.

The Core payload-admission and Event Hubs observer slice now rejects a nontransport proxy before
an admission marker is attached and revalidates serialized metadata after awaited send observers.
Core Unit passed 6,267/6,267 with Microsoft CodeCoverage; the final real Event Hubs emulator run
passed 53/53 with coverage and empty fixture findings. The complete Release Unit/Architecture gate
passed 9,997/9,997 after a zero-warning build, and final adversarial review returned PASS. The
selected Core `Admit` method is at CRAP 23.31 versus baseline 128.99; Event Hubs single and batch
send methods are at 24.02 and 28.16 versus 36.10 and 41.04. See
`payload-admission-observer-phase.md`. The last complete product-wide profile remains
`44d9e3254`; global A+ is open.

Exact source/test commit `0ddaa9928` additionally passed locked restores including the full
Engineering graph, zero-warning Release builds, the complete 9,997/9,997 Unit/Architecture gate,
6,267/6,267 Core Unit tests with Microsoft CodeCoverage, and 53/53 Event Hubs emulator tests with
Microsoft CodeCoverage and empty fixture findings. Exact report hashes and the initial
incomplete-restore architecture diagnostic are in `payload-admission-observer-phase.md`.

The next Event Hubs batch activity-route slice adds four strong cases across three test
methods for uniform partition IDs, uniform keys, mixed routes, and unrecorded activities.
The focused cohort passed 11/11; the full real emulator suite passed 57/57 with Microsoft
CodeCoverage and empty fixture findings. `SetActivityRoute` now measures 12/12 lines, all
reported branches, and CRAP 12 versus 72.75 in the last complete profile. Final read-only
adversarial review returned PASS. See `eventhub-route-activity-phase.md`. No product source
changed, and the global A+ target remains open.

Exact test commit `bc51b63c3` passed locked restore, a zero-warning Release build, and a final
57/57 full Event Hubs emulator run with Microsoft CodeCoverage, xUnit TRX, and empty fixture
findings. An earlier same-byte attempt passed 56/57 in the console because an existing
consumer-retry test timed out; its failed coverage report, findings, and broker logs are retained
separately, but no test-result file exists for it. Exact report hashes, the verifiable final TRX
counters, and run identities are in `eventhub-route-activity-phase.md`.

The Event Hubs receive-header slice adds four hard provider tests. A red run reproduced two
errors: a null SDK event passed construction, and lookup reported blank identifiers that header
enumeration omitted. The corrected provider passed 4/4 focused tests and the complete 61/61
real-emulator suite with Microsoft CodeCoverage and TRX; fixture findings were empty. The
complete Engineering Release build had zero warnings and errors, and the Unit/Architecture
gate passed 9,997/9,997 without failures or skips. The selected `GetAll` iterator is now at
CRAP 8 with 9/9 lines versus baseline CRAP 72.75 with
0/9; `TryGetHeader` is at CRAP 12 with 17/17 lines. Independent read-only adversarial review
returned PASS. See `eventhub-header-provider-phase.md`. The last complete product-wide profile
remains `44d9e3254`; global A+ remains open.

Exact source/test commit `e81be47b8` passed a clean detached locked restore, zero-warning
Release build, and the complete 61/61 Event Hubs/Azurite emulator suite with Microsoft
CodeCoverage, xUnit TRX, and empty fixture findings. Two earlier sandboxed restore attempts
did not produce a validated build; the successful run used the local NuGet cache in the approved
execution context. Hashes, exact TRX counters, and broker-log evidence are in
`eventhub-header-provider-phase.md`.

A fresh product-wide profile on exact source/test commit `4488b29fe` passed 9,552/9,552 Unit,
544/544 provider, and 751/751 supplementary no-AVX2 Abstractions tests. All 36 fresh reports
are parseable, all 32 source assemblies are observed, six fixture finding files are empty, and
the tracked `src`/`tests` diff is empty. It measures 83,067/93,200 lines = 89.1277%, a conservative
branch interval of 29,565–32,111/36,649 = 80.6707–87.6177%, and 97/25,886 methods above
CRAP 30. The no-fixture and missing-outage-control attempts were excluded; details and current
hotspots are in `product-wide-profile-4488b29fe.md`. Global A+ remains open.

The PostgreSQL scheduled-maintenance slice adds one real-database behavior test: two orphaned
messages are removed while a product-sent message and its delivery survive. The focused run
passed 1/1 and the complete PostgreSQL provider suite passed 79/79 with Microsoft CodeCoverage;
both fixture findings were empty. The selected callback moved from 0/8 lines and CRAP 156 in
the last complete profile to 8/8 lines and CRAP 12 in the full provider report. Independent
read-only adversarial review returned PASS. See `postgresql-maintenance-phase.md`. The last
complete product-wide profile still predates this test, so global A+ remains open.

The clean detached exact source/test commit `eb04d4283` passed locked restore, a zero-warning
Release build, and the complete 79/79 PostgreSQL provider suite under a canonical fixture with
empty findings and matching broker-log hash. The worktree was removed after retaining the logs
and fixture evidence; details are in `postgresql-maintenance-phase.md`.

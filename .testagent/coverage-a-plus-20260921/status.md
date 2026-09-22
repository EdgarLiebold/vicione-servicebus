# ServiceBus A+ coverage campaign — status

## Last complete quantitative baseline

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

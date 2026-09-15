# Cache initialization repair and Core reading checkpoint

The original whole-product A+ goal remains active and unchanged. This is an
intermediate implementation and reading checkpoint, not a completed product
iteration, complete Core-owner admission, independent acceptance or A+ certificate.
The constructor ownership code is repaired; its new fault regression, effective
counter-mutants and portable timer policy are not yet proved. CS01 therefore
remains open. All previously open findings remain in scope.

## Actual input and authority

Repository: `repositories/vicione-servicebus`; branch:
`feature/servicebus-a-plus-api`; input commit:
`6c0916b20eb5b8f71ca924c84b87b3bca054f50e`.
Input productive tree: `9dad21b5ca953a16592b0868bf9c75b1c96adc2b`.
Unchanged Core-owner tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.

Before the source edit, an actual independent `git ls-remote` exits 0 and verifies
the input branch, annotated iteration-127 tag object
`8605777f87e079334edcf1359ece875348d6c948` and peeled input commit.
Tag: `servicebus-a-plus-iteration-127-cache-source-structure-checkpoint-2026-09-15`.
All seven previously completely main-read authority inputs are hash-identical to
the iteration-127 authority table, including the selected Development Slice
`5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
Workspace and repository entry instructions and the applicable §4.2/§4.3 rules
are read again. Unrelated repositories, index entries and protected product
`review/**` and `TestResults/**` are not inspected or changed by the main.

## Implemented productive correction

`src/ViciOne.ServiceBus/Caching/ResourceCache.cs:55-75` now acquires disposable
constructor state under an exception cleanup boundary. A local nullable source
records whether the linked lifetime source has been acquired. If linking or
timer creation fails, the source, if acquired, and the observer semaphore are
disposed, and bare `throw` preserves the original exception and stack.
Non-disposable initial state is allocated before the semaphore. The successful
path still publishes the same linked source/token and creates the same timer,
callback, due time and period. No public API, feature, index, factory, transport,
dependency, project, test, directive or gate is removed or changed.

The main fully reads the complete changed file and its relevant options,
cleanup, observer and disposal paths. The entire 18-file productive Cache scope
was fully personally read in iteration 127; 17 unchanged files are now exactly
Git-byte reconciled. Final Cache source has 18 files / 1,719 physical lines.
Changed file final SHA256:
`1d6a4dd32d80e54a41d865a6b735f6729e0a7234e185f7d9aee6eefe798a95fb`.
All code is manually edited using apply_patch. Existing valid functional comments
remain; no generator or script writes comments, code, tests or this report.

CS01 is not closed by compilation or existing happy-path tests. Required remaining
evidence is the original timer creation fault, released constructor-owned state,
no usable partial instance, successful initialization afterward, effective
single-cause cleanup counter-mutants and a verified portable interval contract.
No unverified numeric system-timer maximum or fault receipt is asserted here.

## Complete personal reads actually secured here

The main completely reads every member, test method, fixture, helper, arrangement,
assertion and comment in 43 additional Core-owner files / 9,051 physical lines.
All 170 existing Fact/Theory declarations are reviewed. Exact per-file declaration
membership matches 267 historical passed records from iteration 127 and 267 fresh
passed records from the current compiled source. The shared discovery fixture
has no test declarations and receives full file credit, not an invented test.

Prior complete reads: 212 files / 64,350 physical lines. New cumulative personal
read set: **255/557 files / 73,401 physical lines; 302 remaining**.
The connected 102-file packet remains in progress: 43 complete, 59 remaining.
`DependencyInjection/DependencyInjectionConfigurationContractTests.cs` has only
lines 1–235 visibly read and receives no complete-file or test-review credit here.
Earlier truncated outputs are not credited: missing Container registration and
routing ranges are explicitly reread before complete credit.

§4.3 still prohibits new Core test design, modification or owner acceptance before
the complete tracked owning project, effective shared policies, fixtures, data,
execution/CI configuration and parser/GitReadSet closure have been personally
read and reconciled. Existing execution is not that admission. Criteria below
are deferred independent acceptance requirements, not replacement test code.

Paths are relative to `tests/ViciOne.ServiceBus.Tests/`. Methods count declarations;
cases count actual native records. Input and reviewed bytes are identical.

| Personally read file | Lines | Methods | Passed cases | SHA256 |
| --- | ---: | ---: | ---: | --- |
| Configuration/BindPipeSpecificationTests.cs | 83 | 1 | 1 | 0eaed002c2e6e1ce61b6edda576b76b6dc7c7a6a5a37d9ce455ac931154b771c |
| Configuration/CircuitBreakerOptionsTests.cs | 202 | 8 | 8 | 6054075d975657009ae2eb1c6cc1c897048941f2a5571887a0808229679b23fb |
| Configuration/Configuration/BuiltPipelineConfigurationTests.cs | 599 | 6 | 14 | 09766bc7f807a18ae3aa872706dbfd1e15a0e30718205deb8046c4b27c7dad39 |
| Configuration/Configuration/CompositeFilterTests.cs | 30 | 1 | 1 | cdb310e15acc7a901fe82d53a82afa4b42623307f2ef12370fef2cba693ec249 |
| Configuration/Configuration/ConfigurationObserverTests.cs | 421 | 5 | 5 | 65bd8e8bcb5650d6014c73cc9a3ec6b2f2e75deb48c15f6eecd1cb12dba93369 |
| Configuration/Configuration/ConfigurationValidationTests.cs | 183 | 4 | 11 | 48cb590b75030dffbf8bd7ede046a6eb8b82a8c10819d9f5f8aa892b6c293ff6 |
| Configuration/Configuration/ConsumerMessageConfigurationTests.cs | 179 | 2 | 3 | edc0ecdb8042a754a39400ea0bfe41919340c2f11eccc54a45325aa4af0e5c41 |
| Configuration/Configuration/MessageLimitsConfigurationTests.cs | 138 | 4 | 11 | 796a9149f9ee97a97c9caa428517a4a455cdbc3af533e54300050f9ebd026893 |
| Configuration/Configuration/PublishPipeConfigurationTests.cs | 110 | 1 | 1 | f375e98d90d24d7e429ea48c8c8f58b17816a538b93f890565c1e6601d7c2553 |
| Configuration/Configuration/ReceiverConfigurationTests.cs | 66 | 1 | 1 | 7860e296f611b73c1b6f953de1563bc35e0ae814970b807fc717927aba2a60f4 |
| Configuration/Configuration/Retry/ConsumeContextRetryPipeSpecificationTests.cs | 43 | 2 | 2 | c1eb39e0af6b6c9338ebc0fede9e3818287ead01e434253a9853d07f6cf92a79 |
| Configuration/Configuration/Retry/RetryPipeSpecificationTests.cs | 52 | 2 | 2 | 58cf67864049f3b0b7b58f65dfddee7dfab973cb47bf88ee6c5e3d0b30ffbe3d |
| Configuration/Configuration/SendPipeConfigurationTests.cs | 96 | 1 | 1 | 3b458f58cb2d05a7f691d1a4594b4fa1ea4ed86b058644518890e66e52fb28df |
| Configuration/DependencyInjection/AsyncBusHandleLifecycleTests.cs | 76 | 3 | 3 | 682d06607a524936bcd0bed26737563daa8ed94cbf346e9b0e5d5e997631a1ab |
| Configuration/DependencyInjection/BusCompositionStartupValidationTests.cs | 313 | 9 | 9 | 6356abacd95c0ac7bab76f635851944c1c3a771a43c198e4eff268ffb9a9ba0b |
| Configuration/DependencyInjection/FeatureOptionsStartupValidationTests.cs | 141 | 2 | 13 | 57dd6670a920b1f4b86b98d3cbf2c8c7b7e954286fb212ded5c38cd9dd8fa885 |
| Configuration/DependencyInjection/HostedServiceLifecycleOwnershipTests.cs | 272 | 11 | 11 | 2abeaa9d7637aabbc3b1bc8cfb57dd0f8d7060c5d1e01ccc4c9b1f02e3defba2 |
| Configuration/DependencyInjection/RegistrationConfiguratorExtensionsTests.cs | 40 | 2 | 2 | cdd61a5c15a2282fe11ecc4078fd3781393684c32d4fa203d7a7ce667c067df8 |
| Configuration/DependencyInjection/ViciOneServiceBusHostOptionsValidationTests.cs | 88 | 2 | 5 | fc07f872fbddc3287408e0c56e5243093ba941c60fcbe3675ed4419cdff21ec2 |
| Configuration/EndpointNaming/EndpointNameFormatterTests.cs | 234 | 9 | 17 | 7de7c91c468e9a63d8ac792933f379b0c8613497e5bce7fd07e901ea2e5b562f |
| Configuration/KillSwitchOptionsTests.cs | 138 | 7 | 7 | d8f6b01c4c6b8cc24dfb52790500bdf7b80be7f060c70df89a0f4d7a40a22808 |
| Configuration/MessageJournalConfigurationTests.cs | 86 | 1 | 1 | c2c6bb783d7c9b51dc9bcd4300a803e2da0dfcdf145c338d612b041926200d8a |
| Configuration/MessageRetryConfigurationExtensionsTests.cs | 588 | 8 | 10 | f10c10433900077d6695aeec5a77a193e0830df2a2b891538997380c9cd900bd |
| Configuration/NonContainerOptionsValidationTests.cs | 115 | 6 | 12 | db5132dc35991a4a838c78c33853f0068a9530062d4a8bbb3fd72776515a2f6d |
| Configuration/PartitionMessageConfigurationTests.cs | 547 | 8 | 11 | 5883f41aad1a624c4287c0919fd167dc511472811e2f618a4022a4cfa5db93b5 |
| Configuration/Qos/EndpointQosTopologyValidatorTests.cs | 108 | 5 | 5 | 6bc9f320ea3edc25a768790b31abf4518b2cf32921fb055dd0456726ea309e99 |
| Configuration/RetryConfigurationExtensionsTests.cs | 30 | 1 | 1 | 209e11b1ebe6f9d2297037456416ac7720e4d8605b9e2c84832db379583e9f85 |
| Configuration/ServiceInstanceEndpointTests.cs | 88 | 1 | 1 | 410aaa361faa9d498a49aeb0446cf5a985d0e0f75458421ad6a164968182de04 |
| Configuration/SpecificationOptionsValidationTests.cs | 360 | 14 | 42 | d9fe4b3f75ee77982a60347ba9afacd2dc06c88baa3fc683745574abd2b0d814 |
| Configuration/TransactionConfigurationTests.cs | 148 | 6 | 6 | 26d7f8162595e5e23bd2f4050faf3c70f9334991deb14af5cf2a5a2b4e9259c2 |
| Contracts/InterfaceMessageDispatchTests.cs | 82 | 1 | 1 | 8b7e69b9aacea0bf9280b55945e95b5d8e27ea5c5aae005378a23ad997d6cf29 |
| Contracts/MessageContractCatalogTests.cs | 110 | 5 | 5 | 323bcae277699ef96b2b3f0ff00ed332f7f9504d0d4bf1d4fa29a68be6c81edf |
| DependencyInjection/BusObserverOwnershipTests.cs | 192 | 2 | 2 | f46404bf1ba8e73777235f2a178248fd6775843bf996e1b6aa6a9c13f1e80b09 |
| DependencyInjection/ConfigureEndpointExclusionTests.cs | 225 | 1 | 6 | 71e677603cb5ccb0591c81bfb8408459fc35a249ab77efbe1f7eb6a0c6b564af |
| DependencyInjection/ConsumeContextActivatorExtensionsTests.cs | 142 | 5 | 5 | 17c091ec950bc50c4b71504ecd2f532cd04e85e6c9f51b7f1f0dbea26d2ef611 |
| DependencyInjection/ConsumerKindExtensionTests.cs | 314 | 4 | 4 | 7b4c340d7f27eb16f0661b58c1a1d5f671ea3bcc487c3354129ee47b280dc65e |
| DependencyInjection/ContainerConsumeContextTests.cs | 379 | 2 | 9 | 1c66dfc9a3ecd051856d83a5c0e386a400251c6ed381f7a6305432df5526a94d |
| DependencyInjection/ContainerConsumerRegistrationTests.cs | 721 | 6 | 6 | fb74fd7ccdb8c492d3f5e5280565450afd85cc3f9e167385e9cadf7151289751 |
| DependencyInjection/ContainerDiscovery/ContainerDiscoveryTypes.cs | 99 | 0 | 0 | 70fbd1660088d62d678f998960fd114c0594c2632a5bb587bbf288b2e848c4c5 |
| DependencyInjection/ContainerEndpointRoutingTests.cs | 332 | 4 | 4 | a435da73e8891637e77c198e0ed6285484caa35ce2073732aa6105baf505ded4 |
| DependencyInjection/ContainerNamespaceDiscoveryTests.cs | 120 | 1 | 1 | bc94d41c979e2d745001b58cde8c9e27e96f8e1381d7ae57561f4fdceed4e5d4 |
| DependencyInjection/ContainerOutboxScopeTests.cs | 258 | 2 | 2 | ecccbabc232baa5b6ac057a4ce5409c5246e4d9d6fcd3fddd8e12d0ee0228f41 |
| DependencyInjection/ContainerScopedEndpointTests.cs | 503 | 4 | 5 | 3c389871c4cc866581a68facf45f0dcbbeb5d6efc20dc481e1e9dc0a694b1100 |

## Settled scoped review findings

The tests contain strong real end-to-end and independent identity/state oracles;
they are not blanket coverage-touching or dummy tests. Every declaration in the
table is reviewed. The following eleven scoped findings remain open:
**0 Critical / 8 High / 2 Medium / 1 Low**. None is an executed mutant or a claim
that the corresponding production feature is broken. Owner admission precedes
test changes and complete source reconstruction precedes final API disposition.

### H01 — causal completion and negative synchronization

`ConsumerMessageConfigurationTests.ConsumerFactoryFilter_RunsBeforeAndAfterTheExactConsumerInvocationAsync`
signals its shared Completed source inside the consumer, before the outer filter
records its after trace. A test continuation can inspect the trace too early.
Built pipeline and same-key partition negative observations do not establish
that the second delivery reached its own protected downstream attempt.
`HostedServiceLifecycleOwnershipTests` uses Task.Yield plus IsCompleted after
advancing the clock, which is not a completion barrier. Require outer-filter
completion after the final after record, exact full trace, an independently
observed second owned attempt before the negative check, and deadline-armed and
terminal clock observations. Preserve real batch downstream-attempt barriers.

### H02 — configuration values lose owner pairing

`BuiltPipelineConfigurationTests` accepts a superset of disconnected filter
limits. Bus concurrency 3 and endpoint concurrency 7 can be swapped while both
values remain present. Require the exact owner/path/value pairs, not a union:
bus 3, endpoint 7 and each configured component's own declared limit/rate.
Keep the strong actual five-facet concurrency change from 1 to 2 and the batch's
exact single concurrency filter. No misplaced-value mutant is claimed run.

### H03 — validation labels are not failure-sensitive public admission

Negative `SpecificationOptionsValidationTests` primarily match property text in
validation records without requiring FailureDisposition; turning a failure into
a success/warning with the same text can retain that oracle. Outbox validation
in `NonContainerOptionsValidationTests` directly invokes a private Validate and
does not prove its before-pipe-build claim. Require the independently expected
FailureDisposition and causal rejection at the public owning configuration
boundary while retaining every exact invalid property cause. Transaction and
configuration-observer tests already check disposition and are not implicated.
Separate missing JobService owners before claiming isolated missing-owner cases.

### H04 — failure-sensitive bounds and resource release

Several held lifecycle operations and waits rely only on the runner token;
some success-tail releases, startup-before-try, sequential stops and cancelled
cleanup tokens can leave held operations or another bus unreleased on failure.
`PartitionMessageConfigurationTests.FindKeyForPartition` searches up to
int.MaxValue using the tested hash: a constant-hash mutation can spin for
billions of iterations when selecting another partition. Require the validated
shared operation bound, failure-safe release/stop with an uncancelled cleanup
budget and independently established bounded partition fixtures. Preserve
existing finally releases and validated uncancelled Stops; do not flag every
Guid or command timestamp as flaky synchronization.

### H05 — rejected contract registration does not prove retained maps

`MessageContractCatalogTests` verifies precise conflicts and some later healthy
lookups but does not build and inspect both maps after each rejected conflict,
open generic or value-type registration. Require the original known type/identity
pair unchanged in both directions and no rejected Other/int/open-generic or
changed-identity ghost entry. A healthy later Explicit lookup is useful but
cannot prove all those absence properties.

### H06 — an override precedence input equals its fallback

`ContainerEndpointRoutingTests.ConsumerRegistrationPrecedence_ProducesEveryExactSourceAddressAsync`
sets both OverrideDefinitionNameConsumerDefinition.EndpointName and its inline
override to by_endpoint_name. Ignoring that override can still satisfy the
purported complete matrix. Establish distinct names and require exactly the
inline override source, not the definition fallback, after real delivery/drain.
Retain the other genuinely distinct definition/inline conflicts. Preserve known
command/event correlations instead of checking only the first source address.

### H07 — filter entry substitutes for delivery; leaf correlation is dropped

`ContainerScopedEndpointTests.ScopedBusEndpoints_CarryTheExactCallerScopeThroughEveryOutboundShapeAsync`
records scoped publish/send captures before next.SendAsync, and those matching
consumers record no delivery.
Suppressing forwarding for ScopedPublished/ScopedSent can preserve the four
captures and both independent request responses. Require actual terminal consumed
records with each independently arranged published/sent ID, correct scope and
exact expected multiplicity. Nested mediator CascadeObservation stores leaf.Id
but emits only parent.Id in its result. Require both IDs independently equal
the arranged command ID; wrong leaf correlation must not preserve the oracle.
The real mediator filter/consumer marker pairing and request responses are strong.

### H08 — coverage descriptions claim unarranged owner/absence mechanisms

The default-bus journal startup test describes only-owning-bus isolation without
arranging a foreign journal owner. Shared/custom endpoint all-and-only tests
arrange only successful owned sends. Namespace discovery proves a compensation
endpoint exists but its successful routing slip never invokes compensation;
it also does not inspect final Ponged state after terminal drain. Require the
actual foreign/wrong-contract/failure mechanism and its independent forbidden
effect observation before crediting those labels. The genuinely two-bus observer
isolation test and controlled terminal excluded-consumer test are valid, but
they do not replace journal ownership or compensation execution evidence.

| Medium/Low | Location and deferred requirement |
| --- | --- |
| M01: configured retry budget | JobOptions ConfigureRetry(Immediate(2)) is observed mainly as a non-None Immediate type. Require actual attempts 0, 1, 2 and exactly three terminal attempts for that independently configured budget, not a type-only assertion. |
| M02: final callback counts | Container registration/outbox first completion snapshots and per-ID dictionaries do not observe later duplicate local callbacks/disposal. Require terminal re-read counts where exactly-once is claimed; retain already terminal Single published/consumed outbox and fault snapshots. |
| L01: fixture readability | Packed switch/lock/if bodies and enums in some scoped configuration/DI fixtures obscure inspection. Manually format during the admitted causal edits; cosmetic only, never Critical/High. |

Adjacent analysis still needs full source/owner reconstruction before a finding
is finalized: composite-filter empty/default truth cases, acronym formatting,
precise TextTable argument causes, complete reflected parameter/optional/default
shapes, depth 29 runtime enforcement, saga ACK effects and every remaining DI
contract method. Positive no-throw Validate tests are legitimate acceptance
oracles paired with negatives, not automatically assertion-free false confidence.
Purposeful markers, excluded discoverable consumers and metadata-only proxies
are not productive dummy features. No new test code is designed here.

## Fresh verification and actual raw receipts

SDK: 10.0.302; authoritative global.json runner Microsoft.Testing.Platform;
xunit.v3.mtp-v2 4.0.0. The run-tests skill and detection reference guide the
native executable invocation. Known sandbox IPC/MSBuild/Roslyn restrictions are
handled upfront by authorized escalation, not SDK changes, blind cleans or retries.

Strict Core Release build exits 0, zero warnings/errors, 62.65 seconds:
`dotnet build tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false -warnaserror`.
Fresh native Core execution exits 0: **4,007/4,007 passed; failed/skipped/pending/
other 0**, displayed duration 58.129 seconds. It uses the freshly built DLL,
minimum-expected-tests 4007, zero-tests-policy strict, fail-skips/fail-warns on,
progress off and native CTRF output in the exact own temporary directory.
The 105 Cache cases and 267 newly fully read selection cases are freshly passed
and independently reconciled. No VSTest separator/flags or zero-test acceptance.
git diff --check for the source exits 0. No new whole-product coverage/CRAP,
provider/cloud, production mutation kill or fresh Architecture run is claimed.
Iteration-127 Architecture 439/439 and bidirectional Async result remain historical,
not relabelled as fresh iteration-128 proof of the edited source.

Raw root: `/private/tmp/vsb-iteration128-core-composition-read.NrIHcG/`.

| Raw receipt | SHA256 |
| --- | --- |
| cache-initialization-core-release-build.log | ecfe587bd95768693f55d8836c329fa4d3a9a45818b5e88fe162152c00dddcfa |
| native-core-help.log | b850fbfaff820df015c9ca64f85d8a97eee359b436fcccbb49fbb89e3f0d74d9 |
| personal-read-bindings.log | 7657fba279941fd5d202713f826e6a42531ae91bcc3f42d8642bc54a2255aac6 |
| cache-initialization-core-native.log | e031cba468d2d928f9a8879f2aec73b92e4124bdad0a3adba959b533105b5dc1 |
| cache-initialization-core-tests/cache-initialization-core.ctrf | c77926de8943f1c5b430f794ddf470fc1b5935663e09e518fb74b973e3ef3695 |
| source-and-fresh-native-bindings.log | d8f2a1e2e08dc4f3331f5b65aa8dd108f11c1fa8889c664bc6e5a2cf0e9d100c |

Both read/source/native binding diagnostics terminate 0. The owner uses the exact
qualified glob exclusion learned previously; no empty-scope or newline-byte
miscount is reintroduced. Git byte equality and native membership are diagnostic
accounting, not semantic full-parser admission or substitutes for actual reading.

The initial handwritten-report validation exits 1 because the SendPipe SHA row
omits its measured final hexadecimal character f. Only that report cell is
manually corrected; the original failed validation log is retained. This is not
a test, source or compiler failure and does not justify weakening any check.
The identical strict validation with a separate corrected log must exit 0 before
checkpoint security. All three previous history tails remain byte-preserved.

## Counter-review and continuation correction

Main review is explicitly Author-Red-Team, not independent acceptance. The
attempted separate internal Sol advisor supplies no admitted source judgment:
mandatory authority output is truncated, four frozen source hashes are not
validated and zero productive paths are read. Initial broad workspace navigation
also enumerates protected legacy AGENTS path names; no protected contents are
read or files changed. That boundary violation is retained, not represented as
clean-scope evidence. Broad navigation stops; no independent/code-review credit.
The main does not repeat it and no external product role is impersonated.

The PO correctly points out repetitive resolved-structure answers. In the last
case the continuation summary explicitly marked the question answered, yet the
main answered it again. This is an observed resumption error, not evidence of a
permanent model defect or lost code. The original goal and exact current Git
input remain intact. Continuations must start with the unfinished concrete file/
finding and actual worktree, not reopen already settled informational questions.

Continue the remaining 59 connected files and all other owning inputs through
complete admission; resume the partially read DI contract file at line 236.
Then add the independent constructor fault/release regression and effective
single-cause cleanup mutants, repair the reviewed test oracles and continue the
remaining Cache ownership/comparer paths. Complete-src manual reading/comments,
greenfield API/type/file/owner architecture, all earlier findings, feature
equivalence and real provider/global coverage/CRAP obligations remain unchanged.
Normal scoped commit, annotated tag, atomic push and independent keyed remote
verification secure this intermediate checkpoint; they are not A+ acceptance.

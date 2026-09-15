# Complete job and reliability reading checkpoint

Original whole-product A+ goal: active. This packet advances personal reading;
it does not accept the complete Core test owner or certify the product as A+.

## Secured input and authority

Repository: `repositories/vicione-servicebus`; branch:
`feature/servicebus-a-plus-api`; input commit:
`10d1198187cf7ab1050351cfc0cac97489111b79`.
Core owner tree: `e3be6b831183b3b65636c3b5e167c165696037d2`.
The input is the actually committed, normally atomically pushed and independently
branch/tag/peeled verified iteration-123 checkpoint, all terminal exits 0:

- Tag: `servicebus-a-plus-iteration-123-saga-source-and-test-reading-checkpoint-2026-09-15`.
- Annotated object: `c55909042ca91c31634c89a308eaefe0417a05ac`.
- Push receipt SHA256: `394b3133668060edd67ba56145d58b6417b3e9d70716e45da92d528646f8914c`.
- Independent remote receipt SHA256: `8d79475b66000c12271c9e3ad4b8235686b60a7fc89e2b5161e3e7bbc5bd2d7e`.

The unchanged main-completely-read authority is reused by matching hashes:

| Authority | SHA256 |
| --- | --- |
| AI_WORKING_AGREEMENT.md | e6d5f60db535ad6228fca5445b68abaa7a29cd6e24b5d2f876352bc7de875d2e |
| GLOSSARY.md | 7ce780b178a971e40b57ee7ffb3bec472becdff96ef946726e0143339793adf7 |
| DECISIONS.md | 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4 |
| current/README.md | a7bd61f878b84fb6f93f48402a21becd37ed253e20bab62b7026bdd165469a39 |
| CURRENT_ORDER.yaml | 49691c76d63dea1591fd5a7fa17450d1e254ec62d616ef4705aee082517bfc3d |
| FINDINGS.md | 9a913a7937a5d216edc3ce83940c215a8d47e81e5823ccab286aff0cf2eea397 |
| Selected DEVELOPMENT_SLICE.json | 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199 |

Agreement §4.3 requires the whole owning tracked test project, effective shared
configuration, fixtures, data and execution inputs before test design, changes
or Lead acceptance. Successful execution cannot replace that reading. Equally,
reading-only progress does not itself require an expensive unchanged full run.
No Core test design, change or complete-owner acceptance occurs in this packet.

## Actual complete personal reads

Every file below is completely read by the main, including all fields, nested
types, methods, helpers and comments. Long files are read in continuous bounded
ranges. Truncated output is reread at the exact missing range before credit.
Invoking a tool, an inventory, a static signal or a passing report is not reading.
No advisor substitutes for the main and no new advisor is used in this packet.

Paths are relative to `tests/ViciOne.ServiceBus.Tests/`. This manually authored
sorted table binds the input and unchanged final bytes, not generated reviews.

| File | Lines | SHA256 |
| --- | ---: | --- |
| DurableSend/DurableSenderDeliveryTests.cs | 621 | a83a0fa907ded5739d5620852f6ae7793a36830a201a87d42ae6a8ec2cf145d4 |
| DurableSend/InMemoryReliableStoreTests.cs | 780 | c0444602accbe813ed179a8d438604cb5c5373521b34e12a7111cb80910a8a19 |
| DurableSend/ReliableMessagingProviderGuardTests.cs | 99 | 28378d582bf461b970bf3c439f98517fce5585ca6790024df3a35bdd8a4021cb |
| DurableSend/ReliableMessagingRegistrationAndAdmissionTests.cs | 1420 | ae5a924a8284cbb8ffa97168bbe4b08299973bbcf1ec1a81422ea974d059469f |
| JobService/Api/ApiJob.cs | 6 | e0491e3be6ff10859dc8988e99859c2153f42014f0081f4beed236fa87f52114 |
| JobService/Api/JobServiceContractArchitectureTests.cs | 61 | bafc70219b32e60a1426920b05965d21919c2a2db41038960d8626e788aa7bd8 |
| JobService/Api/JobServiceEventExtensionsTests.cs | 100 | 562d02fb03d8f4ded819fed0e4eaed961fd33dc1ce3d557384582e3ab96ab046 |
| JobService/Api/JobServiceExceptionTests.cs | 31 | c7849aecef9272fa1259b23755b863134bfa7b13270eee816a75d8f0514b3aee |
| JobService/Api/JobServiceExtensionsTests.cs | 281 | d4dee186f7955e951d5c39fb59ce73c780213dbca7160642019d258c77835164 |
| JobService/Api/JobStateResponseTests.cs | 77 | 1f9312b2166103bfd903c635b2c1242eeb9d4db99ab84fdd4a3ee8c3ccc3c9d1 |
| JobService/Api/RecordingPublishEndpoint.cs | 29 | 987ac07b42684db3abe679a86cd00c2b1b6e49e2c1d3072c462ff1482c43e80b |
| JobService/Api/RecordingSubmitJobClient.cs | 36 | 3b48e91bfc3d472ccf5f32dfcb7d7bbb4a2baa97e9d27d3cc5e2f409a09db538 |
| JobService/Api/RecurringJobExtensionsTests.cs | 330 | 140ae2e65ad4c2db9aa1c27608eb52d3daac80443f73cc2625c29b4dae46ac11 |
| JobService/Api/ResponseFactory.cs | 28 | fa4a9d6b00f279f3ec3e9bbdf186144183cfd09a7b9c4d6fd277eb5e3e8e2034 |
| JobService/Configuration/JobConsumerKindContractTests.cs | 332 | 5ae3fe4b201f0889fc964416dc1e91c78e6844e8b8194e6109d0d6f4e22d0ee9 |
| JobService/Configuration/JobConsumerTimeProviderTests.cs | 68 | 20676fe56c86d030601f19e107162a819f5b59e677a3d0c16d796507406bec6d |
| JobService/Configuration/JobSagaPartitionKeyConfigurationTests.cs | 176 | f73e9e8e213a35fcecdc7c66a938e7bf35658c03ae268bbf2b30352d7fd689d4 |
| JobService/Configuration/JobServiceEndpointConfigurationTests.cs | 275 | a4198ec9afb5d8cbd9b9914bbe93041ef2dab05dcad8e8066ddcdc0d91911d71 |
| JobService/Configuration/JobServicePublicConfigurationApiTests.cs | 234 | f0a44850b968373fe331b1d3596847e3e918039f3dc0b146d75162c330135dd2 |
| JobService/Configuration/RecurringJobScheduleConfiguratorTests.cs | 329 | 16bf0609b8bd7f86a730b65e044d4ba6f085321783e5c34fd3be5b1d1f3ba2a6 |
| JobService/Integration/ContainerJobConsumerDiscoveryTests.cs | 81 | b457b6448506458c5e7e4c1c62f80100102d13c43d4f62cb22f1090587db7fb8 |
| JobService/Integration/ContainerJobDiscovery/ContainerJobDiscoveryTypes.cs | 27 | 9aaba201744b383a4ed9a1b5154a73a9f27c2c8fcb5883749c5dc51e31a67453 |
| JobService/Integration/InMemoryJobServiceTests.cs | 841 | 60944619f71f2a3fb52f2f57e31702de83081ae0b7e8f3cbba5c1f49c5290f72 |
| JobService/JobService/ConsumeJobContextCancellationTests.cs | 463 | 72b1d3ca7fdb99e3e71b6620287c73a42866dfa5185f181358f8f95b26b681aa |
| JobService/JobService/JobConsumerMessageFilterTests.cs | 232 | e8a05ba01f6048410bfbd66c24dc485bccaacf993e60b06f052f8fc28bbb6d42 |
| JobService/JobService/JobDistributionStrategyTests.cs | 153 | 152a81db9ff28a6e4f001f426e23f164edf07842affe53b4fe86d547e59f1ca1 |
| JobService/JobService/JobIdentityTests.cs | 46 | 545f204428f10409082f3a054de899725846bfe7845012e25b34263c4b4afbcd |
| JobService/JobService/JobProgressBufferTests.cs | 152 | eec5655e25d5b3bf30702b901bd1d04f7c3daa3fb6bcd6e9c44d70ade437318a |
| JobService/JobService/JobServiceBusObserverTests.cs | 156 | e2c6f242a38e67e08e3bdabd21164b9985f4d0787bfa1c18654574aed01bcb2b |
| JobService/JobService/JobServiceLifecycleTests.cs | 777 | 87b95d84b42e4d99c036e1772714a1e03c4dd991c29db30bd36c26e36a48e2d7 |
| JobService/JobService/StartJobConsumerTests.cs | 226 | 3fbc7ddb3a458af8799a21e8af8e6eb1883e6b24ad0835d7eecd6fc896eca7b2 |
| JobService/JobService/SuperviseJobConsumerTests.cs | 251 | 49fa64b465cd40b14bc7fbcf1ae2f25197a69d382f7ce8d077e2bb0ef2b0e238 |
| JobService/Scheduling/CronExpressionCalendarTests.cs | 113 | 48d19b4fdb2e7e2fef8903a3c35323bcff1ab91f8eadc6ac61c7c43341346db0 |
| JobService/Scheduling/CronExpressionContractTests.cs | 145 | 9a4c3dd6f086ef374a94cb13284a920203f98af0865d0b11461918f251da672c |
| JobService/Scheduling/CronExpressionDaylightSavingTests.cs | 79 | cde18001254d96c5dcae487803e1aeb2df430ed7278a95ee7931bf60840a82af |
| JobService/Scheduling/CronExpressionParsingTests.cs | 351 | 6adeeef4fd6fc33258349f1812a48601f391dc4b7e262caf886e80c69017de79 |
| JobService/Scheduling/CronExpressionSchedulingTests.cs | 137 | aedf985ca8e5f9e1016781d28dc1103ee68bf0282a4dd65570ebc6923f992c60 |
| JobService/Scheduling/TimeZoneResolverTests.cs | 86 | 6fc8ea04cac2f4d77d7f75291f1bd3c8050579d1839ee3122c81569062e17e2a |
| JobService/Serialization/JobPropertyCollectionTests.cs | 157 | bf4066af153b1b26baf1fc4dc9a7f00035dbd8fc8104eccb20ec26fc65050513 |
| JobService/Serialization/JobPropertySnapshotTests.cs | 26 | 777a3ea2d070269eb3b163b350e9f36138c2720f6372a2ea9f5aa645594c7c7a |
| MessageJournal/MessageJournalContractTests.cs | 115 | aeb326dc46bba2901095dc459b170e18a64d4561b2dcf165d121a7190b6aec96 |
| MessageJournal/MessageJournalIntegrationTests.cs | 467 | e34516b51df4edea389d48127ca39e8415574a49718570aedc5a8a53c3431fd1 |
| MessageJournal/MessageJournalTelemetryTests.cs | 476 | 95e8f174b1578971de26f618070aacbcd749205092a3094cd89e182cef505c6f |
| MessageJournal/MessageJournalWriterTests.cs | 442 | 88fbeb65a015ac2821b1a429d91ff3a0e20df5641357ecab302094a555505787 |
| ReliableMessaging/ReliableInMemoryIntegrationTests.cs | 493 | a1f8b3a64041b21987e406fc4e559f7261ef13a135eeb1ac1a0e19413d7261ce |

Actual new reads: **45 files / 11,805 lines**, comprising forty test files and
five standalone fixtures. The preceding [47-file reading record](../ITERATION-123/CORE_OWNER_READ_PROGRESS.md)
is exactly revalidated at this input: 47 files / 13,908 lines, no overlap.
Cumulative personal Core-owner reading: **92/557 files / 25,713 lines**;
**465 files remain**. All 51 tracked files in the selected JobService,
DurableSend, MessageJournal and ReliableMessaging folders are read: six
JobService state-machine files were already read in iteration 123. That folder
closure is not whole-owner admission, repository test closure or full-src closure.
No additional productive source file is credited by this test-reading packet.

## Proof scope and actual diagnostics

Own raw directory: `/private/tmp/vsb-iteration124-core-reliability-read.loTmBj`.
Read-only diagnostics preserve actual stdout, terminate 0 and are personally
inspected; Ruby comparisons use binary `.b` and compatible `map`/`compact`.

| Actual receipt | SHA256 | Outcome |
| --- | --- | --- |
| read-bindings.log | 2536115c94c5ebe9be97a9975aa3ba7d3530ac85e0e6dc59f126f9ad60797c25 | 45 exact unchanged input files, 11,805 lines, owner 557 |
| previous-read-binding.log | 92debd69bc6ce307cd0e2ee638c069108d84852723d53125e44b2ec8f40a5934 | 47 prior exact files, 92 cumulative, 465 remaining |
| reviewed-native-methods.log | ae5f6a15b92ef746dc0c67f2dff2bb6a8a6cac81c888b8d832564a4410b14cd9 | 283 declarations equal 283 historical native methods / 514 passed cases |
| compatible-manual-report-check.log | f23c76164c653024ecdb178dedc969988e124b307f8d7be7a10e4a41f87b2a45 | 45 handwritten manifest rows and all 283 method/case rows exactly match |

Structured diagnostic correction: the initial manual-report checker exits 1
because a UTF8 regex containing the heading's em dash is applied to a binary
ASCII-8BIT report string. Its empty stdout is preserved as
initial-manual-report-check.log, SHA256
`e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`;
the actual Encoding::CompatibilityError is observed in the command output.
The corrected read-only checker explicitly reads textual reports as UTF8 while
retaining binary Git/source equality comparisons. It terminates 0 and finds no
manual row, SHA, line or method/case discrepancy. This diagnostic encoding issue
is not a productive/test failure and does not trigger an unchanged build rerun.

The lightweight declaration enumeration reconciles personally read methods to
existing native records; it is not a language-complete parser or whole-owner
acceptance. Full owner admission still requires the language-appropriate full
parser and exact `Git files = personally read files` closure prescribed by §4.3.

No productive source, test, project, dependency, directive or gate changes.
Only manually authored reading/review evidence and test-agent history prefixes
are written with apply_patch; no generator writes code, comments or reports.
Protected `review/**` and `TestResults/**` are not enumerated, read, edited or
staged by the main. Unrelated user-indexed/worktree changes are preserved.

The last actual fresh strict builds and native runs remain iteration 123:
Core 4,007/4,007 and Architecture 439/439, zero failures/skips, including the
actual bidirectional Async case. Those are historical, input-bound results,
not new iteration-124 execution. They neither kill the newly recorded weak
oracles nor prove genuine external-provider acceptance or current coverage.
Replaying identical builds, all 4,007 Core cases and the four-minute architecture
run for a reading-only packet would not validate new executable behavior.
Its proportionate gates are exact read bindings, manually authored report
reconciliation, receipt hashes and whitespace/diff checks of the five owned files.
Any future executable/comment/test change is evaluated against its actual
affected proof contracts rather than using this decision as an execution waiver.

## Architecture boundary and continuation

Retain sibling SDK project owners under `src`. `src/ViciOne.ServiceBus` owns
the Core assembly, not an umbrella containing optional assemblies. Only the
shared `Directory.Build.props`, not productive C#, is directly at the src root.
Persistence/Scheduling/Transports remain useful provider/integration families.
No relocation, recursive SDK source-exclusion workaround or feature removal
is introduced to make the product look superficially uniform.

The [handwritten review](JOB_AND_RELIABILITY_TEST_REVIEW.md) records four High,
one Medium and one Low finding, all open. Complete the remaining owning inputs
before test changes, then repair exact independent oracles and connected runtime
contracts with genuine red/green and selected effective mutations. All earlier
NST/SMR, runtime/API/cache/rollback/cancellation/recovery candidates, full-src
personal reading/manual comments, type/file/folder naming, greenfield feature
equivalence, metadata/package baseline, genuine durable/provider acceptance and
current whole-product line/branch coverage plus CRAP remain in the same goal.
Checkpoint security is credited only by actual normal commit, annotated tag,
atomic non-force push and independent keyed branch/tag/peeled verification.

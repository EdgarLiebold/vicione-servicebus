# State-accessor checkpoint bindings

Input commit: e280df694ab9e6c50e3877aebf97882dfeeacb5e.
Slice SHA256: 5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199.
Current DECISIONS SHA256: 43d9d6a2969e16284706e4b644de73573930cd9fe408fd8db856340849599cd4.
All source paths below are relative to src/ViciOne.ServiceBus.Sagas/Sagas.
The main completely personally reads these files and checks their comments.
The internal read-only Sol advisor's supplied entry/exit hashes match the final
source column. Neither this eight-file packet nor the advisor admits all src.

| Source | Input lines | Final lines | Input SHA256 | Final SHA256 |
|---|---:|---:|---|---|
| Accessors/ViciOneServiceBusStateMachine.DefaultInstanceStateAccessor.cs | 84 | 88 | ba44a2d782237d098a926a26f9a3e360bd0b440d92c73ee38376f43dfca6c3d8 | 878d84911537b6b38d4ad1b4e0567083be40d6405f9c0c418015698a8f9f2340 |
| Accessors/ViciOneServiceBusStateMachine.IntStateAccessor.cs | 83 | 83 | 5144c7290fdb5a3153424af6d698a0db150bc4d37404dcc9008165651dfe50b9 | 5144c7290fdb5a3153424af6d698a0db150bc4d37404dcc9008165651dfe50b9 |
| Accessors/ViciOneServiceBusStateMachine.StringStateAccessor.cs | 85 | 85 | 80def08326e1752a7094643e5aa9f6f25c8222d370ebafe466af2e29fd2519d7 | 80def08326e1752a7094643e5aa9f6f25c8222d370ebafe466af2e29fd2519d7 |
| Accessors/ViciOneServiceBusStateMachine.RawStateAccessor.cs | 84 | 87 | fb896f6aac2a72865c49f813fb81274fa897c03e88335b1f5b0592f6dfc0c256 | 739a6ed92fd50b29c64afc72215dd24cbe2b71215f536e8b1515ff9e96e59877 |
| StateAccessorExtensions.cs | 31 | 33 | 92c2527e8db686af0b75ad542b074d5c90871ea8dcbe207326b515e947e51487 | 3ada6b02fd87add923fd68f2764689e969a320756aa0d032e4cb70bc0ba06d52 |
| IStateAccessor.cs | 30 | 31 | f3efd6a8aa760ee34f6a0f231fcc6dd10de22592609f45fab9177e8704c1937f | b92f53ec583093b47b4e667b2831e4b49ee9e5877dd2c16302702d66dffb0aca |
| Accessors/ViciOneServiceBusStateMachine.InitialIfNullStateAccessor.cs | 53 | 53 | 17ede1d4d92a66469eca366106de44527edb0e4bed157b266f2104dcc7386f45 | 17ede1d4d92a66469eca366106de44527edb0e4bed157b266f2104dcc7386f45 |
| Accessors/ViciOneServiceBusStateMachine.StateAccessorIndex.cs | 57 | 57 | c1f0257eb824756c6869cc272979491d87e046aed148db9a0f0933a9f959dc13 | c1f0257eb824756c6869cc272979491d87e046aed148db9a0f0933a9f959dc13 |

Exact comparison removes only lines beginning with /// after leading whitespace,
then compares the remaining input/current bytes. All eight comparisons pass;
exactly four files change, with 507 input / 517 final lines. It does not establish
feature equivalence for unrelated prior API changes or close runtime candidates.
The source-equivalence diagnostic terminates 0.

## Actual raw validation receipts

Raw directory: /private/tmp/vsb-iteration122-state-accessor-read.CdDUDv.
Each row names a real receipt, not an intended command or fabricated outcome.
Builds are focused incremental Release builds, not a clean global inventory.

| Receipt relative to raw directory | Terminal result | SHA256 |
|---|---|---|
| source-equivalence.log | 0; eight exact comparisons | 74970be381c60c48b284641661e0b10f05b9d3127e62a748d5288f1bc7d68c2a |
| core-owner-read-bindings.log | 0; exact unchanged owner input and 27 completed read bindings | 0724982e634996c03e3c8e970015ba42c1dc722cc0c13b328ce5a6ce72527888 |
| core-build.log | 0; zero warnings/errors; 64.64s | a9e512af6ddb923eb5c838e62c6d915f775114196fafdb35ff7afc010a762e07 |
| core-host-help.log | 0; actual native host options | b850fbfaff820df015c9ca64f85d8a97eee359b436fcccbb49fbb89e3f0d74d9 |
| core-tests.log | 0; 4,007 passed, zero failed/skipped; 20.439s | 3aa01c37036b429ff2a918c4de5b198e85daf1bd16efd6fff53e3ec90ddc8b38 |
| core-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_17_15_48.661443.ctrf | 4,007 actual passed case records | fe328f4d4f8d65261514a8c893a7ccb21a9e41bbb0af01c0368955dc5c8f12e9 |
| architecture-build.log | 0; zero warnings/errors; 23.66s | 76baaac266121dd7e4d86aac470ed8b37aec54033ee200e8d51b2f49a3aa53a8 |
| architecture-tests.log | 0; 439 passed, zero failed/skipped; 3m33.469s | f13d9a7ccc006aeb700642d518996949e49e37f5e35dd06c40ef2a00fea9c702 |
| architecture-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_17_16_47.605375.ctrf | 439 actual passed case records | 3c6afde22e8096861d5258afab5d8df3843d3b5c64783e9ba6e020535e936afc |
| source-whitespace.log | 0; exact four-source verifier; zero bytes/no writes | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |

Native reports are independently parsed: exact expected case counts, every case
passed, zero non-passed statuses. The Architecture report contains exactly one
passed Product.AsyncApiConventionArchitectureTests.
EveryMethodName_MatchesItsAsynchronousContractBidirectionally case (174206ms).
This actual current guard is not an exhaustive runtime semantics certificate.
Neither report is global line/branch coverage, mutation or cloud acceptance.

Checkpoint security is checked after committing: own commit, annotated tag,
atomic non-force push and an independent reference-keyed remote query. This file
does not invent its own future commit hash or claim a recursive self-hash.

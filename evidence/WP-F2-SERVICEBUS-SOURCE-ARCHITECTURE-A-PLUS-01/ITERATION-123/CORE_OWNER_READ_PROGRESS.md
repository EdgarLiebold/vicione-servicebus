# Complete personal Core-owner read progress

Input commit: 3bb808bc999141638b1620c4adeb5bc51ceacc86.
Owner: tests/ViciOne.ServiceBus.Tests.
Input tree: e3be6b831183b3b65636c3b5e167c165696037d2.
The tracked owner contains 557 files / 553 C# files. The tree is unchanged from
the completely bound iteration-122 input. This is partial owner admission, not
permission to design, change or accept new Core tests from a sampled owner.

The previous [27 complete reads / 7,104 lines](../ITERATION-122/CORE_OWNER_READ_PROGRESS.md)
are preserved at the input commit. All 27 files still equal their bound input
bytes. The following twenty additional files are completely personally read by
the main Lead, including every method, field, nested fixture and helper. Agent
summaries, filename discovery and test execution do not count as personal reads.
Paths below are relative to the owner and sorted. Current bytes equal input bytes.

| Additional completely read file | Lines | Input and current SHA256 |
|---|---:|---|
| JobService/StateMachine/JobAttemptGenerationTests.cs | 485 | f0fc80cc344fc8eab22e35f45a1c52f0c8f8cb55f138f92641f96021edee8b8f |
| JobService/StateMachine/JobAttemptStateMachineTests.cs | 474 | 41bc116c8e2972baf72b3b992e81898c6809655fddf82b7b0b34a63c12bbe2f0 |
| JobService/StateMachine/JobStateMachineLifecycleTests.cs | 552 | 79293da1955048168d9db5be8d2b55a6d113bb4674494d0ca76dbf410811d1a3 |
| JobService/StateMachine/JobTypeCapacityTests.cs | 228 | 1e8a275cf55894b54bb2db12017650a049e0fe4d4bba871d563c180171b8f6cf |
| JobService/StateMachine/JobTypeStateMachineTests.cs | 342 | a2b1afbf33188018f6c16e6ee58a86f4117851b0f58c3ec25311badbb1c983d0 |
| JobService/StateMachine/StateMachineTestScheduler.cs | 153 | fc51c90e26587044d3474a86809751830778292b7d4fd7a026c36363b2256e10 |
| SagaStateMachine/StateMachineConcurrencyIntegrationTests.cs | 458 | e3872e50c2d87356c282c21fedcfd5c6890d5a1a57f04a0fb503c83bcd85a433 |
| SagaStateMachine/StateMachineOutboxSchedulingIntegrationTests.cs | 282 | 0e47b7398edbdd8270b654f1bb57f23adf72a0752afc43488fa69e20b03ae620 |
| SagaStateMachine/StateMachinePolicyIntegrationTests.cs | 590 | 4dbc5a373fb724a119ac934618398303219e8b9637148dc86db5848dd1cf8130 |
| SagaStateMachine/StateMachineRequestIntegrationTests.cs | 472 | 96c02222ab34804a404285c15256a9d01aff23663e043b14d56f3da740437303 |
| SagaStateMachine/StateMachineResponseAndFaultIntegrationTests.cs | 499 | 78ea9c817d33f4c1239f65bf7e0f740728edfc00aef9e835519a9eda5e2fa25d |
| SagaStateMachine/StateMachineSchedulingIntegrationTests.cs | 359 | 78a4a40d791d0cca8f2b7deb42825b1394921efb98d8b541ec49529529acfb79 |
| SagaStateMachine/StateMachineTransportIntegrationTests.cs | 413 | 4849c44607937a25b8bcb12ac0f27becebbaed51c8487af18d5408d0353d4d26 |
| Sagas/Configuration/SagaConnectorTests.cs | 255 | 48fb0d74cd2395499f358692cf51e3abf4cd451e48ea44007ccbfdf1ad98672a |
| Sagas/ContainerSagaIntegrationTests.cs | 183 | 0742fe3591b1045ccee3612518d4af94e67f654f77597749650828635c9ef145 |
| Sagas/InMemorySagaRepositoryConcurrencyTests.cs | 61 | bd94a0327f7c1dcdadcbd2c6c4fc28757b321be0fc86fd2f93eedaa84d43a257 |
| Sagas/LegacySagaIntegrationTests.cs | 472 | a52719e36f5195f870e02e0923f684e3dc06b19a007734af0b8c0b84dea081ba |
| Sagas/SagaMessageFilterBoundaryTests.cs | 76 | c63475fb92b71b4c56562fc04571eaaf6adbe4ed219b9bf2786c10ae5b83c569 |
| Sagas/SagaPartitionerConfigurationTests.cs | 134 | ef192569c53eec78f6465f411e16e8311d998181648ad81da347b318d09cab45 |
| Sagas/SagaRepositoryCapabilityTests.cs | 316 | 6a6b82042fa9667d576ce506bacba09ba3af744ffbcc489d29624a7fe92bb75f |

New complete reads: 20 C# files / 6,804 lines, consisting of nineteen test files
and one scheduler fixture. Cumulative complete personal reads: 47 files / 13,908
lines. There are 510 remaining tracked owner files. All forty tracked files in
SagaStateMachine, Sagas and JobService/StateMachine have now been completely read;
this closes those selected folders only, not the 557-file owner.

Every existing test method in the twenty-file addition is accounted for in the
[handwritten review](SAGA_TEST_REVIEW.md): 93 methods / 137 declared native cases.
The actual final Core report contains exactly 137 passed records for those methods.
No new test method, test design or test change is made before full owner admission.
Shared effective root/test build, package and native execution/CI inputs must also
be completely read or exactly reconciled; they are not silently added to these
forty-seven owner rows. Full owner closure still requires the complete tracked
manifest, content-bound readings, full parser and exact file-set reconciliation.

## Actual byte-comparison diagnostic

The first read-binding checker terminates 1 because Ruby compares a UTF-8 string
returned by git with ASCII-8BIT File.binread using encoding-sensitive equality.
The text partitioner fixture contains Å; its input and current SHA256 are both
ef192569c53eec78f6465f411e16e8311d998181648ad81da347b318d09cab45.
An independently observed binary .b comparison is true and the exact git diff is
empty. This is a diagnostic encoding error, not a source change or test failure.

The original receipt is preserved as
/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/core-owner-read-bindings-initial-failed.log,
SHA256 71df3c74e7eb34d1528a8fd86002adf0ebe4198d489999399b2298c97d6c1178.
The corrected checker compares binary .b bytes, terminates 0 and validates all
twenty new bindings, all forty-seven unchanged input files, the forty-file selected
folder set and the stated totals. Final receipt:
/private/tmp/vsb-iteration123-saga-core-read.oI6Ada/core-owner-read-bindings.log,
SHA256 9b9286bec7f5f610051736aad800b8066d71c042f9b3c4dcca64a2adac23e489.

Continue from these complete reads without restarting them. A subsequently
changed input requires its actual delta to be personally read and understood;
enumeration and passing cases cannot replace unread files or dispose their tests.

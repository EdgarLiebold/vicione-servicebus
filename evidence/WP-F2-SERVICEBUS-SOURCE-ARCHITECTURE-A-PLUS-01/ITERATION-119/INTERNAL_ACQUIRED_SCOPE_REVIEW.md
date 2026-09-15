# Internal acquired-scope counterreview

Date: 2026-09-15. Reviewer: internal read-only gpt-5.6-sol agent
`/root/iteration119_acquired_scope_red_team`, explicitly authorized by the user.
This is not independent external product acceptance. No reviewer test/build execution.

## Immutable design counterreview

Reviewer reads immutable commit0894a7f3 and the human-written acquired-scope plan,
without freezing live source. It identifies a concrete lifetime counterexample:
operation O acquires aliases A/B on marker S; A lease ends, B remains live. Deleting
A's association would lose still-active marker lifecycle/terminal ownership and cause
redundant payload callbacks on A's recognition/re-entry. Preserve acquired identity
until the operation ends, not until an individual alias lease ends.

The reviewer also confirms propagation must inspect both operation entries on the
same marker. A child alias B may discover the parent's marker through payload admission
without an exact parent B mapping; requiring that mapping would wrongly isolate it.
Sharing a marker must still create independent child State, never inherit parent
failure sets. Bookkeeping must serialize final clearing against concurrent retention.

The author accepts these invariants before product correction, adds two human-written
released-alias cases and executes them against unchanged product. The unarmed case
passes; armed case fails causally. The reviewer does not claim to execute this proof.

## Frozen implementation counterreview

Root freezes executable source/test/helper edits and submits the current implementation.
Reviewer personally reads the complete named kernel and affected test/helper files,
capturing the hashes below. Current design retains historical `_contexts` identities
until total ActiveScopes0, with marker-local State scope counts unchanged. Retain and
Release consistently lock operation → marker; no reverse lock/callback under locks.
Mark, recognition, terminal lookup and transfer use acquired references. Actual public
AddOrUpdate failures preserve exact identity. Necessary admission completes before
Current publication and is protected by the acquired input operation. NotifyTerminalAsync
preserves notification/cancellation/terminal ordering without duplicate status branches.

Result: no additional concrete implementation findings in this bounded frozen scope.
Earlier alias-release counterexample is addressed. No concrete lock-order, marker-sharing
isolation, primary/cleanup replacement, cancellation, notification-order or comment-accuracy
defect is identified. Existing33methods76 RetryFilter and11methods23 consume acquisition
cases plus24methods106 ownership cases are68methods205cases; this accounting is not
whole-product test assurance. Author-run greens are not independent reviewer executions.

Reviewer explicitly RELEASES the executable-source/test/helper freeze on completion.
Root also waits for every active build/test/format process to become terminal before
controlled executable counterchanges. Protected review/ and TestResults/ are untouched.

## Actual captured inputs

| Repository-relative file | SHA-256 |
| --- | --- |
| src/ViciOne.ServiceBus/RetryPolicies/RetryOperationState.cs |91f4f2380de58a643b22a09ef7412b15a31a6c1d607b948ae88a8c7391728d1a |
| src/ViciOne.ServiceBus/RetryPolicies/RetryPolicyExecution.cs |94a779a360fd721e0acc8a28512289d8b5850918ae29db4f1999689f7ff1c5ba |
| src/ViciOne.ServiceBus/Middleware/RetryFilter.cs |59bd5c3eb1013852167a96c8c4cb50a2921248994ce2e07f350f6337b8909428 |
| src/ViciOne.ServiceBus/RetryPolicies/RedeliveryRetryExecution.cs |25f540d360fe3a0e0af7a5bf00e50e0b80b7627ccdcd723ec70522ff09a0c9ef |
| src/ViciOne.ServiceBus.Abstractions/Middleware/ScopePipeContext.cs |06eff6d83c5a86fe88ca8baa8558c7b5f0e78174cfadad9a82688c4c80897990 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Payloads/ListPayloadCache.cs |46502518b6a48d1dd51ad95e6fbaf7133970623a6a4a190fc731ff8ab3db9145 |
| tests/ViciOne.ServiceBus.Tests/Middleware/RetryOperationOwnershipTests.cs |512f7a0040028cb13d477d9b322a7e5ce1e7987db2b7628d47e930b03bcc72f1 |
| tests/ViciOne.ServiceBus.Tests/Middleware/RetryFilterTests.cs |2ca4fa3ad01bf7a2dbe7f6e5955af665ff5e0dc9afaf578f0afb3016c42560d6 |
| tests/ViciOne.ServiceBus.Tests/RetryPolicies/ConsumeContextRetryPolicyTests.cs |b704010d25c6ee4136fbc7f2fe3f6e89a68295fa3d60fee51d048c416f567573 |
| tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Retry/RetryFilterTestFactory.cs |c230acd666687c6e70319de6aa581ce20add2c455e0f2c7e1761775d97509055 |
| tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/InMemoryOutbox/InMemoryOutboxTestContextFactory.cs |b5668d870038169eb1513b9159d1edf9ce3c2487e9376765b19d07b8e466eb29 |

Main independently checks the matching current source/helper hashes. Subsequent
controlled mutations must restore these exact executable bytes; no acceptance is
carried to a changed implementation merely by relying on an earlier green report.
Author final Fact/Theory audit corrects the early25/69 estimate to actual24/68;
the reviewer input hashes and executed205-case count are unchanged. This is an
author accounting correction, not an independently rerun reviewer proof.

## Boundary

Static internal counterreview is limited to the above packet and tested invariants.
Actual delayed/RabbitMQ provider publishing/topology cancellation, remaining foundation
contract checks, broader current host/package/API/journey gates and complete source-read
inventory remain open. No entire-product coverage,100% API correctness or overall A+
claim. The original goal remains active.

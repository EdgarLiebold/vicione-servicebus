# Retry operation ownership packet

Status: corrected 35-case and existing76-case selections pass; complete Core passes
3,744/3,744 without skips. Internal read-only counterreview and formatting verification
pass against the frozen source. The separate internal counterreview is complete:
four new causal findings NN-06 through NN-09 remain open, documented in
[INTERNAL_OPERATION_OWNERSHIP_REVIEW.md](INTERNAL_OPERATION_OWNERSHIP_REVIEW.md).
This is not final Iteration119 or entire-goal acceptance.

## Executed causal baseline

Checkpoint product source b6e54731692da2635cc5839e7c0af40d86a04044 remains unchanged
through red execution. The strict focused build passes with zero warnings/errors.
All 35 new cases fail causally after the test fixture is corrected; existing
RetryFilterTests pass 76/76 with no skip. The new failures verify NN-01 through
NN-04 and one additional author-discovered concurrency defect.

The first fixture draft fails compilation because its BasePipeContext import and
two explicit test-cancellation arguments are missing. These are authoring issues,
not sandbox/product defects; existing strict rules are preserved. The first test
execution then records 19 causal failures and 16 invalid redelivery results caused
by a private message type closing DispatchProxy's public consume interface. The
test-only message type is made public. All 35 cases are repeated against unchanged
product source, with no TypeLoadException or other fixture failure. Only this second
35-case artifact is accepted as the complete causal baseline.

Accepted artifacts:

- `/private/tmp/vsb-iteration119-operation-packet-causal-red/operation-packet-causal-red.ctrf.json`
  — 35 failed, zero passed/skipped, expected test-runner exit 2;
  SHA-256 `18ac19794803295278159882f39816acf04e93191ad79877b6a40e3aa2409b5f`.
- `/private/tmp/vsb-iteration119-operation-packet-existing-red-baseline/existing-red-baseline.ctrf.json`
  — 76 passed, zero failed/skipped, exit 0;
  SHA-256 `2e2db881de8e6a66e09a82ed77443198c0ba7361db000b9dce2b6d7424d5b56d`.

Corrected-source build passes with zero warnings/errors in 1m09.15s. The new35
selection passes35/35; the existing76 selection passes76/76; the full Core host
passes3,744/3,744 in24.563s. All have zero skips and exit0. Existing integration,
filtering, nested budgets, typed dispatch, cancellation and async guards are retained.
The reviewer reads the same frozen inputs without executing any commands or editing.

Executed corrected-source artifacts:

- `/private/tmp/vsb-iteration119-operation-packet-green/operation-packet-green.ctrf.json`
  — SHA-256 `28b6f1c30a9ee96382cca61fe60956cb661dcd2bfada940a808ff95fa0e4cbd1`.
- `/private/tmp/vsb-iteration119-operation-packet-existing-green/existing-green.ctrf.json`
  — SHA-256 `c6511d39ce838e41feb525878c863143ed727b3f509930255711e431227f3cae`.
- `/private/tmp/vsb-iteration119-operation-packet-core/operation-packet-core.ctrf.json`
  — SHA-256 `8ebcd0b48f1c8fecafb182448328f84190bc5afe95044cf4ea1cc83f57ee39eb`.

Both actual Product and Unit format verifications exit0 without source changes.
The existing workspace-load warning is not a formatting finding. Git whitespace
verification passes; protected trees are untouched. Source/test/format freezes
do not overlap any executable-input edits. The repeated internal review fully
reads41 methods/111 cases and confirms the inspected local correction boundaries;
it establishes the four new cancellation/admission/ambient-parent findings as open.

The initial format command incorrectly names a nonexistent Product.slnx. It is an
author invocation error, not source formatting evidence; a read-only rg inventory
resolves the actual product solution ViciOne.ServiceBus.slnx and verification is
repeated using it. No relaxed rule, source generator or sandbox workaround is used.

## NN-05 — confirmed P2: concurrent operations share lifecycle identity

The author adds a deterministic entered/release barrier between an outer retry
and an inner creation-observer failure. While that exact failure remains owned by
the held first operation, another independently initiated operation uses the same
context and same exception object as an ordinary first-attempt business failure.
The old per-context failure set suppresses the second operation's legitimate retry.
The causal test fails instead of completing two attempts and one effect. Finally
releases and drains the held operation, preserving its exact original failure.

This is a documented adjacent scope extension under the PO's explicit permission
to correct related defects. It introduces no external coordination or provider.

## Coherent manually authored correction

RetryOperationState replaces RetryLifecycleFaults. Each context carries a locked
dictionary keyed by the current asynchronous retry-operation identity; each entry
tracks exact infrastructure failures, exact terminal business ownership and active
leases. Entries are removed when their last lease ends. Nested context projections
retain the same logical operation identity, whereas independently initiated async
operations on a reused context acquire distinct identities. Failure ownership is
still context-specific, not a global exception registry or blanket retry veto.

The operation key uses AsyncLocal's documented asynchronous control-flow semantics.
The implementation choice is an inference from the [official .NET10 contract](https://learn.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1?view=net-10.0)
and [runtime async-method explanation](https://devblogs.microsoft.com/dotnet/how-async-await-really-works/);
the new concurrent causal test, existing typed/replacement projections and reused
exception tests provide local executable verification. ConfigureAwait(false) does
not disable this flow, as described in the [official ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/).

Retained public RetryContext diagnostics are separate from active business ownership.
Only a previously published owner-held diagnostic may be replaced; caller-owned
payloads are retained. Propagation requires the current active operation and exact
terminal business failure, so old diagnostic payloads cannot suppress future retry.

RetryPolicyExecution centralizes factory/initial-context admission, both decision
boundaries and synchronous classification. Only escaped infrastructure failures
are marked. Real business classification outcomes and exception-filter budgets are
preserved. The acquired policy context is disposed exactly once. A sole cleanup
failure is rethrown exactly; simultaneous primary/cleanup failures produce an
AggregateException preserving ordered primary and cleanup identities. The combined
failure is infrastructure-owned so an outer filter cannot replay committed work.

RedeliveryRetryExecution shares the ordinary/activity policy decision algorithm,
including prior-delivery counting, terminal callbacks and successful scheduling.
Both filter families use the same owned policy lifetime. Scheduling failure retains
the original transport exception wrapping with both failure identities. Acknowledgment
occurs after successful scheduling and is no longer incorrectly described as a
scheduling failure; lifecycle ownership prevents a retry from scheduling again.
This sequencing clarification must receive direct execution evidence before final
acceptance, not merely infer correctness from zero-scheduling negative cases.

## Remaining acceptance

Scheduling/acknowledgment success,
pending and failure cases, cleanup/infrastructure variants across all filter families,
resource lifetime, source-format verification, separate controlled mutations,
read-only internal counterreview, fresh explicit-profile coverage/CRAP and complete
repository/API gates remain open. No public feature or package is removed. Source,
tests, comments and new requirement tuples are authored manually without generators.

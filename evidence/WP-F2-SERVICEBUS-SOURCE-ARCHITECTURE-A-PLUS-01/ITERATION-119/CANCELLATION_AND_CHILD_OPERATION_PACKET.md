# Cancellation and child-operation evidence packet

Status: intermediate correction, not completed Iteration119, whole-product A+ or
independent external acceptance. The original active goal and frozen source-architecture
slice remain unchanged. Checkpoint `4e81719a17cf32847c4d69add362f5ac5404b078`
and its annotated tag are verified on origin before this work. No force push is used.

## Executed red-to-green evidence

After correcting two author-only test mistakes, the unchanged checkpoint product
causally fails all25 new negative cases: eight scheduling/acknowledgment cancellations,
ten direct fault callback cancellations, four terminal getter failures, one actual
context admission failure and two independent children under a live parent. Earlier
35 cases and the valid consume-fixture metadata contract pass:61 total,36 pass,25
fail,zero skip,expected exit2. The first run is explicitly not accepted as the full
causal baseline because it included a disposed-policy fixture misuse.

Corrected source passes61/61 and the unchanged76/76 cases. A further manually written
24-case proof covers actual ordinary/activity scheduling success, genuine scheduling
and acknowledgment failures, null acknowledgment, foreign cancellation, cancellation
after scheduling acceptance and ownership-entry release across all processing/cleanup
boundaries. The expanded suite passes85/85,zero failures/skips,exit0. Focused strict
builds complete with zero warnings/errors. None of these runs claims real cloud or
RabbitMQ broker execution.

| Artifact | SHA-256 |
|---|---|
| Corrected red61 CTRF | `55cf05fee2a035568d8f84d884ef4aeac52069d94a7118fbe7ffe1a9b2c8d216` |
| Expanded green85 CTRF | `6cb362a1ee5e1b1e60d34cab5629aa28cef8283e22d0706d1ea22ad80a2a5372` |
| Accepted RetryOperationState.cs | `05d9711166df15f67ce743ab6c9631c21044a96a474880a9d9dc1a26782a3fa2` |
| Accepted RetryPolicyExecution.cs | `0ac942e51e9d633325e20d2a78aac8d7154d404260870a307afe46e6bda67502` |
| Expanded RetryOperationOwnershipTests.cs | `e65369bf3ee3793764f7007daf92113a8f6fbabc6797b846bf627ca70a92c559` |

CTRF artifacts use separately owned directories below `/private/tmp/vsb-iteration119-`:
`cancellation-frame-causal-red-corrected`, `cancellation-frame-green`,
`cancellation-frame-existing-green`, `cancellation-frame-expanded-green`. Protected
repository review/results directories are not read, modified or staged.

## Architecture and manual source reading

Each policy invocation owns a fresh asynchronous frame; root/current/replacement
context leases inside that frame share its state. Only an actually escaping exact
owned failure or terminal decision transfers to an already active parent association,
after primary/cleanup aggregation. New siblings cannot inherit old parent failure sets.
Completed invocation entries are removed; the retained public diagnostic remains
caller-safe and is never used to govern a later operation.

The shared lifecycle helper links source and selected-decision cancellation through
the framework primitive, preserves either original token in requested cancellation,
and keeps escaped lifecycle failures outside business retry. Scheduling and both
acknowledgments receive that actual effective token. A fresh cancellation check
separates scheduling acceptance from acknowledgment. Genuine scheduling failure
still retains its TransportException and ordered scheduling/business identities;
foreign-token cancellation is not mistaken for requested operation cancellation.

Framework reference:
[CancellationTokenSource.CreateLinkedTokenSource (.NET10)](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.createlinkedtokensource?view=net-10.0).
Invocation-frame semantics are this implementation's context-bound policy decision,
proven by its executed nested and sibling regressions, not a guarantee inferred solely
from framework documentation. No additional runtime dependency, scheduler, store,
communication path or publicly exported implementation type is introduced.

The author personally reads and understands the complete changed implementations,
their contracts, fixtures and manually written tests. Comments describe current code
and functionality, not construction history; no source/test/comment generator is used.
The complete existing test-project/build closure is reused from its unchanged bound
proof, with the exact eight newly changed requirement projections reviewed by hand.
The internal-access observation of private retained entries is test-only and locks
the actual storage; it adds no product telemetry solely for testing.

## Open related source boundaries and remaining gates

NN-06 is corrected at the common retry/redelivery invocation boundary, but personal
reading finds two actual provider continuations still ignoring their supplied policy
token for publish (DelayedMessageRedeliveryContext and RabbitMqQueueRedeliveryContext;
RabbitMQ also uses only the consume token for topology). Their cancellation comments
must be corrected with their real code and causal tests, not documentation alone.
These source-established paths are not yet executed in this packet and remain open
within the same source-owner work. Scheduler-backed redelivery already forwards the
supplied scheduling token.

The consume-policy adapter factory also releases an acquired context from a bare
catch; if representation/projection and that release both fail, cleanup can replace
the primary error before the shared execution shell acquires the adapter. This
source-established adjacent boundary needs its own causal test before correction.
Neither source observation is falsely reported as an executed regression or completed
provider acceptance.

Separate single-cause counterchanges, restored-source build/full Core, explicit-profile
coverage/CRAP, frozen internal read-only counterreview and proportional broader gates
remain pending. This packet is secured only as coherent intermediate progress; the
original goal stays active and the provider/factory findings cannot be hidden by a
green local subset or aggregate coverage percentage.

## Counterchanges in progress

The first separate compilable counterchange reuses the active ambient caller frame
instead of creating independent child ownership. It is killed by exactly5/85 cases:
both sequential/parallel child isolation cases and three nested active-entry-count
resource cases. The other80 pass,zero skips,expected exit2. The accepted state source
is restored to SHA-256 `05d9711166df15f67ce743ab6c9631c21044a96a474880a9d9dc1a26782a3fa2`
before the next independent counterchange. This is executable causal evidence, not
a reviewer inference or a failed compiler used as a mutation result.

The second independent compilable counterchange forwards only the original source
token rather than the linked source/selected-decision token. Exactly9/85 cases kill
it: all five direct-fault policy-token cases and all four scheduling/acknowledgment
policy-token cases. The other76 pass,zero skips,expected exit2. The policy execution
source is restored to SHA-256 `0ac942e51e9d633325e20d2a78aac8d7154d404260870a307afe46e6bda67502`
before changing the separate ownership-entry cleanup boundary.

The third independent compilable counterchange omits removal of completed invocation
entries. Exactly24/85 cases kill it: all twelve lifetime boundary cases and all twelve
ordinary/activity stage cases that also assert released storage. The other61 pass,
zero skips,expected exit2. State source is restored to its accepted SHA-256 before
the separate terminal-getter ownership counterchange.

The fourth independent compilable counterchange removes terminal-publication failure
marking. Exactly the four terminal getter cases fail (81 other cases pass,zero skips,
expected exit2). Both changed implementation files are restored to their accepted
SHA-256 values independently; the restored-source strict focused build then passes
with zero warnings/errors. Adjacent cancellation utility comments are personally
rewritten to describe successful completion signals, forwarding and registration/source
lifetimes accurately; their behavior is unchanged.

Diagnostic invocation lesson: dotnet format does not accept build's --configuration
argument. Those two immediately rejected commands change no source and are not format
results. Re-run only the supported `dotnet format <actual.slnx> --no-restore
--verify-no-changes --verbosity minimal` commands; retain their final exit codes.

Restored source completes the explicit-profile full Core host3,794/3,794,zero
failures/skips,21.928seconds,exit0. Product and Unit format verifications finish
exit0 without edits,with only the known workspace-load warning. Full Core CTRF
SHA-256 `a88a0dc74108385f71b131b058a4c0b87e0681c91e83927f699a5bba55c06da3`;
raw coverage SHA-256 `e9a2b1f4801bb13f848656f19b6ff4eca88b0b27cffc49b8874f6eecf7259b82`.
The owned detailed coverage report lists all14 below-threshold instrumented members:
graph80.9551%line/73.0106%branch; six-file kernel93.3162%line/81.1224%branch,
82 methods,one CRAP>30 (Attempt32.2018). This is not whole-provider/product coverage.

The complete separate internal counterreview reads49 methods/161 cases, accepts
bounded corrected axes and releases unchanged12-input freeze, but establishes a
new High token-getter exception-filter hole. See INTERNAL_CANCELLATION_CHILD_REVIEW.md.
It remains accepted open for immediate red-first regression/correction in the same
original goal, not suppressed by green tests. This packet is now a coherent verified
intermediate backup candidate, not completed119 or A+ acceptance.

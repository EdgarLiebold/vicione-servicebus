# Internal cancellation and child-operation counterreview

Status: bounded internal read-only lead review complete; one accepted High finding
remains open. This is not an external product role, independent external acceptance,
completed Iteration119 or full-product A+ claim. The reviewer edits no files, starts
no build/test/mutation and spawns no agent. All twelve frozen input hashes plus the
immutable slice hash are reverified unchanged before explicit source-freeze release.

## Accepted open High finding — token getter inside an exception filter

`src/ViciOne.ServiceBus/Middleware/RetryFilter.cs:143` evaluates the selected decision's
CancellationToken getter inside a CLR exception filter. If that getter throws, the
CLR treats the filter as false and discards its failure. The following business
catch can classify the original OperationCanceledException and run more business work.

Concrete source-established counterexample, not yet an author-executed regression:
input token None; initial business attempt throws transient; selected decision token
getter returns None on read1 during successful preparation, throws exact infrastructure
failure E on read2, and returns None thereafter. Delay is null and PreRetry completes.
Attempt2 throws an OCE with an already requested foreign token. The filter reaches
read2 and discards E; subsequent CanRetry can return a fresh safe permitted decision,
so attempt3 succeeds. No token mutation or requested source/policy cancellation is
needed. Both exact infrastructure identity and the no-business-replay boundary fail.

Required red-first test: the stateful getter and requested foreign OCE together;
exact E, exactly2 business attempts, zero post-OCE CanRetry/completion, exactly1
policy cleanup and zero retained ownership entries. A nested variant must preserve
the outer observer sequence [create] and forbid outer budget consumption. Evaluate
policy getters outside exception filters through an ownership-protected synchronous
boundary. The current fake faults Exception, not CancellationToken, so the161 read
cases do not distinguish this hole. Root accepts the finding for immediate work in
the same original source-owner goal; no test is weakened to match current behavior.

## Accepted bounded axes

Linked source/decision tokens normalize requested cancellation to its original token,
source first if both cancel. Local scheduling/acknowledgment happens once and in order;
genuine scheduling errors preserve transport wrapper and exact ordered stage/business
failures, while acknowledgment failure does not schedule again. Factory, admission,
actual context/decision validation, lifecycle/delay/terminal Exception getters and
publication have protected paths. Acquired policy cleanup occurs once; compound
failure preserves ordered identities.

Active parent/child, independent siblings, parallel and projection ownership is
accepted in the reviewed scope. Retained diagnostics do not govern later operations.
Actual nested/typed budgets have explicit attempt oracles. Awaitable methods carry
Async, synchronous factory/decision/lease methods do not. Six kernel types are
coherently located under RetryPolicies/Middleware; current comments describe functional
ownership and error behavior rather than construction history.

All49 methods/161 cases are read and assertion-audited:16 ownership methods/85 cases
plus33 old retry methods/76 cases. No assertion-free test, unawaited asynchronous
assertion or merely tautological oracle is found. Pending cases use entered/release
barriers and drained teardown. The reviewer applies test-anti-patterns with its .NET
reference. Its review does not replace the author's required personal source reading.

Root executions are root evidence only: expanded85/85,old76/76 and complete
Core3,794/3,794,zero skips. The reviewer executes none. Actual provider token
continuations and adapter factory compound-failure paths remain open/unexecuted
as documented in the main packet; this review does not close them.

## Complete read scope and freeze

All six frozen kernel files; Util/CancellationTokenExtensions; both complete test
classes; RetryFilterTestFactory; the complete consume fixture; exactly the latest
eight requirement tuples. Additionally complete contracts/projections:
RetryContext,RetryPolicyContext,IRetryPolicy,MessageRedeliveryContext,BasePipeContext,
ConsumeContextProxy including typed projection,BaseRetryContext,BaseRetryPolicyContext,
ConsumeContextRetryPolicyContext and ConsumeContextRetryContext including generic variants.
The immutable REQ-SAR/AC-SAR closure is reused unchanged; no new task/scope/role is
self-issued. Protected review/results and historical/legacy input are untouched.

| Frozen input | SHA-256 |
|---|---|
| RetryOperationState.cs | `05d9711166df15f67ce743ab6c9631c21044a96a474880a9d9dc1a26782a3fa2` |
| RetryPolicyExecution.cs | `0ac942e51e9d633325e20d2a78aac8d7154d404260870a307afe46e6bda67502` |
| RedeliveryRetryExecution.cs | `91fdd358916b9ab3126ffbf8b6accc600cf861f6e19d8d14ad0a38f31fa56b32` |
| RetryFilter.cs | `22524976fbef8e96d6fb41ac1f77c18f22997e625819627d097dc788c6cf1829` |
| RedeliveryRetryFilter.cs | `cfc3d6909165a8eefa89b2bbd502f298a732c5ab33701fca5cc2b9db6b7e92a8` |
| ActivityRedeliveryRetryFilter.cs | `13a9e4b8474c1b3954a616d075bc2725b37d5c4400054bb0c51db9a9504000fe` |
| CancellationTokenExtensions.cs | `e302fb4c0ffc579660e1b9120db4094e6945ed6b3b04439aaa66a262c397b243` |
| RetryOperationOwnershipTests.cs | `e65369bf3ee3793764f7007daf92113a8f6fbabc6797b846bf627ca70a92c559` |
| RetryFilterTests.cs | `2ca4fa3ad01bf7a2dbe7f6e5955af665ff5e0dc9afaf578f0afb3016c42560d6` |
| RetryFilterTestFactory.cs | `f24095e677397d12ed83b56b1906d5bbbefada0b183d1af2ce39ba82066a0ed1` |
| InMemoryOutboxTestContextFactory.cs | `b5668d870038169eb1513b9159d1edf9ce3c2487e9376765b19d07b8e466eb29` |
| CoreRequirements.json | `12941eb5a8ef87ecf048c726dc07fc55cca70942397a29d46161b2744a80b421` |

RELEASE source freeze: the reviewer is complete and all root test/build/format
executions on this snapshot are terminal before any further executable-input edit.

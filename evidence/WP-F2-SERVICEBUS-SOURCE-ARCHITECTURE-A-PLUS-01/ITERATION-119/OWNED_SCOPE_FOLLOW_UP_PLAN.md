# Acquired ownership-scope follow-up

Status: the historical worklist below is implemented in ACQUIRED_OWNERSHIP_PACKET.md
with expanded106/76/23, full Core3,820, Abstractions696, frozen internal counterreview,
explicit-profile risk analysis and four killed compilable counterchanges. Exact source
bytes are restored; final post-mutation build/Core3,820 rerun passes. This is bounded
checkpoint closure, not final119 or overall A+; the original goal remains active.

The immutable de97 internal diagnostic establishes exact callback failures in
projection/child admission, Mark and post-cleanup Propagate. The tested getter/payload/
factory packet remains a useful checkpoint, but guarding Mark by calling Execute
again would recurse into Mark and does not close the underlying architecture defect.

## Proposed bounded direction

An active invocation should retain directly acquired context/state associations and
the active caller relationship. Mark and failure transfer should use those references
without repeating user-defined payload callbacks. Ownership/terminal recognition
should also prefer acquired associations rather than introducing a new callback while
an already-owned primary failure is unwinding. Public retained diagnostic updates
remain actual payload operations with a safe callback-free failure-marking path.

Necessary new-context admission must complete before publishing its lease/current
frame; a failed admission must not replace the current active frame. Preserve exact
context association and independent-child isolation, not a process-wide exception
registry. Association counts, state entries and linked token registrations must be
released when the final lease ends. Never hold operation/state/parent locks in an
inconsistent order or across a custom callback. Existing projection, alias, nested,
parallel and independent-child oracles remain mandatory.

## Required human-written regressions

- Initial projection admission failure before any business invocation; retry projection
  admission after exactly one transient business failure; preserve exact sole/ordered
  compound failure and once-only cleanup without an outer replay.
- Child policy admission on an already-associated projected context; discriminate
  genuinely necessary admission from redundant callbacks that acquired references
  can safely avoid. A redundant failing callback must not be deliberately retained
  solely to satisfy an old callback-specific test expectation.
- PostCreate arms a failing additional GetOrAddPayload and throws an exact primary.
  Callback-free Mark must preserve primary and never invoke that armed callback.
- Successful/failed cleanup arms a failing additional TryGetPayload. Callback-free
  transfer and ownership recognition must preserve primary or its exact ordered
  aggregate without invoking the armed callback or replaying business work.
- Recheck public diagnostic replacement/preservation, independent typed projections,
  same-context aliases, concurrent siblings, all resource entries and cancellation.

When callback-free recognition supersedes the current injected terminal-lookup cases,
document that architectural change explicitly. Replace the oracle with stronger
no-redundant-callback plus exact legitimate business/terminal/cleanup/budget behavior;
do not simply delete or weaken the feature-preservation assertions. Retain the prior
executed red/green/mutation evidence as historical checkpoint evidence.

Personally read the complete affected source/comments before manual edits; no source,
test, comment, namespace or folder generator. Repeat independent compilable mutations,
expanded/full Core and proportional all-host/package/API/coverage gates. Submit the
new frozen packet to internal read-only counterreview, document every remaining
finding, and secure the coherent iteration/checkpoint without force-pushing.

## Next connected work

- Complete real null-fault-task contracts in initial, terminal retry and nested
  decision/policy callbacks, including ordered cleanup, exact generated diagnostics,
  no replay/effects and once-only resource release. Cover genuine initial/current
  projected cancellation-getter boundaries, not fabricated inactive internal state.
- Close ordinary/activity redelivery constructor/probe contracts and reassess all13
  emitted gaps against current source, preserving exact counters and behavior assertions.
- Foundation contract packet is now implemented in CONTEXT_CONTRACT_PACKET.md:
  Base cache/type/factory and Scope type/factory validations, expanded55cases,
  whole Abstractions749 and four killed byte-restored mutations. Remaining
  context/cache subclasses and their full paired tests still require personal
  reading and manual comment/type/file/function review; this is not project closure.
- Provider: fix actual delayed/RabbitMQ topology/publishing cancellation without
  replacing provider behavior with recording fixtures. Execute proportional real
  local-provider acceptance and document any required external/cloud authority.
- Advance complete source/type/file/folder inventory within independent SDK project
  boundaries; do not nest independent projects inside the Core compilation tree.
- Continue the connected saga repository/context capability review and RollingTimer
  public disposal/restart/trigger semantics after the17-file personal read packet.
  Saga milestone and actual activity idle/maximum/root-start defects are corrected
  and causally/mutation-tested there; their bounded acceptance is not full architecture
  or transactional/provider acceptance.
- Saga ownership/boundary contracts are implemented in SAGA_OWNERSHIP_PACKET.md:
  nine complete personal source reads/manual comment rewrites, 29 methods/82 cases,
  complete Core3,910 and Abstractions749, nine individually strict-built killed and
  byte-restored mutations, scoped Product/Unit whitespace checks, fresh graph-only
  coverage and internal frozen Sol review. Active-operation Dispose and exceptional
  test-drain findings are causally repaired, not waived. The next connected packet
  is query/index integrity; explicit saga-acquisition cancellation/token normalization,
  atomic factory unwind, generic query/Undo and cross-provider dispatch cleanup
  remain open. This is bounded checkpoint acceptance, not final119/overall A+.
- Query/index integrity is implemented in SAGA_INDEX_PACKET.md: nine complete
  personal source reads/manual comments,58methods/97cases and2,922catalogue tuples;
  12individually strict-built killed/byte-restored candidates, expanded200 and full
  Core4,007all passing, fresh explicit graph-only coverage and scoped Product/Unit
  whitespace. The empirical initial keyed-filter survivor is repaired by a direct
  positive/negative oracle, not concealed. Frozen internal Sol reviews have15matching
  bindings and final RELEASE. Fresh31package/18journey/3consumer workflow reaches
  30runtime API reflection but exits1 on committed119visibility baseline drift.
  The newly found generic-nested FormatType truncation needs coherent naming
  correction/direct oracles and reviewed contract reconciliation before final119
  acceptance; the original failed gate is retained, not relabeled successful.
  Current normal Git checkpoint is a source/test backup, not whole-gate closure.
  Next connected work is explicit saga-acquisition cancellation/token identity and
  factory unwind, preserving exact primary/ordered cleanup and once-only ownership.
  Genuine allocation/Apply/Rollback paths and generic query/Undo still require their
  own direct evidence; this checkpoint does not claim their closure or whole A+.
- Finish broader current all-host/package/API/journey/isolated-consumer and iteration
  gates after connected corrections. Commit/tag/push checkpoints normally; never
  declare whole-product A+,100% API correctness or complete reading from this packet.

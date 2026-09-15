# Acquired ownership-scope follow-up

Status: design/test worklist only. The original goal remains active. No implementation
or acceptance for these additional paths is claimed by this plan.

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

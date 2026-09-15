# Iteration 119 acquired ownership references

Status: acquired-reference correction and frozen local review are complete; four
independent compilable counterchanges are killed and byte-restored. Full Core3,820
and Abstractions696 pass, zero skip; final restored build/Core3,820 rerun also passes.
This is bounded checkpoint evidence, not final119 or overall A+ goal closure.

The original source-architecture goal stays active. Checkpoint 0894a7f3 is committed,
tagged and verified on origin. No proof process or live-source reviewer freeze is
active at the start of this packet. Protected review and results are untouched.

## Research

The author personally reads the complete RetryOperationState, RetryPolicyExecution,
RetryFilter and RedeliveryRetryExecution and all affected fixture/helper methods.
The testing entry point is applied as human Research → Plan → Implement; the user's
explicit prohibition of source/test/comment generators takes precedence. Existing
source/test pairings and requirement projections are retained.

Three callback failure classes remain: necessary projection admission can escape
outside infrastructure protection, Mark can replace a primary during repeated
GetOrAddPayload, and post-cleanup transfer can replace a primary during repeated
TryGetPayload. Ownership and terminal recognition need acquired references too.

## Acceptance map

| Behavior | Human-written regression and oracle |
| --- | --- |
| Necessary initial/retry projection admission | Four sole/compound cases; exact admission then cleanup, zero/one business attempts, zero effects, one factory/disposal per policy, no outer replay, released storage |
| Callback-free failure marking | Four alias/projection and sole/compound cases; exact observer primary, zero work, armed extra GetOrAdd remains unused, exact ordered cleanup and released storage |
| Callback-free transfer/recognition | Two post-cleanup sole/compound cases; original primary or ordered aggregate, no extra TryGetPayload, one factory/disposal and zero effects |
| Already-associated child admission | A callback armed after parent admission remains unused through child admission and successful business work; one real effect, no retry events |
| Released alias with live shared marker | Two unarmed/armed cases; parent lifecycle/terminal ownership and re-entry remain intact, child state stays independent, final storage is released |
| Real public diagnostic update failure | Two sole/compound cases; the necessary AddOrUpdatePayload failure is preserved, its armed extra GetOrAdd remains unused, no outer replay |
| Terminal business ownership | Replace the previous four injected lookup-failure cases with stronger zero-lookup, exact legitimate business-primary/cleanup, terminal decision/event/budget and storage assertions |
| Existing feature preservation | All prior alias/projection, child isolation, parallel, cancellation, lifecycle, redelivery and acquisition assertions remain mandatory |

The four historical terminal tests deliberately exercised redundant lookups. Their
executed checkpoint red/green/mutation evidence remains historical. Eliminating the
lookup changes the legitimate failure to the business failure; keeping a redundant
callback merely to satisfy the old injected-failure oracle would retain the defect.

## Plan

Write and execute these tests before product edits; separate actual causal failures
from positive guards and fixture/oracle errors. Retain invocation-local context/state
associations with counted leases; do not introduce a global failure registry. Acquire
necessary payload state before publishing the active frame. Retain acquired alias
identities until the invocation ends: an alias's own lease ending must not erase
ownership kept live by another alias on the same marker. Serialize association
retention/final clearing against admission; use the consistent operation → marker
lock order for bookkeeping, never the reverse and never a callback under either lock.
Protect necessary initial
and retry admission on the already-acquired root. Mark, lookup and transfer use direct
references; no callback while unwinding or holding ownership locks. Preserve marker
alias semantics, active-parent association, independent siblings and once-only release.

After green, simplify duplicate terminal notification responsibilities in RetryFilter
without changing order, null-task diagnostics, cancellation or budget behavior. Run
expanded and full Core, independent compilable mutations, current explicit-profile
coverage/CRAP, strict build/format and frozen internal counterreview. Commit/tag/push a
coherent checkpoint; provider/foundation/all-source and final iteration gates stay open.

The first executed unchanged-product run has 104 cases: 89 pass, exactly 15 new or
strengthened cases fail causally, zero skip. The two compound admission cases are
positive guards. Raw `/private/tmp/vsb-iteration119-acquired-scope-red/acquired-scope-red.ctrf.json`,
SHA-256 `f25720572ebd9f121cb590f509c0b54e574af38a936ad88a2ff0884b29d83a2a`.
Source hashes match 0894a7f3. Pre-execution fixture review corrects the diagnostic
failure injection to the actual input context where PublishTerminal updates payloads,
and arms Mark on that input, retaining an independent projection as an alias guard.
These fixture preparations are not counted as failed causal executions. The internal
immutable design counterreview establishes the released-alias lifetime invariant;
the additional two cases are written before any product correction.
Their first compile reports CS0246 for the test-only IPayloadCache namespace; add the
actual ViciOne.ServiceBus.Payloads using and rebuild. This setup/compiler error is
not an executed mutation or a causal product failure.

The unchanged-product alias run compiles cleanly and executes 106 cases: 90 pass,
exactly the previous 15 plus the armed released-alias case fail, zero skip. The
unarmed released-alias case demonstrates the legitimate old marker semantics.
Raw `/private/tmp/vsb-iteration119-acquired-scope-alias-red/acquired-scope-alias-red.ctrf.json`,
SHA-256 `360876053ee567466d579dcee6f5496ae3fba426833540ed5ee7960977fbea8a`.
Both executions precede product correction. The author also personally reads the
complete ScopePipeContext, IPayloadCache and ListPayloadCache when examining alias
semantics; inaccurate Scope documentation and the empty-cache constructor summary
are rewritten manually at this read point, with no behavior change.
The first corrected build rejects two nullable dictionary keys with CS8604. Make
the actual operation-null guards explicit; do not suppress nullable analysis or
execute stale binaries. This compiler feedback is not a mutation proof.
The corrected strict build passes with zero warnings/errors. Ownership106, existing
RetryFilter76 and consume acquisition23 all pass with zero skip against the same
compiled source. The released-alias helper is then strengthened to observe actual
association storage after the final invocation lease ends, not just marker entries;
an independent compilable omitted-clear counterchange must kill that new oracle.

All four independent counterchanges compile and are killed by exactly2/7/2/2 cases,
then restore the exact frozen input bytes. Full restored Core3,820/3,820 passes again,
zero skip. Final declared-method audit counts24 ownership +33 existing +11 consume
=68 methods/205 cases; the early25/69 accounting estimate was one too high, not a
lost or skipped test. All recorded raw artifact hashes are independently byte-checked.
See ACQUIRED_OWNERSHIP_PACKET.md, INTERNAL_ACQUIRED_SCOPE_REVIEW.md and
ACQUIRED_SCOPE_COVERAGE/coverage-analysis.md for exact proofs and remaining gaps.

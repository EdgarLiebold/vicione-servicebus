# T95 — saga request lifecycle publication

Exact product and test commit: `775efa3e2`.

## Product finding and fix

`RequestStartedActivity` and `RequestFaultedActivity` published a lifecycle
event before dereferencing a missing `next` pipeline stage. A completed
publication was therefore externally visible before the activity failed with
`NullReferenceException`. Both activities now validate `context` and `next`
before any context access, fault cast, or publication, matching the adjacent
request completion activities.

## Product contracts checked

`Started_PublishesOwnerRouteAndOriginalPayloadBeforeContinuingAsync` checks
the saga owner, request ID, response and fault routes, expiry, payload type,
exact payload identity, cancellation token, and publication before continuation.
`Faulted_PublishesStructuredCauseAndOriginalOwnerBeforeContinuingAsync`
checks the owner, typed fault payload, fault and failed-message IDs, timestamp,
host, exception identities, cancellation token, and the same ordering.

`StartedAndFaulted_RejectInvalidPipelineBeforePublishingAsync` exercises both
null inputs for both activities and asserts exact argument names with no
publication or continuation. The two publication-failure tests preserve the
exact cause and skip continuation. A non-fault message is rejected before
publication or continuation. Existing T76 tests continue to cover the
completion forms.

## Verification

- On the old product implementation, the new invalid-pipeline test failed
  with `NullReferenceException` instead of the required argument exception.
- Focused class after the fix: 6/6 passed, no failures or skips.
- Complete Core project on exact commit `775efa3e2`: 6,971/6,971 passed,
  no failures or skips.
- A controlled counterprobe changed the published fault payload type from
  `Fault<TRequest>` to `TRequest`. The structured-fault test failed on the
  exact type-name array; source was restored before the complete Core run.
- Read-only Red Team found the partial-publication P2. After the fix and
  negative oracle, independent re-review: PASS with no remaining P1/P2.
- Six requirement variants are projected in `CoreRequirements.json`.

## Measurement boundary

T85 remains the latest complete 33-profile Line/Branch/CRAP checkpoint.
Global A+ remains open. T95 is the tenth focused packet since that checkpoint;
the next complete measurement follows the agreed 12–20 packet cadence.

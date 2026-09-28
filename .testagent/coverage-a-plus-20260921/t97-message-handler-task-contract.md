# T97 — message handler task contract

Exact test commit: `190a35eb4`.

## Product behavior

The eight message-handler adapter shapes (context or message delegate, with
zero through three injected dependencies) now prove exact task identity while
the handler is pending, ordered dependency and input identity, one invocation,
exact asynchronous fault propagation, and successful completion. A second
matrix proves that each shape propagates the exact synchronous exception once.
The tests use a controlled pending task and explicit identity assertions, so
they detect early completion or a substituted task rather than merely raising
execution coverage.

## Verification

- Focused class: 24/24 passed on restored product source.
- Complete Core project on exact test commit `190a35eb4`: 7,015/7,015
  passed, zero failures and skips.
- A two-dependency adapter counterprobe discarded the handler task and
  returned `Task.CompletedTask`; four focused cases failed. A three-dependency
  message adapter counterprobe substituted a null message; three focused
  cases failed. Both product edits were restored and product Git diff was
  empty before the complete Core run.
- Read-only Red Team: PASS, no concrete P1/P2 finding. It checked that the
  tests detect task replacement, early completion, retry, argument swaps, and
  fault swallowing.
- Two requirement variants are recorded in `CoreRequirements.json`.

## Measurement boundary

No product source changed. This is the twelfth focused packet since the T85
33-profile checkpoint. A new full product-wide measurement is due on the
frozen post-packet tree. T85 remains the latest complete Line/Branch/CRAP
result until that aggregate finishes; global A+ remains open.

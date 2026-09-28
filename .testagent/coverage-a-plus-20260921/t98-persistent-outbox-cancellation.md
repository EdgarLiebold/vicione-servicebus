# T98 — persistent outbox cancellation handoff

Exact product and test commit: `f221b6af1`.

## Product behavior

All six persistent send capture paths now pass the caller's cancellation token
through the shared helper to `OutboxSendContext.AddSendAsync`. Previously they
passed it only to transport context creation. A cancellation that arrived
after context creation could still admit a durable send. The change preserves
the existing send context, message and user pipe.

## Verification

- Red first: six focused cases failed against the previous product code because
  persistent admission received no caller token and accepted canceled sends.
- Restored implementation: 21/21 focused cases passed across six capture
  callsites and explicit runtime-type dispatch. They assert token and context
  identity, pipe configuration, no admission after cancellation, and that a
  pending admission holds the outer send open and propagates the exact delayed
  storage exception.
- Controlled counterprobe: removing the caller token only from the untyped
  pipe callsite failed its two corresponding tests; the mutation was restored.
- Complete Core project on exact commit `f221b6af1`: 7,036/7,036 passed,
  zero failures and skips.
- Independent read-only Red Team: PASS, no remaining concrete P1/P2. Its
  findings about requirement keys, the untyped pipe path and delayed admission
  were corrected before this result.
- Three requirement variants are recorded in `CoreRequirements.json`.

## Measurement boundary

T97 on `3cb94a285` remains the latest complete 33-profile product-wide
Line/Branch/CRAP measurement: 92.20544% lines, 84.79432% conservative
branches, zero methods with CRAP above 30. T98 is the first coherent packet
since that checkpoint. The next full aggregate follows the agreed larger
20–30 packet interval unless a shared runtime or measurement change warrants
an earlier run. Global Line and Branch A+ remain open.

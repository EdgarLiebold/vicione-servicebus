# T102 — EF outbox cursor and cleanup integrity

Implementation commit: `bb89661c2`. The latest global Line/Branch/CRAP
checkpoint remains T97 `3cb94a285`; no new global grade is claimed here.

## Product failures and fix

- A Pending outbox with `LastSequenceNumber` past an unsent message produced an
  empty page, was marked Delivered, and then removed with its message. A
  Delivered outbox with a remaining message was directly removed. Both real
  SQLite cases failed red-first: the worker reported progress and lost the
  message. The source now refuses completion or cleanup while those messages
  remain.
- The first guard was too late when persisted messages straddled the cursor:
  a later message was sent externally before the older one triggered the
  invariant exception. A bus `ISendObserver` measured two send attempts in the
  red run. The cursor check now happens inside the claim transaction before
  loading the first page or sending anything. The completion check remains as
  a race guard.
- Permanent invariant exceptions are excluded from operational retry. The
  transaction rolls back the claim and keeps State and messages for repair.

Two new requirement variants cover the empty/Delivered contradictions and a
mixed cursor window. The tests assert two attempts with no reported progress,
zero pre/post transport sends, unchanged lock, status and cursor, and exact
retention of message sequence numbers and IDs. A controlled counterprobe
disabling the late completion guard failed the regression and was reverted. Existing
healthy paged delivery and final cleanup still pass.

The focused source class passed 11/11. The complete EF unit project passed
371/371 on exact commit `bb89661c2`, no failures or skips. The targeted real
PostgreSQL local integration class passed 8/8 under canonical fixture run
`vicione-1512d32c5bfd`; fixture findings were empty and the broker log hash
was `29f750c7194204fa4631e586967fddb82b14c1ae7e1db31d8555656fc611e0`.
A direct invocation without the required LocalIntegration profile failed
configuration before provider behavior ran; it is excluded from pass evidence.
Independent read-only adversarial re-review: **PASS**, no concrete P1/P2.

This is one coherent remediation packet. The product-wide 33-profile
measurement remains due on the agreed larger-packet interval; global Line and
Branch A+ are still open.

# T161 durable message identity during serialization

## Defect and scope

Two persistence paths retained a mutable send context across serializer callbacks.
The EF transactional outbox captured `MessageId` for its record ID before
serialization, then read it again afterward for the persisted message ID. A
bounded serializer could therefore stage a record with inconsistent IDs or an
empty message ID. The typed durable sender likewise read `MessageId` after a
serializer callback, allowing a callback to replace the identity before durable
admission.

## Behavioral proof

- EF SQLite test: a bounded serializer changes `MessageId` to another GUID or
  `Guid.Empty`. Both variants failed before the fix. They now throw before any
  active session, tracked record or capacity state appears. A subsequent valid
  send commits with matching record and message IDs and correct capacity.
- Typed sender test: a public serializer changes `MessageId` to another GUID or
  `Guid.Empty`. Both variants failed before the fix by reaching the admission
  stub. They now reject the change before admission.
- An initial fix incorrectly equated the message ID with the idempotency key.
  The complete Core suite exposed the supported scheduler case with an explicit
  `ScheduleOptions.MessageId` different from the key. The final code snapshots
  the message ID after schedule options and before serialization. The existing
  scheduler persistence test passes with those distinct IDs.

## Verification

- EF full suite after its fix: 403/403.
- Core scheduler counterexample: 1/1 after the corrected snapshot.
- Core serializer regression variants: 2/2 after the corrected snapshot.
- Core full suite after both fixes: 7,387/7,387 (local .NET test named-pipe
  sandbox restriction required the authorized unrestricted run).
- Read-only adversarial review of the final diff: no concrete P1/P2 finding.
  Its first pass missed the scheduler semantics; a second pass reviewed the
  complete Core failure and corrected code.
- The preceding exact-commit 33-profile coverage and CRAP baseline is recorded
  in `product-wide-profile-096e8ecfa.md`. This iteration changes only two
  small guards and their behavioral tests; no new full profile was taken.

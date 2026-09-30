# T159 — EF transactional outbox rejects an empty message ID

`EntityFrameworkScopedBusContext.CreateStagedRecord` checked whether
`SendContext.MessageId` had a value but accepted `Guid.Empty`. It used that
empty value as both the durable record key and message ID. The older EF
`OutboxMessageFactory.Create` already rejects an empty ID. The transactional
path now rejects it before payload serialization, staging and capacity
reservation, and uses the validated local ID when constructing the record.

The new SQLite behavior test
`EmptyMessageId_RejectsBeforeStagingAndPreservesTheSessionAsync` first ran
against the original product source. It failed because no `MessageException`
was thrown; the invalid message was staged. After the product correction,
the focused test passed 1/1. The final test checks the rejection message,
inactive session and empty record/capacity trackers **before** cleanup. It
then sends a valid message through the same context, commits, and reads a
fresh DbContext to verify that only the valid ID was stored and exactly its
bytes were reserved. The requirement projection maps it to
`REQ-VSB-EF-TRANSACTIONAL-OUTBOX`.

The complete EF test project passed 401/401 after the final test edit, with
zero failures and skips. Independent read-only Red Team review confirmed the
product defect and found a P2 oracle flaw in the first test version: aborting
before checking tracker state could hide an invalid staged record. The
assertions now run before cleanup, and the final read-only re-review reports
no remaining concrete P1/P2 in this diff.

The subsequent [T160 product-wide checkpoint](product-wide-profile-096e8ecfa.md)
includes this fix and the new test in a complete exact-commit 33-profile
Coverage/CRAP aggregate.

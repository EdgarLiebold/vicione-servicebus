# T77 reliable scheduler timing and lease ownership

The frozen T74 aggregate was used only to locate a connected uncovered
area. The public `IMessageScheduler` and real in-memory `IOutboxStore` are
exercised together. The existing absolute-time and successful-cancel test
was kept; the new tests check previously unproved relative-time, rejection
and lease-ownership contracts. The T75 Roslyn source/test pairing artifact
was reused. No production implementation changed in this packet.

One test injects a fake clock, rejects negative and pre-canceled relative
send/publish calls without storing an intent, then successfully schedules a
send and publish at two different clock instants. It checks exact due times,
destinations, payloads, distinct token IDs, persisted body and one-tick
claim boundaries. A second test claims a due intent, rejects scheduler
cancellation of that intent with the exact identity in the error, then uses
the original claimed fencing lease to schedule a retry. That counterprobe
proves the rejected cancel retained lease ownership. An independent
neighbor can still be canceled; the retried intent reappears at its exact
due time with the updated attempt count and status.

The affected xUnit v3/MTP class passes 39/39. The first test draft exposed
an incorrect test lease-duration assumption, which was corrected before
the final targeted run. Independent read-only Red Team found the missing
original-lease capability check and requirement projections; both were
added. Final independent read-only Red Team re-review is PASS with no
remaining concrete P1/P2 finding. The affected class passes 39/39 on the
final source/test tree. The complete Core suite is grouped with the next
multi-packet checkpoint under the revised measurement cadence.
No numeric mutation score or current product-wide coverage value is
claimed. The T74 33-profile report remains the latest complete
Line/Branch/CRAP measurement; the next aggregate follows the agreed
multi-packet interval unless a broad contract change requires it.

# T84 typed receive dispatcher lifecycle

The frozen T74 profile left part of the public typed receive-dispatcher
surface without direct behavioral verification. Existing tests proved raw
JSON and empty-body delivery, while lower-level dispatcher tests covered
settlement and metrics separately.

Two new real-harness tests exercise the public typed dispatcher. Concurrent
serialized messages reach distinct consumers and remain pending until each
consumer is released. The tests assert both payloads, active and cumulative
counts, peak concurrency, cumulative delivery metrics, and the exact point at
which zero activity is signaled. Separate releases prove that completing the
first delivery does not report zero while the second remains active. A gated
zero-activity handler proves dispatch completion waits for the notification
task. A second
test cancels a pending consumer through the dispatch caller token, verifies
the active count clears, and delivers a successor through the same dispatcher.
Cleanup awaits every started dispatch with a bound.

The read-only Red Team first found that a shared consumer release hid early
zero signaling and that cancellation cleanup did not await its dispatch.
After both were corrected, a re-review found a fire-and-forget zero-handler
mutation; the gated async handler now detects it. Final re-review was PASS
with no surviving concrete P1/P2 gap.

The targeted dispatcher class passes 4/4 without failures or skips. The
affected Core xUnit v3/MTP project passes 6,944/6,944 with no failures or
skips on exact test commit `04bd0287c`. T74 remains the latest complete
Line/Branch/CRAP profile; the next
whole-product profile is due at this tenth-packet checkpoint.

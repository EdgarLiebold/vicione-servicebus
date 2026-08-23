# Minimal-envelope redelivery validation

The replacement uses the real System.Text.Json envelope reader, configured endpoint, delayed
redelivery middleware, send observer, in-memory queue, and in-memory delay provider. It contains no
sleep, polling loop, wall-clock assertion, inherited TestFramework message, or NUnit lifecycle.

## Product-defect reproduction

The first unfiltered 855-case run retained the inherited `Task.Run` boundary in
`MessageQueue.DeliverWithDelay`. It produced 854 passes and one timeout: virtual time advanced while
the delayed queue operation was not yet registered. Removing that unnecessary thread-pool boundary
made the same unchanged test pass under the complete profile. Five additional fresh-process focused
runs passed in 0.96–1.05 seconds each.

## One-cause mutations

Each mutation was applied independently, built in Release, run through the one-case native xUnit/MTP
executable, and reverted before the next mutation.

| Mutation | Observed causal result |
| --- | --- |
| Configure 30 minutes while the contract requires one hour | 1 total; the sole test failed immediately with expected `01:00:00`, actual `00:30:00` |
| Increment `MessageHeaders.RedeliveryCount` by two instead of one | 1 total; the sole test failed with expected second count `1`, actual `2` |

The final source restores the exact one-hour interval and one-step delivery count. No mutation is
retained.

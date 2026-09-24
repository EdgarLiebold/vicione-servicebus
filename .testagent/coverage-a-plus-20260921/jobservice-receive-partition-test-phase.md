# JobService receive partition regression test

The exact 36-report product profile at `4e7b54c22` ranked the job endpoint
configuration closure first (CRAP 44, 28/28 lines). A proposed extraction
reduced its focused CRAP to 8, with two fully covered helper methods at 18
each. The adversarial review found that existing tests did not establish the
receive partition selectors across all 18 message contracts. That source
extraction was reverted; this phase changes only tests and the requirement
projection. Product-wide coverage and CRAP remain at the `4e7b54c22` baseline
until a new complete profile is collected.

`JobEndpoint_SerializesTheSameJobAcrossMessageTypesWhileAnotherPartitionContinuesAsync`
is a regression on the real in-memory JobService receive endpoint. It asserts
the exact 18 partitioned message types, exactly one partition filter per type,
one shared coordinator identity, partition count 2, and endpoint concurrency
limit 2 from the bus probe. It then blocks `IJobSlotUnavailable` for one JobId,
observes `IJobSlotWaitElapsed` for another partition entering concurrently,
and observes `IJobSlotWaitElapsed` for the blocked JobId arriving but not
entering until the first operation is released. The test uses task gates,
bounded waits, and no sleeps.

The focused Release test passed 1/1 against the unchanged product code. The
final Release Unit/Architecture gate passed 10,262/10,262 tests with zero
failures and skips.
Adversarial review returned PASS for this test-only change and its
`REQ-VSB-JOB-ENDPOINT-CONFIGURATION` mapping. The test establishes the
registrations and cross-message behavior above. It does not establish the
JobId selector of every one of the 18 contracts. The attempted source
extraction remains deferred until those selectors, especially contracts with
both JobId and AttemptId and the nested Fault contracts, have hard behavioral
oracles.

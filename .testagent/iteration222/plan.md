# Iteration 222 plan

1. After iteration 221's technical gates, partition the ten files without overlap:
   A `RequestActivity`, `RequestActivityImpl`, `RequestStateMessagePipe` (request creation,
   endpoint token, timeout/metadata snapshot); B `RequestStartedActivity`,
   `RequestCompletedActivity`, `RequestFaultedActivity` (lifecycle emissions); C
   `RespondActivity`, `ScheduleActivity`, `SendActivity`, `UnscheduleActivity`
   (transport/scheduler cancellation and token ownership). Keep the §4.3 full-owning-test-
   project read/ordering finding explicit before any test-code acceptance.
2. Establish exact API-to-test/test-to-API, failure/cancellation/concurrency/ownership and
   asynchronous naming contracts, correcting only evidenced product defects.
3. Integrate source hashes, requirement projections, independent cross-audits, compiled
   single-cause mutations, coverage/CRAP and complete warning-clean regressions.
4. Commit and annotate the verified local admission, then continue the next connected packet.

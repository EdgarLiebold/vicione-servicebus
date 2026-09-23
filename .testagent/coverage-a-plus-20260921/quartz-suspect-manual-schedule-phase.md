# Quartz suspect-attempt manual status checks

The complete Unit/Architecture gate at product/test commit `7e184ded0`
failed once in
`SuspectRetry_IgnoresThePreviousAttemptCompletionAndCompletesTheCurrentAttemptAsync`.
The test waited 30 seconds for one of three observed status-check schedules
and timed out before reaching the retry and stale-completion path. The same
Quartz assembly passed in the prior instrumented Unit run, the named test
passed 1/1 in isolation, and a complete gate retry without simultaneous
ServiceBus brokers passed 10,226/10,226. This evidence did not establish a
product defect or the exact cause of the timeout.

The test manually fires every captured Quartz status-check job through
`TriggerJob`. Its configured natural `StatusCheckInterval` had been 30 seconds,
equal to the test's 30-second `OperationTimeout`. The single test-source
change moves natural firing to five minutes. It does not change the wait
budget, job timeout, number of expected schedules or status checks, or any
fault, retry, stale-completion, and terminal-state assertion. This removes a
plausible collision between Quartz's natural firing and the test's manual
control. The test suite still contains separate 30-second lifecycle coverage.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/coverage-a-plus-20260923-7e184ded0/unit-architecture-gate.log` | Original parallel gate: Quartz status-schedule wait timed out at 30 seconds; failed run retained | `96139c4fc6cee9bf958721f0c752a9135da20be4c32d6055f6bf1718abefd55c` |
| `artifacts/coverage-a-plus-20260923-7e184ded0/quartz-suspect-isolated.log` | Before change, named test 1/1 green without broker load | `b52b3e0a9bcb018f0e3807a5b4121a0a7f1429b09be4c581f0bd2d3f82290a87` |
| `artifacts/quartz-suspect-schedule-20260923-focused.log` | After change, both suspect-attempt integration tests 2/2 green | `03ee863e82a582668eb7b760cd4d7daaa7ded85c00d52b8fa95d070ff5b770e0` |
| `artifacts/quartz-suspect-schedule-20260923-project.log` | Complete Quartz project 267/267 green | `b041e8df937f13c3582916ae40ac7daf9a945def67fac469a36bdf21a4ae68c9` |
| `artifacts/quartz-suspect-schedule-20260923-parallel-gate.log` | Complete Unit/Architecture gate during broker startup: 10,226/10,226 green; Quartz finished before the brokers were ready | `e3519e9b72cdcf1ed5843b819c8ad48e2a102da61042989ee3192049e2880cb0` |
| `artifacts/quartz-suspect-schedule-20260923-release-build.log` | Complete Release build: zero warnings/errors | `6ffe4a997d87229e5617992ea6161d9c9cd7b41899da74c1551af84266cf1074` |
| `artifacts/quartz-suspect-schedule-20260923-broker-load.log` | Load fixture failed while starting RabbitMQ (`.erlang.cookie: eacces`); no RabbitMQ test command ran in this fixture | `05a008e97e6139127025d19166ff69f7b2ffafc438b255ff57c1ad75802ab4df` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load/unit-architecture-gate.log` | Six brokers ready; Quartz passed, S3 regionless test failed; overall 10,225/10,226 | `28559927fe9dfb239d67e51db220c89915935af903399f518f0b0c1b66089066` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load/postgresql-provider.log` | Concurrent PostgreSQL provider 79/79 green | `d19146d3eed7db3fd03e920c5f1cae58434c52bdb9db8d326f6870e1da75167d` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load-fixture.log` | Fixture identity `vicione-66c0dfce05e2`; six brokers ready, empty findings | `c09b8d3be3af480744fe55d50879748d3a791bf264128682aead2d02e496b2b3` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load-fixed/unit-architecture-gate.log` | Six brokers ready; S3 and Quartz passed, saga observation raced removal; overall 10,225/10,226 | `fa8b2136b53966258840d406c849202e37646b48f885bd81912448a6a53c1d01` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load-fixed/postgresql-provider.log` | Concurrent PostgreSQL provider 79/79 green | `ea7dc727153a332c9eb225f495db1070f6590a7166bf42263e99858cb89a4e33` |
| `artifacts/quartz-suspect-schedule-20260923-ready-load-fixed-fixture.log` | Fixture identity `vicione-6b01b2ce8302`; six brokers ready, empty findings | `4182934cf0f2a71b38be9eaff69eee615b9c3b20f16935b3d1f67fcf4417c949` |
| `artifacts/quartz-suspect-schedule-20260923-final-unit-gate.log` | Current test bytes: complete Unit/Architecture gate 10,227/10,227 green | `b13409f96df4a6cc12d62c3ff651b3e145628cf7e9b8d5aab6c0a8792e932eb7` |
| `artifacts/quartz-suspect-schedule-20260923-final-release-build.log` | Restricted Engineering build exited 1 after 10 minutes with no diagnostic and zero warnings/errors; excluded from passing evidence | `5c17dc595573b1378cd023949c8a9448ef333bd6b8d218e676516de3b58640f6` |
| `artifacts/quartz-suspect-schedule-20260923-final-release-build-escalated.log` | Complete Engineering Release build in approved execution context: exit 0, zero warnings/errors | `7dac754f88e16625b28d844dcdbc884191e56153dfa8a0163f5ca9ba6221f46e` |

In a separate six-broker-ready run, the Quartz assembly passed while the
brokers and PostgreSQL provider tests were active. The Unit/Architecture gate
failed one unrelated Amazon S3 test: its assumed regionless SDK config inherited
the fixture's `AWS_REGION`. This run is retained as a failed gate, not counted
as a clean gate. The test now supplies an explicit `IClientConfig` proxy with
both region properties absent, independent of process environment. A positive
case separately supplies only `RegionEndpoint` and checks successful bucket
creation with `UseClientRegion=true`.

The second six-broker-ready run used the corrected S3 test binary. S3 and
Quartz both passed, and PostgreSQL passed 79/79. A state-machine integration
test then exposed another asynchronous observation race: its consumed-message
signal preceded saga removal, leaving a briefly visible `Final` state. The
test now polls the repository until the saga is absent with the existing
bounded harness helper and still checks the repository directly. The second gate is also
retained as failed evidence. Both fixture findings files are empty.

The passing Quartz assemblies under ready broker load are counterexamples to
the earlier timeout, not proof that the test can never flake. A read-only
adversarial review confirmed that the behavior assertions remain unchanged,
the manually triggered path is still exercised, and the one-minute job
timeout is separate from the status-check interval. No product change was
made. The product-wide A+ goal remains open.

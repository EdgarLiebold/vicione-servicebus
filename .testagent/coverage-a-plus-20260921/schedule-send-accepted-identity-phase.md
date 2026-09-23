# Delayed-send accepted identity

The latest complete product-wide profile at `b6ffcfbdf` measured
`ScheduleSendPipe<TMessage>.ScheduledMessageId` at 0/5 lines, complexity 6,
and CRAP 42. The public getter reports three distinct stages: the configured
token before application, the live send-context token after application, and
the accepted token snapshot after the transport completes. A caller must not
observe later mutations to the original context or pipe as a different
accepted scheduled message.

`DelayedSendPipe_PreservesTheAcceptedTransportIdentityAsync` checks those
stages with distinct GUIDs. It verifies the clock-derived delay and
context token, simulates the transport assigning its final token and message
ID, asserts both returned properties and the `AcceptResult` snapshot, then
mutates the pipe and original context. The accepted identity remains stable;
repeated acceptance returns the same result and reusing the accepted pipe
throws. This catches returning the configured token instead of the transport
token, ignoring the transport's final context values, and continuing to read
the mutable context after acceptance. It exercises the real pipe contract
using an in-memory send context; it does not claim a broker handoff.

The test's `RequirementCoverage` entry is mirrored in
`CoreRequirements.json`. No product source changed. The focused test passed
1/1, and the complete Core project passed 6,374/6,374 with Microsoft
CodeCoverage. Both reports measure the selected getter at 5/5 lines and
CRAP 6. The complete Engineering Release build passed with zero warnings and
errors. The complete Unit/Architecture gate passed 10,228/10,228 without
failures or skips. No tracked test source changed between these observed runs;
the log files alone do not prove byte identity.

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `artifacts/schedule-send-accepted-identity-focused.log` | Named behavior test 1/1 green | `d79416261db45d82a2880e943aceffe275dc6b6c8f6e43120934e31d18bc7246` |
| `artifacts/schedule-send-accepted-identity/coverage.log` | Focused Microsoft CodeCoverage 1/1 green | `f9987e1e603d65b09ed4e1d0a2ea209a60c60f2baa548a46490c5c0d658c5d91` |
| `artifacts/schedule-send-accepted-identity/coverage.cobertura.xml` | Selected getter 5/5 lines, complexity 6, CRAP 6 | `55f6b10a6491a20c2934ad88e07faa593f2c59c1635fe9f796ddbe56cf707770` |
| `artifacts/schedule-send-accepted-identity/core-coverage.log` | Complete Core 6,374/6,374 green | `a693bb64b34e6ef90d51116600273150c3af21245f2244de95a806272428c53c` |
| `artifacts/schedule-send-accepted-identity/core-coverage.cobertura.xml` | Selected getter 5/5 lines and CRAP 6 in complete Core suite | `3a986a2329e37636aea48b515c0535e0eef3e3c8d66666702e3e2edc694e7618` |
| `artifacts/schedule-send-accepted-identity/release-build.log` | Full Engineering Release build: zero warnings/errors | `4fbeaf296f80439aa6b33abb6cf111a62794dc84726bd8e499eb5de865c85257` |
| `artifacts/schedule-send-accepted-identity/unit-architecture-gate.log` | Full Unit/Architecture gate 10,228/10,228 green | `48283bce71c12c41eb0132339d485e2e0633548a7f6e0bd9c9ca726fb3ee32cb` |

Independent read-only adversarial review returned PASS for behavior and
requirement projection. The global A+ goal remains open; the last complete
product-wide profile predates this test.

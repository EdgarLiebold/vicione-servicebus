# Incomplete product coverage attempt at f3848534c

The collection started with exact source and test commit `f3848534c` and no
tracked source or test changes. It produced 24 parseable Cobertura reports:
18 native Unit, four additionally instrumented Unit, one Abstractions repeat
without AVX2, and one broker-independent Abstractions LocalIntegration run.
Their logs record 9,892 + 280 + 913 + 3 = 11,088 passing executions, with no
failures or skips. The Unit and provider Release builds completed with zero
warnings and errors. The artifact is
`artifacts/coverage-a-plus-20260925-f3848534c/`.

The 24-report partial aggregation observes 30 product assemblies,
77,929/92,182 lines (84.5382%), and 105 methods above CRAP 30. Its two
Cobertura branch observations are 28,835/36,316 (79.4003%) by conservative
merge and 30,398/36,316 (83.7042%) by capped sum. These figures omit the
broker-backed providers, including SQL Server and RabbitMQ paths, and are
neither a current whole-product result nor comparable to the complete
36-report profile at `c073a5e30`. The partial aggregation lives in
`analysis-unit-23/`; despite that folder's earlier name, its summary records
all 24 reports after the independent Abstractions local run was added.

The broad provider run stopped before its first test when `docker compose down`
remained blocked in the host. The first Unit build also stopped at a blocked
`grpc.tools` `protoc` process. The tracked proto had not changed since
September 2; an identical previously generated C# output was staged into the
isolated artifact build directory. A build with
`Protobuf_ProtocFullPath=/usr/bin/false` then completed, proving that the
generated output was reused without invoking the blocked tool. This is an
environment workaround, not fresh proto generation.

The architecture gate exposed stale Python-tool inventory, a two-namespace
SQS test source, SQS async naming violations, and a fixture-file naming error.
The complete Architecture suite passed 445/445 after repair; the SQS suite
passed 295/295, and read-only adversarial review returned PASS. The fixes
made after this collection mean the 24 reports are historical evidence for
`f3848534c`; they are not an exact profile of the next commit. A fresh
36-report profile is required before updating the global A+ assessment.

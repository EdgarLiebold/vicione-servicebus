# Exact-commit partial coverage receipt audit

Measurement commit: `2d8635d3a`. The official
`tools/ci/aggregate_coverage_receipts.py --partial` run verified 20
receipts, all with `sameCommit: true`, across 17 of 18 required unit
projects, all three Abstractions portability modes, and 1 of 13 local
integration projects. The receipts contain 5,321 successful test
executions, with no failures or skips; the three Abstractions modes each
execute the same 922 test cases, so this is not a unique-test count.

Aggregator output:
`artifacts/coverage-unit-subset-2d8635d3a.json`, SHA-256
`187ad0fc8fc18bd450c6a645a24f8320cdeaf3a55403d5b68501c665cc6d45ad`.
It reports 2,661 tracked C# source files seen, 54.8241% line coverage,
49.4222% conservative Cobertura branch coverage, and 712 methods with
CRAP above 30. These values are **partial** and must not be interpreted
as product-wide A+ or as a final hotspot ranking. The Core unit receipt
is missing, as are 12 local integration receipts and four product
assemblies: EventHubs, EventHubs.Testing, Futures and Mediator.

An earlier Core receipt attempt at commit `aabda46d8`, before the
measurement commit, is archived at
`artifacts/coverage-receipt-core-aabda46d8/`. It did not produce a test
log or receipt; its build log ends without a completion summary. During the
run, interactive `ps` checks showed its Grpc.Tools `protoc` child in macOS
uninterruptible (`U`) status with no CPU time or new build-log lines for
several minutes. The waiting Python and dotnet wrappers were then
terminated. A separate diagnostic copy of the binary under `/tmp` also
entered `U` on `--version`; older uninterruptible `protoc` processes were
seen. These process observations are not hash-bound receipt evidence and
may change with host state. No output from these attempts is included in
the aggregate. The host condition must be resolved before another fresh
Core receipt can finish.

The 12 missing local integration receipts require their provider services.
An interactive check found a Docker CLI but no Docker socket, Podman
executable, Docker.app or OrbStack.app at the checked paths; this
temporary host state is not part of a coverage receipt. The project and
lockfile CodeCoverage references are now present for all 13 local
integration projects, and the Abstractions local integration receipt is
verified.

The global A+ goal remains open. The next measurement must keep every
receipt on one unchanged source/test commit and include all required
unit, portability and local integration projects before issuing any
product-wide line, branch or CRAP grade.

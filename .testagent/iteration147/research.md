# Iteration 147 — Mediator research

## Bound source and tests

The packet personally reads all 28 current C# files and 2,755 physical lines in
`ViciOne.ServiceBus.Mediator`, its project file and every comment. It also reads all 14 direct
owner test files / 3,374 lines, the adjacent 214-line mediator test-harness contract, the test
project and the complete Mediator requirement projection.

The assembly retains one explicit direct factory, conventional dependency-injection composition,
strict loopback addressing, mandatory message limits, owned canonical JSON bodies, isolated
send/publish/consume observers, direct exception propagation, virtual-clock request deadlines,
thread-safe scoped client-factory materialization and owned asynchronous cleanup. Public and
internal type, namespace, file and folder responsibilities remain coherent.

## Review result

No current correctness, lifetime, concurrency, cancellation, dependency, naming, placement,
comment or public-API defect was reproduced. The owner is therefore admitted without product or
test changes. This is a current-tree read admission, not a claim that every defensive branch was
executed.

Fresh instrumentation over both direct Mediator test namespaces reports 832/918 lines (90.6318%)
and 242/314 branches (77.0701%) across 283 methods. Direct CRAP calculation from the same
Cobertura XML reports no score above 30; the maximum is 24 at 100% line coverage. Thirty-nine
members below 80% are trivial forwarding/accessor methods, null/cancellation arms, defensive
registration delegation, probe hooks and the rare multiple-cleanup-failure arm already identified
by the prior owner hardening. They remain visible rather than being mislabeled as covered.

The isolated Roslyn pairing analyzer classifies 28 source and 14 direct test files, with 12
name-paired and 16 statically unpaired. The unpaired set is dominated by internal implementations
exercised through public contracts, extension methods invoked as instance methods, dependency-
injection resolution and `GlobalUsings.cs` with zero declarations; these are documented analyzer
limitations, not substituted for runtime coverage.

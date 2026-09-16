# Iteration 145 — analyzer and code-fix read admission

## Bound source

This packet personally reads all 16 current C# files and 3,018 physical lines in
`ViciOne.ServiceBus.Analyzers` and `ViciOne.ServiceBus.Analyzers.CodeFixes`, every source comment,
both project files, and the shipped/unshipped rule records. It also reads all 22 directly owning
C# test/support files and 4,466 lines, both test projects and both requirement projections.

The current implementation retains the Iteration-96 architecture: exactly eight diagnostic
analyzers and two code-fix providers are public, shared Roslyn mechanics are internal, analyzer
instances retain no compilation-bound state, and compiler/workspace dependencies remain separated.
Producer identity, task observation, cancellation forwarding, structural contracts, supported
conversions, consumer safety, configuration ownership and code-fix syntax construction agree with
their comments and tests. No new source, comment, naming, placement, dependency or public-API
finding was identified.

## Fresh effectiveness evidence

The native baselines pass 164/164 Analyzer and 36/36 CodeFix cases. Fresh instrumentation reports
95.5538% line / 85.4072% branch coverage for Analyzers and 94.4915% line / 71.9697% branch coverage
for CodeFixes. No Analyzer method has CRAP above 30. CodeFixes has one score above 30: the fully
line-covered compiler-generated recursive async state machine at 32; the highest genuine method is
25.87.

Three compile-valid counterchanges were killed and restored: removal of discard-task recognition,
acceptance of a private getter as serializable, and treating concrete classes rather than interfaces
as recursively constructible CodeFix values. The final frozen source manifest matches exactly.

One first full Core run exposed an unrelated pre-existing time-sensitive batching observation: the
string grouping case saw four partial results `[1,1,2,2]` after its three-result wait. The isolated
unchanged case passed 1/1 and the complete unchanged retry passed 4,799/4,799. This is retained as
transparent timing-instability evidence and is not attributed to the Analyzer packet.

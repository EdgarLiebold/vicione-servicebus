# Iteration 145 — analyzer and code-fix read admission

## Scope and result

This bounded packet personally rereads every current C# file in
`ViciOne.ServiceBus.Analyzers` and `ViciOne.ServiceBus.Analyzers.CodeFixes`, every comment, both
project files, the shipped/unshipped diagnostic records, all directly owning C# tests/support and
both requirement projections.

No new correctness, architecture, naming, namespace, placement, dependency, comment or public-API
finding was identified. The current owner preserves the Iteration-96 architecture: exactly eight
diagnostic analyzers and two code-fix providers are exported, shared compiler mechanics remain
internal, compilation-bound Roslyn symbols are not retained by analyzer instances, and compiler-only
versus workspace dependencies remain in their proper assemblies.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 16 / 3,018 | `7bc317225a0312dc8e4bd2ccda879aa92ded8e8e9f109b80bf5e67781b117a65` | `d3a0eee8d486f20ad0c2eba6315fb764b87138844ae05f0c89f106eaf0df9e80` |
| Owning tests/support | 22 / 4,466 | `9e61b42138411f019457243658d28c99da5435e445fbfc66f4037d54c283f746` | `84e90470467abc8377bd6d3a7b22c4dff6a1b2654295512b40be3ab035147c3d` |

The chains extend Iteration-144 manifest hashes
`a9467b8d0cff66e2c12f979e5dfd004a522bbf755018d4a42129b5e3a499e563` and
`1651c5cf9b8347b99fa0cb9e627464e4c1816554feedec10796c410eb3642e8c`. Both manifests validate
against the final tree with zero mismatches. Cumulative current personal source admission is 137
of 4,116 current C# files.

## Behavior and assertion review

The tests cover exact producer identities and lookalikes, every supported task-observation form,
cancellation overload shape and scope, message-contract recursion and conversions, headers,
collection/dictionary/`MessageData<T>` boundaries, canonical consumer types and framework symbols,
all configuration write forms, generated-code exclusion, shared-instance parallel determinism,
exact CodeFix source changes and exported provider metadata.

Personal assertion review found no assertion-free, trivial-only, tautological, unawaited, skipped,
randomized or swallowed-exception owner test. Positive, negative, equality, collection, type,
diagnostic location/message/severity, exact syntax and concurrency observations are all represented.

Three isolated counterchanges compiled with zero warnings/errors and were killed before immediate
manual restoration:

| Counterchange | Focused result | Causal observation |
| --- | ---: | --- |
| Stop recognizing discarded producer tasks | 0/1 | Expected four diagnostics; only one remained. |
| Treat a private getter as serializable | 0/1 | Unexpected `WriteOnly` missing-property diagnostic. |
| Recursively construct classes instead of interfaces in CodeFix | 9/25 | Sixteen exact initializer projections differed. |

Final source-manifest validation proves that no counterchange remains.

## Static pairing review

The mandatory Roslyn analyzer ran once successfully in isolated directory
`/private/tmp/vsb-iteration145-pairing.zsMPop`. Its raw 23-source / 16-test result includes the
copied analyzer itself and classifies six shared test-infrastructure files as source. Normalized to
the product packet, 10 of 16 files pair directly and six are filename-heuristic misses:

- `AnalyzerSymbolExtensions.cs`
- `ConversionGraph.cs`
- `MessageTypeConversion.cs`
- `OperationExtensions.cs`
- `SymbolIndex.cs`
- `ServiceBusSymbolFacts.cs`

These internal mechanics are exercised through the public analyzers and CodeFixes. Direct
call-chain review plus 95.5538% Analyzer and 94.4915% CodeFix line instrumentation make the static
misses navigation evidence, not proof of absent behavior coverage. Tool trimming warnings are not
product build warnings.

## Terminal validation

| Gate | Result | Duration |
| --- | --- | ---: |
| `ViciOne.ServiceBus.slnx` strict Release build, no restore | 0 warnings / 0 errors | 1:57.33 |
| `ViciOne.ServiceBus.Tests.Unit.slnx` strict Release build, no restore | 0 warnings / 0 errors | 2:44.91 |
| Engineering format verification | Exit 0, no differences | — |
| Unit format verification | Exit 0, no differences | — |
| Native Analyzer final | 164 passed; 0 failed/skipped/other | 33.970 s |
| Native CodeFix final | 36 passed; 0 failed/skipped/other | 19.295 s |
| Native Core complete retry | 4,799 passed; 0 failed/skipped/other | 30.618 s |

The initial sandboxed coverage and format attempts were rejected as environmental evidence because
Microsoft Testing Platform and Roslyn build hosts could not create local named pipes. Identical
commands passed outside that IPC restriction.

Analyzer baseline/final sorted-name SHA-256 is unchanged at
`2883024b7b7feceafe8a347b884746ced147d95018f91dd9773282434aee9521`; final CTRF SHA-256 is
`e5ed44f8114129006346cb449ef7c97d81e2b7aeeb2110fdbd2a9524c5e22836`.
CodeFix baseline/final sorted-name SHA-256 is unchanged at
`9bacad81863367c28b5dac0abfd33fbc46aa39b830f19d0fd2e7543b633d2aa5`; final CTRF SHA-256 is
`48e1b7e76056b526646c3bba71f912c93e2b1348ff22c68e50d2701a624f4329`.

Core retains the exact Iteration-144 4,799-name multiset at
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; the accepted retry CTRF
SHA-256 is `26e8690a34031febcfc5e43422fbaff79ee44babce64b0b34aefb6a62228d7e1`.

The first complete Core attempt passed 4,798/4,799. The unrelated
`StringGrouping_ProducesBatchesOfOneTwoAndThreeIncludingNullAsync` timing case observed four partial
results with counts `[1,1,2,2]` after waiting for three publications. With no source or test change,
the isolated case passed 1/1 and the complete retry passed 4,799/4,799. This instability is retained
explicitly rather than laundering the first result; it is not caused by or assigned to this
Analyzer/CodeFix packet.

## Coverage and CRAP

Coverage uses repository configuration SHA-256
`3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.

| Assembly | Lines | Branch rate | Complexity | Methods | CRAP > 30 | Below 80% / zero |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Analyzers | 1,211/1,267 (95.5538%) | 85.4072% | 905 | 179 | 0 | 7 / 2 |
| CodeFixes | 220/233 (94.4915%) | 71.9697% | 138 | 29 | 1 | 0 / 0 |

Analyzer Cobertura/CTRF SHA-256 values are
`f060ce32b2bdd1a4b44bdcb4cb08480fc0dac8111019dfcdaa3cba1266019528` and
`1d10358a0ac4910383bf272fedf4204b727a9e67e7258abc39b3c88567302f77`.
CodeFix Cobertura/CTRF values are
`ece3dff600346f403384570b9fcdfc8116b124fa2e37c9e76f101d2d49c82033` and
`6ae8a6336da7e017298959bdde3a98e1eaee85164e00e23ac9d369b4bbecfea4`.

The sole score above 30 is the compiler-generated async `MoveNext` for recursive anonymous-message
traversal: complexity 32, line coverage 100%, CRAP 32. The highest genuine CodeFix method is
`CancellationTokenOverloadMethodFixer.RegisterCodeFixesAsync` at 25.87; the highest Analyzer method
is `MessageContractAnalyzer.EnumerableTypesAreStructurallyCompatible` at 26.67. No new remediation
is justified by these fully or substantially exercised paths.

The bundled PowerShell analyzer was unavailable because `pwsh` is absent; the documented
unique-line coverage and CRAP formula were evaluated directly. No optional HTML report was
requested or generated.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration145-*`. Protected review, TestResults and
legacy trees were neither modified nor used as evidence.

The intended annotated tag is
`servicebus-a-plus-iteration-145-analyzer-and-codefix-read-admission-2026-09-16`. Local commit and tag
occur after final exact-path verification. External publication is not pre-claimed and remains
subject to destination-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and real durable-provider/external acceptance remain open. This packet proves only
the current Analyzer/CodeFix admission and its fresh evidence.

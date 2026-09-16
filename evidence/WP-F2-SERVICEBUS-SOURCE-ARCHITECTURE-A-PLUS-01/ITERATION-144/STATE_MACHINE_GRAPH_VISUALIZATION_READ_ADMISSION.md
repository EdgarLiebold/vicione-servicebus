# Iteration 144 — state-machine graph visualization read admission

## Scope and result

This bounded packet personally rereads the complete four-file
`ViciOne.ServiceBus.StateMachineVisualizer` implementation and the seven immutable graph/visitor
files it consumes from `ViciOne.ServiceBus.Sagas`. It also reads every source comment, the
Visualizer project file, every C# file in the owning test project, its requirements and project
file, and the Core graph-contract tests.

No new correctness, architecture, naming, namespace, placement, dependency, comment or public-API
finding was identified. The current Visualizer source is byte-identical to the Iteration-92
remediated boundary: it owns deterministic Graphviz and Mermaid serializers directly, has no
QuikGraph dependency, and exposes exactly two sealed synchronous generators. Synchronous
`Generate()` remains the correct API for bounded in-memory graph projection and text serialization.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 11 / 953 | `a9467b8d0cff66e2c12f979e5dfd004a522bbf755018d4a42129b5e3a499e563` | `f7be4d2bd29f3cf669a19b6c52f7ec703c4fe3bd973d9359952216ea3200ab88` |
| Owning tests | 14 / 1,754 | `1651c5cf9b8347b99fa0cb9e627464e4c1816554feedec10796c410eb3642e8c` | `2f438ba913a88f839868a45506126e13088c62c2ff020ed5df90484d894ee1e6` |

The source chain extends Iteration-143 source-manifest hash
`1b93f065293b65bf14c5f09b9a7b5f9072c4997723e52117afbd6104650ec179`; the test chain extends
`c663654f8ab40b097678d95d8b3070ad96f06c619f5c8d403a9ccc2e506f3f69`. Both manifests validate
against the final working tree with zero mismatches. Cumulative current personal source admission
is 121 of 4,116 current C# files.

The four current Visualizer source hashes are:

- `StateMachineGraphProjection.cs`:
  `8a505bd8e1e156cfbd2594852beeccdbd82d2ecc89f73eec98b1e810bdeb52c5`
- `StateMachineNodeLabelFormatter.cs`:
  `59e300721f7359673d91ba2a5e58083ab0b995c220f9b79b80e040ee226ff8ff`
- `StateMachineGraphvizGenerator.cs`:
  `7638cda417f764ba2ae86b60d0fe5726c081b17b44187b23526296e8e4dc5e65`
- `StateMachineMermaidGenerator.cs`:
  `e50637f53141a3988d87cfc00f4422d37dfdd61925f3526d79d27a48f267f2fd`

## Test quality and counterchanges

All 29 Visualizer tests were personally reviewed. Their 181 assertion call sites cover equality,
strings, collections, types, nulls, exceptions, negative cases, exact structure, and concurrency or
repeatability state. There is no assertion-free, trivial-only, tautological, unawaited, flaky,
randomized, skipped or swallowed-exception test.

Three isolated compile-valid counterchanges were applied one at a time and immediately restored:

| Counterchange | Focused result | Causal observation |
| --- | ---: | --- |
| Emit Mermaid C0 controls verbatim | 0/1 | Exact entity-encoded document comparison failed. |
| Keep the `Fault<T>` wrapper in event labels | 0/2 | Both typed-event generator labels failed. |
| Style event binding instead of state inheritance in Graphviz | 0/1 | Exact inheritance document comparison failed. |

Every mutated fixture compiled with 0 warnings and 0 errors. Final source-admission validation
confirms that no counterchange remains.

## Static pairing review

The mandatory Roslyn analyzer ran once successfully in isolated directory
`/private/tmp/vsb-iteration144-pairing.Ha5RHQ`. Its raw 12-source count included the copied analyzer;
excluding that tool file gives the actual packet: 11 source files, 14 test files, seven paired and
four unpaired heuristic results.

The heuristic misses are `StateMachineGraphExtensions.cs`, `StateMachineGraphVisitor.cs`,
`StateMachineGraphProjection.cs`, and `StateMachineNodeLabelFormatter.cs`. Extension invocation,
visitor traversal and internal helpers are exercised through their public owners rather than
filename-matched tests. Direct call-chain review, focused behavior tests and 100% package line and
branch instrumentation establish Visualizer execution. Analyzer-tool trimming warnings are not
product build warnings.

## Terminal validation

| Gate | Result |
| --- | --- |
| `ViciOne.ServiceBus.slnx` strict Release build, no restore | 0 warnings / 0 errors |
| `ViciOne.ServiceBus.Tests.Unit.slnx` strict Release build, no restore | 0 warnings / 0 errors |
| Engineering format verification | Exit 0, no findings |
| Unit format verification | Exit 0, no findings |
| Unfiltered native Visualizer | 29 passed; 0 failed/skipped/other |
| Unfiltered native Core | 4,799 passed; 0 failed/skipped/other |
| Targeted Visualizer coverage | 29 passed; 0 failed/skipped/other |

The focused baseline and terminal Visualizer inventories both contain 29 names. Their identical
sorted-name SHA-256 is
`1337f6874b9c471b7f73352ca65a64836bf00dc79844c24c13330d2f6884bd72`; final CTRF SHA-256 is
`ca910f1421caaba7f1f6fd450f716f5d6c1c0e0e78b8f66705b276b96e7213f5`.

Core retains all 4,799 Iteration-143 names with no addition or removal. Its sorted-name SHA-256
remains `6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`; final CTRF SHA-256 is
`b509a2c3e8ab8fe1a23ccfc3e0fe30babe7fe095167ddccf02ea34091fcf6335`.

## Coverage and CRAP disposition

Fresh direct instrumentation of `ViciOne.ServiceBus.StateMachineVisualizer` reports 183/183 lines,
100% branches and all 20 methods covered. Cobertura SHA-256 is
`f216ac0f2b41d0e387ceb6b0d5ddca8c8e48e9266c692a518f7c79ff89f7768b`; its CTRF SHA-256 is
`ba7cf71d3df9e0a9bccf7f1788a8b276bd78531dc1eec9b092af10137bc15d8d`. Coverage uses repository
configuration SHA-256 `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.

The documented unique-line CRAP calculation finds one method above 30:
`StateMachineMermaidGenerator.EscapeLabel`, complexity 35, line coverage 100%, CRAP 35. This is an
explicit, fully exercised character-classification switch for grammar delimiters, line controls,
C0 controls and malformed surrogates, not an uncovered risk. The next scores are 26 for Graphviz
label escaping, 22 for named-type formatting and 10 for graph projection construction; no method
is below 80% coverage.

The bundled PowerShell analyzer was unavailable because `pwsh` is absent; the same documented
method identity, unique-line coverage and CRAP formula were evaluated directly. No optional HTML
report was requested or generated.

## Checkpoint and continuing goal

Raw artifacts remain under `/private/tmp/vsb-iteration144-*`, including baseline, mutations,
pairing, terminal and coverage outputs. Protected review, TestResults and legacy trees were neither
read nor modified.

The intended annotated tag is
`servicebus-a-plus-iteration-144-state-machine-graph-visualization-read-admission-2026-09-16`.
Local commit and tag occur after final exact-path diff and manifest verification. External
publication is not pre-claimed and remains subject to destination-specific authorization.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global
bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork
coverage/CRAP and real durable-provider/external acceptance remain open. This packet proves only
the current graph/visualization read admission and its fresh behavior evidence.

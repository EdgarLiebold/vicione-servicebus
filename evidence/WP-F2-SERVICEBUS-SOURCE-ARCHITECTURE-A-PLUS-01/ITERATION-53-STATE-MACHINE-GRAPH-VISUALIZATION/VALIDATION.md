# Iteration 53: State-machine graph and visualization architecture

Date: 2026-09-08

Branch: `feature/servicebus-a-plus-api`

Baseline: `d749a055572b73200d73288c7759dddc5d9b1f84`
(`servicebus-a-plus-remediation-iteration-52-2026-09-08`)

## Scope and review method

This iteration continues the complete, file-by-file source architecture review authorized by
`WP-F2-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`. The coherent source cohort comprises the public
state-machine graph contract, its saga visitor, the Graphviz and Mermaid projections, and the state
and exception-activity contracts that supply the graph data.

Every production and test file changed by this iteration was read, interpreted, and reviewed in
context before it was edited. The review covered runtime behavior, failure semantics, public API,
type identity, nullability, mutability, naming, namespaces, type-to-file placement, folder placement,
dependencies, comments, tests, and rendered output. Comments were written manually after the owning
code was understood. No source-comment generator or bulk comment-rewrite script was used.

The current source census contains 4,123 C# files below `src`. This document closes the graph and
visualization cohort; it does not claim that the remaining source files have already completed their
personal file-by-file review.

## Findings and corrections

### Public graph model

The former `Vertex`, `Edge`, `GraphStateMachineVisitor`, and `GraphStateMachineExtensions` surface
exposed generic names, mutable collections, incomplete edge meaning, and implementation-oriented
terminology. It could not faithfully distinguish event binding, transition, exception handling,
composite-event contribution, and state inheritance.

Correction:

- replaced the former surface with `StateMachineGraphNode`, `StateMachineGraphEdge`,
  `StateMachineGraphNodeKind`, `StateMachineGraphEdgeKind`, and `StateMachineGraphExtensions`;
- made graph nodes and edges immutable reference-identity objects and made `StateMachineGraph` a
  sealed, validated immutable aggregate;
- added factories for state, event, and exception nodes with closed-type validation;
- required a legal edge kind for every source/target kind combination and rejected null nodes,
  duplicate nodes, duplicate edges, and edges whose endpoints are absent from the graph;
- moved traversal behind the public contract into the internal generic
  `StateMachineGraphVisitor<TSaga>`;
- renamed the public visualizer namespace to `ViciOne.ServiceBus.StateMachineVisualizer` and removed
  the obsolete friend-assembly dependency from Abstractions.

### Graph completeness and identity

The previous visitor collapsed reused events globally, omitted explicit event bindings, inheritance,
lifecycle handlers, ignored/action-only events, nested exception branches, and composite-event
contributions. It also assumed the concrete built-in state-machine implementation, violating the
public `StateMachine<TSaga>` substitution boundary.

Correction:

- identifies event nodes by their owning state and event, so a reused event remains distinct in each
  state;
- emits explicit state-to-event bindings for ordinary, lifecycle, ignored, and action-only behavior;
- emits typed transition, exception-handler, composite-contribution, and state-inheritance edges;
- represents every exception branch separately and supports catches nested beneath catches;
- permits composite contributions from ordinary event nodes and exception nodes;
- seeds declared but otherwise unbound events, and adds `State<TSaga>.DeclaredEvents` to preserve the
  distinction between declarations and next-event behavior;
- resolves states through the public state-machine contract rather than casting to a concrete
  implementation;
- preserves deterministic node and edge insertion order while retaining constant-time identity
  lookup.

### Exception-activity contract

Exception activities were discoverable only through concrete implementation knowledge. A new public
`IStateMachineExceptionActivity` exposes the handled exception type, and `StateMachineVisitor`
provides the matching visit overload. `CatchFaultActivity` implements the contract, rejects a null
behavior, and exposes its closed exception type.

### Renderer correctness

The renderers previously copied a lossy subset of the graph and inferred meaning from node shape.
They now project every node and edge with its explicit semantic kind. Graphviz renders inheritance as
a dashed, labelled edge; Mermaid renders it with the corresponding inheritance relation. Both
renderers preserve reused-event identity and nested exception/composite topology.

The shared label formatter now handles arrays, `Fault<T>`, exception namespaces, nested generic
types, and multiple generic arguments without relying on an arbitrary stack allocation. Mermaid
labels escape syntax-significant quotation marks, and deterministic graph order produces stable
output.

### Type, file, namespace, and folder placement

Each new top-level type has its own matching file. Test-only fixture types that previously obscured
the file/type relationship were separated into `Envelope.cs`, `RestartData.cs`,
`GenericGraphFixtureException.cs`, `GenericGraphFixtureOuter.cs`, and `Pair.cs`. The former
`StateMachineGraphFilteringTests` name was replaced by `StateMachineGraphProjectionTests`, matching
the behavior actually verified.

No physical source-project move was justified by this cohort. The direct `src/ViciOne.ServiceBus.*`
directories currently represent first-class product or cross-cutting capability packages, while
`Persistence`, `Scheduling`, and `Transports` group provider families. There are no loose C# files
directly in the `src` root. Moving this visualizer project solely to make the first level look
symmetrical would weaken rather than clarify the capability taxonomy. That classification remains
subject to the dependency and responsibility evidence gathered as every remaining project is read.

### Comments and repository hygiene

All comments in the reviewed production files were checked against the final implementation and
rewritten where necessary. They describe current code and behavior, not migration history or how the
code was produced. A repository-wide directive search found no `#pragma`, conditional-compilation,
nullable, region, warning, error, or line directives in any C# file below `src`.

The broad dummy-term scan found no dummy, stub, fake, TODO, or FIXME implementation in this cohort.
Occurrences of “temporary” elsewhere in `src` refer to real temporary broker entities or runtime
state. The `NotImplementedException` match is a real exception classification case, and the two
“schedule placeholder” occurrences describe the state machine's actual schedule declaration object;
neither is a dummy implementation. This static scan is supporting evidence, not a substitute for the
still-running personal review of all 4,123 source files.

## Requirement and test coverage

The visualizer requirement inventory and the core requirement inventory were extended before the
new tests were accepted. The focused suites cover:

- every graph node kind, every edge kind, and every invalid source/target combination;
- immutable graph construction, duplicate rejection, missing endpoint rejection, and reference
  identity;
- ordinary, ignored, action-only, unbound, lifecycle, reused, and name-colliding events;
- transitions, state inheritance, nested exception handling, and composite contributions;
- custom state-machine implementations through the public contract;
- null exception behavior, invalid open generic types, arrays, `Fault<T>`, nested generics, and
  multiple generic arguments;
- exact Graphviz and Mermaid output, deterministic ordering, renderer syntax, quoting, disconnected
  graphs, and empty graphs;
- developer-facing construction and public API accessibility.

Focused final results:

- State-machine visualizer tests: 25 passed, 0 failed, 0 skipped;
- core graph contract tests: 7 passed, 0 failed, 0 skipped;
- corrected documentation architecture rules: 2 passed, 0 failed, 0 skipped.

## Test effectiveness

Eight deliberate mutations were applied one at a time. Each mutation was compiled, killed by the
expected focused test, and restored before the next mutation:

| Mutation | Detecting behavior | Result |
| --- | --- | --- |
| Collapse state-local event nodes into one global node | Reused-event identity and two-state projection | KILLED |
| Omit state-inheritance edges | Substate graph contract | KILLED |
| Treat catch activities as terminal leaves | Nested-catch traversal | KILLED |
| Accept an open generic message type | Graph-node factory validation | KILLED |
| Permit duplicate graph nodes | Graph aggregate validation | KILLED |
| Omit composite-contribution edges | Composite-event projection | KILLED |
| Drop declared but unbound events | Unbound-event projection | KILLED |
| Stop escaping Mermaid quotation marks | Mermaid syntax contract | KILLED |

`git diff --check` passed after every mutation had been restored.

The internal read-only Red Team found an initially stale packed API baseline and a missing
multiple-generic-argument formatter case. The baseline was regenerated through the real package gate,
and the `Pair<T1, T2>` fixture plus exact output assertions close the formatter gap. It also challenged
edge legality, graph identity, ordering, custom state-machine substitution, lifecycle events, nested
catches, composite contributions, filenames, namespaces, comments, dummy markers, directives, and
feature preservation. Its final bounded review reported no remaining code blocker. This was internal
adversarial review, not independent external acceptance.

## Validation results

### Restore and build

Locked restores of the product, engineering, and unit solution graphs passed. Strict Release builds
of all three graphs passed with warnings treated as errors. The final post-correction builds reported:

- Unit solution: 0 warnings, 0 errors;
- Engineering solution: 0 warnings, 0 errors.

### Unit and architecture tests

`ViciOne.ServiceBus.Tests.Unit.slnx`, Release, Microsoft Testing Platform v2, one test module at a
time:

- 4,566 passed;
- 0 failed;
- 0 skipped;
- 4 minutes 1 second.

The first complete run correctly found two documentation-rule failures caused by one inaccurate use
of “message-specific behavior” in `IStateMachineActivity`. The owning interface and behavior were
read again, the sentence was manually corrected to describe execution for the supplied message, the
two focused rules passed, and the complete unfiltered 4,566-test profile then passed. The failure was
not suppressed or excluded.

### Formatting

`dotnet format --verify-no-changes --no-restore --severity warn` passed for both the engineering and
unit solution graphs. The known workspace-load warning does not represent a formatting difference;
both commands exited successfully without modifying a file.

### Packaged developer journeys and public API

`tools/ci/verify_developer_journeys.sh` passed against freshly built packages:

- 18 executable developer-journey scenarios;
- exactly 30 freshly packed ViciOne packages;
- 3 isolated provider-testing package consumers built and executed;
- 29 runtime package APIs matched the committed baseline;
- public API contract: 21,603 lines;
- generated contract SHA-256:
  `e205d1d069773febf069fee87790e2e4d4cbd329fe7889b95db7bbf4e3d9e043`.

The packed inventory includes the graph edge kind, exception-node factory,
`IStateMachineExceptionActivity`, `State<TSaga>.DeclaredEvents`, and the new visitor overload. The
developer-facing API was therefore verified through the shipped NuGet surface, not only through
project references.

## Iteration conclusion

The state-machine graph and visualization cohort now has an explicit, immutable, semantically typed
public model; complete traversal of the supported state-machine topology; deterministic and safe
renderers; coherent names, namespaces, files, comments, and tests; and direct mutation evidence for
the highest-risk semantics. No feature was removed: formerly invisible state-machine behavior is now
represented explicitly.

This is a secured iteration boundary, not completion of the A+ source architecture goal. The next
iteration continues the same personal file-by-file review with the next coherent source cohort and
keeps the final `src` project-family placement decision evidence-driven.

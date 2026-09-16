# Iteration 141 — scalar and task-boundary converter ownership research

## Bound scope and contract

The original whole-fork A+ source/API/architecture objective remains active. This connected packet covers the remaining asynchronous scalar converter boundaries in `VariablePropertyConverter`, converting `ToNullablePropertyConverter`, converting `TaskPropertyConverter`, and the caller-owned task inputs exposed by direct task and message-data adapters.

The distinction is ownership, not whether a task happens to be awaited. A variable value task and a downstream property-converter task are collaborator operations started by the converter with the exact forwarded caller token. Once accepted, their original success, ordinary fault or producer cancellation must be observed even if the caller token changes later. Conversely, a task supplied as the property input is caller-owned data. The task adapter may stop waiting for that input when the caller cancels; no downstream conversion has been accepted at that point. `MessageData<T>.Value` is likewise a task-valued input surface with no cancellable producer invocation, so its existing caller-local wait remains intentional.

`TaskPropertyConverter<TResult, TInput>` therefore has a split contract. Its source-task wait remains locally cancellable. Once that source succeeds and `_converter.ConvertAsync` returns a task, the accepted conversion must be observed directly. Its reverse direction deliberately returns the underlying conversion task as the property value; the outer completed task must preserve that inner task rather than await or replace it.

Direct-return adapters such as `FromNullablePropertyConverter` and `NamedInitializerValuePropertyConverter` do not compose an accepted task behind another wait; they return the collaborator task to the caller and therefore do not contain the abandonment defect. Synchronous type/object adapters have no accepted asynchronous collaborator. These paths remain compatibility evidence, not correction targets.

## Findings

Five productive waits violate accepted-task ownership: one direct variable wait, two waits in converted-variable composition, one converting nullable wait, and the post-source downstream wait in the converting task adapter. All five use `WaitAsync(cancellationToken)` after starting a collaborator with that token. The smallest correction is direct observation of those accepted tasks while preserving every pre-start cancellation check and the two caller-owned input waits.

Existing scalar tests cover values, nulls, pre-cancellation, completed/pending/faulted/canceled task inputs, null collaborator tasks and ordinary conversion. They do not cancel the caller after a variable/converter task has been accepted, distinguish downstream ownership from task-input cancellation in the converting adapter, or independently settle the abandoned originals. The existing message-data cancellation case proves local cancellation but leaves its never-completing source unresolved; the new bounded ownership fixture will settle and observe controlled caller-owned tasks in cleanup.

No public API, factory selection, null/default behavior, package, dependency or target framework needs to change. Functional comments should state only the ownership boundary that the implementation enforces.

## Static pairing

The mandatory Roslyn pairing engine ran once against `/private/tmp/vsb-iteration141-pairing.piXBNB`, an isolated Initializers-only copy. Protected review, TestResults and legacy trees were not copied or scanned. It classified 90 source files and 50 tests: 78 paired and 12 unpaired infrastructure files. TaskPropertyConverter, VariablePropertyConverter, ToNullablePropertyConverter and MessageDataPropertyConverter are all paired to existing owning tests. This static result is a navigation heuristic, not assertion-strength, behavior or coverage evidence.

The repository uses .NET SDK 10, Microsoft Testing Platform and xUnit v3. Focused validation will use the repository's SDK-10 project/filter syntax after a fresh compile.

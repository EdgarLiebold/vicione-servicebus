# Iteration 140 — nested and collection converter ownership research

## Bound scope and contract

The whole-fork A+ objective remains active. This connected packet covers `InitializePropertyConverter<TProperty, TInput>`, its runtime-input fallback, `ArrayPropertyConverter<TElement, TInputElement>`, `ListPropertyConverter<TElement, TInputElement>`, `DictionaryKeyPropertyConverter`, and both converting `DictionaryPropertyConverter` forms. Their factories, interfaces, comments, direct consumers, and owning tests were read before correction. Identity-only collection materializers remain in scope for compatibility review but contain no accepted collaborator tasks.

Each converter validates its context and pre-cancellation before accepting work, forwards the exact caller token to nested or element conversion, and must then observe every accepted task to its original terminal outcome. Caller cancellation may prevent a later element or entry from starting; it must not detach a caller from an already started nested initializer, element conversion, or collection adapter. Success, ordinary failure, and producer cancellation therefore remain distinguishable after the caller token is canceled.

The combined key/value converter starts both conversions for an accepted entry. The current key-first outcome priority is retained, but either accepted sibling must be observed before the entry completes. If value conversion throws synchronously or returns a null task after key conversion was accepted, the key task still has to settle before the value diagnostic is propagated. Enumerators remain owned until all accepted work for the current traversal has settled.

## Findings

Every pending nested, array, list, dictionary-key, and dictionary-value wait currently uses `WaitAsync(cancellationToken)`. That abandons producer-owned tasks when the caller token changes after acceptance, can dispose an active enumerator while its converter is still running, and lets list/dictionary shape adapters complete before their accepted core traversal.

The combined dictionary converter has a second lifetime defect independent of `WaitAsync`: it starts key and value tasks and then awaits the key first. A failed or canceled key exits without observing the accepted value task. A synchronous value invocation failure or null-task diagnostic likewise escapes after the key task has already been accepted. The correction must drain both accepted siblings while preserving deterministic key-first result priority.

The typed and runtime nested converters resolve their initializer internally, so current tests can verify factory selection but cannot hold and independently settle the accepted nested initializer. A small internal constructor/resolver seam is justified for deterministic tests. The parameterless production constructors continue to use `MessageInitializerCache`; no public API or factory behavior changes.

The collection loops check cancellation before each new conversion. They may advance an enumerator once to learn that another item exists, but do not read or convert that item after cancellation. This packet preserves that boundary and proves that no second conversion starts. Null input, empty input, capacity hints, exact shape adaptation, null-task diagnostics, exact-key non-null enforcement, and pre-cancellation behavior remain unchanged.

## Existing evidence and static pairing

Iteration 139 ended at commit `c6d212d19d4bdd98b432f3f27f434dbbb69bd8c4` with 4,696 unfiltered Core cases. Existing collection tests cover identity/materialization, all list and dictionary shapes, synchronous and pending conversions, pending-to-completed transitions, exact token forwarding, completed failures, null collaborator tasks, and pre-cancellation. They do not cancel the caller after an element task has been accepted, track enumerator ownership, or exercise simultaneous key/value failures. Existing object-graph tests prove nested shape construction but not nested initializer lifetime.

The required Roslyn pairing engine ran once against `/private/tmp/vsb-iteration140-pairing.sEpbIO`, an isolated copy containing only Initializers source/tests and their project files. Protected review, TestResults, and legacy trees were neither copied nor scanned. It classified 87 source files and 49 tests: 75 paired and 12 statically unpaired infrastructure files. All six target converter files are paired; `InitializePropertyConverter` is paired only through factory-selection tests, confirming the behavioral gap. Static pairing is a navigation aid, not coverage or assertion evidence.

The repository uses .NET SDK 10, Microsoft Testing Platform, and xUnit v3. Focused runs therefore use SDK-10 `dotnet test --project ... --filter-class ...` syntax after a fresh compile.

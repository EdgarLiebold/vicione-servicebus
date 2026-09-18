# Iteration 211 — saga message-filter admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: six previously unadmitted `ViciOne.ServiceBus.Sagas` message-filter sources

## Outcome

The filter contract, four role-specific saga invocation filters and terminal state-machine filter
are admitted. All send and probe boundaries now reject invalid inputs deterministically. Saga,
continuation, event, completion-query and saga-completion null tasks produce owned diagnostics.
Pre-delivery and inter-stage cancellation checkpoints prevent user or continuation work after the
caller has cancelled, while collaborator failure and cancellation identity remain intact.

The four invocation filters retain their public extensibility and compatible `Consume` probe
surface while enforcing exactly-once saga-before-continuation ordering. Delivery cancellation is
not misreported as an error activity; genuine saga and continuation failures are recorded without
replacement. Shared-filter concurrency retains message and activity isolation.

The state-machine filter raises the typed event, remains terminal at the saga-message layer and
completes the saga only after an awaited positive completion decision. Probe ownership uses a
canonical filter scope with a child machine scope. Unhandled events preserve the event-supplied
state name and fall back safely when diagnostic state lookup is absent, pending or faulted.
Telemetry state reads are nonblocking and cannot replace the business result. A final extraction
reduced maximum method CRAP from 32 to 14 without changing the public surface or lifecycle order.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`d35f63a3ad38fcd62dd8cbc2ab337027e1caa0feb641ac0de3db70958041c62c`. Chaining that hash from
iteration 210 yields
`0308dcdc7e913903402deecb0f916088ed86b26947c78eac046548f4f09e909b`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Middleware/ISagaMessageFilter.cs` | 12 | `636f231dad8f00b9b2863a6a4c8a1e005e78a4a0be392208988cc2b4136ca295` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/InitiatedByOrOrchestratesSagaMessageFilter.cs` | 68 | `3f9ae2a7dcca59adac060e1421c5aa47ed42422f49057f75f8ea2776a300a4d1` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/InitiatedBySagaMessageFilter.cs` | 68 | `5df0438ebc2cf9c37b79e8588517c31cf9af31289ba8e30aef0b079f997d7635` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/ObservesSagaMessageFilter.cs` | 70 | `7d93c97006f0fadd8e5c389c695995341a7296f1be04e241bbc814d062c920ca` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/OrchestratesSagaMessageFilter.cs` | 70 | `d6f40348352aff215e72362b7fe0e48e74d224afe092978ca3b96362676120ce` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/StateMachineSagaMessageFilter.cs` | 216 | `15b2ae4f0d170c6e7f1c94b6fad6e807e1b06dc83054f460111031e5cd6804e2` |

The exact source packet is 504 lines. Lead-read progress is 742 of 4,118 C# sources, or 18.019%.

## Test manifest, API mapping and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis,
assertion-quality audit and independent parallel counter-audits drove the research-to-plan-to-test
sequence. Three classes contain 44 test methods, 103 executed cases and 44 unique requirement
variants. Every compile-visible or behaviorally material member is mapped to direct evidence.

Test manifest SHA-256 is
`7b52df083a66dd6933acad9e6559240cad05f4b82a5d8ad9e81cff029fb39fa3`; chained from iteration
210 it yields
`37e5712b2c796254a4dbf428811142b0b89b3dfb3e205a6896d48581179feccf`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaInitiationMessageFilterDeepContractTests.cs` | 905 | `d792e4ea3bf1554d3df74dffc48db1824bd4a39fbb8f09b35c0cdb4a01561ce2` |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaMessageFilterContractDeepContractTests.cs` | 1,026 | `f135a78d48844a03f833f0b226ba45701879959011202555cc9f9a72ae6469cf` |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaObservationMessageFilterDeepContractTests.cs` | 816 | `ee3e3c2006d4c8e953ef86c1c2627af0bba20225dc058b3248ed50ad516770e0` |

The exact test packet is 2,747 lines. `CoreRequirements.json` SHA-256 is
`6fb63a92ccc0ec8ce86a127932f4d9a7e409eeaaab1b602fb319d30413c3ff5c`.

## Independent final re-audit

The first independent contract audit found missing direct metrics evidence, incomplete diagnostic
fallback cases, two unverified public generic parameter names and cancellation documentation that
described only delivery-owned cancellation. Three disjoint Sol-xhigh remediation passes closed all
four findings. A second audit then identified cancellation- and unhandled-event-specific metric
classification as the remaining gap; three further disjoint passes closed it without adding a new
requirement variant.

The ultimate contract audit reports no finding. It reconciles 44 deep-contract methods with 44
registry entries, confirms all 13 unhandled-state fallback variants, and verifies success, ordinary
failure, pre-cancellation, delivery cancellation, unrelated collaborator cancellation and unhandled
translation metrics across all five filters. The independent assertion-quality audit also reports no
finding: assertions are non-vacuous, meter scopes are isolated, every ambient `LogContext` mutation
is restored in `finally`, and no timing sleep or unbounded wait exists. A deliberately duplicated
`MetricOperation.Complete` call is an equivalent mutant because `Complete` is idempotent by
contract; the suites prove its observable completion effect exactly once.

## Mutation proof

Eight compiled, isolated, material single-cause mutants were killed and restored:

1. remove the initial cancellation checkpoint from the initiating filter;
2. remove the post-saga cancellation checkpoint from the initiating-or-orchestrating filter;
3. remove the null saga-task guard from the observing filter;
4. report caller-owned cancellation as an error in the orchestrating filter;
5. force the state machine through the untyped event-raise overload;
6. remove the cancellation checkpoint after a negative completion decision;
7. pass the root rather than child probe context to the state machine; and
8. ignore the state name supplied by `UnhandledEventException`.

Every counted mutant compiled and failed its owning test at the intended invariant. Every source
was restored, then the final formatting pass and no-incremental warning-clean builds regenerated
the tested artifacts. The later state-machine extraction moved existing logic only; the same
mutated invariants remain covered by the final 92/92 focused baseline.

## Coverage, CRAP and gaps

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-211-admission.MUPu8D/ultimate/iteration211-ultimate.cobertura.xml`,
SHA-256 `28d4b43764598bb339387133c02fd0a286265d24b93a3bce50ae17018db12cb8`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines and branch conditions use maximum coverage across duplicate entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Filter contract | 0/0 | 0/0 | n/a |
| Initiated-by-or-orchestrates filter | 26/26 | 14/14 | 14.0 |
| Initiated-by filter | 26/26 | 14/14 | 14.0 |
| Observes filter | 26/26 | 14/14 | 14.0 |
| Orchestrates filter | 26/26 | 14/14 | 14.0 |
| State-machine filter | 95/98 | 57/58 | 14.0 |
| **Total executable** | **199/202** | **113/114** | **14.0** |

The four role filters have complete line and branch coverage. All state-machine event, completion,
cancellation, translation and metric paths are covered. Its only residuals are `TrySetStateTag`'s
contract-violating null-task return and synchronous-exception suppression: three sequence points and
one branch used exclusively to prevent optional state-tag enrichment from replacing the business
result. They cannot alter event raising, completion decisions, saga completion, cancellation
propagation or continuation behavior. The independent extraction found no material domain-flow gap;
the residual helper's CRAP is 6.354 and maximum owner method CRAP is 14, below 30.

Focused CTRF and Cobertura SHA-256 values are respectively
`071af520121b700e0e1651992e04118eeb4becc07f533f987fc535ae7cddc61c` and
`3ad80ca6602e887216953d80484d7756262987f7c5ed4df2057b18ab00b23683`.

Sorted display-name SHA-256 is
`83c37c24c13ec7e5fa7d84b504add47575c9f7b6f85e38838d2d4c7b552e9669` across 5,807 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-211-admission.MUPu8D/ultimate/iteration211-ultimate.ctrf.json`,
SHA-256 `c21ca715d75998bea07a45f4aecc3a670a428667c5eff106a9b9f9db1cacc773`.

## Gates

| Gate | Result |
| --- | --- |
| Three final owned classes | 103/103 passed |
| Saga-wide regression (`*Saga*`) | 1,044/1,044 passed |
| Full Core Release | 5,807/5,807 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core / EF / EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 8/8 compiled isolated material mutants killed |

Two preliminary instrumented full runs each exposed a different existing timing-sensitive
integration failure outside this packet. Both tests passed immediately in isolation; the fresh final
instrumented run passed all 5,796 tests and is the artifact hashed above.

All genuine asynchronous APIs in the packet retain the `Async` suffix; synchronous construction,
properties and `Probe` do not. No unresolved message-filter correctness, terminality, cancellation,
failure-identity, telemetry-isolation, concurrency, public-contract, requirement-projection,
formatting or material coverage-risk finding remains in this admitted packet.

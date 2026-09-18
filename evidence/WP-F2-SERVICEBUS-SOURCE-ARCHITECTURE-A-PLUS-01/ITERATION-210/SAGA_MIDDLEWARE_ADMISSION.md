# Iteration 210 — saga middleware admission

Date: 2026-09-18

Branch: `feature/servicebus-a-plus-api`

Scope: 11 previously unadmitted `ViciOne.ServiceBus.Sagas` middleware sources

## Outcome

The correlation/query ingress, missing-instance redelivery, repository lifecycle, missing-policy,
context split/merge and rescue owners are admitted. Query and correlated paths now enforce their
own null boundaries, keep repository/downstream/observer ordering explicit and do not attribute a
non-saga downstream failure to saga observers. Redelivery validates its inputs, remains bounded and
cancelable, probes its child pipe and preserves operation and policy-disposal failures causally.

Repository actions, saves, missing decisions and cleanup execute exactly once under explicit
ownership. Async disposal has priority, null tasks and contexts produce stable diagnostics, stale
query results enter the missing policy, and simultaneous operation/cleanup failures retain causal
order. Split/merge adapters preserve the original message and saga owners independently, including
typed adapters. Rescue exception metadata uses one atomic lazy publication. Independent API-to-test
and assertion-quality counter-audits found and closed the initial lifecycle, cancellation, adapter,
payload, task-identity and concurrency gaps.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`27ccc5f207b5d418c2416d35b6b6bbd9c90f420ed85a38f1fa6b17a91f3c4441`. Chaining that hash from
iteration 209 yields
`58e229161debb02be55da4163e975ba68675d0def15ca479f76a1faa28afb8c6`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Middleware/CorrelatedSagaFilter.cs` | 92 | `2081a43f7abbd028a7b28a3fb16654ac4de8481b14bc05a3980828dcc22e55f4` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/MissingInstanceRedeliveryPipe.cs` | 182 | `2ac9aaf9df5e8f0db468239580b654714287aafaf85586b43073b07be0855b17` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/MissingSagaPipe.cs` | 104 | `ddbec61b78d56da44e171b09cb090083a67f71cad680c99c4fd235c40b4af372` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/QuerySagaFilter.cs` | 124 | `056d205056bff7fb66e020f8ce1f5a9a6dd6ca9f8a83cee583469a0e50d57c82` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/Rescue/RescueExceptionSagaConsumeContext.cs` | 59 | `f0188538c833fa77897bd516a687181119ef51cbc47aa224e50b219b0b41b9f9` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SagaMergePipe.cs` | 106 | `7a1f419622bc6ae6ec0e5077caadc4fd83141280c48009807d41dbdec4894773` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SagaMessageMergePipe.cs` | 56 | `9658552f574fe338ea58c3d8143368aafe93c083cbbc8574491726bd578d186c` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SagaMessageSplitFilter.cs` | 49 | `f022c72c9b093a2453bde1cc7da0171f705835ffb79cf1ce4b0d32e1c4485887` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SagaSplitFilter.cs` | 49 | `c6e8117924769e63d8efeccb3ba77e08b4b050ad6d3bac69729c45f156a82a22` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SendQuerySagaPipe.cs` | 68 | `c79f8baf22d8ff49f66b4f6266d4ccc324ac3d91a568b07c8e7e86996ea07e65` |
| `src/ViciOne.ServiceBus.Sagas/Middleware/SendSagaPipe.cs` | 180 | `d651c58083540f0fb65826c8e10638c97cdcfac267cd8e5913383893be2dae94` |

The exact source packet is 1,069 lines. Lead-read progress is 736 of 4,118 C# sources, or 17.873%.

## Test manifest, API mapping and requirements

The mandatory code-testing workflow, static source/test pairing, pseudo-mutation gap analysis,
assertion-quality audit and independent parallel counter-audits drove the research-to-plan-to-test
sequence. Three classes contain 48 test methods, 64 executed cases and 48 unique requirement
variants. Every compile-visible or behaviorally material member is mapped back to direct evidence.

Test manifest SHA-256 is
`5c3d05088dc2c8799a8155c2d4548e8d86b5561450b9bf9274707f35c5bb0c95`; chained from iteration
209 it yields
`77bb73f0290ffface9cb8403721465b1fc0ca66d8f6851c4c085f3b4a7cc2f5a`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaIngressRedeliveryDeepContractTests.cs` | 1,290 | `e886f17f84213cfd0de24d7fc207b6befbc52f3bfa8248b408b38f08378238f0` |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaRepositoryLifecycleDeepContractTests.cs` | 1,136 | `13c4eaba42c8e38bebbfb650b88a37ff110e143627c67863ac282e8c15a2b5d7` |
| `tests/ViciOne.ServiceBus.Tests/Middleware/SagaSplitMergeRescueDeepContractTests.cs` | 680 | `8ccacb6169c354d562cc0a3f8f7c3a0a314d5b7ae6c67c4e8de92242d2f6d4a7` |

The exact test packet is 3,106 lines. `CoreRequirements.json` SHA-256 is
`965b0d47bbbda6479a067090def35b68ee57a32a3859000e5072840583552fd6`.

## Mutation proof

Eight compiled, isolated, material single-cause mutants were killed and restored:

1. publish consumed before correlated downstream completion;
2. remove the successful-null query-result guard;
3. discard the primary operation when redelivery policy disposal also fails;
4. retry a completed discard after its own failure;
5. treat a loaded query result as absent and invoke the missing policy spuriously;
6. swap original-message and adapted-saga ownership in typed `SagaMergePipe`;
7. trust a conflicting typed saga owner in `SagaMessageMergePipe`; and
8. return each rescue contender's exception projection instead of the atomically published winner.

Every counted mutant compiled and failed its owning test at the intended invariant. An equivalent
exception-wrapping probe was excluded. The final no-incremental build restored every product and
test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is
`/private/tmp/vicione-servicebus-iteration-210-admission.aBHERD/iteration210-final.cobertura.xml`,
SHA-256 `9d72467f1344b2bc7d1a4f2bcbbeb62f784848911f3123e49a1b84117eaa9d01`.
Exact source-basename selection includes compiler-generated classes belonging to the same source.
Unique executable lines use maximum coverage across duplicate entries.

| Owner source group | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| Correlated saga filter | 35/36 | 0/0 | 6 |
| Missing-instance redelivery | 100/100 | 0/0 | 24 |
| Missing saga policy | 51/51 | 0/0 | 12 |
| Query saga filter | 47/50 | 0/0 | 8 |
| Rescue exception context | 13/13 | 0/0 | 4 |
| Saga merge pipe | 33/33 | 0/0 | 8 |
| Saga-message merge pipe | 17/17 | 0/0 | 4 |
| Saga-message split filter | 12/12 | 0/0 | 2 |
| Saga split filter | 12/12 | 0/0 | 2 |
| Send-query saga pipe | 24/24 | 0/0 | 10 |
| Send saga pipe | 77/77 | 0/0 | 14 |
| **Total executable** | **421/425** | **0/0** | **24** |

The four uncovered sequence points are compiler-generated state-machine continuations after
non-returning exception rethrows: the correlated catch close and three query-filter helper/caller
continuations. All 421 reachable owner lines are covered, the format exposes no representable owner
branch conditions, and no method exceeds the CRAP threshold of 30.

Sorted display-name SHA-256 is
`f059285e136cd67981cae327a8a2bd7467b844106dba3719cc473369d664955b` across 5,704 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-210-admission.aBHERD/iteration210-final.ctrf.json`,
SHA-256 `cf8b53733db3634ce49c5cc7419feca4e3b68c9a802b45fb64af58c239356ca5`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 64/64 passed |
| Saga-wide regression (`*Saga*`) | 941/941 passed |
| Full Core Release | 5,704/5,704 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 8/8 compiled isolated material mutants killed |

All genuine asynchronous APIs in the packet retain the `Async` suffix; synchronous constructors,
properties and `Probe` do not. No unresolved middleware lifecycle, correlation, redelivery,
cleanup, observer-order, public-contract, null-boundary, context-ownership, rescue-concurrency,
requirement-projection or material coverage-risk finding remains in this admitted packet.

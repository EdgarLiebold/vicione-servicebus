# Iteration 197 — saga outbound extension admission

## Outcome

The saga publish, respond and send extension surfaces are admitted across all 73 public overloads.
Every overload now owns deterministic receiver and required-input validation before binder effects,
while preserving binder result identity, exact activity selection, lazy destination/factory
execution, callback payload identity and all normal, data, faulted and data-faulted shapes.

The lead personally read all three current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 637/4,118 files (15.469%). The packet contained 1,421
physical lines before the change and 1,673 after admission.

## Corrections and direct contracts

- All 20 publish overloads validate their receiver; task and delegate forms validate their owned
  input before activity construction. Direct messages retain the existing downstream
  `InitializedMessage<T>` validation instead of duplicating an equivalent boundary.
- Publish callback uplift retains null, defers invocation and delivers the exact
  `PublishContext<T>` payload once.
- All 13 respond overloads validate their receiver; task and delegate forms validate their owned
  input before any binder or callback effect, with the same direct-message equivalence treatment.
- All 40 send overloads validate receiver, fixed/provider destination and message/task/factory
  inputs before binder effects.
- Fixed destinations remain closure-captured; destination providers and message factories remain
  lazy and receive the exact behavior context.
- Every overload preserves the returned binder, selects the exact normal/data/faulted/data-faulted
  activity type and forwards the exact send callback context.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`0db46565297475d28c26bb5f67c0688fc5b42bfe07bde9da559e3ad656a84d73`. Chaining that hash from
iteration 196 yields
`1edf94dd4da6ba494edb8c68f8526b4d6481e6de899c89a002bea1ae58eace1f`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/PublishExtensions.cs` | 439 | `85a0732db968af7489f72dd9e016b01bb412e7a351b1cd09078b49cdb68ba5f8` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/RespondExtensions.cs` | 289 | `02c17cb66981dec50dcee898c822fa8a9b3c1f80c08899fa3ea76ee8562a68c0` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SendExtensions.cs` | 945 | `b8968bf292a9284ab0c0190d0525b381cc86a29fbcb13055dd5d9b255a925f32` |

## Test manifest, assertions and requirements

The three classes contain 17 test methods, 17 expanded cases and 17 unique requirement variants.
Every test has causal structural, equality, identity, exact-type, exception, parameter-name,
non-invocation, lazy-execution or callback-payload assertions. The mandatory code-testing workflow,
static source/test pairing, pseudo-mutation gap analysis and assertion-quality audit drove the
research-to-plan-to-test sequence.

Test manifest SHA-256 is
`3c4a36f12eef07931fb0533716252a1d4c40cb6d2e037b589573c6c1ec0c65d9`; chained from iteration
196 it yields
`4b6f85eca280531163208f5b47a56715e5299402d69e335555f4bee90275f8de`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaPublishExtensionDeepContractTests.cs` | 457 | `d831d8736ff0fc4a1cc8d9d09103ad4824c839ae7cb56022ee98b58dd6aed028` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaRespondExtensionDeepContractTests.cs` | 547 | `d6044c25f251a8f4d937d8f84209256ff0c816e38e7256f2a464270426b92400` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaSendExtensionDeepContractTests.cs` | 305 | `56b001b24059e2f2f0afd203c133914abbf26476b2eb5a824de94d7a6587f7c6` |

`CoreRequirements.json` SHA-256 is
`15b4e37952db1a1a912b0bf743ff83744d120b24778c3ad3bf69986d61d57739`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the first publish receiver guard was removed;
2. the first publish synchronous-factory guard was removed;
3. the first respond receiver guard was removed;
4. the first respond synchronous-factory guard was removed;
5. the first send destination guard was removed; and
6. the first send receiver guard was removed.

Each mutant caused its owning deep-contract class to fail at the exact intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-197-final.cobertura.xml`, SHA-256
`89adc85a40e7b6bc2fabbb965400672f4f0fb7a1ba2927fa11ee7e4c8ad4b95c`. The three executable
owners reach 276/276 lines (100.000%): publish 64/64, respond 36/36 and send 176/176. No branches
are instrumented in these owner classes. Maximum method CRAP is 2 with complete line coverage; no
coverage gap needs a disposition.

Sorted display-name SHA-256 is
`a283451dad27e16ebc267eb5ca5367c92ac7dabc120034bcf2f7a15b14d8c774` across 5,388 displays.
The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-197-results/iteration197-final.ctrf.json`, SHA-256
`0179272f3820f0c1224825187f1d7f81ee79f2b35f898ebfb68aa5c86917706b`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 17/17 passed |
| Sagas namespace regression | 161/161 passed |
| Full Core Release | 5,388/5,388 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

No unresolved correctness, lifecycle, callback, overload-shape, compatibility, coverage or
architecture finding remains in this admitted packet.

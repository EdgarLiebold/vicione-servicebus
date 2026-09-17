# Iteration 198 — saga schedule and convention extension admission

## Outcome

The date-time schedule, time-span schedule/unschedule and convention-based send surfaces are
admitted across all 104 public overloads. Every overload now owns deterministic receiver and
required-input validation before binder effects while preserving binder result identity, activity
selection, lazy provider and route execution, context identity, UTC arithmetic and callback payloads.

The lead personally read all three current sources before delegation. They are newly unique, moving
cumulative exact unique source coverage to 640/4,118 files (15.542%). The packet contained 2,260
physical lines before the change and 2,676 after admission.

## Corrections and direct contracts

- All 20 date-time schedule overloads validate their receiver, schedule, time provider and owned
  task/factory input before activity construction. Provider execution stays lazy and receives the
  exact behavior context.
- All 40 time-span schedule overloads validate receiver, schedule and owned task, factory or delay
  provider inputs. Their synthesized due time remains the context time provider's UTC time plus the
  exact schedule-owned or explicitly supplied delay.
- All four unschedule overloads validate receiver and schedule, select the exact normal or faulted
  activity and preserve returned binder identity.
- All 40 convention-send overloads validate their receiver. Task and factory forms validate their
  owned input; the required context-aware callback family validates its callback. Direct messages
  retain downstream-equivalent validation except where the context-aware factory boundary otherwise
  defers it. Nullable simple callbacks remain optional.
- Convention lookup, message factories and callbacks remain lazy; all receive the exact runtime
  behavior or send context.

## Source manifest

Each record is `path<TAB>content-sha256<LF>`, sorted ordinally. Manifest SHA-256 is
`0142882369933ffc0a7b98808b171b2349f38e36d5434f6d351b42de9f09455e`. Chaining that hash from
iteration 197 yields
`3b70fd7b2171f7ada17b3d03765a513a4354602615a73869058890faec1b0251`.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleDateTimeExtensions.cs` | 527 | `a79476a5a0d06e6acedf21f5ce4a045ec2173e604860fa62b34a518e0f01694d` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/ScheduleTimeSpanExtensions.cs` | 1,255 | `51c132876d4298e4ae6b696dd875f7e6208f177c2bfb73980a976a95794b7f37` |
| `src/ViciOne.ServiceBus.Sagas/Sagas/SendByConventionExtensions.cs` | 894 | `95ea1a4a497d3b1894ab47033f4ee9fc2456d52372fb320b5676dc6f51649de0` |

## Test manifest, assertions and requirements

The three classes contain 10 test methods and 10 unique requirement variants. Their reflection
matrices expand across every public overload and assert public shape, causal ordering, exact type,
equality, identity, exception parameter names, non-invocation, lazy execution, arithmetic and
callback payloads. The mandatory code-testing workflow, static source/test pairing,
pseudo-mutation gap analysis and assertion-quality audit drove the research-to-plan-to-test
sequence.

Test manifest SHA-256 is
`760169ae13719e5fcc55a0a78b79d3429f28f106b46edff146f20f4cbc74eff6`; chained from iteration
197 it yields
`9e29af62e34767d6dc19a4643c6d7d5addb032b2207def0ed2fdc91559ab6ff6`.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaScheduleDateTimeExtensionDeepContractTests.cs` | 292 | `b7c7ae2968a53efbd1fec1ba4bd5a1f1124cbba623d08f9d64718b08b21471e1` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaScheduleTimeSpanExtensionDeepContractTests.cs` | 386 | `cd142398b5c85d4484a0c6ccea8f385bf6c89b59dd985a5fc5ed1920817fd87c` |
| `tests/ViciOne.ServiceBus.Tests/Sagas/SagaSendByConventionExtensionDeepContractTests.cs` | 343 | `fb7031537bffce87abc8386e13385212195c8c257f33c47fe5005376722270d6` |

`CoreRequirements.json` SHA-256 is
`7d4eaa99efa81d808cd00f60dc4fa92b660250f12d4c505958c87736ad802405`.

## Mutation proof

Six compiled, isolated, material single-cause mutants were killed and restored:

1. the first date-time receiver guard was removed;
2. the first date-time time-provider guard was removed;
3. the first time-span receiver guard was removed;
4. the first time-span schedule guard was removed;
5. the first required convention callback guard was removed; and
6. the first context-aware convention direct-message guard was removed.

Each mutant caused its owning deep-contract class to fail at the exact intended invariant. The final
no-incremental build restored every product and test-host artifact.

## Coverage, CRAP and gates

Final Cobertura is `/private/tmp/vicione-servicebus-iteration-198-final.cobertura.xml`, SHA-256
`4253c0a358ea6acb5bad3fc9d72b9c2e3525b3238d59188f0c3bc2ff65f843ce`. Exact source-filename
selection includes compiler-generated classes while excluding foreign sources. Unique executable
line numbers use the maximum hit count across duplicate generated entries.

| Owner source | Lines | Branches | Maximum method CRAP |
| --- | ---: | ---: | ---: |
| `ScheduleDateTimeExtensions.cs` | 109/109 | 0/0 | 1 |
| `ScheduleTimeSpanExtensions.cs` | 244/244 | 0/0 | 1 |
| `SendByConventionExtensions.cs` | 176/176 | 80/80 | 2 |
| **Total** | **529/529** | **80/80** | **2** |

Sorted display-name SHA-256 is
`95cdd4b2539ea763da8e566feb5c226050d16f84fd68550f5da708fa5c6ee5de` across 5,398 unique
displays. The strict Core CTRF artifact is
`/private/tmp/vicione-servicebus-iteration-198-results/iteration198-final.ctrf.json`, SHA-256
`d16da1b065ee0dc32cc8cd3ceac0e5c79adf1068d449df6a57ccfb6c85c2fd78`.

| Gate | Result |
| --- | --- |
| Three final owned classes | 10/10 passed |
| Sagas namespace regression | 171/171 passed |
| Full Core Release | 5,398/5,398 passed, 0 failed, 0 skipped |
| Full EF unit Release | 249/249 passed, 0 failed, 0 skipped |
| Core-test / EF-unit / EF-local Release builds | 0 warnings, 0 errors |
| Core/EF/EF-local requirement projections | 1/1, 1/1 and 1/1 passed |
| Full EF-local diagnostic | external `UnitArchitecture` PostgreSQL profile absent; 1/60 passed |
| Scoped format, JSON and diff checks | Exit 0; no formatting or whitespace errors |
| Mutation probes | 6/6 compiled isolated material mutants killed |

The full EF-local diagnostic is not an admission gate for this Core/Sagas packet and cannot execute
without the canonical external PostgreSQL fixture configuration. Its project build and isolated
requirement projection both pass. No unresolved product correctness, lifecycle, callback,
overload-shape, compatibility, coverage or architecture finding remains in this admitted packet.

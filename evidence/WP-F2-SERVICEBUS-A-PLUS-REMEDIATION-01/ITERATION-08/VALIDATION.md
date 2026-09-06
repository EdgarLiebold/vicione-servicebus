# Iteration 8 Validation

## Scope

Iteration 8 makes application-owned outgoing options deterministic at asynchronous boundaries:

- `SendOptions`, `PublishOptions`, `ScheduleOptions`, and `RequestOptions` use immutable empty header defaults;
- send, publish, schedule, and request adapters copy caller-owned headers into a frozen snapshot before asynchronous endpoint work;
- nested reliable-scheduling options are snapshotted before endpoint acquisition;
- zero and negative message lifetimes fail before endpoint or provider use; and
- every physical product C# source must belong to exactly one evaluated product compilation.

The previously uncompiled `IJobSagaOptionsConfigurator` source file was unused, unimplemented, and inconsistent with the active `JobSagaOptions` contract. Because it was not present in any current product assembly and carried no executable feature, it was removed as a stale source-only compatibility artifact. Reintroducing that obsolete public shape would have expanded the greenfield API without a working capability.

## Red/green evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Immutable defaults and outgoing header snapshots | 4 failed | 4 passed |
| Non-positive outgoing lifetime validation | 2 failed | 2 passed |
| Request header snapshot and lifetime validation | 3 failed, 1 passed | 4 passed |
| Exact product compile ownership | one source file had zero owners | all physical product sources have exactly one owner |

The request snapshot test holds endpoint acquisition behind a deterministic gate, mutates the caller-owned dictionary after API entry, and then observes the serialized request context. The other three snapshot tests mutate their caller-owned dictionaries after pipe construction and assert the original values at the context boundary. No wall-clock delays or polling are used.

## Mutation evidence

Four isolated regressions were introduced and removed:

1. Retaining the caller dictionary instead of copying it caused all three send, publish, and schedule snapshot tests to fail on the mutated value.
2. Changing the lifetime boundary from `<= TimeSpan.Zero` to `< TimeSpan.Zero` caused the exact zero case to fail while the negative case continued to pass.
3. Replacing the immutable default on `SendOptions` with a mutable dictionary caused the all-options default-shape test to fail.
4. Adding a temporary C# source path excluded from every product project caused the compile-ownership architecture test to fail with that exact path and zero owners.

All mutations were removed before the final build and test run.

## Test-quality review

The added tests use direct application entry points and deterministic protocol-boundary doubles. Assertions independently cover collection mutability, original header values, added-key isolation, both adjacent invalid lifetime partitions, exact exception parameter names, endpoint non-use, and evaluated MSBuild ownership. There are no sleeps, skipped cases, broad exception catches, swallowed failures, or assertion-free tests.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,813 passed, 0 failed, 0 skipped |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Requirement-manifest validation | PASS through the complete Architecture test profile |
| Physical product-source compile ownership | PASS — every source has exactly one evaluated owner |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not independent external acceptance.

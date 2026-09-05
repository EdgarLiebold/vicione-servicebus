# A+ remediation test plan

## Iteration 1 outcome

Eliminate confirmed persistence defects, silent configuration contracts, discarded cancellation tokens, mutable process-global identifier configuration, and the unimplemented Azure message-session query path without losing supported behavior.

## Requirement-to-test map

| Requirement | Behavior partition | Test owner | Required evidence |
|---|---|---|---|
| `REQ-APlus-RUNTIME-001` | EF abandoned inbox entry stores the exact supplied UTC instant and remains terminal after a fresh context/store | EF reliable-store tests | SQLite provider behavior plus restart query |
| `REQ-APlus-CONFIG-001` | Receiver wrapper forwards topology flags, dependencies, dependents, and generic/runtime message topology changes | core configuration tests | Direct state and readiness/completion observations |
| `REQ-APlus-CONFIG-002` | In-memory `AutoStart` changes the bus endpoint configuration | in-memory configuration tests | Both boolean values observed through the built configuration |
| `REQ-APlus-CONFIG-003` | Endpoint registration inclusion is mutable only through the authoritative registration owner | dependency-injection tests | True/false round trip and downstream selection behavior |
| `REQ-APlus-API-001` | Composite filter collections are get-only and matching remains behaviorally correct | abstraction/core tests plus architecture test | Compile-time/reflection shape and include/exclude truth table |
| `REQ-APlus-ASB-001` | Message-session saga query correlation evaluates the current persisted session state and forwards matching queries | Azure Service Bus tests | Matching, non-matching, identity, count, and cancellation behavior |
| `REQ-APlus-ASB-002` | Message-session saga writes propagate the caller cancellation token | Azure Service Bus tests | Exact token identity for save and update operations |
| `REQ-APlus-JOB-001` | Job lifecycle notifications and progress propagation preserve caller cancellation | job-service tests | Exact token identity at provider, send, and progress-buffer boundaries |
| `REQ-APlus-NEWID-001` | The static `NewId` façade is immutable after startup and remains unique under parallel generation | abstractions tests | Public API-shape audit and 100,000-value parallel uniqueness run |

## Test partitions

- Positive: each supported configuration affects its runtime owner.
- Negative: null arguments, pre-cancelled tasks, and unsupported capabilities fail with the narrowest stable exception.
- Boundary: true/false flags, empty/non-empty filter predicates, exact timestamps including non-UTC offsets.
- Persistence: reload with a new EF context and a newly created reliable store.
- Composition: validation happens before a receive pipeline handles a message.
- Compatibility: no alias or obsolete member is added; supported feature behavior remains available.

## Mutation obligations

- Remove the EF `CompletedAt` assignment: the exact timestamp/restart assertion must fail.
- Replace each forwarding setter/method with a no-op: its direct behavior test must fail.
- Reintroduce a composite-filter setter or invert include/exclude semantics: API-shape or truth-table tests must fail.
- Replace Azure query predicate evaluation with an unconditional match: the non-matching and count assertions must fail.
- Drop the Azure state-write or job notification cancellation token: the exact token-identity tests must fail.
- Reintroduce a public process-global `NewId` mutator: the façade API-shape test must fail.

## Execution order

1. Add failing tests against the baseline for each confirmed defect.
2. Implement the smallest coherent behavior correction.
3. Run focused owner projects.
4. Run test-gap, assertion-quality, and anti-pattern checks for changed tests.
5. Execute one-cause mutations and restore byte-for-byte.
6. Run clean Release builds, the complete Unit/Architecture profile, relevant provider profiles, format/static gates, and zero-test/skip guards.
7. Record evidence, commit, tag, push, and verify remote hashes.

## Later iterations

Subsequent plans will cover application options and parameter completeness; member-level public API baselines and layering; source navigation and package ownership; XML documentation/comment/directive cleanup; topology hashing and SQL nullability; then full coverage and final multidimensional re-review.

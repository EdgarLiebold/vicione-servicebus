# A+ remediation iteration 6 validation

Date: 2026-09-06

## Scope

This iteration removes externally mutable process-global metadata and shared routing-slip state, and replaces the RabbitMQ cluster-node parser with a canonical .NET parsing contract:

- message-type, message-contract-name, reflected-property, consumer-convention, and core type-metadata caches expose stable `IReadOnlyList<T>` instances backed by read-only collections;
- internal call sites that require arrays receive explicit local copies instead of access to cached storage;
- no-argument routing-slip activities use a private read-only dictionary rather than a public mutable sentinel;
- `ClusterNode` is an immutable value with get-only properties and implements `IParsable<ClusterNode>` and `ISpanParsable<ClusterNode>`;
- DNS, IPv4, bracketed IPv6, and raw IPv6 hosts round-trip through canonical culture-independent text; and
- malformed hosts and ports outside `1..65535` fail before RabbitMQ connection work.

The changes preserve the complete feature set. Array-requiring serialization and scheduling boundaries receive defensive copies, while read-only consumers avoid allocations and cannot corrupt shared cache state.

## Red/green contract evidence

| Contract | Baseline result | Corrected result |
|---|---:|---:|
| Message metadata cache immutability | 2 failed, 348 passed | 350 passed |
| Consumer cache and routing-slip isolation | 3 failed, 4 passed | 7 passed |
| Core metadata facade | added as a direct regression guard | 1 passed |
| RabbitMQ cluster-node standard parsing | test project did not compile because the standard parsing members were absent | 19 focused cases and all 184 RabbitMQ tests passed |

The complete profile contains 25 additional test cases in this iteration.

## Adversarial and mutation evidence

Five isolated regressions were injected, detected, and removed:

1. Returning the cached message-type array directly was detected by the runtime-shape and mutation guard.
2. Replacing the private Courier read-only dictionary with a mutable dictionary was detected by the cross-builder contamination test.
3. Reintroducing a public `NoArguments` field was detected by the exact API-shape test.
4. Reversing nullable-port formatting was detected across DNS, IPv4, and IPv6 canonical round trips.
5. Accepting TCP port zero was detected by the invalid-boundary partition.

The three mutated product files were restored to their recorded SHA-256 values before the final build and test run.

## Test-quality review

The added tests use direct public/reflection contracts and deterministic in-process values. They contain no sleeps, wall-clock polling, external services, skips, broad exception catches, or assertion-free paths. Each state-bearing value is asserted independently, invalid parsing covers null, empty, whitespace, host, bracket, syntax, and both adjacent port boundaries, and the parallel cache read checks stable observations without shared test mutation.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,802 passed, 0 failed, 0 skipped |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Requirement-manifest validation | PASS through the complete Architecture test profile |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not independent external or Red Team acceptance.

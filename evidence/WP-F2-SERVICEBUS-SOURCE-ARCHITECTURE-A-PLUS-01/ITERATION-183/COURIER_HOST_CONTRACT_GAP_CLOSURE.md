# Iteration 183 — Courier host contract gap closure

## Result

This connected hardening packet closes the concrete gaps retained by Iteration 182 without
changing product code. Three GPT-5.6-Sol xhigh agents worked concurrently under disjoint ownership:
context API/forwarding, host result parameters and host pipeline outcomes. The lead owned sanitizer
boundaries and centralized integration, mutation, coverage, final gates, evidence and publication.

The 15 new requirement variants produce 21 cases. They prove that Courier scopes expose their
payloads without changing activity state; direct base notification forwards the exact context,
duration, consumer, token and returned task; sanitized slips reject a missing context or message,
preserve identity and normalize every nullable collection; all execute and compensate result
factory shapes materialize their parameters; matching execution failures are preserved while
nonmatching results are replaced; and activity-owned versus delivery-owned cancellation remains
distinct in both host directions.

No source file is newly admitted, so cumulative personal source admission remains 417/4,118
current C# files (10.126%) and the Iteration 182 source chain remains
`7d6c32243324bb6ff432a855dceb7c44ff460abf6f17575f510a926efd28f4dd`.

| Hardening manifest | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Tests | 4 / 1,798 | `104516113053d2d46c8a808bfc71329ca7855c8f07a239769de4fcc3f05934b9` | `131fb9c1631c9d166a430635fc58103f7e72e8a5ee0e4244e840bc74e527a8ca` |

The manifest hash covers ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. The chain hash covers the Iteration 182 test chain followed by this manifest hash, each
with a terminal newline.

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierActivityContextApiContractTests.cs` | 345 | `20a79dbc7290a9fc33985e4a432ed0748b484f36132f7ffcda7d60208be72352` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostContextDeepContractTests.cs` | 494 | `80d09c9ea578128f22e8186cbe27e8160a23cda62746a0c58124b07cff46c5a8` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostPipelineContractTests.cs` | 654 | `55cca92a29f06a6894b27af988344a41efe9d8c5b397f107da9583be8108e9b8` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierHostResultParameterContractTests.cs` | 305 | `29779c1becc5626fab79f77f9ace275bb6990056fc7b5a276fc091940253acb7` |

## Proof

Eight successfully compiled single-cause mutants were killed and restored:

1. omit scoped payload forwarding;
2. replace direct consumed-notification forwarding with a completed task;
3. remove the sanitized missing-message guard;
4. remove itinerary null normalization;
5. omit typed completion-log propagation;
6. omit object completion-variable propagation;
7. invert matching execution-failure preservation; and
8. invert execute-host activity/delivery cancellation classification.

Every mutant failed its exact owning test. A scoped diff against the five temporarily mutated
product files is empty after restoration. The final assertion audit finds no assertion-free,
trivial-only or self-referential additions: all new cases observe exact state, identity,
cardinality, exception ownership, callback phase or call arguments.

Final Courier Cobertura is
`/private/tmp/vicione-servicebus-iteration-183-final.cobertura.xml`, SHA-256
`3d1c7b5bd6741266afbe6f4ffafa1df0e82cdab5d05163dd64df7c555b3e0df6`.
Both result-context classes and the sanitizer constructor report 100% line coverage. The execute
and compensate host `SendAsync` state machines report 96.30% line coverage. `SanitizedRoutingSlip`
reports 89.09% line and 91.30% branch coverage. Maximum target method complexity and CRAP are both
28, below the threshold of 30.

Fifteen requirement variants are embedded in `CoreRequirements.json`, final SHA-256
`f9e26a59cc0f9722d875a0541d319dc9961ffdd6661a392b8e3a5e8a198b9fc8`. Unit sorted-display-name
SHA-256 is `64eba4614564e1bd4c7e78cfc6c46a427a20c42103dfe596734491f3786c0604`
across 4,901 displays. Removing exactly the 21 additions reproduces Iteration 182's known hash.

| Gate | Result |
| --- | --- |
| Four final owned classes | 43/43 passed |
| Complete Courier namespace with fresh coverage | 221/221 passed |
| Full Core Release | 4,901/4,901 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Release Core-test / EF-unit / EF-local builds | 0 warnings, 0 errors |
| Core/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped test format and diff checks | Exit 0; no changes required |
| Mutation probes | 8/8 compiled mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The database-dependent local matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written. Process-global telemetry hooks
remain a future `OpenTelemetryGlobalCollection` candidate because direct host instrumentation uses
global `ActivityListener` and ambient `LogContext` state and would make the parallel host class
order-sensitive.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-183-courier-host-contract-gap-closure-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication is an independent delivery step and cannot pause or
deactivate the active goal.

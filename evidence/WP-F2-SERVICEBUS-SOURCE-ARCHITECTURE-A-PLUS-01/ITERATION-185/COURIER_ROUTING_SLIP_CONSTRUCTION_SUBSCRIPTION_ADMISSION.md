# Iteration 185 — Courier routing-slip construction and subscription admission

## Result

This connected packet admits three builder/extension files, five routing-slip message models and
five subscription/capture files (13 source files / 1,391 final lines). Three GPT-5.6-Sol xhigh
agents worked concurrently under disjoint ownership. The lead personally read every admitted
source file and the 1,053-line directly owning test baseline, then owned integration, requirements,
mutation, coverage, final gates, evidence and publication.

Twenty-five requirement variants produce 27 cases. They bind route ordering, both execution
extension shapes, failure and cancellation propagation, atomic variables, all clone constructors,
transition records, malformed received state, single-read materialization, detached and read-only
containers, exhaustive subscription flags, all capture overload boundaries, observer ordering and
authoritative error precedence.

Seven product files contain the smallest corrections demonstrated by those cases:

1. the activity, activity-log, activity-exception and compensation-log copy materializers capture
   and validate each mutable interface member exactly once;
2. aggregate routing-slip construction rejects a default creation timestamp;
3. received subscription materialization detaches its message envelope and owned metadata
   collections; and
4. a send-fault observer failure is logged without replacing the pipeline, target or post-observer
   failure that caused fault notification.

Cumulative personal source admission is now 447/4,118 current C# files (10.855%).

| Admission manifest | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 13 / 1,391 | `5c977266fef4e8765cabf3474dbaae85b1e29b694fad10ced2e1313545cba34d` | `8fa906815e8ea3fb1fff7012944a5cf77bba1bc48af35b996851c0e36fb4c0cc` |
| Tests | 3 / 1,404 | `7d841d7ea79c0536e9db857f64cf475e5236ae977d0f51c1eff3fbe1f802bfc1` | `9947372bab316d38684380122ef33f068fcf8bd1fe1e1e3968cd2fc296596ad6` |

Each manifest hash covers ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Each chain hash covers the preceding iteration chain followed by this manifest hash, each
with a terminal newline.

| Source file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `Courier/Contracts/ISubscription.cs` | 23 | `d7393422d2279f9217541b1c63b716c8990b4488f33629cfa77dacd5de07edfc` |
| `Courier/IRoutingSlipBuilder.cs` | 12 | `c5c1650bef836830344dad20a9388537732bfd6409650e4d57ea7cc1af5f3d13` |
| `Courier/IRoutingSlipSubscriptionTarget.cs` | 17 | `5b1bf982801a66fddc7ea834b195348a73199e4f1ca636f03d1d004e62c56c07` |
| `Courier/Messages/RoutingSlipActivity.cs` | 63 | `29e893cf8479181ca19a913f62609fe5d63126d62b0eac8f2ca5e1d99ee22756` |
| `Courier/Messages/RoutingSlipActivityException.cs` | 88 | `30c79a70346150ad8035b603c7cbcc4da4ed36719afd1c7b132c5001ce2af5ee` |
| `Courier/Messages/RoutingSlipActivityLog.cs` | 76 | `ecd376ccfcfe028ef16a23a6ce09d1b68fb48ff53790f37b2ff096c5757e0344` |
| `Courier/Messages/RoutingSlipCompensateLog.cs` | 64 | `b1ccb69c575489fe0cf6e534bfa5003f55748914fe066c5f48db92db571ec35d` |
| `Courier/Messages/RoutingSlipRoutingSlip.cs` | 68 | `d3923df3f19ff057fe4b15c83aaa354622749182d554afe197d4811a7e905880` |
| `Courier/Messages/RoutingSlipSubscription.cs` | 105 | `395e847cb9e9ec21e4e26886efaf32272c9115c7649dd0ed89ae9a448c573011` |
| `Courier/Messages/RoutingSlipSubscriptionSelection.cs` | 49 | `1ea24098816649216748c0f76ba94d1e1d811db1405cc43d0c1b8ae70867d9ad` |
| `Courier/RoutingSlipBuilder.cs` | 493 | `3ea0dcf4417fc4ac044cf6a5d8417819f0666d664c2fd5afe002cd7cdd32a00b` |
| `Courier/RoutingSlipExtensions.cs` | 71 | `f123024ac98318d46547a7b1e5913d9a1acb645dbcccccb0f2a53d457d0ccdc9` |
| `Courier/RoutingSlipSubscriptionCaptureEndpoint.cs` | 262 | `a4bbcd0a2032841394ad22d5e02a180fb25b73a2882d731dc647d5f086079a7d` |

| Test file | Lines | Content SHA-256 |
| --- | ---: | --- |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierRoutingSlipBuilderDeepContractTests.cs` | 412 | `87b4b5193a8b1bfcabdf62725ca034f4a89dd20fcaead3eb4f75ccd2f31b33a9` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierRoutingSlipMessageModelDeepContractTests.cs` | 480 | `90dea3c5d1d16dc5ded67cb0ca3d83abeedf0f02d3065959d272ca382be53a56` |
| `tests/ViciOne.ServiceBus.Tests/Courier/CourierRoutingSlipSubscriptionDeepContractTests.cs` | 512 | `8cfa09efeccd0524065ccb41a17558393682cb58f796f722fb5bfb3cf92bdd6e` |

## Proof

Six successfully compiled, isolated single-cause mutants were killed and restored:

1. reread an activity name after validating its captured value;
2. remove the aggregate creation-timestamp guard;
3. retain a received subscription's mutable envelope by reference;
4. select the last rather than first execute address;
5. apply variable updates before enumeration completes; and
6. let a fault-observer failure replace the authoritative capture failure.

Every counted mutant failed only its owning behavioral contract. A preliminary observer probe with
an incompletely restored envelope line was discarded and rerun in isolation; it is not included in
the 6/6 result. All temporary source changes are restored. The final assertion audit finds no
assertion-free, trivial-only or self-referential additions: cases combine exact equality, identity,
type, exception ownership, ordering, cardinality, mutability, state transition and side-effect
assertions.

Final Courier Cobertura is
`/private/tmp/vicione-servicebus-iteration-185-final.cobertura.xml`, SHA-256
`09ec7014fcffe45258feb6f55acbd6e4d7e71d68ab7fb843bdf61da2d925f0e0`.
All executable admitted classes except `RoutingSlipBuilder` report 100% line coverage; the builder
reports 96.53%. Maximum admitted-target method complexity and CRAP are both 14, below the threshold
of 30.

Twenty-five variants are embedded in `CoreRequirements.json`, final SHA-256
`9c0891d59192943d76fa027de1e66feb1ebd1b85abd52a963de2c19a57f6396d`. Unit sorted-display-name
SHA-256 is `4a617275e7ebf8b99edca0cd36b6aca19cfc1beb300a7b7f0d7cb4c74372b2d0`
across 4,960 displays.

| Gate | Result |
| --- | --- |
| Three final owned classes | 27/27 passed |
| Complete Courier namespace with fresh coverage | 280/280 passed |
| Full Core Release | 4,960/4,960 passed with suite parallelism disabled |
| Full EF unit Release | 249/249 passed |
| Release Core-test / EF-unit / EF-local builds | 0 warnings, 0 errors |
| Core/EF/local requirement projections | 1/1, 1/1 and 1/1 passed |
| Scoped format and diff checks | Exit 0; no whitespace errors |
| Mutation probes | 6/6 compiled isolated mutants killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
The database-dependent local matrix remains externally configured; its requirement projection and
Release build are green, and no credential was inferred or written.

Residual boundaries are external subscription activity/address matching, concurrent observer
connection stress and transport-level concurrent builder mutation. Parameterless wire models remain
mutable until hydration/sanitization by design. Nested payloads, `HostInfo` and `ExceptionInfo`
preserve reference identity while their owning collections and envelopes are detached.

Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-185-courier-routing-slip-construction-subscription-admission-2026-09-17`.

Whole-fork personal reading, global API/naming/nullability/coverage and configured external-provider
acceptance remain open. Remote publication is an independent delivery step and cannot pause or
deactivate the active goal.

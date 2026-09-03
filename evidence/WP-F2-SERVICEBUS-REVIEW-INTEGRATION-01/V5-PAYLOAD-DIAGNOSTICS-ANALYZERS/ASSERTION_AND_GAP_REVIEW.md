# V5 payload, diagnostics, and analyzers assertion and gap review

Date: 2026-09-03

The review covers the 42 package-owned xUnit 4 methods across Core, MessagePack, analyzers, and Architecture. Theory rows
expand those methods to 58 native cases.

| Metric | Result |
|---|---:|
| Test methods | 42 |
| Executed package cases | 58 |
| Direct assertion calls | 228 |
| Average direct assertions per method | 5.43 |
| Assertion-free methods | 0 |
| Trivial-only methods | 0 |
| Self-referential/tautological methods | 0 |
| Distinct assertion APIs | 18 |

The suite uses equality, boolean, null, exception, assignability/type, string, collection, comparison, range, identity,
negative, side-effect, and structural/source-order assertions. Assertions bind results to independent constants, captured
wire bytes, observer/provider counts, exact exception stages and limits, Roslyn diagnostic IDs/locations, weak-reference
liveness, and deterministic source ordering.

Pseudo-mutation analysis found and closed five material gaps before acceptance:

1. Inline MessageData evidence initially used a plain payload rather than a real address-less MessageData value. The
   final case constructs the actual inline carrier and kills removal of the address guard.
2. The common-boundary architecture assertion initially failed indirectly through an invalid index. It now first proves
   boundary presence and then proves ordering, yielding a causal failure message.
3. Blocking-call lookalike coverage did not distinguish a foreign type named `Task`. The analyzer fixture now does.
4. Parallel VOSB5005 coverage used only one consumer. It now presents two consumers of the same payload and proves one
   deduplicated diagnostic.
5. JSON envelope capacity was calibrated across variable live timestamps. Fixed metadata now drives the real production
   serializer; three independent class runs prove repeatability.

The complete suite then exposed a product gap not visible in the initial focused set: empty MessageData fault forwarding.
The production guard was corrected, both existing interface/concrete fault cases pass, and its removal is mutation M35.

Approximate numeric assertions are intentionally absent: package contracts are exact byte counts, discrete states,
bounded strings, diagnostic identities, and deterministic ordering rather than floating-point estimates. No remaining
high-risk pseudo-mutation gap was identified in the assigned package scope.

# API A+ control

The first-read remainder for current `src` files has one owner:
[source-read-remainder.txt](../source-read-remainder.txt). This document concerns
the separate public API and test-mapping gate.

Generate a fresh packed/reflected public and protected API from the existing
`tools/public-api-baseline/PublicApiBaseline.cs` package gate. The tracked
`docs/api/packed-public-api.txt` is a candidate baseline, not a current manual
review: current declarations such as `IBoundedMessageSerializer`,
`ICopiedEnvelopeBodyLocator`, and `SerializedTransportTextFormat` are absent.

For every current assembly, type, member, signature, and behaviorally relevant
parameter, record the intended Application, Advanced, Provider, Operations, or
Testing layer and the matching architecture requirement. Retain, change, or
remove a member only with feature-preservation and compatibility evidence.
Reconcile member/parameter-to-test and test/requirement-to-member mappings in
both directions, including positive, negative, boundary, failure, cancellation,
concurrency, and lifecycle behavior or a justified nonbehavioral disposition.

Final A+ acceptance also requires the current source-review decisions, full
product coverage and CRAP review, real-provider matrix, independent review,
and remote verification. A Git edit and the PO first-read convention do not
close those gates.

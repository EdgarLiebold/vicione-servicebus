# V5 package 1 donor disposition

Date: 2026-09-03

A disposition of “integrated” means the donor information was reconciled with the current native tree and
either adopted, corrected, or strengthened. The isolated donor regression project was never treated as the
repository's executable architecture.

| Donor commit | Review purpose | Native disposition |
|---|---|---|
| `367cf81` | stable message contract identity | Integrated and strengthened. Name/version parsing is canonical and bounded; attributes are direct; immutable bidirectional lookup rejects value/open-generic types, unknowns, duplicate identities, and conflicting type remaps. Assembly-qualified identity and runtime type fallback are absent. |
| `0a0c4a0` | endpoint transport-QoS ownership | Integrated into real combined-endpoint discovery. Endpoint declarations converge exactly; retained consumer-owned transport QoS is accepted only while truly dedicated and rejected when shared. Legacy consumer concurrency no longer changes transport prefetch/concurrency. |
| `6dcb588` | first-class consumer concurrency | Integrated and connected to the actual consume pipeline. Serial and bounded parallel policies share a consumer-wide owner; typed fixed partitioning has same-key exclusion, different-key progress, hard bounds, cancellation, fault release, drain, conflict, and exact-probe owners. |
| `6ab00c3` | final public/source hardening relevant to these contracts | Reconciled. Public XML documentation and nullable/analyzer contracts are clean, identity/version/allocation bounds are explicit, deterministic error ordering is retained, and analyzer-active builds are warning-free. |

## Corrections and boundaries

- The current `ConfigurationException` hierarchy is preserved for eager QoS startup failures rather than
  reintroducing the donor's standalone base exception.
- Empty QoS declarations participate in endpoint/consumer discovery but do not invent an effective
  transport policy.
- Consumer-definition `ConcurrentMessageLimit` is a consumer execution policy, not a retained consumer-
  owned transport-QoS declaration. The provider-specific Azure assertion was updated to this correct split.
- The management-endpoint overload retains its existing endpoint-specific dynamic limit semantics; the
  ordinary typed consumer extension maps into the new first-class owner.
- FIFO is not inferred from semaphore behavior and is not claimed.
- The V5.1 `AddViciOneMessageContracts` composition convenience and the durable sender's use of serialized
  contract identity remain explicitly assigned to later packages; the catalog owner itself is complete and
  immutable here.

There is no unresolved package-1 donor behavior. The remaining V5 commits are intentionally owned by the
next three V5 packages, followed by V5.1 and API-review integration.

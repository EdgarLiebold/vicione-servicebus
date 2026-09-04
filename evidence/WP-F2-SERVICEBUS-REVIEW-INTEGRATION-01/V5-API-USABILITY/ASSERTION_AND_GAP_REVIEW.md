# V5 API usability assertion and gap review

Date: 2026-09-04

The V5.4 tests were reviewed as behavioral owners rather than API-shape snapshots. The package adds or strengthens
native Core, EF, MessagePack, Architecture, package-consumer, analyzer, and startup-validation observations.

## High-risk observations

| Risk | Direct observation |
|---|---|
| Typed send silently bypasses existing pipeline semantics | Tests observe stable catalog identity, destination, message id, headers, serialized body, envelope metadata, `MessageData` evidence, and admission rejection before persistence. |
| Duplicate contract or durable composition owner wins by registration order | Additive catalog contributions are aggregated; duplicate/missing provider ownership fails at startup. |
| MultiBus state collides in one database | Outbox and durable sender resolve the same bounded identity for one bus and distinct identities for different buses. |
| Pagination loses or repeats equal-time rows | Pages use a strict composite seek key, traverse more than 1,000 rows, reject unsupported tokens, and prove exact-size final pages have no continuation. |
| Operator outcomes collapse missing and invalid-state cases | Requeue/discard assertions distinguish `Succeeded`, `NotFound`, and current-state rejection. |
| EF behavior differs after restart | A file-backed SQLite WAL database is reopened and continues seek traversal and shared-identity access against persisted rows. |
| Package examples accidentally compile through project references | The sample project declares no `ProjectReference`, restores from an isolated freshly packed feed in locked mode, and treats warnings as errors. |
| Internal SPI appears to be the recommended application surface | Packed-assembly architecture tests inspect visibility metadata, while all developer journeys use the typed facade. |
| Public API drift is inferred from source only | A file-based .NET tool loads the assemblies restored from the fresh packages and emits deterministic public/protected type and member signatures plus assembly/package hashes. |

## Gap analysis outcome

Pseudo-mutation review found five initially insufficiently isolated contracts. All were strengthened before acceptance:

1. typed durable send now proves payload admission runs before the persistence boundary and leaves the store empty when
   the payload is rejected;
2. an unsupported continuation-token version is rejected explicitly rather than failing incidentally;
3. an exact-size terminal page proves that `HasMore` is based on a one-row look-ahead rather than page fullness;
4. discard distinguishes a present non-quarantined item from a missing item;
5. EF maps and validates the shared persistence identity through the single `BusPersistenceIdentity.MaximumLength`
   constant, including exact-boundary acceptance and one-over rejection.

The resulting 20-mutation audit kills every selected change for the intended semantic reason. The relevant suites contain
no assertion-free acceptance case, and the strongest tests observe persisted state, side effects, absence of writes,
restart behavior, ordering, exact boundary values, and failure classification rather than only return values.

External broker/cloud crash semantics are deliberately not downgraded into static assertions. They remain separately
named release gates because a local mock or source inspection cannot prove provider durability.

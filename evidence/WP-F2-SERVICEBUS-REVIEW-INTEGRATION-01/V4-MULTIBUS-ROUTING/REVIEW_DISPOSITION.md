# Final V4 donor-tail disposition

Date: 2026-09-03

This disposition covers every V4 commit after the previously integrated bounded-background-work
checkpoint `633cf9726d852826240ee4fd3a9faa679ffe2a39` through final V4 head
`f8050928715e536b60c42d800d1cbb81c085818f`. A disposition of “reconciled” means the review information
was compared against the current native tree and either integrated, corrected, or proven superseded; it
does not mean the donor tree was copied mechanically.

| Donor commit | Review purpose | Native disposition |
|---|---|---|
| `9aa3921` | multibus routing and scoped reliability | Integrated and strengthened. Routing, message-data policy, requests, outbox identity, scheduler/time-zone state, and provider resolution are bus-owned. Exact/inherited/ambiguous/frozen route cases and three-bus isolation have native owners. `BusInstance<TBus>` forwarding was added after regression exposed the donor integration gap. |
| `6ab374c` | bounded RabbitMQ fault redrive | Superseded by the stronger package-7 RabbitMQ implementation and real-provider evidence. No weaker duplicate API or retry owner was reintroduced. |
| `2e2cdb7` | technical identities and public surface | Integrated semantically. Quartz technical commands use `TokenId`; obsolete public implementation contracts are internalized or removed, with source and architecture owners. |
| `564e1a0` | isolated test-harness infrastructure | Integrated and corrected. Shared and provider harnesses live in four engineering-only Testing projects. Observation retention, snapshot timing, null previous-state, and readonly snapshot semantics use the stronger current native owners. |
| `46a93f9` | native verification composition | Reconciled with the current split Unit/Architecture, LocalIntegration, and Engineering profiles. The shipping solution excludes Testing projects; the engineering profile builds all native owners. No second verdict path was created. |
| `8f24ea4` | normalized source layout | Integrated using exact moves where possible. The index contains 710 `R100` moves, no immediately repeated `src` directory component remains, and the gate intentionally scans C# source paths. |
| `300704e` | preserved regression capability | Superseded by the current native Futures, Courier, and terminal capability-ledger owners. The donor's extra regression project would duplicate the current verdict graph and was not transplanted. |
| `648877a` | final dependency graph | Integrated. All 67 projects restore from locked dependencies, stale project references are removed, and shipping/engineering compositions are architecture-tested. |
| `c7e030b` | final ownership and durability audits | Integrated. Scheduler/request owners, cache and endpoint disposal, synchronous assembly scanning, and lifecycle boundaries have native source owners and mutations. |
| `0d90e53` | repository text formatting | Reconciled without replacing the stronger current `.editorconfig`. Safe touched-file whitespace was normalized. Four trailing-space-only donor edits in a historically nonconforming benchmark file were intentionally not retained because they would pull an unrelated whole-file rewrite into this package. The full 228-file package scope passes format verification. |
| `f805092` | architecture and release gates | Re-expressed in current native architecture tests and this hash-bound evidence. Donor release documents and TODO state were not copied because the current repository and architecture ledger are authoritative. |

## Completeness decisions

- The V4 bundle itself is immutable and still hashes to
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`.
- The donor regression-project content was not silently dropped: every TestFramework capability has a
  terminal executing/retired disposition in the 147-row ledger, and architecture tests bind its count,
  hash, native-owner existence, and absence of the obsolete project.
- Existing tests were removed only where the ledger proves a complete stronger native replacement.
- All V4 tail changes affecting behavior have direct tests and mutation evidence. Pure exact moves and
  dependency/solution composition have architecture and build gates.
- V5 durable sender admission, V5.1 provider hardening, and the independent API usability report are not
  claimed by V4; they remain the next active goal phases.

The final V4 tail is fully dispositioned. There is no unresolved V4 donor commit or undocumented copied
change.

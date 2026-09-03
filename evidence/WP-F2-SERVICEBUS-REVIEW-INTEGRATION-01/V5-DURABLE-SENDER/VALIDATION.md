# V5 durable sender validation

Date: 2026-09-03

## Bound inputs and scope

- Package: V5 integration 2/4, including the coherent V5.1 RT-002 through RT-004 corrections.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-13` at local architecture
  commit `f51a5e96`.
- Product baseline: `ee4edfe8f4d780e30bfbe31d25a8d1fe3a78db24`, tree
  `e56479c08bccddab408b72c94b84f8c1f6b31b4a`.
- Protected review aggregate: `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`.
- V5 cumulative patch: `88f2c61a3b2fc470f525ce68a3972463e731bea418cfa9de0d58f4e745a1a344`.
- V5 bundle: `af76f8f4266efc7aa6d2bb34d29b30c0b73bb04dee7cb8237e4ab42717e1fdf5`.
- V5.1 delta: `fac6328e141fd71daf01db0efe9987a79a5d561e8929ab209c4e70b9dbe7562c`.
- V5.1 findings closure: `798ba64f234a096f9cfea35aec24ac5bdd243a34f9c52722ba45a54712b342a6`.
- Frozen semantic sources: durable sender `f1dba410`, crash-safe completion `c0a60009`, and final
  hardening `6ab00c32`.
- `review/**` remained read-only, unmodified, untracked, and unstaged throughout.

The donor is semantic evidence rather than an executable verdict path. Its product mechanisms were adapted to the
current generic-bus, dependency-injection, diagnostics, native xUnit 4, MTP v2, requirements-projection, and
architecture conventions.

## Durable contract and admission boundary

The public Abstractions SPI now owns the immutable serialized intent, nonempty durable id and lease token, explicit
completion and failure classifications, bounded operation contracts, payload-free quarantine evidence, store snapshot,
dispatcher, sender, and operator contracts. Persisted identity uses `MessageContractIdentity`; no CLR type name is part
of the record contract.

`SerializedDurableSend.StorageSize` means logical retained content bytes: body plus ServiceBus infrastructure metadata.
Address and content-type text are bounded, control characters are rejected, and a zero-byte body remains valid because
the independent record-count limit prevents unbounded zero-byte admission.

The application composes exactly one eager immutable message-contract catalog. `DurableSender<TBus>` validates a stable
identity against that catalog before calling the store, closing V5.1 RT-002. Admission uses the host `TimeProvider`,
returns only after the store boundary, preserves exact idempotence, and rejects a reused id carrying any different
immutable intent.

## Store and delivery state machines

The internal in-memory owner provides a deterministic, thread-safe semantic implementation for native and InMemory
transport execution. Count and byte admission are atomic; caller-owned buffers are copied before success. Claims are
bounded, ordered, leased, and takeover-safe. Retry due times, failure evidence, quarantine, requeue, discard, and capacity
release have explicit transitions. Lease tokens fence competing workers; generation tokens fence stale completion
capabilities across discard and re-admission.

The delivery service claims no more than its immediately available concurrency. Transport acceptance retires the intent;
volatile transports remain retained until consumer completion. Early completion, overlapping ambiguous dispatch failure,
timeout, late completion, retry exhaustion, unsupported completion modes, and post-dispatch state-persistence failure all
have distinct fail-safe outcomes. Only provider-classified transient failures retry; permanent, unknown, invalid, or
classifier-failure outcomes never become guessed-transient work. Retry jitter is deterministic per id and attempt but
remains decorrelated at the configured ceiling.

## Real InMemory and EF execution

The InMemory adapter copies the already serialized body into the real endpoint pipeline and carries its completion
capability only as process-local context. It never serializes durable control data and never claims broker-durable
transport acceptance. Completion runs after the existing receive-owned logical completion boundary and only after a
successful pipeline; consumer failure leaves the producer intent retained for recovery.

The EF store uses serializable transactions, a conditional count-plus-byte ledger update, compare-and-set leases,
generation fencing, server-side snapshot aggregation, and server-side missing-ledger reconstruction. Quarantine remains
capacity-owned until explicit discard. The model has bounded persisted text and the required claim/lease indexes.

V5.1 RT-003 is integrated as a fail-closed provider/session preflight before the first store query. SQL Server rejects
`DELAYED_DURABILITY=FORCED`; PostgreSQL requires `fsync=on` and a local-WAL-flush synchronous-commit mode; SQLite requires
WAL FULL/EXTRA or persistent rollback-journal EXTRA. Unknown or unconfigured providers fail with the public
`ConfigurationException` boundary unless the host supplies a provider-certified validator. A real file-backed SQLite WAL
session proves query behavior and connection ownership; the full store lifecycle, concurrency, recovery, idempotence,
lease and generation fencing execute against SQLite.

This local package does not relabel static SQL Server/PostgreSQL mode matrices as real-provider crash evidence. The
V5.1 release gates requiring real SQL Server/Azure SQL, PostgreSQL HA, and abrupt process/power-loss trials remain
release-environment evidence, consistent with the PO decision to finish local implementation before cloud pipelines.

## Native execution evidence

All Release applications were invoked directly because the restricted sandbox denies the named-pipe server used by
`dotnet test`. Every application used its exact nonzero floor, strict zero-test policy, serial in-process execution, and a
15-minute timeout.

| Scope | Result |
|---|---:|
| Abstractions | 328/328 passed, 0 skipped |
| Core | 1,669/1,669 passed, 0 skipped |
| EF integration unit owner | 119/119 passed, 0 skipped |
| Architecture | 171/171 passed, 0 skipped |
| Other 19 Unit executables | 1,115/1,115 passed, 0 skipped |
| Complete Unit/Architecture aggregate | 3,402/3,402 passed, 0 skipped |
| Shipping Release build | 0 warnings, 0 errors |
| Engineering Release build | 0 warnings, 0 errors |

Key final CTRF SHA-256 values are:

```text
d100065a610d86893a0c1d44ecaac7429b3a04b9c79f0b43f090d635e6669b88  Abstractions
673b7e356bfd5e4eee6f02046fb357979085c93081057b1f1f0d711f4c43be23  Core
f09d01783e8b3b68e045918a53d8807de5d440b33b43f7940b4b84ddd00f4852  EF
0a8e25658e7cc9290744142d9c6284189eaf1e4f41cec253cdfe92373a664939  Architecture
```

## Quality and hygiene gates

- 57 package tests contain 335 assertion calls, no assertion-free or trivial-only case, and exercise equality,
  boolean, null, exception, type, string, collection, comparison, negative, state/side-effect, and structural contracts.
- 33 independent buildable behavioral product mutations and one buildable structural ordering sabotage are causally
  rejected; every mutant was restored before positive execution.
- The mutation audit found and closed initially correlated EF count/byte assertions, plus missing provider-level
  metadata-intent-conflict and stale-lease assertions.
- all 57 changed/new C# paths pass scoped `dotnet format --verify-no-changes`;
- all requirements JSON files parse and their metadata projections pass in the complete native run;
- the protected review manifest and independently recorded V5/V5.1 hashes pass;
- no protected review file is staged or modified, no non-build empty directory or repeated `src/src` path remains;
- `git diff --check` passes.

V5 package 2/4 is locally validation-complete and ready for its stationary product/architecture freeze. No remote
publication is authorized or implied.

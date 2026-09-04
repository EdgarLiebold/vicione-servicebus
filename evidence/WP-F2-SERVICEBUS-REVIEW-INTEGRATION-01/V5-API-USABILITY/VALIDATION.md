# V5 API usability validation

Date: 2026-09-04

## Bound inputs and scope

- Package: V5 integration 4/4, API usability, developer journeys, and final V5 reconciliation.
- Architecture assignment: `PO-2026-09-03-SERVICEBUS-REVIEW-INTEGRATION-04` at local architecture commit
  `ae3d37c4`.
- Product baseline: `4137a679642ab098a63836c79103d6fb9e7c2acf`, tree
  `511dfdaa69c2e5c129b4bc9cf4c93724f66e1686`.
- Protected API review SHA-256:
  `d6b0e2732051d96f1b161b89d4b5ea0aa19f60a13f16f3379653950e7162e43a`.
- Protected review manifest SHA-256:
  `0857f4f72a0aba2020de25acaf77948bbbd219b9681cfc057baa81fb4e35ee8b`.
- `review/**` remained read-only, unmodified, untracked, and unstaged throughout.

The review's original 6.4/10 verdict was based on an intentionally incomplete, unbuilt V4+V5 materialization. This
closure verifies the current merged source and package graph directly. All three P1 findings, all five P2 findings, the
actionable P3 consistency item, and all eight verification unknowns now have explicit product, test, package, or release-
gate dispositions. Optional API-010 and API-011 remain V6 opportunities rather than falsely claimed defects.

## Application surface

`IDurableSender<TBus>` now accepts a typed message and standard send options. The implementation builds a real
`MessageSendContext`, resolves the application contract catalog and endpoint routing, assigns deterministic envelope
identity and metadata, executes the configured serializer and `MessageData` path, enforces payload admission before any
store write, and returns a typed receipt. The raw serialized admission contract remains available to store/provider
implementers but is hidden from ordinary IntelliSense as SPI.

`UseDurableSender` is the single high-level composition flow. Provider ownership, required services, option bounds, and
duplicate/missing composition are validated at startup. Contract catalogs are additive across independent registration
sites rather than dependent on last-registration wins.

The operator surface uses typed transition results and a bounded, deterministic seek-page contract. Continuation tokens
are opaque, URL-safe, explicitly versioned, and rejected when invalid or unsupported. Ordering is
`QuarantinedAt` descending then durable-send id ascending; the store reads one row beyond the requested page to establish
continuation without confusing an exact-size final page with more data.

## Persistence and MultiBus

`BusPersistenceIdentity<TBus>` is the sole bounded persistence identity for a bus instance. EF outbox and durable-send
storage consume the same identity, removing feature-local defaults and protecting MultiBus isolation. The public and EF
model bounds share one 128-character constant; native tests accept exactly 128 and reject 129.

The EF owner verifies seek pagination, tie-breaking, typed operations, shared identity, and restart behavior against a
real file-backed SQLite WAL database. This is meaningful persistent-provider evidence but is not relabeled as SQL
Server, PostgreSQL, Azure, AWS, RabbitMQ, or ActiveMQ crash certification.

## Package and API proof

The retained `tools/ci/verify_developer_journeys.sh` gate packs eight current ViciOne packages into an isolated temporary
feed. The locked package-only consumer has no project references and compiles fourteen representative application
journeys with warnings as errors. A retained file-based .NET tool then inventories public/protected API from the restored
`lib/net10.0` assemblies rather than from source assumptions.

The baseline contains 20,659 lines and 2,619,542 bytes with SHA-256
`188dd228f462707da25bef8ce0953be2a0356c7cc1d97e862306701006c950a5`. The complete Shipping pack produced 19
packages; their hashes are bound in `PACKAGE_SHA256.txt`.

## Native execution evidence

All final Release tests use Microsoft.Testing.Platform with strict zero-test policy and serial execution. The initial
positional Solution invocation was correctly rejected because it selected zero tests; the final command uses the
required .NET 10 `--solution` form and a floor of 3,477.

| Scope | Result |
|---|---:|
| Core focused owner | 1,726/1,726 passed, 0 skipped |
| EF focused owner | 121/121 passed, 0 skipped |
| MessagePack focused owner | 58/58 passed, 0 skipped |
| Architecture final | 178/178 passed, 0 skipped |
| Complete Unit/Architecture solution | 3,477/3,477 passed, 0 skipped |
| Unit solution Release build | 0 warnings, 0 errors |
| Engineering solution Release build | 0 warnings, 0 errors |
| Shipping solution Release build | 0 warnings, 0 errors |
| Shipping pack | 19 packages, 0 warnings, 0 errors |
| Package-only developer journeys | 14/14 compiled, warnings treated as errors |

Focused CTRF SHA-256 values:

```text
a06cbe78c44848185f23e81d6f7287ebfee6208d6fbf7c338bce7818732a21ae  Core
ac809f4a634cb3518447f1ecf752288136f23cb624c33ff8b541292a73b59956  EF
c981a37c2a08d22da3e380eba109032dbde5b020a7a9b288dc618e81a9745c13  MessagePack
a2fee0585d03c4b7ede35d58e62527a1dd1e0282cc551d6a425769fb150fcf78  Architecture
```

## Quality and hygiene gates

- 20/20 independent buildable behavioral mutations are causally killed and every mutant is restored;
- the mutation/gap audit found and closed five previously under-isolated contracts before acceptance;
- the packed assembly API baseline prevents source-only or project-reference-only API claims;
- all 81 changed/new C# paths pass scoped `dotnet format --verify-no-changes`;
- provider-capability, requirement, journey lock, and baseline-tool lock JSON files parse;
- the protected 23-file review manifest passes and no review file is modified or staged;
- no legacy `build/verification` path remains; the retained gate lives under `tools/ci`;
- `git diff --check` passes.

## External acceptance boundary

The local host has no available Docker daemon and no configured real cloud/broker acceptance environment. U08 runtime,
failover, abrupt-process-loss, and provider durability certification is therefore **not executed** and remains an
explicit release-environment gate. All locally actionable API review findings and verification gaps are closed without
turning static or emulator evidence into a false provider-runtime claim.

V5 package 4/4 and the complete local review integration are validation-complete and ready for the stationary
product/architecture freeze. No remote publication is authorized or implied.

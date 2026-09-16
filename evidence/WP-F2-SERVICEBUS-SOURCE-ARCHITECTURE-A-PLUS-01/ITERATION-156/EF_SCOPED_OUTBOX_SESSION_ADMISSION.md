# Iteration 156 — EF scoped-outbox session admission

## Result

This packet personally reads the seven EF scoped-session selection files (267 lines) and both
owning test files (512 lines). The implementation already preserves one session per bus,
DbContext and DI scope, selects an explicit default deterministically and retains consume-context
semantics for both reliable and transactional persistence. No functional product correction was
required. The two empty generic types are retained as compile-time registration/notification
identities and now state that purpose explicitly instead of appearing to be placeholders.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 7 / 267 | `b5369eef39cb26248cf5cde960e0ab799ca6ed8ee614cf972343b3ffc9226e89` | `bc4c6b286d9c9c4de141892065a102821638c3c2f4f8ce866bc132fb98840f0b` |
| Tests | 2 / 512 | `387166f0951af52a1ee818eaf178c6db27dd4184bfb0b8ada6766dc580dab75e` | `edf007e2d4b784d6908a6ab28f448de914b3f0f44904829dbb5d13fe30b1eced` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 155 source/test chains with the corresponding manifest
hash. Cumulative personal source admission is 272/4,116 current C# files.

## Proof

Seven new tests cover null and empty selector inputs, exact factory metadata, sequential and
16-way concurrent session reuse, reliable and transactional registry paths, scoped alias identity,
and both same-bus/global ambient consume-context branches. Requirement projection entries bind
every new method. Assertions observe type, reference identity, metadata and exact diagnostics;
none is assertion-free, trivial-only or self-referential.

Six compiled single-cause mutants were killed and restored: remove the registry cache hit, remove
the empty-registration diagnostic, invert each reliable/transactional same-bus context branch,
discard the default flag and invert the global transactional consume-context branch.

Final instrumentation covers 125/125 executable owner lines and 22/22 branches (100% each) across
13 methods. Maximum CRAP is 10 and none exceeds 30. Unit sorted-name SHA-256 is
`196d59a69bb71920a9f0206653796572d9f5173aaac1fd165151c9960da9a768`; Cobertura SHA-256 is
`76838ba194600454c1ba949f21ac8acee115ba187351fcd625978796179804b3`.

| Gate | Result |
| --- | --- |
| Focused owners | 16/16 passed |
| Full EF unit | 200/200 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0; no changes |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race in
one batching integration test; the complete serialized gate is green. Raw artifacts remain under
`/private/tmp/vsb-iteration156-*`; protected trees were not read or modified. Intended tag:
`servicebus-a-plus-iteration-156-ef-scoped-outbox-session-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

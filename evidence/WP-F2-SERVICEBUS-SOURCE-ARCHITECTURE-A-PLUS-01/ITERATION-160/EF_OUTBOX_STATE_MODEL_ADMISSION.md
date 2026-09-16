# Iteration 160 — EF outbox state-model admission

## Result

This packet personally reads all eight EF inbox/outbox state, failure, quarantine and message-
factory files (409 lines) and five concrete owning test files (713 lines). The retained state model
is intentionally persistence-oriented and provider-neutral. The factory boundary now rejects an
empty message identity and every orphaned, partial, empty or ambiguous persistence-owner
combination before serialization. Valid receive-side messages require both inbox identifiers;
valid transactional messages require one nonempty outbox identifier; the two owner forms are
mutually exclusive. Both production callers already satisfy this invariant.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 8 / 409 | `739884024f45b93126fe6f6bc089ad724984ccc0e7012b3ac0c6205bcb047693` | `0b554c646744bf5b76bfaf8eaeeb26755a6086b6d8f4f51b1943377af874fb95` |
| Tests | 5 / 713 | `07a2026a07d23e6b6d916418fe0c889d68d6d0636d958b2596b210d94c00b868` | `b136c2849576bb1dd22f627989cd4441cece700b4c9e991c16f5ba51c6903b32` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes extend the Iteration 159 source/test chains with the corresponding manifest
hash. The source manifest contains:

| File | Content SHA-256 |
| --- | --- |
| `InboxState.cs` | `e9833249ffe40963dc41d013c8521cb85291da43766c3e1baff249bb9256f387` |
| `OutboxDeliveryStatus.cs` | `48af41f123dd4eb455c5aed9bacdc566b687c7a7624b67c7566e2a1a52e2bf36` |
| `OutboxFailureCode.cs` | `c1039601911a49d49f9117b009014559d10b559ba0782d7fbe02f4e399c64191` |
| `OutboxFailureKind.cs` | `67b1962bd9ddcebb7f0d1a15659664b16070e68d4bb39a32dd34bbc2858abb61` |
| `OutboxMessage.cs` | `9508b4d070161d52b7adf110b595ff0afdf261604f071bbc900fee3e8d319efb` |
| `OutboxMessageFactory.cs` | `49cbb1101e744375edcc52e20730e0bc1361cd8bd10b2c5a0ee978b0b3e5ee0a` |
| `OutboxQuarantineEntry.cs` | `468c3b94c13ef2f752538813618de7de6122dcbd1be41542dc4b66ac7249121d` |
| `OutboxState.cs` | `a6b12883c5025f2215213af6b2473b6c98f0bc63054945911e5e274ae05e3e6a` |

The test manifest contains `EntityFrameworkTimeProviderTests.cs`,
`EntityFrameworkOutboxOperationsTests.cs`, `MessagePackOutboxReplayTests.cs`,
`OutboxMessageFactoryTests.cs` and `OutboxMessageTests.cs` at content hashes
`bcddbd9dcc1f4cda31350ba2dd17783fa9609bf7b072ee74fc1d2ab2ff8feeec`,
`638f235da49f1dc11a2c819a97333be6bda7d5b29cd0a07eeb6ac58660a6c686`,
`b9e3ce1e0f4e788b5616ea7b2cdca8138b14dda87562e1cddb9e80dddbefe286`,
`98437298ef1ead328f1cd0c600e9754dbe05094add18dd22cbf9e9eb47f573ed` and
`193cdf169998adc364fde101baf00aa129d7c8f9e972b226b48b19446627802c`.
Cumulative personal source admission is 286/4,116 current C# files.

## Proof

Four focused cases cover missing collaborators, null and empty message identity, missing owner,
partial inbox owner, empty identifiers, dual ownership, valid inbox ownership, valid transactional
ownership and the complete envelope/header/transport-property/timestamp projection. Four new
requirement-projection entries bind every new method. Assertions observe exact exception types,
owner columns, identifiers, addresses, content metadata, serialized body, headers, properties and
derived times; none is assertion-free, trivial-only or self-referential.

Six compiled single-cause mutants were killed and restored: accept an empty message identity,
accept an empty inbox-message identifier, accept an empty inbox-consumer identifier, accept an
empty transactional-outbox identifier, invert the exclusive-owner decision and bypass owner
validation entirely.

Final focused Cobertura is `/private/tmp/vicione-servicebus-iteration-160-final.cobertura.xml`,
SHA-256 `c5a6aaea5c857f7c0934377edbfb2196379b587be7860d12835f160f3760b979`.
`OutboxMessageFactory` has 100% line and 86.84% branch coverage; `ValidateOwner` has 100% line and
branch coverage. Residual generated branches are optional envelope-field and nullable-time
lowering plus the non-transport send-context arm; no owner or identity decision is uncovered.
Maximum owner-method CRAP is 20 and none exceeds 30. Unit sorted-display-name SHA-256 is
`f11ac8e4336a2329aaad67c5fdedade4f791a1043142248e6157c3091355845a`.

| Gate | Result |
| --- | --- |
| Focused owner | 4/4 passed |
| Full EF unit | 234/234 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0 |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 6/6 killed |

Core remains serialized because Iteration 155 demonstrated a pre-existing parallel-suite race.
Protected `review/**`, `TestResults/**` and `vicione-legacy/**` trees were not enumerated, read or
modified. Intended tag:
`servicebus-a-plus-iteration-160-ef-outbox-state-model-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

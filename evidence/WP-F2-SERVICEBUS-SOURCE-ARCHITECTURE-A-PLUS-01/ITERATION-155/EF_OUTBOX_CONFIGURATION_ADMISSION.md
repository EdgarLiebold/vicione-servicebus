# Iteration 155 — EF outbox configuration admission

## Result

This packet personally reads all six EF outbox configuration/public-contract product files
(743 lines) and five owning configuration/context-selection test files (964 lines). The retained
configuration is fail-closed and needs no functional product correction. Eight new test cases bind
the previously implicit persistence boundaries, one-time enablement, bus/DbContext uniqueness and
the typed MultiBus default-selection contract. The only product edit removes one redundant blank
line from the public extension file.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 6 / 743 | `8a0af6c3999f1eebcdbb6f955ed67d11c72147337b615b159c31464fbffc289c` | `a5c56d7af95130d1274140f1a187af77e3204d0e5dbe975a41a2eaf879b15c50` |
| Tests | 5 / 964 | `324638c3fc9ba21e32460de72b15c17546b41be4d8652f44414ee37dbd7cced0` | `c15cf654575e35c7a3c6265a4b2de6ebba3387152f44466a59de34ce1e0ef5cd` |

Manifest hashes cover the ordinally sorted `path<TAB>content-sha256` records with a terminal
newline. Chain hashes cover the Iteration 154 chain hash followed by this manifest hash, each with
a terminal newline. Cumulative personal source admission is 265/4,116 current C# files.

## Proof

The new cases prove all four invalid persistence settings at registration, reject a second
transactional-outbox enablement, reject the same bus/DbContext pair twice, propagate one explicit
default through the typed MultiBus extension and reject two explicit defaults. Requirement
projection entries bind every new method. Each test has outcome assertions at the public
configuration boundary; none is assertion-free, trivial-only or self-referential.

Four compiled single-cause mutants were killed and restored:

1. accept a zero duplicate-detection window;
2. remove the one-time transactional-outbox guard;
3. remove the bus/DbContext uniqueness guard;
4. discard the explicit-default flag.

Final instrumentation covers 383/390 executable owner lines (98.2051%) and 95/108 branches
(87.9630%) across 53 methods. Maximum CRAP is 24 and none exceeds 30. The seven unexecuted lines
are the two scoped DI forwarding delegates and the receive-endpoint observer connection path;
their retained behavior is composition glue, not an untested state or validation branch.

Unit sorted-name SHA-256 is
`78a6113cc5cbf415a632be2d41c3862702900a509d61b47d0efcab85e6af89f8`; Cobertura SHA-256 is
`4735abe7be7e11498f18fa2ce9bbeec7e5e1cbe1ea8f46bdaa020d3ab55d4665`.

| Gate | Result |
| --- | --- |
| Focused configuration owner | 28/28 passed |
| Full EF unit | 193/193 passed |
| Strict product/unit/local builds | 0 warnings, 0 errors |
| Product/unit/local format | Exit 0; no changes |
| Core | 4,799/4,799 passed with suite parallelism disabled |
| Unit requirement projection | 1/1 passed |
| Local requirement projection | 1/1 passed |
| Mutation probes | 4/4 killed |

Two parallel Core attempts each exposed the same pre-existing batching suite-interference failure
in `DuplicateMessageId_ProducesOneSingleItemBatchAsync` (4,798/4,799); the isolated test passed
1/1 and the complete serial run passed 4,799/4,799. No Core product or test file was changed.

Raw artifacts remain under `/private/tmp/vsb-iteration155-*`; protected trees were not read or
modified. Intended tag:
`servicebus-a-plus-iteration-155-ef-outbox-configuration-admission-2026-09-16`.

Whole-fork personal reading, global API/naming/coverage and configured external-provider acceptance
remain open.

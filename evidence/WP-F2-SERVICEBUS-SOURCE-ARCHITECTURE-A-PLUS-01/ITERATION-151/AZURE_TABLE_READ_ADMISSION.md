# Iteration 151 — Azure Table read admission

## Scope and result

This bounded packet personally rereads all current C# files/comments in
`ViciOne.ServiceBus.Azure.Table`, both owning test projects, their fixture and requirement
projections: 37 product files / 2,585 lines and 16 test/support files / 4,263 lines.

No current correctness, lifetime, cancellation, naming, placement, dependency, comment or
public-API defect was reproduced. Product and C# test code are unchanged. The only implementation
change adds the centrally pinned `Microsoft.Testing.Extensions.CodeCoverage` package to the unit
owner and records its deterministic direct/transitive lock entries.

| Admission | Files / lines | Manifest SHA-256 | Chain SHA-256 |
| --- | ---: | --- | --- |
| Source | 37 / 2,585 | `5fddbdbc2708e4d06f2146b343ec6bf00f14c39ac5390c24c239e2c028535571` | `1ec77d5396ff7e18028267f8a8d8d9271d479f03dcd8d059b4195e98a601b13e` |
| Owning test/support | 16 / 4,263 | `e7ec3f07399130dfb4e574da81239cb549d017371e2891a161cc2012dce438c4` | `9e67955dec032c0e49e6e3baddd8da304a8330c275856b86b6f249dadcfd8509` |

The chains extend Iteration-150 hashes
`6df76f6b74131c694e90e31751d5b081c33dc3b7815a88e7a7b87b9dcbcf2165` and
`f2fdad3eef2b47a8874c6c369371c890196e0be9a89ebfc8d6356631bfd01f37`.
Cumulative current personal source admission is 227 of 4,116 current C# files.

## Review and external specification check

Personal review covers composition and DI, key/table validation, saga lifecycle and exact ETag
concurrency mapping, caller cancellation, entity conversion and property limits, atomic bounded
journal repair, retention/capacity/foreign-row ownership, plus real-provider Saga, Future, Courier
and Job Service contracts. No shallow, assertion-free, unawaited, skipped, wall-clock-dependent or
swallowed-exception owner test was found.

The 1,024-character PartitionKey/RowKey boundary was checked against current Microsoft Table
Storage primary documentation; it explicitly specifies characters, not KiB. The current validator
and Unicode boundary assertion are therefore preserved.

## Pairing, coverage and risk

The isolated analyzer input `/private/tmp/vsb-iteration151-pairing.IpDcKs` contains exactly 37
source and 16 test files. It reports 33 direct name pairs. The four name-unpaired internal files —
the key/name validators and property type/value validators — are exercised indirectly by the key,
composition and entity-conversion owners.

Fresh instrumentation covers 1,456/1,788 lines (81.4318%) and 408/528 branches (77.2727%) across
156 methods, package complexity 292. Fifty-four methods are below 80%, largely defensive,
registration and provider-only paths. No method exceeds CRAP 30; the highest CRAP values remain
fully or near-fully covered. No risk-driven product/test change is justified by this packet.

## Provider boundary and validation

The local requirement projection passes. All 26 real Azure Table/Azurite cases stop in the
canonical fixture before product behavior because profile `UnitArchitecture` has no configured
endpoint or credentials. Values were not fabricated or committed; provider acceptance remains
explicitly open.

| Gate | Result | Duration |
| --- | --- | ---: |
| Unit baseline | 69 passed; 0 failed/skipped/other | 0.636 s |
| Unit final coverage | 69 passed; 0 failed/skipped/other | 1.379 s |
| Local requirement projection | 1 passed | — |
| Local provider attempts | 26 blocked by missing profile before product behavior | — |
| Strict product Release build | 0 warnings / 0 errors | 110.55 s |
| Strict unit-owner Release build | 0 warnings / 0 errors | 91.80 s |
| Product/unit/local format verification (`warn`) | Exit 0, no differences | — |
| Native Core gate | 4,799 passed; 0 failed/skipped/other | 26.236 s |

Final unit sorted-name SHA-256 is
`80a11102d62581b41c56fb4baabf03119856205942ebc4c3a3ddeb8901549373`. Coverage CTRF and
Cobertura hashes are `a446a67c0d66cb77fd16b8fa2cf62f699548e4d2b4a2f58bf16da56e77889c8c`
and `df461b974b3d2e7b8ead939c89a6624df5747aca0dcf98a01660f545afd5544a`.
Core CTRF is `009bee84941d82c8f28bbca52c26d8a9537089fa725eb00f0c4614b5e3e6036c`;
its sorted-name hash remains
`6f8c418b9a75c807825e4758f8cc5dd92e80b16cb657b27cfe32365c249b09f5`.
Final project/lock hashes are `5d6a6ab3ae7cbfb0aba7dc252a03c4e510db592e39a13f73c892d67c213071f6`
and `59b77f202bdc7816e25ffa7e2e280dd2f1c0a6cb79d9c6e3d0a6a7d3cb1e7118`.

## Checkpoint

Raw artifacts remain under `/private/tmp/vsb-iteration151-*`. Protected review, TestResults and
legacy trees were not modified or used as evidence. Intended tag:
`servicebus-a-plus-iteration-151-azure-table-read-admission-2026-09-16`.
External publication remains subject to destination- and payload-specific authorization.

Whole-fork admission, global API/naming/coverage checks and configured real-provider acceptance
remain open. This packet proves only the current Azure Table owner and its fresh local evidence.

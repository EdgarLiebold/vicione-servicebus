# Roslyn API and XML-comment inventory, 30 September 2026

This is a dated inventory, not an A+ certification. The [complete JSON
snapshot](roslyn-all-repos-api-2026-09-30-relative.tar.gz) contains the public and
protected symbols, their signatures, documentation IDs, source locations,
own XML comments, and diagnostics for every discovered `src/**/*.csproj`
under the ten ViciOne repositories. Its SHA-256 is
`1cc6b90e333928579e57ee70087017c512b21a8aec7245886c8e1121bf682702`.
All 31,701 absolute workspace-path occurrences in the original JSON were
replaced with paths relative to the repositories root (`./`); all 70 JSON
documents still parse. The original unmodified snapshot is preserved under
this archive's former name in Git tag `archive/servicebus-pre-review-cleanup-20260930`.
The source tool and exact project list are
[`RoslynApiCommentInventory.cs`](../../tools/api-conventions/RoslynApiCommentInventory.cs)
and [`roslyn-api-projects.txt`](../../tools/api-conventions/roslyn-api-projects.txt).

The tool accepted all 69 manifest entries and emitted 70 JSON files: one
summary and one per project. It observed 5,824 Roslyn documents, 30,339
exposed symbols and 1,519 explicit symbols with no own XML comment. Nineteen
projects have at least one compiler or workspace diagnostic, so the command
returned exit code 5. Their API surfaces cannot be certified from this run.

| Repository | Projects | Flagged | Symbols | Missing own XML |
| --- | ---: | ---: | ---: | ---: |
| Blazor Components | 3 | 2 | 1,474 | 15 |
| HostManagement | 15 | 12 | 3,317 | 12 |
| Journal | 1 | 0 | 92 | 0 |
| Licensing | 5 | 0 | 2,498 | 65 |
| Licensing Update Service | 5 | 0 | 3,487 | 1,021 |
| Localization | 1 | 0 | 416 | 0 |
| Monochrome Icons | 4 | 3 | 307 | 237 |
| ServiceBus | 33 | 1 | 18,569 | 0 |
| SystemMonitoring | 1 | 1 | 179 | 169 |
| UI Design | 1 | 0 | 0 | 0 |

The only flagged ServiceBus project is
`ViciOne.ServiceBus.Analyzers.CodeFixes`. Roslyn reported a project reference
without a matching metadata reference to `ViciOne.ServiceBus.Analyzers`, but
reported zero compiler errors. A separate Release `dotnet build --no-restore`
of CodeFixes passed with zero warnings and zero errors. The other 32
ServiceBus projects had neither compiler nor workspace diagnostics in this
inventory. No explicit exposed ServiceBus symbol lacked its own XML comment.
Because CodeFixes is flagged, its found symbols do not prove that its
compiled API surface is complete. A clean build does not clear the separate
Roslyn workspace diagnostic under this tool's gate.

The Blazor Components project has 399 Roslyn compiler diagnostics, including
duplicate generated type descriptors. A separate Debug build stopped earlier
in Sass and TypeScript tooling because local `node_modules/three-dots` and
the `DotNet` TypeScript namespace were unavailable. That build neither
confirms nor disproves the downstream Roslyn compiler diagnostics. The
HostManagement and SystemMonitoring diagnostics include unresolved symbols;
they likewise require repository-specific build and source review before
classification. Missing-own-XML counts include generated and record members
and do not by themselves prove deficient documentation.

This inventory checks presence, not whether comments accurately describe
runtime behavior. The earlier ServiceBus product-wide test checkpoint was
`490dde6d6` (its measurement packet is preserved in Git tag
`archive/servicebus-pre-review-cleanup-20260930` at
`.testagent/coverage-a-plus-20260921/product-wide-profile-490dde6d6.md`):
14,138 passing test executions, 92.39550% line coverage, 85.12921%
conservative branch coverage and zero CRAP scores above 30. Its independent
Red Team review validated the measurement and found no concrete P1/P2
evidence defect; one GC-sensitive test remains a reliability follow-up.
The [current review status](../quality-status.md) records the later T176
measurement and open SQL Server reliability finding. Neither checkpoint
establishes that the product is free of every defect.

Snapshot repository heads: Blazor `dc61f8aa6882`, HostManagement
`b18e8b7fc194`, Journal `e06ee49a5fef`, Licensing `32855fd9d24e`,
Licensing Update Service `1e21ba1babf7`, Localization `c3f6f5eedecb`,
Monochrome Icons `597dbe9f6cdd`, ServiceBus `359162bd59d0`,
SystemMonitoring `f1d353a41eae`, UI Design `11445731b031`. The inventory
also includes any uncommitted source changes present when it ran. Normalizing
paths does not reconstruct those source bytes or provide per-source hashes:
the archive hash identifies this particular inventory, not a reproducible
source commit across all repositories. Regenerate the inventory against clean
repository commits for a current cross-repository API verdict.

An independent read-only Red Team review checked all 69 project JSON totals
against the summary and accepted this as a provisional triage inventory. It
rejected an A+ conclusion because XML presence does not establish comment
correctness or API compatibility, CodeFixes remains flagged by the tool, and
the archive has no per-source-file hash manifest. Those limits remain open.

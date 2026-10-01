# Roslyn API and XML-comment inventory, 1 October 2026

This is a dated cross-repository inventory, not an A+ certification. The
[complete snapshot](roslyn-all-repos-api-2026-10-01-relative.tar.gz) contains
the public and protected symbols, signatures, documentation IDs, source
locations, own XML comments, and diagnostics for all 69 discovered
`src/**/*.csproj` projects in the ten ViciOne repositories. SHA-256:
`83d0ccacabdf7bbb74cbe462e900420d435081351084d66ace7794643cd856d6`.

The archive contains 70 Roslyn JSON documents and `source-manifest.json`.
The manifest records SHA-256 hashes for all 7,007 files under the ten `src`
trees outside generated `bin`, `obj`, and `artifacts` directories, each
repository HEAD, and whether `src` had uncommitted changes.
The file paths and hashes were checked again after the inventory; no source
file was added, removed, or changed during the run. All 31,515 occurrences
of the absolute repositories-root path in the Roslyn JSON were replaced by
relative `./` paths. The 71 JSON documents parse after normalization.
The exact project manifest and generator are
[`roslyn-api-projects.txt`](../../tools/api-conventions/roslyn-api-projects.txt)
and [`RoslynApiCommentInventory.cs`](../../tools/api-conventions/RoslynApiCommentInventory.cs).

Roslyn observed 5,828 project documents and 30,339 exposed symbols. Of those,
1,519 explicit symbols have no own XML comment. Sixteen projects have at least
one compiler or workspace diagnostic; the inventory command exited with code
5. These diagnostics prevent a complete API verdict for the affected projects.
The compiler-error totals depend on the local workspace and resolved references,
so a change in flagged-project count from an older inventory is not evidence of
a product-code improvement.

| Repository | Projects | Flagged | Symbols | Missing own XML |
| --- | ---: | ---: | ---: | ---: |
| Blazor Components | 3 | 1 | 1,474 | 15 |
| HostManagement | 15 | 9 | 3,317 | 12 |
| Journal | 1 | 0 | 92 | 0 |
| Licensing | 5 | 0 | 2,498 | 65 |
| Licensing Update Service | 5 | 0 | 3,487 | 1,021 |
| Localization | 1 | 0 | 416 | 0 |
| Monochrome Icons | 4 | 3 | 307 | 237 |
| ServiceBus | 33 | 2 | 18,569 | 0 |
| SystemMonitoring | 1 | 1 | 179 | 169 |
| UI Design | 1 | 0 | 0 | 0 |

In ServiceBus, 31 projects have no Roslyn compiler or workspace diagnostic,
and no explicit exposed symbol lacks its own XML comment. The CodeFixes
project has one workspace diagnostic: its project reference to Analyzers has
no matching metadata reference in this workspace. It has zero compiler errors.
The packaging-only `ViciOne.ServiceBus.Analyzers.Package` project exposes zero
symbols and has 36 compiler errors because its `netstandard2.0` reference
assemblies were not resolved after the generated build outputs were cleaned.
An offline restore of that project did not complete. These 36 errors are an
inventory-environment limitation, not a demonstrated source defect. The
other 31 ServiceBus projects resolved without diagnostics using the T176
project assets for the current, unchanged ServiceBus source tree.

Blazor Components, HostManagement, Monochrome Icons, and SystemMonitoring
retain compiler or workspace diagnostics. Their symbol sets may be incomplete
until each repository resolves its project graph and validates the compiled
surface. Blazor Components and Licensing Update Service had uncommitted
`src` changes when the inventory ran. Their state is identified by the source
hashes, but the uncommitted file contents are not archived and cannot be
reconstructed from the listed HEAD alone. Missing-own-XML counts
include generated and record members and do not by themselves prove that a
public API is poorly documented.

Snapshot repository heads: Blazor `dc61f8aa6882`, HostManagement
`b18e8b7fc194`, Journal `e06ee49a5fef`, Licensing `32855fd9d24e`,
Licensing Update Service `1e21ba1babf7`, Localization `c3f6f5eedecb`,
Monochrome Icons `597dbe9f6cdd`, ServiceBus `a3fe92c9c928`,
SystemMonitoring `f1d353a41eae`, UI Design `11445731b031`.
ServiceBus's product `src` tree is
`0871a1f1b28cfb4f3303ae8c5fbf7f47010fc6e4`, the same tree as the
[T176 quality checkpoint](../quality-status.md). Relative to the
30 September inventory, ServiceBus has no added or removed documentation IDs,
changed signatures, or changed own XML-comment text.

This inventory checks XML-comment presence and records the comment text; it
does not determine whether the text accurately describes runtime behavior or
whether an API change is compatible. A+ API approval still requires resolution
of the workspace diagnostics and semantic review of the API and comments.
The earlier inventory remains available through Git history at commit
`42a028a7fa8ed6facf941da3d72064cbf438f196`.

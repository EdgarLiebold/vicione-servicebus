# Roslyn API and XML-comment inventory, 29 September 2026

This is a **provisional inventory**, not an A+ certification. It records the
effective public and protected C# symbols that Roslyn could compile from every
`src/**/*.csproj` under the ten sibling ViciOne repositories. The matching
source XML comments, signatures, documentation IDs, locations, and project
diagnostics are in [the complete compressed JSON snapshot](roslyn-all-repos-api-2026-09-29.tar.gz).
The snapshot contains `summary.json` and one JSON file per project (70 files,
1.6 MiB compressed). Its SHA-256 is
`ed5b1abe4f0d6524ebfad0df5f7bd5b6db0fdf6fb1237738132e77e23217dddb`.

The source tool is
[`RoslynApiCommentInventory.cs`](../../tools/api-conventions/RoslynApiCommentInventory.cs).
Rerun the inventory from the ServiceBus repository root with a new output path:

```sh
dotnet run tools/api-conventions/RoslynApiCommentInventory.cs -- ../ /private/tmp/vicione-api-inventory tools/api-conventions/roslyn-api-projects.txt
```

The [checked project manifest](../../tools/api-conventions/roslyn-api-projects.txt)
must match the discovered project paths exactly; a wrong or empty scope exits 3.
An existing output directory is rejected to prevent stale JSON. Relative
project paths distinguish identically named projects. Exit code 5 means at
least one project has a compiler error, workspace
diagnostic, or load failure. These cases remain in the output and must not be
treated as a successful API check. The tool reads source and writes only to the
chosen output directory.

Temporary .NET counterprobes checked duplicate project basenames; public,
protected, nested and sealed visibility; XML extraction; accessor filtering;
manifest mismatch; empty scope; and existing-output rejection. These probes
are recorded as manual tool evidence, not as a product test verdict.

| Repository | Projects | Flagged projects | Visible symbols | Explicit symbols without own XML, in unflagged projects |
| --- | ---: | ---: | ---: | ---: |
| Blazor Components | 3 | 2 | 1,474 | 0 |
| HostManagement | 15 | 12 | 3,317 | 0 |
| Journal | 1 | 0 | 92 | 0 |
| Licensing | 5 | 0 | 2,498 | 65 |
| Licensing Update Service | 5 | 0 | 3,487 | 1,021 |
| Localization | 1 | 0 | 416 | 0 |
| Monochrome Icons | 4 | 3 | 307 | 236 |
| ServiceBus | 33 | 1 | 18,569 | 0 |
| SystemMonitoring | 1 | 1 | 179 | 0 |
| UI Design | 1 | 0 | 0 | 0 |
| **Total** | **69** | **19** | **30,339** | **1,322** |

The 69 project compilations contained 5,824 Roslyn documents. `MissingXml`
across **all** projects is 1,519; the 1,322 above excludes flagged projects.
Of ServiceBus's 33 projects, only `ViciOne.ServiceBus.Analyzers.CodeFixes`
is flagged: Roslyn reports a project reference without a matching metadata
reference to `ViciOne.ServiceBus.Analyzers`; its compilation has zero errors.
The other 32 ServiceBus projects have no workspace or compiler diagnostics in
this run and no exposed explicit symbol with an empty own XML comment.

The missing-XML count is a triage list, not a quality grade. For example,
Licensing's 65 entries are record properties that may be explained by the
containing type's `<param>` tags, and Monochrome Icons' 236 entries are enum
members. The tool does not judge whether text is correct, complete, useful, or
consistent with runtime behavior. It also does not prove that a flagged
project's API surface is complete. The next API review must resolve those
diagnostics, assess comment meaning against code and tests, and examine the
missing-XML candidates in their owning repositories before assigning a grade.

This snapshot includes the working-tree state of sibling repositories on
29 September 2026, some of which had pre-existing uncommitted changes. Head
commits were: Blazor `dc61f8aa6882`, HostManagement `b18e8b7fc194`, Journal
`e06ee49a5fef`, Licensing `32855fd9d24e`, Licensing Update Service
`1e21ba1babf7`, Localization `c3f6f5eedecb`, Monochrome Icons
`597dbe9f6cdd`, ServiceBus `de21fe37dea7`, SystemMonitoring `f1d353a41eae`,
and UI Design `11445731b031`.
Absolute paths and generation time are included in the JSON, so rerunning
against the same code does not promise byte-identical output. The archive hash
identifies this snapshot, not the underlying source trees.

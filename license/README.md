# Repository change overview

Run `python3 license/repository_diff.py` from anywhere. It compares the committed
MassTransit `MassTransit/v8.5.10` tag with the latest locally available `main`
commit and writes two current reports to the ignored `artifacts/policy` directory:

- `REPOSITORY_DIFF.md` starts with repository totals, then summarizes file status
  and added/removed lines by project in separate `src`, `tests`, `samples`,
  `benchmarks`, and other sections. Project rows show directories without a
  `.csproj` suffix. Project State is `New` for a project present only in `main`,
  `Modified` for a project with an unambiguous original predecessor, and
  `Removed` for a project present only in the original tree. Shared-file rows
  are not projects and have no project State. Former project appears only for
  unambiguous current project successors outside `tests/`. State describes
  project identity, so a New project can contain files moved from old modules.
- `REPOSITORY_DIFF_DETAILS.md` lists every file under its area, project, and status.
  Outside `tests/`, each row shows the old and current path, any old project,
  and added/removed lines. Test rows show one path
  and its added or removed lines in separate original and current sections.
  The reports link to each other.

Use `--output-dir PATH` to write both reports elsewhere. A relative path is
resolved from the repository root. Existing reports with these names are replaced
by the current run. The working tree, index, and checked-out branch do not affect
the result. Fetch `main` and the tag first if the local refs are stale.

The overview recognizes Git's content-based renames and also pairs files by the
known `MassTransit` to `ViciOne.ServiceBus` path change or a C# filename unique
within the same top-level tree on both sides. It also recognizes one-to-one
successor projects through the known package naming changes, then matches files
by relative path or a C# filename unique within that project pair. These matches
express likely file continuity through the large rewrite, not unchanged content.
Ambiguous files remain additions and removals. A file that has no matching current
path remains removed even when its old project has a successor; the report groups
that removal under the successor within the same top-level tree and shows the
original project in the details.
Only project files in the two compared Git trees can name report projects.
Current `ViciOne.ServiceBus` projects must exist in `main`. An original project
without a current successor remains under its original MassTransit name and is
marked Removed. Historical interim names are not endpoints of this comparison.

The `tests/` trees are a complete replacement. The script removes both test
trees from file matching and always emits independent records for their paths,
even when names or content look similar.
All original test-tree files count as removed; all current test-tree files count
as added. No test file has a Former project. The summary lists every original
test project as `Removed` and every current test project as `New`, in separate
tables with separate totals. The file-level report likewise separates both
snapshots and has no predecessor column for tests.
The `--files` inventory marks each test row as `original` or `current` and leaves
its `old_project` field empty. Original benchmark files
inside `tests/` likewise remain removals in the `tests` section; the current
`benchmarks/` files are additions in the `benchmarks` section.

A file moving between `src`, `tests`, `samples`, `benchmarks`, or the remaining
repository area counts as removed from its old area and added to its new area.
A rejected pairing may still be selected by a later, unambiguous rule; paths
left unmatched by all rules are shown separately. Neither case stops report
generation.

Use `python3 license/repository_diff.py --files` for a tab-separated inventory
on standard output when a machine-readable format is needed. Neither detail
format includes a match-method column. The line counts
come from Git's diff engine. Git reports no line counts for binary changes, so
those are marked `binary` and counted in the project's Binary column. Temporary
comparison trees are stored outside the repository and deleted after each run.

Use `python3 license/repository_diff.py --patch` for the complete binary-safe Git
patch. Its `tests/` portion also shows removals and additions, without rename
inference. Outside that tree, Git's patch format marks its own similarity matches
as renames.

[The changelog](../CHANGELOG.md) explains product behavior, removed capabilities,
and defect corrections.

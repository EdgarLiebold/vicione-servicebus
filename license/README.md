# Repository change overview

Run `python3 license/repository_diff.py` from anywhere. It compares the committed
MassTransit `MassTransit/v8.5.10` tag with the latest locally available `main`
commit and writes two current reports to the ignored `artifacts/policy` directory:

- `REPOSITORY_DIFF.md` starts with repository totals, then summarizes file status
  and added/removed lines by project in separate `src`, `tests`, `samples`,
  `benchmarks`, and other sections. Project rows show directories without a
  `.csproj` suffix. The State column distinguishes current from removed packages.
  The Former project column identifies the original MassTransit project where
  a unique relationship is recorded.
- `REPOSITORY_DIFF_DETAILS.md` lists every file under its area, project, and status.
  Each row shows the old and current path, any old project, added/removed lines,
  and how the paths were matched. The reports link to each other.

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
For a removed source project, the script searches the history of `main` for its
interim `ViciOne.ServiceBus` project path. When found, it shows that directory
with State `Removed` and the original MassTransit directory as Former project.
The displayed interim path is historical and absent from current `main`. Projects
with neither a current successor nor a recorded interim path remain under their
old name.

The `tests/` trees are a complete replacement. The script never pairs an original
path under `tests/` with any current path, even when names or content look similar.
All original test-tree files count as removed; all current test-tree files count
as added. The summary groups the original tests in one removed suite row and
lists only current test projects individually. The file-level report preserves
the original test project paths in a separate historical subsection. Original benchmark files
inside `tests/` likewise remain removals in the `tests` section; the current
`benchmarks/` files are additions in the `benchmarks` section.

Use `python3 license/repository_diff.py --files` for a tab-separated inventory
on standard output when a machine-readable format is needed. The line counts
come from Git's diff engine. Git reports no line counts for binary changes, so
those are marked `binary` and counted in the project's Binary column. Temporary
comparison trees are stored outside the repository and deleted after each run.

Use `python3 license/repository_diff.py --patch` for the complete binary-safe Git
patch. Its `tests/` portion also shows removals and additions, without rename
inference. Outside that tree, Git's patch format marks its own similarity matches
as renames.

[The changelog](../CHANGELOG.md) explains product behavior, removed capabilities,
and defect corrections.

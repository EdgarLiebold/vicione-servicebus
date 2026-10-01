# Repository change overview

Run `python3 license/repository_diff.py` from anywhere. It compares the committed
MassTransit `MassTransit/v8.5.10` tag with the latest locally available `main`
commit. The default Markdown report totals added, modified, removed, moved, and
unchanged files and added/removed lines for each project, with separate sections
for `src`, `tests`, `samples`, and `benchmarks`. Other repository files have their
own section. The working tree, index, and checked-out branch do not affect the
result. Fetch `main` and the tag first if the local refs are stale.

The overview recognizes Git's content-based renames and also pairs files by the
known `MassTransit` to `ViciOne.ServiceBus` path change or a C# filename unique
within the same top-level tree on both sides. Benchmark files moved from the old test tree
are also paired. These latter matches express likely file continuity through the
large rewrite, not unchanged content. Ambiguous files remain additions and
removals. The report gives counts for each matching method. A moved file's line
diff belongs to its current project; a removed file belongs to its old project.

Use `python3 license/repository_diff.py --files` for the tab-separated inventory
of every file, including old path, current path, status, project, and its added
and removed lines. The line counts come from Git's diff engine. Git reports no
line counts for binary changes, so those are marked `binary` in the inventory
and counted in the project's Binary column. Temporary comparison trees are
stored outside the repository and deleted after each run.

Use `python3 license/repository_diff.py --patch` for the complete binary-safe Git
patch. Git's patch format marks only its own similarity matches as renames.

[The changelog](../CHANGELOG.md) explains product behavior, removed capabilities,
and defect corrections.

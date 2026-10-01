# Repository change overview

Run `python3 license/repository_diff.py` from anywhere. It compares the committed
MassTransit `MassTransit/v8.5.10` tag with the latest locally available `main`
commit and prints a compact overview for the entire repository, including
product source and tests. The working tree, index, and checked-out branch do not
affect the result. Fetch `main` and the tag first if the local refs are stale.

The overview recognizes Git's content-based renames and also pairs files by the
known `MassTransit` to `ViciOne.ServiceBus` path change or a C# filename unique
within the same top-level tree on both sides. Benchmark files moved from the old test tree
are also paired. These latter matches express likely file continuity through the
large rewrite, not unchanged content. Ambiguous files remain additions and
removals. The report gives counts for each matching method.

Use `python3 license/repository_diff.py --patch` for the complete binary-safe Git
patch. Git's patch format marks only its own similarity matches as renames.

The overview gives path counts by area. [The changelog](../CHANGELOG.md)
explains product behavior, removed capabilities, and defect corrections.

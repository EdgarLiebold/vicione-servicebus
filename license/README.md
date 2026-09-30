# Repository change overview

Run `python3 license/repository_diff.py` from anywhere. It reads the repository's
first Git commit and the current tracked working tree, then prints a compact
overview for the entire repository, including product source and tests. It
requires no edited path list or generated snapshot. Untracked files are outside
Git's comparison; the script rejects untracked source and test files so they
cannot silently disappear from the overview.

The overview gives path counts by area. [The changelog](../CHANGELOG.md)
explains product behavior, removed capabilities, and defect corrections.

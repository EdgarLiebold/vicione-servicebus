#!/usr/bin/env python3
"""Print the current repository-wide change summary against the first import."""

from __future__ import annotations

import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path


BASELINE = "9be1da2046218be2503c77c529b68d96fe113008"
ROOT = Path(__file__).resolve().parents[1]
AREAS = (
    "Product source",
    "Tests",
    "Build, CI, and tooling",
    "Samples and benchmarks",
    "Documentation and licensing",
    "Historical work evidence",
    "Other repository files",
)


def git(*arguments: str) -> str:
    return subprocess.check_output(
        ["git", "-C", str(ROOT), *arguments], stderr=subprocess.PIPE
    ).decode("utf-8", errors="surrogateescape")


def area(path: str) -> str:
    top = path.split("/", 1)[0]
    if top == "src":
        return "Product source"
    if top == "tests":
        return "Tests"
    if top in {".github", ".devcontainer", "build", "tools"} or path.endswith(
        (".slnx", ".props", ".targets")
    ):
        return "Build, CI, and tooling"
    if top in {"samples", "benchmarks"}:
        return "Samples and benchmarks"
    if top in {"evidence", ".testagent"}:
        return "Historical work evidence"
    if top in {"docs", "license"} or path.endswith((".md", ".txt")) or top in {
        "NOTICE", "COPYRIGHT"
    }:
        return "Documentation and licensing"
    return "Other repository files"


def summary() -> str:
    roots = git("rev-list", "--max-parents=0", "HEAD").splitlines()
    if roots != [BASELINE]:
        raise ValueError("The pinned first-import commit is not this repository's root")
    if git("ls-files", "--others", "--exclude-standard", "--", "src", "tests"):
        raise ValueError("Untracked source or test files would be absent from the Git diff")

    raw = git("diff", "--no-renames", "--name-status", "-z", BASELINE, "--", ".")
    fields = raw.split("\0")
    if fields[-1] == "":
        fields.pop()
    if len(fields) % 2:
        raise ValueError("Git returned an incomplete name-status record")

    counts: dict[str, Counter[str]] = defaultdict(Counter)
    for status, path in zip(fields[::2], fields[1::2]):
        kind = status[0]
        if kind not in {"A", "M", "D", "T"}:
            raise ValueError(f"Unexpected Git status: {status}")
        counts[area(path)]["M" if kind == "T" else kind] += 1

    head = git("rev-parse", "HEAD").strip()
    dirty = bool(git("status", "--porcelain", "--untracked-files=no"))
    lines = [
        "# Current repository change overview",
        "",
        f"First import: `{BASELINE}`. Current HEAD: `{head}`.",
        "Comparison: `git diff --no-renames " + BASELINE + " -- .`.",
        "The comparison covers every tracked repository path, including all product source and tests.",
        "Renamed paths count as one removal and one addition. These counts do not measure",
        "rewritten code or changed lines. Untracked files are excluded.",
        f"Working tree: {'modified tracked files included' if dirty else 'tracked files match HEAD'}.",
        "",
        "| Area | Added paths | Changed paths | Removed paths |",
        "|---|---:|---:|---:|",
    ]
    total: Counter[str] = Counter()
    for name in AREAS:
        count = counts[name]
        total.update(count)
        lines.append(f"| {name} | {count['A']} | {count['M']} | {count['D']} |")
    lines.append(f"| **Total** | **{total['A']}** | **{total['M']}** | **{total['D']}** |")
    lines.extend(("", "The changelog explains behavior and removed capabilities; Git retains the exact patch.", ""))
    return "\n".join(lines)


if __name__ == "__main__":
    try:
        sys.stdout.write(summary())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Cannot summarize repository changes: {error}", file=sys.stderr)
        raise SystemExit(1) from error

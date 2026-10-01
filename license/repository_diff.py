#!/usr/bin/env python3
"""Compare the original MassTransit source with the committed ServiceBus main tree."""

from __future__ import annotations

import argparse
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path


BASELINE_TAG = "MassTransit/v8.5.10"
UPSTREAM_COMMIT = "62ab339afa3bac2e9b3fe1769d0d35d7e44778e9"
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


def diff_arguments(baseline: str, main: str, *options: str) -> list[str]:
    # The repository-wide identity change exceeds Git's default rename limit.
    return [
        "-c", "diff.renameLimit=10000", "diff", "--no-ext-diff", "--no-textconv",
        "--find-renames=50%", *options, baseline, main, "--", ".",
    ]


def endpoints() -> tuple[str, str, str]:
    baseline = git("rev-parse", "--verify", f"refs/tags/{BASELINE_TAG}^{{commit}}").strip()
    if baseline != UPSTREAM_COMMIT:
        raise ValueError(f"{BASELINE_TAG} does not point to the pinned MassTransit commit")

    for main_ref in ("refs/heads/main", "refs/remotes/origin/main"):
        try:
            main = git("rev-parse", "--verify", f"{main_ref}^{{commit}}").strip()
            return baseline, main_ref, main
        except subprocess.CalledProcessError:
            continue
    raise ValueError("No local main or origin/main reference is available")


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


def changes(baseline: str, main: str) -> tuple[dict[str, Counter[str]], Counter[str]]:
    raw = git(*diff_arguments(baseline, main, "--name-status", "-z"))
    fields = raw.split("\0")
    if fields[-1] == "":
        fields.pop()

    counts: dict[str, Counter[str]] = defaultdict(Counter)
    methods: Counter[str] = Counter()
    added: set[str] = set()
    removed: set[str] = set()
    index = 0
    while index < len(fields):
        status = fields[index]
        kind = status[:1]
        if kind == "R":
            if index + 2 >= len(fields):
                raise ValueError("Git returned an incomplete rename record")
            destination = fields[index + 2]
            counts[area(destination)]["R"] += 1
            methods["git"] += 1
            index += 3
        elif kind in {"A", "M", "D", "T"}:
            if index + 1 >= len(fields):
                raise ValueError("Git returned an incomplete change record")
            path = fields[index + 1]
            if kind == "A":
                added.add(path)
            elif kind == "D":
                removed.add(path)
            else:
                counts[area(path)]["M"] += 1
            index += 2
        else:
            raise ValueError(f"Unexpected Git status: {status}")

    # Preserve continuity through the known repository-wide product rename even
    # where extensive source edits leave less than 50% textual similarity.
    for old in sorted(removed):
        new = old.replace("MassTransit", "ViciOne.ServiceBus")
        if new != old and new in added:
            removed.remove(old)
            added.remove(new)
            counts[area(new)]["R"] += 1
            methods["renamed path"] += 1

    # A unique C# filename on both sides is a useful, auditable indication of
    # a moved source file. Generic repeated names cannot be paired this way.
    old_by_name: dict[str, list[str]] = defaultdict(list)
    new_by_name: dict[str, list[str]] = defaultdict(list)
    for path in removed:
        if path.endswith(".cs"):
            old_by_name[Path(path).name].append(path)
    for path in added:
        if path.endswith(".cs"):
            new_by_name[Path(path).name].append(path)
    for name, old_paths in old_by_name.items():
        new_paths = new_by_name[name]
        if len(old_paths) == len(new_paths) == 1:
            old, new = old_paths[0], new_paths[0]
            old_group, new_group = old.split("/", 1)[0], new.split("/", 1)[0]
            if old_group != new_group and not (
                old.startswith("tests/MassTransit.Benchmark") and new_group == "benchmarks"
            ):
                continue
            removed.remove(old)
            added.remove(new)
            counts[area(new)]["R"] += 1
            methods["unique C# filename"] += 1

    for path in added:
        counts[area(path)]["A"] += 1
    for path in removed:
        counts[area(path)]["D"] += 1
    return counts, methods


def summary() -> str:
    baseline, main_ref, main = endpoints()
    counts, methods = changes(baseline, main)

    lines = [
        "# Current repository change overview",
        "",
        f"Original source: `{BASELINE_TAG}` (`{baseline}`).",
        f"Current main: `{main_ref}` (`{main}`).",
        f"Comparison: the committed trees at `{BASELINE_TAG}` and `{main_ref}`.",
        "The comparison covers every committed repository path, including product source and tests.",
        "Renames are matched by Git at 50% similarity, then by the known MassTransit-to-ViciOne",
        "path substitution, then by a C# filename unique within the same top-level tree (or moved",
        "from the old test tree into benchmarks). The last two rules infer",
        "file continuity; they do not prove that the implementation is unchanged.",
        f"Matches: {methods['git']} Git, {methods['renamed path']} renamed path, "
        f"{methods['unique C# filename']} unique C# filename.",
        "Counts describe paths, not rewritten code or changed lines. The worktree and index are excluded.",
        "Run `python3 license/repository_diff.py --patch` for the complete Git diff; Git's patch",
        "format only marks its own similarity matches as renames.",
        "",
        "| Area | Added paths | Changed paths | Removed paths | Renamed paths |",
        "|---|---:|---:|---:|---:|",
    ]
    total: Counter[str] = Counter()
    for name in AREAS:
        count = counts[name]
        total.update(count)
        lines.append(f"| {name} | {count['A']} | {count['M']} | {count['D']} | {count['R']} |")
    lines.append(f"| **Total** | **{total['A']}** | **{total['M']}** | **{total['D']}** | **{total['R']}** |")
    lines.extend(("", "The changelog explains behavior and removed capabilities; Git retains the exact patch.", ""))
    return "\n".join(lines)


def patch() -> None:
    baseline, _, main = endpoints()
    subprocess.run(
        ["git", "-C", str(ROOT), *diff_arguments(baseline, main, "--binary")],
        check=True,
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--patch", action="store_true", help="print the complete committed Git diff")
    arguments = parser.parse_args()
    try:
        if arguments.patch:
            patch()
        else:
            sys.stdout.write(summary())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Cannot summarize repository changes: {error}", file=sys.stderr)
        raise SystemExit(1) from error

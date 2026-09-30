#!/usr/bin/env python3
"""Generate a concise source-area overview from the original MassTransit import."""

from __future__ import annotations

import argparse
import subprocess
import sys
from collections import Counter, defaultdict
from pathlib import Path


ORIGINAL_IMPORT = "9be1da2046218be2503c77c529b68d96fe113008"
CHANGE_LIST = "CHANGELIST.md"
AREAS = (
    "Core and contracts",
    "Workflow and application packages",
    "Persistence",
    "Scheduling",
    "Transports",
    "Serialization, diagnostics and tooling",
    "Other source",
)


def git(root: Path, *args: str) -> bytes:
    return subprocess.check_output(["git", "-C", str(root), *args], stderr=subprocess.PIPE)


def area(path: str) -> str:
    parts = path.split("/")
    if len(parts) < 2 or parts[0] != "src":
        raise ValueError(f"Unexpected source path: {path}")
    if parts[1] == "Persistence":
        return "Persistence"
    if parts[1] == "Scheduling":
        return "Scheduling"
    if parts[1] == "Transports":
        return "Transports"
    name = parts[1]
    if name in {"MassTransit", "MassTransit.Abstractions", "ViciOne.ServiceBus", "ViciOne.ServiceBus.Abstractions"}:
        return "Core and contracts"
    if name in {"ViciOne.ServiceBus.Sagas", "ViciOne.ServiceBus.Courier", "ViciOne.ServiceBus.Futures",
                "ViciOne.ServiceBus.JobService", "ViciOne.ServiceBus.Mediator", "ViciOne.ServiceBus.Initializers"}:
        return "Workflow and application packages"
    if name.startswith(("MassTransit.", "ViciOne.ServiceBus.")):
        return "Serialization, diagnostics and tooling"
    return "Other source"


def changes(root: Path) -> dict[str, Counter[str]]:
    untracked = git(root, "ls-files", "--others", "--exclude-standard", "-z", "--", "src")
    if untracked:
        raise ValueError("Stage or remove untracked source files before generating the overview")

    raw = git(root, "diff", "--no-renames", "--name-status", "-z", ORIGINAL_IMPORT, "--", "src")
    fields = raw.decode("utf-8", errors="surrogateescape").split("\0")
    if fields[-1] == "":
        fields.pop()
    if len(fields) % 2:
        raise ValueError("Git returned an incomplete name-status record")

    totals: dict[str, Counter[str]] = defaultdict(Counter)
    for status, path in zip(fields[::2], fields[1::2]):
        kind = status[0]
        if kind not in {"A", "M", "D", "T"}:
            raise ValueError(f"Unexpected Git status: {status}")
        totals[area(path)]["M" if kind == "T" else kind] += 1
    return totals


def render(root: Path) -> str:
    if git(root, "rev-parse", "--verify", f"{ORIGINAL_IMPORT}^{{commit}}").decode().strip() != ORIGINAL_IMPORT:
        raise ValueError("The original MassTransit import commit is unavailable")
    totals = changes(root)
    original_projects = sum(
        path.endswith(".csproj")
        for path in git(root, "ls-tree", "-r", "--name-only", ORIGINAL_IMPORT, "src").decode().splitlines()
    )
    current_projects = len(list((root / "src").rglob("*.csproj")))
    lines = [
        "# Source change overview",
        "",
        "This overview is generated from the original MassTransit 8.5.10 import and the current",
        "source tree. It summarizes scale by source area; the [changelog](CHANGELOG.md) explains",
        "behavior, replacements, removed capabilities and fixes. Git retains the exact file history.",
        "",
        f"Original import: `{ORIGINAL_IMPORT}`.",
        f"Source projects: {original_projects} in the import; {current_projects} in the current tree.",
        "Regenerate with `python3 tools/identity/change_list.py --write`; check with",
        "`python3 tools/identity/change_list.py`.",
        "The underlying comparison is `git diff --no-renames " + ORIGINAL_IMPORT + " -- src`.",
        "",
        "| Source area | Added paths | Changed paths | Removed paths |",
        "|---|---:|---:|---:|",
    ]
    for name in AREAS:
        count = totals[name]
        lines.append(f"| {name} | {count['A']} | {count['M']} | {count['D']} |")
    overall = Counter()
    for count in totals.values():
        overall.update(count)
    lines += [
        f"| **Total** | **{overall['A']}** | **{overall['M']}** | **{overall['D']}** |",
        "",
        "The repository-wide identity and path migration counts as additions and removals",
        "because rename detection is disabled. These numbers describe Git paths, not independent",
        "features or the amount of original code retained. Read the area-based changelog for",
        "the corresponding product changes.",
        "",
    ]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--write", action="store_true", help="Write the summary instead of checking it")
    args = parser.parse_args(argv)
    root = args.repository.resolve()
    try:
        expected = render(root)
    except (subprocess.CalledProcessError, ValueError) as error:
        print(f"FAIL source change overview: {error}", file=sys.stderr)
        return 1
    target = root / CHANGE_LIST
    if args.write:
        target.write_text(expected, encoding="utf-8")
        print(f"PASS source change overview written {CHANGE_LIST}")
        return 0
    if not target.is_file() or target.read_text(encoding="utf-8") != expected:
        print(f"FAIL source change overview differs from {CHANGE_LIST}", file=sys.stderr)
        return 1
    print("PASS source change overview matches the original-import Git diff")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

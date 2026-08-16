#!/usr/bin/env python3
"""Generate and verify the root change list that carries the Apache 2.0 section 4(b) evidence.

The per file notice was retired, so this document is the only place that states which files this fork
changed against its upstream baseline. That makes it evidence rather than documentation: it is
generated from the pinned baseline and the working tree, never edited by hand, and the check mode
rejects a file that differs from what the generator produces by a single byte.

Standard library only, so it runs before any restore.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from identity_rules import BASELINE_COMMIT, BASELINE_TREE  # noqa: E402
from identity_gate import (  # noqa: E402
    baseline_archive,
    baseline_bytes,
    commit_candidate_files,
    map_path,
)

CHANGE_LIST = "CHANGELIST.md"

HEADER = """# ViciOne.ServiceBus change list

This file is generated. It is the change evidence Apache License 2.0 section 4(b) asks for: it names
every file this fork adds, changes, deletes or renames against its upstream baseline. Do not edit it
by hand; regenerate it with `python3 tools/identity/change_list.py --write`.

| Baseline | Value |
|---|---|
| Upstream | MassTransit 8.5.10 |
| Baseline commit | `{commit}` |
| Baseline tree | `{tree}` |

| Status | Count |
|---|---|
| Added | {added} |
| Modified | {modified} |
| Deleted | {deleted} |
| Renamed | {renamed} |

| Path | Status | Baseline path |
|---|---|---|
"""


def classify(root: Path) -> list[tuple[str, str, str]]:
    """Every difference against the baseline, as (path, status, baseline path).

    The status vocabulary is fixed, so a file that was both renamed and edited has to pick one word.
    Content wins: section 4(b) asks which files were changed, and this fork renamed almost everything,
    so calling those Renamed would report a path move and stay silent about the edit underneath.
    Renamed therefore means moved with identical bytes, and the baseline path column carries the
    origin either way, so the rename is never lost.
    """
    baseline = baseline_archive(root)
    present = {
        str(p.relative_to(root)).replace("\\", "/")
        for p in commit_candidate_files(root)
    }

    rows: list[tuple[str, str, str]] = []
    mapped_targets: set[str] = set()

    for source in sorted(baseline):
        target = map_path(source)
        mapped_targets.add(target)
        if target not in present:
            rows.append((source, "Deleted", source))
            continue
        if (root / target).read_bytes() != baseline_bytes(root, source):
            rows.append((target, "Modified", source))
        elif target != source:
            rows.append((target, "Renamed", source))

    for path in sorted(present - mapped_targets):
        rows.append((path, "Added", ""))

    return sorted(rows)


def render(root: Path) -> str:
    rows = classify(root)
    counts = {status: sum(1 for _, s, _ in rows if s == status)
              for status in ("Added", "Modified", "Deleted", "Renamed")}

    body = HEADER.format(commit=BASELINE_COMMIT, tree=BASELINE_TREE,
                         added=counts["Added"], modified=counts["Modified"],
                         deleted=counts["Deleted"], renamed=counts["Renamed"])
    for path, status, origin in rows:
        body += f"| `{path}` | {status} | {f'`{origin}`' if origin else ''} |\n"
    return body


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--write", action="store_true", help="Write the change list instead of checking it.")
    args = parser.parse_args(argv)

    expected = render(args.repository)
    target = args.repository / CHANGE_LIST

    if args.write:
        target.write_text(expected, encoding="utf-8")
        print(f"PASS change-list written {CHANGE_LIST}")
        return 0

    if not target.is_file():
        print(f"FAIL change-list {CHANGE_LIST} is missing; the change evidence has no carrier", file=sys.stderr)
        return 1

    actual = target.read_text(encoding="utf-8")
    if actual != expected:
        expected_lines, actual_lines = expected.splitlines(), actual.splitlines()
        for number, (want, got) in enumerate(zip(expected_lines, actual_lines), 1):
            if want != got:
                print(f"FAIL change-list line {number} differs\n  expected: {want}\n  actual:   {got}", file=sys.stderr)
                return 1
        print(f"FAIL change-list has {abs(len(expected_lines) - len(actual_lines))} line(s) too "
              f"{'few' if len(actual_lines) < len(expected_lines) else 'many'}", file=sys.stderr)
        return 1

    print(f"PASS change-list matches the generated evidence ({len(expected.splitlines()) - 20} entries)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

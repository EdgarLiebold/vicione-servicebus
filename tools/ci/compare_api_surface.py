#!/usr/bin/env python3
"""Compare two public API surfaces and separate testing changes from messaging changes.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-09.

The package hashes in the pack gate prove that an artifact did not change after it was built. They
cannot show what it exposes, so a work package that touches types inside shipped assemblies can move
the public surface without any gate noticing. This compares the surface of the baseline against the
surface of the current tree and splits every difference in two:

  testing surface    types that exist to drive a test: the Testing namespaces of the product
                     assemblies and everything in ViciOne.ServiceBus.TestFramework. The task
                     contract binds those directories, so additions here are in scope.
  messaging surface  everything else. The outcome of this work package states that the public
                     messaging API is not touched, so any difference here is a stop, not a diff.

Exit code 1 means the messaging surface moved. Testing differences are reported and are not an
error; they are the record the re-slice asks for.

Standard library only.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

TESTING_ASSEMBLIES = {"ViciOne.ServiceBus.TestFramework"}
TESTING_NAMESPACE_MARKERS = (".Testing.", ".Testing+")


def is_testing(assembly: str, member: str) -> bool:
    """True when a surface line belongs to the test-driving surface rather than to messaging."""
    if assembly in TESTING_ASSEMBLIES:
        return True
    # A member line is '<kind> <qualified name>[ <shape>]', a type line is 'type <kind> <name>'.
    # Reading the last token instead of the right one classified every property by its accessor
    # shape, which put a testing property on the messaging side.
    parts = member.split(" ")
    qualified = parts[2] if parts[0] == "type" and len(parts) > 2 else parts[1]
    return any(marker in qualified for marker in TESTING_NAMESPACE_MARKERS)


def load(path: Path) -> dict[str, list[str]]:
    data = json.loads(path.read_text(encoding="utf-8"))
    return {name: list(members) for name, members in data.get("assemblies", {}).items()}


def compare(baseline: dict[str, list[str]], current: dict[str, list[str]]) -> dict[str, object]:
    added: list[dict[str, str]] = []
    removed: list[dict[str, str]] = []

    for assembly in sorted(set(baseline) | set(current)):
        before = set(baseline.get(assembly, []))
        after = set(current.get(assembly, []))
        for member in sorted(after - before):
            added.append({"assembly": assembly, "member": member,
                          "surface": "testing" if is_testing(assembly, member) else "messaging"})
        for member in sorted(before - after):
            removed.append({"assembly": assembly, "member": member,
                            "surface": "testing" if is_testing(assembly, member) else "messaging"})

    messaging = [entry for entry in added + removed if entry["surface"] == "messaging"]
    testing = [entry for entry in added + removed if entry["surface"] == "testing"]
    return {
        "schemaVersion": 1,
        "kind": "PUBLIC_API_SURFACE_COMPARISON",
        "assembliesOnlyInBaseline": sorted(set(baseline) - set(current)),
        "assembliesOnlyInCurrent": sorted(set(current) - set(baseline)),
        "totals": {
            "baselineMembers": sum(len(x) for x in baseline.values()),
            "currentMembers": sum(len(x) for x in current.values()),
            "added": len(added),
            "removed": len(removed),
            "messagingChanges": len(messaging),
            "testingChanges": len(testing),
        },
        "messagingSurfaceUnchanged": not messaging,
        "messagingChanges": messaging,
        "testingChanges": testing,
        "added": added,
        "removed": removed,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, type=Path)
    parser.add_argument("--current", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)

    result = compare(load(args.baseline), load(args.current))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    totals = result["totals"]
    for entry in result["testingChanges"]:
        print(f"testing   {entry['assembly']}: {entry['member']}")
    for entry in result["messagingChanges"]:
        print(f"MESSAGING {entry['assembly']}: {entry['member']}", file=sys.stderr)

    if result["messagingSurfaceUnchanged"]:
        print(f"PASS api-surface messaging unchanged; {totals['testingChanges']} testing change(s), "
              f"{totals['currentMembers']} public members")
        return 0

    print(f"FAIL api-surface {totals['messagingChanges']} messaging surface change(s)", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())

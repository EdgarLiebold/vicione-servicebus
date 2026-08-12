"""Collect exactly the assemblies a census names, from their own project's output, or fail.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-13.

A public surface comparison is only worth its scope. An earlier collection globbed one target
framework across every bin directory, so two assemblies that build no output for it — one of them a
shipped package — dropped out of the measurement while the run stayed green and said "31 assemblies"
as if that were the whole set. Nothing contradicted it, because nothing had been told what the whole
set is.

So the expected set is an input here, not a discovery. Every entry names the assembly, the project
that owns it and the framework this comparison measures, and the collector reads that one path. It
never walks a bin directory looking for a name, which is what let a dependency copy or a second build
of the same assembly be picked at random; and every deviation — a missing file, a project that is not
where the census says, a framework the project does not build, a duplicate identity, a census whose
size disagrees with itself — ends the run with a non-zero exit code and the difference spelled out.

    python3 tools/ci/collect_api_assemblies.py --census <census.json> --root <tree> \
        --out <assembly directory> --manifest <manifest.json>

Standard library only.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path


class CensusError(Exception):
    """A deviation between what the census promises and what the tree holds."""


def load_census(path: Path) -> dict:
    census = json.loads(path.read_text(encoding="utf-8"))
    entries = census.get("entries")
    if not isinstance(entries, list) or not entries:
        raise CensusError(f"the census at {path} lists no assemblies")

    expected = census.get("expectedCount")
    if expected != len(entries):
        raise CensusError(
            f"the census disagrees with itself: expectedCount is {expected} but it lists {len(entries)} entries"
        )

    seen: dict[str, str] = {}
    for entry in entries:
        for field in ("assembly", "project", "targetFramework"):
            if not isinstance(entry.get(field), str) or not entry[field].strip():
                raise CensusError(f"a census entry is missing '{field}': {entry}")
        if entry["assembly"] in seen:
            raise CensusError(
                f"duplicate assembly identity '{entry['assembly']}': claimed by {seen[entry['assembly']]} "
                f"and by {entry['project']}"
            )
        seen[entry["assembly"]] = entry["project"]
    return census


def collect(census: dict, root: Path, out: Path) -> list[dict]:
    out.mkdir(parents=True, exist_ok=True)
    collected: list[dict] = []
    problems: list[str] = []

    for entry in census["entries"]:
        assembly, project, tfm = entry["assembly"], entry["project"], entry["targetFramework"]

        project_path = root / project
        if not project_path.is_file():
            problems.append(f"{assembly}: the census names {project}, which does not exist under {root}")
            continue

        # The one path this assembly may come from. Never a search, so a dependency copy of the same
        # file name in some other project's output cannot be picked up instead.
        source = project_path.parent / "bin" / "Release" / tfm / f"{assembly}.dll"
        if not source.is_file():
            problems.append(
                f"{assembly}: expected {source.relative_to(root)} from its own project's {tfm} output, which is missing"
            )
            continue

        shutil.copy2(source, out / f"{assembly}.dll")
        collected.append({
            "assembly": assembly,
            "project": project,
            "targetFramework": tfm,
            "source": str(source.relative_to(root)),
            "sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        })

    if problems:
        raise CensusError(
            f"{len(problems)} of {len(census['entries'])} expected assemblies could not be collected:\n  "
            + "\n  ".join(problems)
        )

    if len(collected) != census["expectedCount"]:
        raise CensusError(
            f"collected {len(collected)} assemblies where the census expects {census['expectedCount']}"
        )

    return collected


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--census", required=True, type=Path)
    parser.add_argument("--root", required=True, type=Path, help="Tree whose project outputs are read.")
    parser.add_argument("--out", required=True, type=Path, help="Directory the assemblies are copied to.")
    parser.add_argument("--manifest", required=True, type=Path, help="Where the input manifest is written.")
    parser.add_argument("--commit", help="Identity of the tree, recorded in the manifest.")
    args = parser.parse_args(argv)

    try:
        census = load_census(args.census)
        collected = collect(census, args.root, args.out)
    except CensusError as error:
        print(f"FAIL api-assemblies: {error}", file=sys.stderr)
        return 1

    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps({
        "schemaVersion": 1,
        "kind": "API_SURFACE_INPUT_MANIFEST",
        "commit": args.commit,
        "census": {"path": str(args.census), "sha256": hashlib.sha256(args.census.read_bytes()).hexdigest()},
        "assemblyCount": len(collected),
        "assemblies": sorted(collected, key=lambda item: item["assembly"]),
    }, indent=2) + "\n", encoding="utf-8")

    print(f"PASS api-assemblies {len(collected)} of {census['expectedCount']} expected assemblies -> {args.manifest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

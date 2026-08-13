"""Collect exactly the assemblies a census names, from their own evaluated project, or fail.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-13.

A public surface comparison is only worth its scope. An early collection globbed one target framework
across every bin directory, so two assemblies that build no output for it -- one of them a shipped
package -- dropped out of the measurement while the run stayed green and reported "31 assemblies" as
if that were the whole set. Nothing contradicted it, because nothing had been told what the whole set
is. The expected set is therefore an input here, not a discovery.

Two later versions of this tool claimed guarantees they did not have, and records 0081 and 0083
demonstrated each of them:

  * reading the project file as XML is not evaluating it. Imports, Directory.Build.props, conditions,
    property expansion and SDK defaults are invisible to a regular expression, and a version of this
    tool skipped the check entirely when it could not read a literal. The frameworks, the assembly
    name and the output path now come from MSBuild's own evaluation for Release and the chosen
    framework, and a property MSBuild cannot resolve is a failure, never a reason to skip.
  * a file name is what someone called the file. A real ViciOne.ServiceBus.dll copied over the name
    ViciOne.ServiceBus.Abstractions.dll was measured as Abstractions with the wrong member count. The
    surface reader now keys on the assembly's own identity and refuses a file whose name disagrees
    with it, so the census name is held against both the evaluated AssemblyName and the metadata.
  * emptying the output directory with a recursive delete made an evidence tool destructive towards
    any path it was handed. A non-empty output directory is now refused and left exactly as it was.

    python3 tools/ci/collect_api_assemblies.py --census <census.json> --root <tree> \
        --out <empty directory> --manifest <manifest.json> [--commit <sha>]

Standard library only; MSBuild and the repository's own surface reader are invoked as processes.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

WANTED = ("TargetFrameworks", "TargetFramework", "AssemblyName", "TargetPath")


class CensusError(Exception):
    """A deviation between what the census promises and what the tree evaluates to."""


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


def evaluate(project: Path, tfm: str | None) -> dict[str, str]:
    """MSBuild's own answer for Release, optionally for one chosen framework.

    Called twice per entry, and the difference matters. Passing TargetFramework as a global property
    overrides the project's own declaration: a project that declares netstandard2.0 and nothing else
    answers 'net9.0' when asked with that property set, so a check built on the forced evaluation
    confirms whatever it was told. Measured, that is exactly what happened -- the probe for a framework
    the project does not build came back green. What the project declares is therefore read without
    forcing anything, and only the output path and assembly name are read for the chosen framework.
    """
    forced = [f"-property:TargetFramework={tfm}"] if tfm else []
    result = subprocess.run(
        ["dotnet", "msbuild", str(project), "-property:Configuration=Release", *forced,
         *(f"-getProperty:{name}" for name in WANTED)],
        capture_output=True, text=True,
    )
    if result.returncode != 0:
        raise CensusError(
            f"{project.name}: MSBuild could not evaluate the project for {tfm}: "
            + (result.stderr.strip() or result.stdout.strip())[:300]
        )
    try:
        properties = json.loads(result.stdout)["Properties"]
    except Exception as error:
        raise CensusError(f"{project.name}: the MSBuild evaluation was not readable: {error}") from error

    missing = [name for name in WANTED if name not in properties]
    if missing:
        raise CensusError(f"{project.name}: MSBuild did not report {', '.join(missing)}, so nothing confirms the entry")
    return properties


def collect(census: dict, root: Path, out: Path) -> list[dict]:
    # Refused rather than emptied. This directory is whatever the caller passed, and an evidence tool
    # that recursively deletes a caller supplied path is a worse defect than the one it guards against.
    if out.exists() and any(out.iterdir()):
        raise CensusError(
            f"the output directory {out} is not empty; it is left untouched. Point --out at a new directory, "
            "so that nothing the census does not name can be measured."
        )
    out.mkdir(parents=True, exist_ok=True)

    collected: list[dict] = []
    problems: list[str] = []

    for entry in census["entries"]:
        assembly, project, tfm = entry["assembly"], entry["project"], entry["targetFramework"]

        project_path = root/project
        if not project_path.is_file():
            problems.append(f"{assembly}: the census names {project}, which does not exist under {root}")
            continue

        try:
            # What the project itself declares, asked without forcing a framework on it.
            own = evaluate(project_path, None)
            declared = {part.strip() for part in
                        (own["TargetFrameworks"] or own["TargetFramework"]).split(";") if part.strip()}
            if not declared:
                problems.append(f"{assembly}: {project} evaluates to no target framework at all, so nothing confirms the entry")
                continue
            if tfm not in declared:
                problems.append(
                    f"{assembly}: the census measures '{tfm}', which {project} does not build; MSBuild evaluates its "
                    f"own frameworks to {', '.join(sorted(declared))}"
                )
                continue

            # Only now the chosen framework, for the output path and the assembly name.
            evaluated = evaluate(project_path, tfm)
        except CensusError as error:
            problems.append(str(error))
            continue

        if evaluated["AssemblyName"] != assembly:
            problems.append(
                f"{assembly}: {project} evaluates its AssemblyName to '{evaluated['AssemblyName']}', so the census "
                "names an assembly this project does not produce"
            )
            continue

        source = Path(evaluated["TargetPath"])
        if not source.is_file():
            problems.append(
                f"{assembly}: MSBuild reports its output as {source}, which is missing; the project is not built "
                f"for {tfm}"
            )
            continue

        shutil.copy2(source, out/f"{assembly}.dll")
        collected.append({
            "assembly": assembly,
            "project": project,
            "targetFramework": tfm,
            "evaluatedAssemblyName": evaluated["AssemblyName"],
            "source": str(source),
            "sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
        })

    if problems:
        raise CensusError(
            f"{len(problems)} of {len(census['entries'])} expected assemblies could not be collected:\n  "
            + "\n  ".join(problems)
        )
    return collected


def verify_identities(reader: Path, directory: Path, expected: set[str]) -> None:
    """The surface reader keys on each file's AssemblyDefinition and refuses a name that disagrees."""
    with tempfile.TemporaryDirectory() as scratch:
        out = Path(scratch)/"surface.json"
        result = subprocess.run(["dotnet", "run", str(reader), "--", str(directory), str(out)],
                                capture_output=True, text=True)
        if result.returncode != 0 or not out.is_file():
            raise CensusError(
                "the surface reader rejected the collected assemblies, so their identity is not established: "
                + (result.stderr.strip() or result.stdout.strip())[:400]
            )
        surface = json.loads(out.read_text(encoding="utf-8"))

    assemblies = surface.get("assemblies")
    identities = set(assemblies) if isinstance(assemblies, dict) else {item["name"] for item in assemblies}
    if identities != expected:
        raise CensusError(
            "the collected assemblies do not identify as the census expects; "
            f"only in the files: {sorted(identities - expected)}; only in the census: {sorted(expected - identities)}"
        )


def head_of(root: Path) -> tuple[str | None, str | None]:
    def git(*args: str) -> str | None:
        result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)
        return result.stdout.strip() if result.returncode == 0 else None

    return git("rev-parse", "HEAD"), git("rev-parse", "HEAD^{tree}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--census", required=True, type=Path)
    parser.add_argument("--root", required=True, type=Path, help="Clean, commit bound tree that is measured.")
    parser.add_argument("--out", required=True, type=Path, help="Empty directory the assemblies are copied to.")
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--commit", help="Identity claimed for the tree; checked against its HEAD.")
    parser.add_argument("--reader", type=Path, default=Path(__file__).with_name("api_surface.cs"))
    args = parser.parse_args(argv)

    try:
        census = load_census(args.census)

        head, tree = head_of(args.root)
        if args.commit:
            if head is None:
                raise CensusError(
                    f"--commit {args.commit} was claimed, but {args.root} is not a git work tree, so nothing confirms it"
                )
            if args.commit != head:
                raise CensusError(f"--commit {args.commit} does not match the HEAD of {args.root}, which is {head}")

        collected = collect(census, args.root, args.out)
        verify_identities(args.reader, args.out, {entry["assembly"] for entry in census["entries"]})
    except CensusError as error:
        print(f"FAIL api-assemblies: {error}", file=sys.stderr)
        return 1

    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps({
        "schemaVersion": 1,
        "kind": "API_SURFACE_INPUT_MANIFEST",
        "measuredCommit": head,
        "measuredTree": tree,
        "commitClaimVerified": bool(args.commit) and args.commit == head,
        "propertySource": "dotnet msbuild -getProperty for Configuration=Release and the censused framework",
        "identityCheck": "the surface reader keys on AssemblyDefinition and refuses a file name that disagrees",
        "census": {"path": str(args.census), "sha256": hashlib.sha256(args.census.read_bytes()).hexdigest()},
        "assemblyCount": len(collected),
        "assemblies": sorted(collected, key=lambda item: item["assembly"]),
    }, indent=2) + "\n", encoding="utf-8")

    print(f"PASS api-assemblies {len(collected)} of {census['expectedCount']} expected assemblies -> {args.manifest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

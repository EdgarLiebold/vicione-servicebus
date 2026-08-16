"""Build one commit in a worktree this tool owns, and collect exactly the assemblies a census names.

A public surface comparison is only worth its scope and its provenance, and each of those was learned
the hard way, in that order:

  * an early collection globbed one target framework across every bin directory, so two assemblies
    that build no output for it -- one of them a shipped package -- silently left the measurement.
    The expected set is therefore an input, a census, not a discovery.
  * reading a project file as XML is not evaluating it, and forcing TargetFramework as a global
    property overrides the very declaration the check is about. What a project declares is read from
    MSBuild with nothing forced; only the output path and assembly name are read for the chosen
    framework.
  * a file name is what someone called the file, so the surface reader keys on AssemblyDefinition.
  * a verified commit said nothing about bytes in an ignored directory: a counterfeit assembly in a
    clean checkout's bin passed with a green commit check and an empty status.
  * and finally, building in a root the caller prepared proves nothing either. Given a caller chosen
    solution, an external project wrote a counterfeit into the measured root after every up front
    check had passed.

So the caller no longer supplies the measurement space at all. This tool is given the repository, the
commit and the census; it creates its own temporary directory, adds a fresh detached worktree of that
commit inside it, refuses to continue unless that worktree starts clean and empty of build output,
builds the one solution that lies within it, collects, and removes only what it created itself. There
is no --solution and no --reader: an external build target or an external reader is not expressible.

    python3 tools/ci/collect_api_assemblies.py --repository <repo> --commit <sha> \
        --census <census.json> --out <empty directory> --manifest <manifest.json> \
        [--build-log <log>]

Standard library only; git, MSBuild and the reader beside this file are invoked as processes.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

WANTED = ("TargetFrameworks", "TargetFramework", "AssemblyName", "TargetPath")
SOLUTION = "ViciOne.ServiceBus.sln"
BUILD = ("dotnet", "build", SOLUTION, "-c", "Release", "--nologo")
READER = Path(__file__).with_name("api_surface.cs")


class CensusError(Exception):
    """A deviation between what is promised and what the measured space actually holds."""


def git(root: Path, *args: str, check: bool = True) -> str:
    result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)
    if check and result.returncode != 0:
        raise CensusError(f"git {' '.join(args)} failed in {root}: {result.stderr.strip()[:300]}")
    return result.stdout.strip()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_census(path: Path) -> dict:
    census = json.loads(path.read_text(encoding="utf-8"))
    entries = census.get("entries")
    if not isinstance(entries, list) or not entries:
        raise CensusError(f"the census at {path} lists no assemblies")
    if census.get("expectedCount") != len(entries):
        raise CensusError(
            f"the census disagrees with itself: expectedCount is {census.get('expectedCount')} "
            f"but it lists {len(entries)} entries"
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


def require_pristine(worktree: Path, commit: str, when: str) -> None:
    """The measured worktree must hold the bound commit, unmodified in everything git tracks."""
    head = git(worktree, "rev-parse", "HEAD")
    if head != commit:
        raise CensusError(f"{when}: the measured worktree is at {head}, not at the bound commit {commit}")

    dirty = git(worktree, "status", "--porcelain")
    if dirty:
        raise CensusError(f"{when}: the measured worktree is not clean:\n  " + "\n  ".join(dirty.splitlines()[:5]))

    if subprocess.run(["git", "-C", str(worktree), "diff", "--quiet"]).returncode != 0:
        raise CensusError(f"{when}: tracked files in the measured worktree differ from the bound commit")
    if subprocess.run(["git", "-C", str(worktree), "diff", "--cached", "--quiet"]).returncode != 0:
        raise CensusError(f"{when}: the index of the measured worktree differs from the bound commit")


def require_no_output(worktree: Path) -> None:
    leftovers = [line.removeprefix("Would remove ").strip()
                 for line in git(worktree, "clean", "-ndx").splitlines() if line.strip()]
    if leftovers:
        raise CensusError(
            f"the freshly created worktree already holds {len(leftovers)} untracked or ignored path(s), which cannot "
            f"be: {', '.join(leftovers[:5])}"
        )


def canonical_within(root: Path, candidate: Path) -> Path:
    """A canonical, symlink free path that stays inside the measured worktree."""
    resolved = Path(os.path.realpath(candidate))
    try:
        resolved.relative_to(root)
    except ValueError as error:
        raise CensusError(f"{candidate} resolves to {resolved}, which is outside the measured worktree") from error
    return resolved


def evaluate(project: Path, tfm: str | None) -> dict[str, str]:
    """MSBuild's own answer for Release, optionally for one chosen framework.

    Called twice per entry, and the difference matters: passing TargetFramework as a global property
    overrides the project's own declaration, so a project that declares only netstandard2.0 answers
    net9.0 when asked that way. What the project declares is read with nothing forced.
    """
    forced = [f"-property:TargetFramework={tfm}"] if tfm else []
    result = subprocess.run(
        ["dotnet", "msbuild", str(project), "-property:Configuration=Release", *forced,
         *(f"-getProperty:{name}" for name in WANTED)], capture_output=True, text=True)
    if result.returncode != 0:
        raise CensusError(f"MSBuild could not evaluate the project for {tfm}: "
                          + (result.stderr.strip() or result.stdout.strip())[:300])
    try:
        properties = json.loads(result.stdout)["Properties"]
    except Exception as error:
        raise CensusError(f"the MSBuild evaluation was not readable: {error}") from error
    missing = [name for name in WANTED if name not in properties]
    if missing:
        raise CensusError(f"MSBuild did not report {', '.join(missing)}")
    return properties


def collect(census: dict, worktree: Path, out: Path) -> list[dict]:
    if out.exists() and any(out.iterdir()):
        raise CensusError(f"the output directory {out} is not empty; it is left untouched")
    out.mkdir(parents=True, exist_ok=True)

    collected: list[dict] = []
    problems: list[str] = []

    for entry in census["entries"]:
        assembly, project, tfm = entry["assembly"], entry["project"], entry["targetFramework"]
        try:
            project_path = canonical_within(worktree, worktree/project)
            if not project_path.is_file():
                raise CensusError(f"the census names {project}, which does not exist in the measured worktree")

            own = evaluate(project_path, None)
            declared = {part.strip() for part in
                        (own["TargetFrameworks"] or own["TargetFramework"]).split(";") if part.strip()}
            if not declared:
                raise CensusError(f"{project} evaluates to no target framework at all")
            if tfm not in declared:
                raise CensusError(f"the census measures '{tfm}', which {project} does not build; MSBuild evaluates "
                                  f"its own frameworks to {', '.join(sorted(declared))}")

            evaluated = evaluate(project_path, tfm)
            if evaluated["AssemblyName"] != assembly:
                raise CensusError(f"{project} evaluates its AssemblyName to '{evaluated['AssemblyName']}'")

            source = canonical_within(worktree, Path(evaluated["TargetPath"]))
            if not source.is_file():
                raise CensusError(f"MSBuild reports its output as {source}, which is missing")
        except CensusError as error:
            problems.append(f"{assembly}: {error}")
            continue

        shutil.copy2(source, out/f"{assembly}.dll")
        collected.append({"assembly": assembly, "project": project, "targetFramework": tfm,
                          "evaluatedAssemblyName": evaluated["AssemblyName"],
                          "source": str(source.relative_to(worktree)), "sha256": sha256(source)})

    if problems:
        raise CensusError(f"{len(problems)} of {len(census['entries'])} expected assemblies could not be collected:\n  "
                          + "\n  ".join(problems))
    return collected


def verify_identities(directory: Path, expected: set[str]) -> None:
    with tempfile.TemporaryDirectory() as scratch:
        out = Path(scratch)/"surface.json"
        result = subprocess.run(["dotnet", "run", str(READER), "--", str(directory), str(out)],
                                capture_output=True, text=True)
        if result.returncode != 0 or not out.is_file():
            raise CensusError("the surface reader rejected the collected assemblies: "
                              + (result.stderr.strip() or result.stdout.strip())[:400])
        surface = json.loads(out.read_text(encoding="utf-8"))
    assemblies = surface.get("assemblies")
    identities = set(assemblies) if isinstance(assemblies, dict) else {item["name"] for item in assemblies}
    if identities != expected:
        raise CensusError("the collected assemblies do not identify as the census expects; "
                          f"only in the files: {sorted(identities - expected)}; "
                          f"only in the census: {sorted(expected - identities)}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True, type=Path, help="Source repository; never measured directly.")
    parser.add_argument("--commit", required=True, help="The commit whose bytes are measured.")
    parser.add_argument("--census", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path, help="Empty directory the assemblies are copied to.")
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--build-log", type=Path)
    args = parser.parse_args(argv)

    owned: Path | None = None
    worktree: Path | None = None
    repository = args.repository.resolve()
    collected: list[dict] = []
    commit = tree = ""

    try:
        census = load_census(args.census)

        commit = git(repository, "rev-parse", f"{args.commit}^{{commit}}")
        if commit != args.commit:
            raise CensusError(f"--commit {args.commit} is not a full commit id of {repository}; it resolves to {commit}")
        tree = git(repository, "rev-parse", f"{commit}^{{tree}}")

        # The measurement space is created here and belongs to this run alone. Whatever the caller's
        # own worktree holds -- modified tracked files, ignored build output, a counterfeit assembly --
        # cannot reach it.
        owned = Path(tempfile.mkdtemp(prefix="api-surface-provenance-"))
        worktree = Path(os.path.realpath(owned/"worktree"))
        git(repository, "worktree", "add", "--detach", str(worktree), commit)

        require_pristine(worktree, commit, "before the build")
        require_no_output(worktree)

        solution = canonical_within(worktree, worktree/SOLUTION)
        if not solution.is_file():
            raise CensusError(f"{SOLUTION} does not exist in the measured worktree")

        result = subprocess.run(list(BUILD), cwd=worktree, capture_output=True, text=True)
        if args.build_log:
            args.build_log.parent.mkdir(parents=True, exist_ok=True)
            args.build_log.write_text(result.stdout + result.stderr, encoding="utf-8")
        if result.returncode != 0:
            raise CensusError(f"the build of {SOLUTION} failed: "
                              + (result.stderr.strip() or result.stdout.strip())[-400:])

        # Only untracked build output may have appeared; nothing tracked may have moved.
        require_pristine(worktree, commit, "after the build")

        collected = collect(census, worktree, args.out)
        verify_identities(args.out, {entry["assembly"] for entry in census["entries"]})
    except CensusError as error:
        print(f"FAIL api-assemblies: {error}", file=sys.stderr)
        return 1
    finally:
        # Only what this run created. A caller supplied path is never removed.
        if worktree is not None and worktree.exists():
            subprocess.run(["git", "-C", str(repository), "worktree", "remove", "--force", str(worktree)],
                           capture_output=True, text=True)
        if owned is not None and owned.exists():
            shutil.rmtree(owned, ignore_errors=True)

    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.write_text(json.dumps({
        "schemaVersion": 1,
        "kind": "API_SURFACE_INPUT_MANIFEST",
        "measuredCommit": commit,
        "measuredTree": tree,
        "provenance": ("measured in a detached worktree of this commit that the tool created under its own temporary "
                       "directory, proved clean and free of build output before the build and unchanged in its tracked "
                       "files after it, then removed by the tool. The caller supplies neither the measured root nor "
                       "the build target nor the reader."),
        "buildCommand": " ".join(BUILD),
        "tools": [{"path": f"tools/ci/{Path(__file__).name}", "sha256": sha256(Path(__file__))},
                  {"path": f"tools/ci/{READER.name}", "sha256": sha256(READER)}],
        "census": {"path": str(args.census), "sha256": sha256(args.census)},
        "assemblyCount": len(collected),
        "assemblies": sorted(collected, key=lambda item: item["assembly"]),
    }, indent=2) + "\n", encoding="utf-8")

    print(f"PASS api-assemblies {len(collected)} of {census['expectedCount']} expected assemblies -> {args.manifest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

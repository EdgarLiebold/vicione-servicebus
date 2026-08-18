#!/usr/bin/env python3
"""The one active verification truth, loaded and checked in both directions.

build/verification/VERIFICATION_MODEL.json says what this product carries, which project belongs to
which capability, and how each capability is verified. It replaced two files that said overlapping
things: a capability matrix that classified only the source projects, and a not-executed inventory that
listed the same categories a second time by hand. A second hand kept list is a second truth, and the
two had already drifted.

The invariants below are checked by tools/ci/policy_validator.py before any restore, and each of them
has a sabotage case in tools/ci/test_policy_validator.py.

Run it directly to print the model:

    python3 tools/ci/verification_model.py
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from xml.etree import ElementTree


MODEL_FILE = "build/verification/VERIFICATION_MODEL.json"

REQUIRED_RUN_CLASSES = frozenset({"LOCAL_REQUIRED_RUN", "PINNED_FIXTURE_REQUIRED_RUN"})

PROJECT_KEYS = ("sourceProjects", "testProjects", "supportProjects", "toolProjects")


class ModelError(RuntimeError):
    pass


def load(root: Path) -> dict:
    path = root / MODEL_FILE
    if not path.is_file():
        raise ModelError(f"{MODEL_FILE} is missing, so there is no active verification truth to read")

    try:
        model = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise ModelError(f"{MODEL_FILE} is not parsable: {error}") from error

    if model.get("kind") != "SERVICEBUS_VERIFICATION_MODEL":
        raise ModelError(f"{MODEL_FILE} is not a verification model")

    return model


def runs(model: dict) -> list[dict]:
    """Every required run the model declares, with the capability it belongs to."""
    declared: list[dict] = []
    for capability in model.get("capabilities", []):
        for run in capability.get("runs", []):
            declared.append(dict(run, capability=capability["id"], capabilityClass=capability.get("class")))

    return declared


def category(model: dict, name: str) -> dict | None:
    for run in runs(model):
        if run.get("category") == name:
            return run

    return None


class WorkflowShapeError(RuntimeError):
    """The workflow is not in the shape this reader understands.

    A small reader that guesses is worse than no reader: it agrees with the model for the wrong
    reason. Anything it was not written for is refused so that somebody looks, rather than silently
    producing a job list that happens to be empty.
    """


def workflow_jobs(root: Path) -> dict[str, str]:
    """Job name to the text of that job, read from the required workflow.

    Deliberately a small line reader rather than a YAML parser, because it must not depend on a
    package to run before a restore. It therefore refuses everything it was not written for: a
    missing or empty jobs section, a job key that is not a plain name, a flow mapping, an anchor or
    a merge key.
    """
    workflow = root / ".github/workflows/build.yml"
    if not workflow.is_file():
        raise WorkflowShapeError(".github/workflows/build.yml is missing")

    lines = workflow.read_text(encoding="utf-8").splitlines()
    if not any(line.rstrip() == "jobs:" for line in lines):
        raise WorkflowShapeError("the workflow has no plain 'jobs:' section")

    jobs: dict[str, str] = {}
    current: str | None = None
    inside = False
    for number, line in enumerate(lines, 1):
        if line.rstrip() == "jobs:":
            if inside:
                raise WorkflowShapeError(f"line {number}: a second jobs section")
            inside = True
            continue
        if not inside:
            continue
        if line and not line.startswith(" ") and line.rstrip().endswith(":"):
            break
        if line.startswith("  ") and not line.startswith("    ") and line.strip():
            key = line.strip()
            if not key.endswith(":"):
                raise WorkflowShapeError(f"line {number}: '{key}' is not a plain job key")
            name = key.rstrip(":").strip()
            if not name or not all(part.isalnum() or part in "-_" for part in name):
                raise WorkflowShapeError(f"line {number}: '{name}' is not a plain job name")
            if name in jobs:
                raise WorkflowShapeError(f"line {number}: job '{name}' is declared twice")
            current = name
            jobs[current] = ""
        elif current is not None:
            if line.lstrip().startswith(("<<:", "&", "*")):
                raise WorkflowShapeError(f"line {number}: anchors and merge keys are not supported here")
            jobs[current] += line + "\n"

    if not jobs:
        raise WorkflowShapeError("the jobs section is empty")

    return jobs


def resolve_indirect_verification(root: Path, capabilities: list[dict]) -> list[str]:
    """Follows every verifiedThroughCapability to a run, and refuses a link that proves nothing.

    A prose link is not a proof. It has to end at a capability that really declares a run, it may not
    lead in a circle or back to itself, and the capability that leans on it has to name the fixtures
    inside that run's test project which actually exercise it - checked to exist, and checked to
    belong to that project rather than to some other one with a similar name.
    """
    problems: list[str] = []
    by_id = {capability.get("id"): capability for capability in capabilities}

    for capability in capabilities:
        identity = capability.get("id")
        through = capability.get("verifiedThroughCapability")
        if not through:
            continue

        seen = [identity]
        current = through
        terminal: dict | None = None
        while True:
            if current in seen:
                problems.append(
                    f"capability '{identity}' is verified through a cycle: {' -> '.join(seen + [current])}")
                break
            seen.append(current)

            target = by_id.get(current)
            if target is None:
                problems.append(
                    f"capability '{identity}' says it is verified through '{current}', which is not a capability")
                break

            if target.get("runs"):
                terminal = target
                break

            current = target.get("verifiedThroughCapability")
            if not current:
                problems.append(
                    f"capability '{identity}' is verified through '{seen[-1]}', which declares no run "
                    "either, so the chain ends without a proof")
                break

        if terminal is None:
            continue

        anchors = capability.get("testAnchors")
        if not anchors:
            problems.append(
                f"capability '{identity}' leans on the run of '{terminal['id']}' but names no test anchor, "
                "so nothing states which cases there exercise it")
            continue

        projects = [root / project for project in terminal.get("testProjects", [])]
        sources = [source for project in projects for source in project.rglob("*.cs")
                   if "/bin/" not in source.as_posix() and "/obj/" not in source.as_posix()]
        text = "\n".join(source.read_text(encoding="utf-8-sig", errors="replace") for source in sources)

        for anchor in anchors:
            namespace, _, name = anchor.rpartition(".")
            if f"namespace {anchor}" in text:
                continue
            if f"namespace {namespace}" in text and (f"class {name}" in text or f"namespace {anchor}" in text):
                continue
            problems.append(
                f"capability '{identity}' names the anchor '{anchor}', which does not exist in the test "
                f"project of '{terminal['id']}'")

    return problems


def findings(root: Path) -> list[str]:
    """Every way the model can stop being the truth, as a list of sentences."""
    try:
        model = load(root)
    except ModelError as error:
        return [str(error)]

    problems: list[str] = []
    classes = set(model.get("verificationClasses", {}))
    capabilities = model.get("capabilities", [])

    if not capabilities:
        problems.append("the model lists no capability, so it proves nothing")

    seen_ids: set[str] = set()
    owner_of_project: dict[str, str] = {}

    for capability in capabilities:
        identity = capability.get("id", "<unnamed>")

        if identity in seen_ids:
            problems.append(f"capability '{identity}' is listed twice")
        seen_ids.add(identity)

        capability_class = capability.get("class")
        if isinstance(capability_class, list):
            problems.append(f"capability '{identity}' carries more than one verification class")
        elif capability_class is None:
            problems.append(f"capability '{identity}' carries no verification class")
        elif capability_class not in classes:
            problems.append(f"capability '{identity}' carries the unknown class '{capability_class}'")

        for key in PROJECT_KEYS:
            for project in capability.get(key, []):
                if project in owner_of_project:
                    problems.append(
                        f"project '{project}' belongs to '{owner_of_project[project]}' and to '{identity}'")
                owner_of_project[project] = identity
                if not list((root / project).glob("*.csproj")):
                    problems.append(f"capability '{identity}' names '{project}', which holds no project")

        through = capability.get("verifiedThroughCapability")
        if capability_class in REQUIRED_RUN_CLASSES and not capability.get("runs") and not through:
            problems.append(
                f"capability '{identity}' is verified by a required run but declares none and names no "
                "capability whose run covers it")
        if through and capability.get("runs"):
            problems.append(
                f"capability '{identity}' declares its own run and also claims to be verified through "
                f"'{through}', so which one proves it is undecided")

        for run in capability.get("runs", []):
            for field in ("job", "category", "project"):
                if not run.get(field):
                    problems.append(f"capability '{identity}' declares a run without a {field}")

            project = run.get("project")
            if project and not (root / project).is_file():
                problems.append(f"capability '{identity}' runs '{project}', which is not a project file")

            floor = run.get("minimumExecutedCases")
            if not isinstance(floor, int) or floor <= 0:
                problems.append(
                    f"the run of category '{run.get('category')}' declares no executed floor, so its "
                    "case count can fall without a single failure")

    problems.extend(resolve_indirect_verification(root, capabilities))

    # Every project of this repository is classified exactly once. A project nobody classifies ships
    # without anyone stating how it is verified.
    present = {p.parent.relative_to(root).as_posix() for p in root.rglob("*.csproj")
               if not p.relative_to(root).as_posix().startswith("artifacts/")}
    for project in sorted(present - set(owner_of_project)):
        problems.append(f"project '{project}' is retained but no capability classifies it")
    for project in sorted(set(owner_of_project) - present):
        problems.append(f"the model classifies '{project}', which this repository does not contain")

    # Both directions against the workflow: every declared run has a job that starts exactly it, and
    # every job of the required profile is explained by the model.
    try:
        jobs = workflow_jobs(root)
    except WorkflowShapeError as error:
        problems.append(f"the required workflow cannot be read: {error}")
        return problems

    explained: set[str] = {"policy", "build", "pack"}

    # A category is started by exactly one run. A job may hold several distinct categories - core-unit
    # runs core and abstractions - but two runs of one category are two truths about the same thing.
    declared = [run.get("category") for run in runs(model)]
    for category in sorted({name for name in declared if declared.count(name) > 1}):
        problems.append(f"category '{category}' is declared by more than one run")

    tuples = [(run.get("job"), run.get("category"), run.get("project")) for run in runs(model)]
    for entry in sorted({tuple(t) for t in tuples if tuples.count(t) > 1}):
        problems.append(f"the run {entry} is declared more than once")

    for run in runs(model):
        job = run.get("job")
        explained.add(job)
        if job not in jobs:
            problems.append(f"category '{run.get('category')}' names job '{job}', which the workflow does not have")
            continue

        body = jobs[job]
        if f"--category {run.get('category')}" not in body:
            problems.append(f"job '{job}' does not start category '{run.get('category')}'")
        if run.get("project") and run["project"] not in body:
            problems.append(f"job '{job}' does not run '{run.get('project')}' for category '{run.get('category')}'")

    for job in sorted(set(jobs) - explained):
        problems.append(f"the workflow has job '{job}', which no capability in the model explains")

    return problems


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args(argv)

    problems = findings(args.root)
    if problems:
        for problem in problems:
            print(f"FAIL verification-model {problem}", file=sys.stderr)
        return 1

    model = load(args.root)
    width = max(len(c["id"]) for c in model["capabilities"])
    print("Retained capabilities and how each one is verified:\n")
    for capability in sorted(model["capabilities"], key=lambda c: (c["class"], c["id"])):
        started = ", ".join(run["category"] for run in capability.get("runs", [])) or "no required run"
        print(f"  {capability['id']:<{width}}  {capability['class']:<28}  {started}")
    print("\nA capability that is not listed here is not part of this product.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

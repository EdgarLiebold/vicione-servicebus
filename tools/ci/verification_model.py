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


def workflow_jobs(root: Path) -> dict[str, str]:
    """Job name to the text of that job, read from the required workflow."""
    workflow = root / ".github/workflows/build.yml"
    if not workflow.is_file():
        return {}

    jobs: dict[str, str] = {}
    current: str | None = None
    inside = False
    for line in workflow.read_text(encoding="utf-8").splitlines():
        if line.rstrip() == "jobs:":
            inside = True
            continue
        if not inside:
            continue
        # A top level key other than jobs ends the section; anything at two spaces inside it is a job.
        if line and not line.startswith(" ") and line.rstrip().endswith(":"):
            break
        if line.startswith("  ") and not line.startswith("    ") and line.rstrip().endswith(":"):
            current = line.strip().rstrip(":")
            jobs[current] = ""
        elif current is not None:
            jobs[current] += line + "\n"

    return jobs


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
        if through and through not in {c.get("id") for c in capabilities}:
            problems.append(
                f"capability '{identity}' says it is verified through '{through}', which is not a capability")
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
    jobs = workflow_jobs(root)
    explained: set[str] = {"policy", "build", "pack"}
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

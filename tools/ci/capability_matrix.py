#!/usr/bin/env python3
"""The one active capability truth, loaded and checked.

build/test-infrastructure/capability-matrix.json lists what this product carries and how each of it is
verified. Everything that used to be modelled as a state value on a removed capability is gone: a
capability that was removed is absent here, and its removal lives in the keep/remove decision,
CHANGELIST.md and the evidence. That is the modelling error this file exists to prevent, not a
formatting preference - the previous inventory said "source and tests are preserved" for eight
capabilities whose files the tree does not contain, and "no fixture" for two that run as required
gates.

The invariants below are checked by tools/ci/policy_validator.py before any restore, and each of them
has a sabotage case in tools/ci/test_policy_validator.py.

Run it directly to print the matrix:

    python3 tools/ci/capability_matrix.py
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


MATRIX_FILE = "build/test-infrastructure/capability-matrix.json"

REQUIRED_RUN_CLASSES = frozenset({"LOCAL_REQUIRED_RUN", "PINNED_FIXTURE_REQUIRED_RUN"})


class MatrixError(RuntimeError):
    pass


def load(root: Path) -> dict:
    path = root / MATRIX_FILE
    if not path.is_file():
        raise MatrixError(f"{MATRIX_FILE} is missing, so there is no active capability truth to read")

    try:
        matrix = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise MatrixError(f"{MATRIX_FILE} is not parsable: {error}") from error

    if matrix.get("kind") != "SERVICEBUS_CAPABILITY_MATRIX":
        raise MatrixError(f"{MATRIX_FILE} is not a capability matrix")

    return matrix


def findings(root: Path) -> list[str]:
    """Every way the matrix can stop being the truth, as a list of sentences."""
    try:
        matrix = load(root)
    except MatrixError as error:
        return [str(error)]

    problems: list[str] = []
    classes = set(matrix.get("verificationClasses", {}))
    capabilities = matrix.get("capabilities", [])

    if not capabilities:
        problems.append("the matrix lists no capability, so it proves nothing")

    seen_ids: set[str] = set()
    owner_of_source: dict[str, str] = {}

    for capability in capabilities:
        identity = capability.get("id", "<unnamed>")

        if identity in seen_ids:
            problems.append(f"capability '{identity}' is listed twice")
        seen_ids.add(identity)

        capability_class = capability.get("class")
        if capability_class is None:
            problems.append(f"capability '{identity}' carries no verification class")
        elif capability_class not in classes:
            problems.append(f"capability '{identity}' carries the unknown class '{capability_class}'")

        # A capability with two classes cannot be verified in one defined way, which is exactly the
        # ambiguity the previous inventory allowed.
        if isinstance(capability_class, list):
            problems.append(f"capability '{identity}' carries more than one verification class")

        for source in capability.get("sourceProjects", []):
            if source in owner_of_source:
                problems.append(
                    f"source project '{source}' belongs to '{owner_of_source[source]}' and to '{identity}'")
            owner_of_source[source] = identity
            if not list((root / source).glob("*.csproj")):
                problems.append(f"capability '{identity}' names '{source}', which holds no project")

        for test in capability.get("testProjects", []) + capability.get("toolProjects", []):
            if not list((root / test).glob("*.csproj")):
                problems.append(f"capability '{identity}' names '{test}', which holds no project")

        if capability_class in REQUIRED_RUN_CLASSES and not capability.get("requiredJobs"):
            problems.append(
                f"capability '{identity}' is verified by a required run but names no job that runs it")

    # Every retained source project has to be claimed by exactly one capability. An unclassified one
    # would ship without anyone stating how it is verified.
    tracked = {p.parent.relative_to(root).as_posix() for p in root.glob("src/**/*.csproj")}
    for source in sorted(tracked - set(owner_of_source)):
        problems.append(f"source project '{source}' is retained but no capability classifies it")

    return problems


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args(argv)

    problems = findings(args.root)
    if problems:
        for problem in problems:
            print(f"FAIL capability-matrix {problem}", file=sys.stderr)
        return 1

    matrix = load(args.root)
    width = max(len(c["id"]) for c in matrix["capabilities"])
    print("Retained capabilities and how each one is verified:\n")
    for capability in sorted(matrix["capabilities"], key=lambda c: (c["class"], c["id"])):
        print(f"  {capability['id']:<{width}}  {capability['class']}")
    print("\nA capability that is not listed here is not part of this product.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

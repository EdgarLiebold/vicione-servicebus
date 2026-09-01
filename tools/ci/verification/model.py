#!/usr/bin/env python3
"""The one active verification truth, loaded and checked in both directions.

build/verification/VERIFICATION_MODEL.json says what this product carries, which project belongs to
which capability, and how each capability is verified. It replaced two files that said overlapping
things: a capability matrix that classified only the source projects, and a not-executed inventory that
listed the same categories a second time by hand. A second hand kept list is a second truth, and the
two had already drifted.

This model belongs only to the inherited test estate. The replacement xUnit/MTP estate is independent
and does not extend this model.

Run it directly to print the model:

    python3 tools/ci/verification/model.py
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree


MODEL_FILE = "build/verification/VERIFICATION_MODEL.json"

REQUIRED_RUN_CLASSES = frozenset({"LOCAL_REQUIRED_RUN", "PINNED_FIXTURE_REQUIRED_RUN"})

# Where an expected identity set lives, and the only place one is read from.
EXPECTED_DIRECTORY = "build/verification/expected"

PROJECT_KEYS = ("sourceProjects", "testProjects", "supportProjects", "toolProjects")

VERIFY_ENTRYPOINT = "python3 tools/ci/verify.py"
CHECKOUT_ACTION_STEP = "- uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1"
SETUP_DOTNET_ACTION_STEP = "- uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0"
UPLOAD_ARTIFACT_ACTION_STEP = "- uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1"

# The line reader intentionally supports only the canonical subset used by this repository. GitHub
# accepts additional YAML spellings, but accepting syntax that this reader does not understand would
# let YAML semantics differ from the text being verified. Workflow-level defaults are deliberately
# absent because they can replace the shell of every required verification step.
WORKFLOW_TOP_LEVEL_KEYS = ("name", "on", "permissions", "env", "jobs")

VERIFICATION_JOB_KEYS = ("name", "runs-on", "timeout-minutes", "steps")
VERIFICATION_JOB_KEYS_WITH_ENV = ("name", "runs-on", "timeout-minutes", "env", "steps")
VERIFICATION_JOB_ENVIRONMENT = ("      DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: false",)

# These required jobs do not map to one verification selection, but they are executable gates rather
# than decorative workflow entries. Their normalized non-comment lines are kept as explicit contracts
# so deleting, reordering or weakening any command cannot leave the required profile falsely green.
REQUIRED_SUPPORT_JOB_CONTRACTS = {
    "legacy-tooling": (
        '    name: "Required: Legacy Test Tooling"',
        "    runs-on: ubuntu-24.04",
        "    timeout-minutes: 10",
        "    steps:",
        f"      {CHECKOUT_ACTION_STEP}",
        "      - name: Verification model",
        "        run: python3 tools/ci/verification/model.py",
        "      - name: CI tool self-tests",
        "        run: python3 -m unittest discover -s tools/ci -p 'test_*.py'",
        "      - name: Identity tool self-tests",
        "        run: python3 -m unittest discover -s tools/identity -p 'test_*.py'",
    ),
    "build": (
        '    name: "Required: Build"',
        "    runs-on: ubuntu-24.04",
        "    timeout-minutes: 30",
        "    steps:",
        f"      {CHECKOUT_ACTION_STEP}",
        f"      {SETUP_DOTNET_ACTION_STEP}",
        "        with:",
        "          dotnet-version: ${{ env.DOTNET_VERSION }}",
        "      - name: Restore",
        "        run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode",
        "      - name: Build",
        "        run: dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore",
        "      - name: Restore the engineering solution",
        "        run: dotnet restore ViciOne.ServiceBus.Engineering.slnx --locked-mode",
        "      - name: Build the engineering solution",
        "        run: dotnet build ViciOne.ServiceBus.Engineering.slnx -c Release --no-restore",
    ),
    "pack": (
        '    name: "Required: Pack"',
        "    runs-on: ubuntu-24.04",
        "    timeout-minutes: 30",
        "    needs:",
        "      - legacy-tooling",
        "      - build",
        "      - benchmarks",
        "      - rabbitmq",
        "    steps:",
        f"      {CHECKOUT_ACTION_STEP}",
        f"      {SETUP_DOTNET_ACTION_STEP}",
        "        with:",
        "          dotnet-version: ${{ env.DOTNET_VERSION }}",
        "      - name: Restore",
        "        run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode",
        "      - name: Build",
        "        run: dotnet build ViciOne.ServiceBus.slnx -c Release --no-restore",
        "      - name: Pack",
        "        run: |",
        "          rm -rf artifacts/packages",
        "          dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore -o artifacts/packages",
        "      - name: Hash packages",
        "        run: |",
        "          set -euo pipefail",
        '          test -n "$(ls -A artifacts/packages)" || { echo "pack produced no package"; exit 1; }',
        "          sha256sum artifacts/packages/*.nupkg | tee artifacts/packages/SHA256SUMS",
        f"      {UPLOAD_ARTIFACT_ACTION_STEP}",
        "        with:",
        "          name: required-packages",
        "          path: artifacts/packages",
        "          if-no-files-found: error",
    ),
}


class SelectionError(RuntimeError):
    """A selection that cannot be resolved to a set of categories."""


def resolve_selection(model: dict, name: str) -> list[str]:
    """The categories a selection names, transitively.

    A member is a category when the model declares one by that name, and a selection otherwise. A
    selection may carry the name of the single category it stands for, which is not a circle. Unknown
    members and real circles are refused rather than resolving to a smaller scope, because a scope that
    quietly shrinks is how a narrow run comes to look like a complete one.
    """
    selections = model.get("selections") or {}
    if name not in selections:
        raise SelectionError(f"'{name}' is not a selection this model declares")

    categories = {run.get("category") for run in runs(model)}
    resolved: list[str] = []

    def walk(member: str, path: tuple[str, ...]) -> None:
        if member in categories:
            if member not in resolved:
                resolved.append(member)
            return
        if member not in selections:
            raise SelectionError(
                f"selection '{path[-1]}' names '{member}', which is neither a selection nor a category")
        if member in path:
            raise SelectionError(f"selection '{name}' resolves in a circle: {' -> '.join(path + (member,))}")
        for nested in selections[member].get("members", []):
            walk(nested, path + (member,))

    for member in selections[name].get("members", []):
        walk(member, (name,))

    return sorted(resolved)


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


def canonical_workflow_header(root: Path) -> tuple[str, ...]:
    """The exact non-comment header of the required workflow.

    Trigger, permission and environment changes alter whether or how verification runs. They are
    therefore part of the same executable contract as the job commands, not free-form workflow
    decoration. The SDK value is derived from global.json so there is one version truth.
    """
    global_json = root / "global.json"
    if not global_json.is_file():
        raise WorkflowShapeError("global.json is missing, so the workflow SDK cannot be verified")
    try:
        sdk_version = json.loads(global_json.read_text(encoding="utf-8"))["sdk"]["version"]
    except (json.JSONDecodeError, KeyError, TypeError) as error:
        raise WorkflowShapeError("global.json has no readable sdk.version") from error
    if not isinstance(sdk_version, str) or re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", sdk_version) is None:
        raise WorkflowShapeError("global.json sdk.version is not an exact stable SDK version")

    return (
        "name: Required CI",
        "on:",
        "  push:",
        "    branches:",
        "      - '**'",
        "  pull_request:",
        "  workflow_dispatch:",
        "permissions:",
        "  contents: read",
        "env:",
        f"  DOTNET_VERSION: '{sdk_version}'",
        "  DOTNET_CLI_TELEMETRY_OPTOUT: 1",
        "  DOTNET_NOLOGO: 1",
        "jobs:",
    )


def workflow_jobs(root: Path) -> dict[str, str]:
    """Job name to the text of that job, read from the required workflow.

    Deliberately a small line reader rather than a YAML parser, because it must not depend on a
    package to run before a restore. It therefore refuses everything it was not written for: a
    quoted, escaped, tagged or complex top-level key, a BOM, workflow defaults, a missing or empty
    jobs section, a job key that is not a plain name, a flow mapping, an anchor or a merge key.
    """
    workflow = root / ".github/workflows/build.yml"
    if not workflow.is_file():
        raise WorkflowShapeError(".github/workflows/build.yml is missing")

    raw_workflow = workflow.read_bytes()
    if raw_workflow.startswith(b"\xef\xbb\xbf"):
        raise WorkflowShapeError("the workflow must be UTF-8 without a byte-order mark")
    try:
        lines = raw_workflow.decode("utf-8").splitlines()
    except UnicodeDecodeError as error:
        raise WorkflowShapeError("the workflow must be valid UTF-8") from error

    top_level_keys: list[str] = []
    for number, line in enumerate(lines, 1):
        if not line or line.startswith("#") or line != line.lstrip():
            continue
        match = re.fullmatch(r"([a-z][a-z-]*):(?:[ \t].*)?", line)
        if match is None:
            raise WorkflowShapeError(
                f"line {number}: top-level workflow keys must use the supported unquoted plain form")
        key = match.group(1)
        if key == "defaults":
            raise WorkflowShapeError(
                f"line {number}: top-level defaults are forbidden because they can replace or "
                "weaken every verification shell")
        if key not in WORKFLOW_TOP_LEVEL_KEYS:
            raise WorkflowShapeError(f"line {number}: unsupported top-level workflow key '{key}'")
        if key in top_level_keys:
            raise WorkflowShapeError(f"line {number}: top-level workflow key '{key}' is declared twice")
        top_level_keys.append(key)

    if "jobs" not in top_level_keys or not any(line.rstrip() == "jobs:" for line in lines):
        raise WorkflowShapeError("the workflow has no plain 'jobs:' section")
    if tuple(top_level_keys) != WORKFLOW_TOP_LEVEL_KEYS:
        raise WorkflowShapeError(
            f"workflow top-level keys are {tuple(top_level_keys)!r}; expected {WORKFLOW_TOP_LEVEL_KEYS!r}")

    jobs_line = next(number for number, line in enumerate(lines) if line.rstrip() == "jobs:")
    actual_header = tuple(
        line for line in lines[:jobs_line + 1]
        if line.strip() and not line.lstrip().startswith("#")
    )
    expected_header = canonical_workflow_header(root)
    if actual_header != expected_header:
        raise WorkflowShapeError(
            "workflow triggers, permissions and environment do not match the canonical required header")

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


def verification_step_findings(job: str, job_text: str, selection: str) -> list[str]:
    """Refuse a workflow job that does not execute its model-owned selection exactly once.

    The verification model owns the job-to-selection mapping. The workflow owns only the mechanical
    call into that model. This boundary is deliberately narrow: checkout, SDK setup, verification and
    result upload are the only steps, in that order, and every executable action is commit-pinned.
    Conditions, defaults, extra commands and shell composition would all create a second way for the
    workflow to weaken or replace the model. The job mapping itself is therefore a canonical plain-key
    structure as well; YAML aliases or escaped keys cannot hide controls from this reader.
    """
    problems: list[str] = []
    expected = ("- name: Verify", f"run: {VERIFY_ENTRYPOINT} --selection {selection}")

    lines = job_text.splitlines()
    direct_keys: list[str] = []
    direct_lines: dict[str, tuple[int, str]] = {}
    for number, line in enumerate(lines, 1):
        if not line.strip() or line.startswith("    #"):
            continue
        if line.startswith("    ") and not line.startswith("      "):
            directive = line[4:]
            match = re.fullmatch(r"([a-z][a-z-]*):(.*)", directive)
            if match is None:
                problems.append(
                    f"workflow job '{job}' line {number} does not use a supported unquoted plain key")
                continue
            key = match.group(1)
            if key in direct_lines:
                problems.append(f"workflow job '{job}' declares job key '{key}' twice")
                continue
            direct_keys.append(key)
            direct_lines[key] = (number - 1, directive)

    key_sequence = tuple(direct_keys)
    if key_sequence not in (VERIFICATION_JOB_KEYS, VERIFICATION_JOB_KEYS_WITH_ENV):
        problems.append(
            f"workflow job '{job}' has job keys {key_sequence!r}; expected the canonical verification "
            "job structure")

    if "name" in direct_lines and re.fullmatch(r"name: .+", direct_lines["name"][1]) is None:
        problems.append(f"workflow job '{job}' must have a non-empty display name")
    if "runs-on" in direct_lines and direct_lines["runs-on"][1] != "runs-on: ubuntu-24.04":
        problems.append(f"workflow job '{job}' must run on ubuntu-24.04")
    if "timeout-minutes" in direct_lines and re.fullmatch(
        r"timeout-minutes: [1-9][0-9]*", direct_lines["timeout-minutes"][1]
    ) is None:
        problems.append(f"workflow job '{job}' must have a positive integer timeout")
    if "steps" in direct_lines and direct_lines["steps"][1] != "steps:":
        problems.append(f"workflow job '{job}' must declare steps as a block sequence")

    if "env" in direct_lines and "steps" in direct_lines:
        env_start = direct_lines["env"][0] + 1
        env_end = direct_lines["steps"][0]
        environment = tuple(
            line for line in lines[env_start:env_end]
            if line.strip() and not line.lstrip().startswith("#")
        )
        if environment != VERIFICATION_JOB_ENVIRONMENT:
            problems.append(
                f"workflow job '{job}' has environment {environment!r}; only the globalization "
                "setting required by the database fixtures is supported")

    blocks: list[list[str]] = []
    current: list[str] | None = None
    inside_steps = False
    for line in lines:
        if line == "    steps:":
            inside_steps = True
            continue
        if not inside_steps:
            continue
        if line.startswith("      - "):
            if current is not None:
                blocks.append(current)
            current = [line.strip()]
        elif current is not None and line.startswith("        "):
            stripped = line.strip()
            if stripped and not stripped.startswith("#"):
                current.append(stripped)
        elif line.strip() and not line.startswith("      "):
            if current is not None:
                blocks.append(current)
                current = None
            inside_steps = False
    if current is not None:
        blocks.append(current)

    expected_checkout = (CHECKOUT_ACTION_STEP,)
    expected_setup = (
        SETUP_DOTNET_ACTION_STEP,
        "with:",
        "dotnet-version: ${{ env.DOTNET_VERSION }}",
    )
    upload_is_exact = (
        len(blocks) == 4
        and len(blocks[3]) == 7
        and blocks[3][0] == UPLOAD_ARTIFACT_ACTION_STEP
        and blocks[3][1] == "if: always()"
        and blocks[3][2] == "with:"
        and re.fullmatch(r"name: required-[a-z0-9-]+", blocks[3][3]) is not None
        and blocks[3][4:] == ["path: |", "artifacts/verification", "artifacts/run-output"]
    )
    if not (
        len(blocks) == 4
        and tuple(blocks[0]) == expected_checkout
        and tuple(blocks[1]) == expected_setup
        and tuple(blocks[2]) == expected
        and upload_is_exact
    ):
        rendered = "; ".join(" | ".join(block) for block in blocks) or "<missing>"
        problems.append(
            f"workflow job '{job}' must contain exactly pinned checkout, pinned SDK setup, the "
            f"canonical step '{expected[0]} | {expected[1]}', and pinned result upload; found: "
            f"{rendered}")

    return problems


def support_job_findings(job: str, job_text: str) -> list[str]:
    """Compare a required support job with its explicit executable contract."""
    expected = REQUIRED_SUPPORT_JOB_CONTRACTS[job]
    actual = tuple(
        line for line in job_text.splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    )
    if actual == expected:
        return []

    common = min(len(actual), len(expected))
    first_difference = next((index for index in range(common) if actual[index] != expected[index]), common)
    actual_line = actual[first_difference] if first_difference < len(actual) else "<missing>"
    expected_line = expected[first_difference] if first_difference < len(expected) else "<no further line>"
    return [
        f"required workflow job '{job}' differs at executable line {first_difference + 1}: "
        f"expected {expected_line!r}, found {actual_line!r}"
    ]


def pack_needs_findings(pack_job_text: str, job_map: dict[str, str]) -> list[str]:
    """Require pack to wait for both support gates and every model-owned verification job."""
    needs: list[str] = []
    inside_needs = False
    for line in pack_job_text.splitlines():
        if line == "    needs:":
            inside_needs = True
            continue
        if not inside_needs:
            continue
        if line.startswith("      - "):
            needs.append(line.removeprefix("      - "))
            continue
        if line.strip() and not line.lstrip().startswith("#"):
            break

    expected = {"legacy-tooling", "build", *job_map}
    if len(needs) != len(set(needs)) or set(needs) != expected:
        return [
            f"pack waits for {tuple(needs)!r}; expected every required gate {tuple(sorted(expected))!r}"
        ]
    return []


LINE_COMMENT = re.compile(r"//[^\n]*")
BLOCK_COMMENT = re.compile(r"/\*.*?\*/", re.S)
NAMESPACE_DECLARATION = re.compile(r"^\s*namespace\s+([A-Za-z_][\w.]*)", re.M)
TEST_ATTRIBUTE = re.compile(r"\[\s*(?:Test|TestCase|TestCaseSource|Theory)\b")


def declared_fixtures(root: Path, project: str) -> tuple[set[str], set[str]]:
    """Every class of a test project that could be a fixture, and every namespace it declares.

    Comments are removed first. The counterexample that made this necessary was an anchor satisfied by
    a source file which contained nothing but words: the old check searched the raw text for
    'namespace X' and 'class Y', so a sentence in a comment was as good as a declaration.

    A class only counts when the file it is declared in also carries a test attribute. That is what
    separates a fixture from an ordinary class - the second thing the old check accepted.

    Deliberately generous about nesting: a file that declares two namespaces registers its classes under
    both. This is a check before a run, and being wrong in the permissive direction here costs nothing -
    tools/ci/run_test_category.py binds the same anchor to an executed identity of the real run, where a
    name that belongs to another namespace has no case at all.
    """
    fixtures: set[str] = set()
    namespaces: set[str] = set()

    for source in sorted((root / project).rglob("*.cs")):
        if "/bin/" in source.as_posix() or "/obj/" in source.as_posix():
            continue

        text = source.read_text(encoding="utf-8-sig", errors="replace")
        text = LINE_COMMENT.sub("", BLOCK_COMMENT.sub("", text))

        declared = NAMESPACE_DECLARATION.findall(text)
        namespaces.update(declared)
        if not declared or not TEST_ATTRIBUTE.search(text):
            continue

        for name in re.findall(r"\bclass\s+([A-Za-z_]\w*)", text):
            for namespace in declared:
                fixtures.add(f"{namespace}.{name}")

    return fixtures, namespaces


def resolve_indirect_verification(root: Path, capabilities: list[dict]) -> list[str]:
    """Follows every verifiedThroughCapability to a run, and refuses a link that proves nothing.

    A prose link is not a proof. It has to end at a capability that really declares a run, it may not
    lead in a circle or back to itself, and the capability that leans on it has to name the exact test
    fixtures which exercise it, each together with the category whose run executes them.

    Naming the category is the point. An anchor that only says which type it means leaves open which of
    the terminal capability's runs is supposed to prove it, and a run cannot bind what it does not know
    is its own. With the category on the anchor, tools/ci/run_test_category.py collects exactly the
    anchors of the category it is running and requires each of them to have executed.
    """
    problems: list[str] = []
    by_id = {capability.get("id"): capability for capability in capabilities}
    fixtures_of: dict[str, tuple[set[str], set[str]]] = {}

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

        runs_by_category = {run.get("category"): run for run in terminal.get("runs", []) if run.get("category")}

        for anchor in anchors:
            if not isinstance(anchor, dict) or not anchor.get("category") or not anchor.get("fixture"):
                problems.append(
                    f"capability '{identity}' names the anchor {anchor!r}, which is not a category and a "
                    "fixture; an anchor that does not say which run executes it binds nothing")
                continue

            category, fixture = anchor["category"], anchor["fixture"]
            run = runs_by_category.get(category)
            if run is None:
                problems.append(
                    f"capability '{identity}' anchors '{fixture}' in category '{category}', which the "
                    f"capability '{terminal['id']}' it leans on does not run")
                continue

            project = run.get("testProjectDirectory")
            if not project:
                problems.append(
                    f"the run of category '{category}' names no test project directory, so an anchor in it "
                    "cannot be checked to exist")
                continue

            if project not in fixtures_of:
                fixtures_of[project] = declared_fixtures(root, project)
            fixtures, namespaces = fixtures_of[project]

            if fixture in namespaces:
                problems.append(
                    f"capability '{identity}' anchors the namespace '{fixture}', which names every case "
                    "that happens to live under it rather than the ones that exercise this capability")
                continue

            if fixture not in fixtures:
                problems.append(
                    f"capability '{identity}' anchors '{fixture}', which is not a test fixture of "
                    f"'{project}'")

    return problems


def run_shape(run: dict) -> list[str]:
    """Every field of one run that decides what happens, checked for being a value of that kind.

    A model is only an independent truth while everything in it is readable as what it claims to be.
    A budget that is a string is a budget nothing can compare against, and a broker list that is not a
    list is a fixture nobody can start; both used to be carried into a command and discovered there.
    """
    category = run.get("category")
    problems = []

    budget = run.get("budgetSeconds")
    if not isinstance(budget, (int, float)) or isinstance(budget, bool) or budget <= 0:
        problems.append(f"the run of category '{category}' declares the budget {budget!r}, so a test "
                        "process that never returns would hold it for as long as the machine stays up")

    brokers = run.get("brokers")
    if brokers is None:
        brokers = []
    if not isinstance(brokers, list) or not all(isinstance(name, str) and name for name in brokers):
        problems.append(f"the run of category '{category}' declares the brokers {brokers!r}, which is "
                        "not a list of broker names")
        brokers = []
    elif len(set(brokers)) != len(brokers):
        problems.append(f"the run of category '{category}' names a broker twice: {brokers}")

    outage = run.get("allowBrokerOutage")
    if outage is not None and not isinstance(outage, str):
        problems.append(f"the run of category '{category}' declares the outage permission {outage!r}, "
                        "which is not a broker name")
    elif isinstance(outage, str) and outage not in brokers:
        problems.append(f"the run of category '{category}' permits an outage of '{outage}', which is "
                        "not one of the brokers it starts")

    refusal = run.get("oneRefusalPerVhost")
    if refusal is not None and not isinstance(refusal, str):
        problems.append(f"the run of category '{category}' declares the refusal rule {refusal!r}, "
                        "which is not a pattern")

    declared = run.get("expectedIdentities")
    if declared is not None:
        if not isinstance(declared, str):
            problems.append(f"the run of category '{category}' declares the expected set {declared!r}, "
                            "which is not a path")
        elif declared != f"{EXPECTED_DIRECTORY}/{category}.txt":
            problems.append(f"the run of category '{category}' names the expected set '{declared}', "
                            f"and the expected set of a category is '{EXPECTED_DIRECTORY}/"
                            f"{category}.txt'")

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

            problems.extend(run_shape(run))

    problems.extend(resolve_indirect_verification(root, capabilities))

    # Every project owned by the inherited verification estate is classified exactly once. The
    # replacement xUnit/MTP estate under tests2 is intentionally outside this model and runs through
    # .github/workflows/native-tests.yml. Excluding that one path is a boundary, not an exemption:
    # every product, inherited test, benchmark and tool project remains fail-closed here.
    present = {p.parent.relative_to(root).as_posix() for p in root.rglob("*.csproj")
               if not p.relative_to(root).as_posix().startswith(("artifacts/", "tests2/"))}
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

    job_of_selection = model.get("jobs") or {}
    explained: set[str] = set(REQUIRED_SUPPORT_JOB_CONTRACTS)
    for job in REQUIRED_SUPPORT_JOB_CONTRACTS:
        if job not in jobs:
            problems.append(f"the required workflow has no '{job}' support job")
        else:
            problems.extend(support_job_findings(job, jobs[job]))
    if "pack" in jobs:
        problems.extend(pack_needs_findings(jobs["pack"], job_of_selection))

    # A category is started by exactly one run. A job may hold several distinct categories, but two
    # runs of one category are two truths about the same thing.
    declared = [run.get("category") for run in runs(model)]
    for category in sorted({name for name in declared if declared.count(name) > 1}):
        problems.append(f"category '{category}' is declared by more than one run")

    tuples = [(run.get("job"), run.get("category"), run.get("project")) for run in runs(model)]
    for entry in sorted({tuple(t) for t in tuples if tuples.count(t) > 1}):
        problems.append(f"the run {entry} is declared more than once")

    # The model is the only owner of the job-to-selection map. The workflow must nevertheless prove
    # that each job mechanically invokes that exact selection; otherwise a job can run a different,
    # green selection while the model validates only its own disconnected map.
    # Every selection, not only the ones a job names. A selection that resolves in a circle or to
    # nothing is a scope somebody can ask for, and asking for it is where it would be found otherwise.
    for name in sorted(model.get("selections") or {}):
        try:
            if not resolve_selection(model, name):
                problems.append(f"selection '{name}' resolves to no category at all")
        except SelectionError as error:
            problems.append(str(error))

    reached: dict[str, list[str]] = {}
    for job, selection in sorted(job_of_selection.items()):
        explained.add(job)
        if job not in jobs:
            problems.append(f"the model gives job '{job}' the selection '{selection}', and the workflow "
                            "has no such job")
            continue
        problems.extend(verification_step_findings(job, jobs[job], selection))
        try:
            for category in resolve_selection(model, selection):
                reached.setdefault(category, []).append(job)
        except SelectionError as error:
            problems.append(str(error))

    for run in runs(model):
        category = run.get("category")
        jobs_reaching = reached.get(category, [])
        if not jobs_reaching:
            problems.append(f"category '{category}' is declared and no required job's selection reaches "
                            "it, so nothing runs it")
        elif len(jobs_reaching) > 1:
            problems.append(f"category '{category}' is reached by {jobs_reaching}, so two required jobs "
                            "run it and its result belongs to neither")
        elif run.get("job") != jobs_reaching[0]:
            problems.append(f"category '{category}' declares the job '{run.get('job')}' and the job "
                            f"map routes it through '{jobs_reaching[0]}', so the model says two "
                            "different things about which job proves it")

    for job in sorted(set(jobs) - explained):
        problems.append(f"the workflow has job '{job}', which the model's job map does not explain")

    return problems


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    # This module sits one directory deeper than the entry points, so the root is three levels up
    # rather than two. A default that is off by one directory reads a model that is not there.
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[3])
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


# -- what one category is expected to execute, and what it may leave alone -------------------

def read_identity_file(path: Path) -> list[str]:
    """One identity per line, comments and blank lines ignored."""
    identities = []
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith("#"):
            identities.append(stripped)

    return identities


def expected_identities(run: dict, repo_root: Path) -> list[str] | None:
    """What this category is expected to execute, or None when nothing has recorded it yet.

    None is a state, not an omission. A category whose expected set nobody has recorded from a
    complete clean run cannot be part of a passing receipt, and the receipt says which ones those are.
    """
    declared = run.get("expectedIdentities")
    if not declared:
        return None

    path = repo_root / declared
    if not path.is_file():
        raise ModelError(
            f"category '{run.get('category')}' names the expected set '{declared}', which is not there")

    return read_identity_file(path)


def permitted_not_executed(run: dict) -> list[str]:
    """The cases the model permits this category to leave unexecuted, as full identities.

    An entry without a full identity is not a permission: naming a case by its short form would
    authorise every case that happens to share it.
    """
    permitted = []
    for case in run.get("notExecuted", []):
        identity = case.get("identity") if isinstance(case, dict) else None
        if isinstance(identity, str) and identity:
            permitted.append(identity)

    return permitted


# -- what one category is, and the command that starts it -----------------------------------------

EVIDENCE_DIR_OPTION = "--evidence-dir"


def declared_run(model: dict, category: str) -> dict:
    runs = [run for capability in model.get("capabilities", [])
            for run in capability.get("runs", []) if run.get("category") == category]
    if len(runs) != 1:
        raise ModelError(
            f"the model declares {len(runs)} runs for category '{category}', so which one is meant is "
            "undecided")

    return runs[0]


def child_command(run: dict, repo_root: Path, evidence_dir: Path) -> list[str]:
    """The exact command for this category, built from the model rather than from a workflow.

    A category with no broker goes straight to the category runner. One with brokers goes through the
    broker runner, which owns the fixture and hands the same run root down again.
    """
    category, project = run["category"], run["project"]
    if not run.get("brokers"):
        return [sys.executable, str(repo_root / "tools/ci/run_test_category.py"),
                "--category", category, "--project", project, EVIDENCE_DIR_OPTION, str(evidence_dir)]

    command = [sys.executable, str(repo_root / "tools/ci/run_broker_category.py")]
    for broker in run["brokers"]:
        command += ["--broker", broker]
    if run.get("allowBrokerOutage"):
        command += ["--allow-broker-outage", run["allowBrokerOutage"]]
    command += ["--category", category, "--project", project, EVIDENCE_DIR_OPTION, str(evidence_dir)]
    if run.get("oneRefusalPerVhost"):
        command += ["--one-refusal-per-vhost", run["oneRefusalPerVhost"]]

    return command


def without_evidence_dir(command: list[str]) -> list[str]:
    """The command without the one value a caller chooses, so two of them can be compared."""
    remaining = list(command)
    if EVIDENCE_DIR_OPTION in remaining:
        index = remaining.index(EVIDENCE_DIR_OPTION)
        del remaining[index:index + 2]

    return remaining


def command_findings(command: list[str], run: dict, repo_root: Path) -> list[str]:
    """Whether the command a receipt reports is the command this category is declared to run.

    Compared with the model rather than believed. Everything that decides what ran - the runner, the
    category, the project, the brokers, the outage permission - comes from the model and has to match
    it, so a record that says one category and ran another is refused instead of read.
    """
    reported = without_evidence_dir(command)[1:]
    canonical = without_evidence_dir(child_command(run, repo_root, Path("<evidence>")))[1:]
    if reported != canonical:
        return [f"the reported command is {reported} and this category is declared to run {canonical}"]

    return []

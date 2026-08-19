#!/usr/bin/env python3
"""The one command that verifies this repository.

    python3 tools/ci/verify.py --selection all
    python3 tools/ci/verify.py --selection activemq

One entry point, one responsibility, and no orchestration duplicated into YAML. What a selection
contains, which categories need the pinned fixture, how long each of them may take and which cases it
is expected to execute all live in build/verification/VERIFICATION_MODEL.json, which is the
independent expected truth. The workflow only names a selection.

The terminal answer is a set comparison, not a count. A category passes when the identities it
executed are exactly the identities the model expects of it - nothing missing, nothing unexpected,
nothing twice, nothing failed, and nothing skipped that the model did not approve. A count can stay
constant while one case is dropped and another is added, which is precisely the shape a floor cannot
see.

Every run writes one receipt. It is the machine-readable record of what really happened, it is bound
to the commit, the tree and the model it was produced against, and tools/ci/validate_receipt.py
refuses one that does not match. That is an engineering completeness proof; it is not, and does not
claim to be, protection against somebody with administrative rights over this repository.

Standard library only.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_ownership  # noqa: E402  (repository local, resolved from this file's folder)
import run_test_category  # noqa: E402
import verification_model  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[2]
MODEL_FILE = REPO_ROOT / "build/verification/VERIFICATION_MODEL.json"
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"
RECEIPT_NAME = "verification-receipt.json"
RECEIPT_KIND = "SERVICEBUS_VERIFICATION_RECEIPT"
RECEIPT_SCHEMA_VERSION = 1


class VerificationError(RuntimeError):
    """Raised when the run cannot be carried out at all, as opposed to failing on its result."""


# -- the model -----------------------------------------------------------------------------------

def load_model() -> tuple[dict, str]:
    """The model and the hash of the exact bytes that were read.

    The hash goes into the receipt, so a receipt cannot be presented against a model that has changed
    since it was produced.
    """
    if not MODEL_FILE.is_file():
        raise VerificationError(f"{MODEL_FILE.relative_to(REPO_ROOT)} is missing, so nothing states "
                                "what this repository verifies")
    raw = MODEL_FILE.read_bytes()
    try:
        model = json.loads(raw.decode("utf-8"))
    except json.JSONDecodeError as error:
        raise VerificationError(f"the verification model is not readable: {error}") from error

    return model, hashlib.sha256(raw).hexdigest()


def resolve_selection(model: dict, name: str) -> list[str]:
    """The categories a selection names, transitively.

    The resolution itself lives in verification_model, because the policy validator has to reach the
    same answer this entry point does and two implementations of one rule are two rules.
    """
    try:
        resolved = verification_model.resolve_selection(model, name)
    except verification_model.SelectionError as error:
        raise VerificationError(str(error)) from error

    if not resolved:
        raise VerificationError(f"selection '{name}' resolves to no category at all")

    return resolved


def declared_run(model: dict, category: str) -> dict:
    runs = [run for capability in model.get("capabilities", [])
            for run in capability.get("runs", []) if run.get("category") == category]
    if len(runs) != 1:
        raise VerificationError(
            f"the model declares {len(runs)} runs for category '{category}', so which one is meant is "
            "undecided")

    return runs[0]


# -- expected identities -------------------------------------------------------------------------

def read_identity_file(path: Path) -> list[str]:
    """One identity per line, comments and blank lines ignored."""
    identities = []
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith("#"):
            identities.append(stripped)

    return identities


def expected_identities(run: dict) -> list[str] | None:
    """What this category is expected to execute, or None when nothing has recorded it yet.

    None is a state, not an omission. A category whose expected set nobody has recorded from a
    complete clean run cannot be part of a passing receipt, and the receipt says which ones those are.
    """
    declared = run.get("expectedIdentities")
    if not declared:
        return None

    path = REPO_ROOT / declared
    if not path.is_file():
        raise VerificationError(
            f"category '{run.get('category')}' names the expected set '{declared}', which is not there")

    return read_identity_file(path)


def executed_identities(trx_path: Path) -> tuple[list[str], list[str], list[str]]:
    """(executed, failed, not executed) identities of one result file, duplicates preserved."""
    if not trx_path.is_file():
        return [], [], []
    try:
        root = ElementTree.parse(trx_path).getroot()
    except ElementTree.ParseError as error:
        raise VerificationError(f"{trx_path} is not a readable result file: {error}") from error

    namespace = run_test_category.TRX_NAMESPACE
    definitions = {}
    for definition in root.findall("t:TestDefinitions/t:UnitTest", namespace):
        method = definition.find("t:TestMethod", namespace)
        if method is not None:
            definitions[definition.attrib.get("id")] = run_test_category.identity_of(method)

    executed, failed, skipped = [], [], []
    for result in root.findall("t:Results/t:UnitTestResult", namespace):
        identity = definitions.get(result.attrib.get("testId"))
        if identity is None:
            continue
        outcome = result.attrib.get("outcome")
        if outcome == "NotExecuted":
            skipped.append(identity)
            continue
        executed.append(identity)
        if outcome not in ("Passed",):
            failed.append(identity)

    return executed, failed, skipped


def compare_identities(expected: list[str], executed: list[str]) -> dict[str, list[str]]:
    """The exact set comparison the terminal answer is read from.

    Counted rather than set-subtracted, so a case that ran twice is visible. A plain set difference
    would report nothing for a run that executed one identity twice and another not at all while the
    total stayed the same.
    """
    wanted, ran = Counter(expected), Counter(executed)

    return {
        "missing": sorted((wanted - ran).elements()),
        "unexpected": sorted((ran - wanted).elements()),
        "duplicate": sorted(identity for identity, count in ran.items() if count > 1),
    }


# -- running one category ------------------------------------------------------------------------

def child_command(run: dict, run_root: Path, evidence_dir: Path) -> list[str]:
    """The exact command for this category, built from the model rather than from a workflow.

    A category with no broker goes straight to the category runner. One with brokers goes through the
    broker runner, which owns the fixture and hands the same run root down again.
    """
    category, project = run["category"], run["project"]
    if not run.get("brokers"):
        return [sys.executable, str(REPO_ROOT / "tools/ci/run_test_category.py"),
                "--category", category, "--project", project, "--evidence-dir", str(evidence_dir)]

    command = [sys.executable, str(REPO_ROOT / "tools/ci/run_broker_category.py")]
    for broker in run["brokers"]:
        command += ["--broker", broker]
    if run.get("allowBrokerOutage"):
        command += ["--allow-broker-outage", run["allowBrokerOutage"]]
    command += ["--category", category, "--project", project, "--evidence-dir", str(evidence_dir)]
    if run.get("oneRefusalPerVhost"):
        command += ["--one-refusal-per-vhost", run["oneRefusalPerVhost"]]

    return command


def verify_category(run: dict, evidence_parent: Path) -> dict[str, object]:
    """Runs one category under its own root and budget, and compares its result set with the model."""
    category = run["category"]
    run_root, token = run_ownership.mint_run_root(RAW_RUN_OUTPUT_DIR)
    environment = run_ownership.handover(run_root, token, dict(os.environ))
    command = child_command(run, run_root, evidence_parent / category)

    print(f"verify {category}: {run_root.name}, budget {run['budgetSeconds']} s", flush=True)
    child = run_test_category.run_child(command, environment, float(run["budgetSeconds"]))
    sys.stdout.write(child["stdout"])
    sys.stderr.write(child["stderr"])

    trx_path = run_root / f"{category}.trx"
    executed, failed, skipped = executed_identities(trx_path)
    approved = run_test_category.permitted_not_executed(run)
    expected = expected_identities(run)

    comparison = ({"missing": [], "unexpected": [], "duplicate": []} if expected is None
                  else compare_identities(expected, executed))
    unapproved = sorted((Counter(skipped) - Counter(approved)).elements())
    omitted = run_test_category.omitted_results(trx_path)

    findings = []
    if expected is None:
        findings.append("no expected identity set is recorded for this category, so its completeness "
                        "is not proven by this run")
    if child["timedOut"]:
        findings.append(f"the child did not finish within {run['budgetSeconds']} s and its tree was "
                        "taken down")
    if child["survivingOwnedProcesses"]:
        findings.append(f"{len(child['survivingOwnedProcesses'])} process(es) of this run survived it")
    if child["exitCode"] != 0:
        findings.append(f"the child exited with {child['exitCode']}")
    for name in ("missing", "unexpected", "duplicate"):
        if comparison[name]:
            findings.append(f"{len(comparison[name])} {name} identity/identities")
    if failed:
        findings.append(f"{len(failed)} failed identity/identities")
    if unapproved:
        findings.append(f"{len(unapproved)} unapproved not-executed identity/identities")
    if omitted:
        findings.append(f"{len(omitted)} defined case(s) the result file reports no result for")

    return {
        "category": category,
        "project": run["project"],
        "runRoot": run_root.relative_to(REPO_ROOT).as_posix(),
        "command": command,
        "budgetSeconds": run["budgetSeconds"],
        "childExitCode": child["exitCode"],
        "timedOut": child["timedOut"],
        "escalatedToKill": child["escalatedToKill"],
        "survivingOwnedProcesses": child["survivingOwnedProcesses"],
        "expectedRecorded": expected is not None,
        "expected": sorted(expected) if expected is not None else None,
        "executed": sorted(executed),
        "failed": sorted(failed),
        "skipped": sorted(skipped),
        "approvedNotExecuted": sorted(approved),
        "unapprovedNotExecuted": unapproved,
        "missing": comparison["missing"],
        "unexpected": comparison["unexpected"],
        "duplicate": comparison["duplicate"],
        "resultsOmitted": omitted,
        "rawResultSha256": digest(trx_path),
        "findings": findings,
        "terminal": "PASS" if not findings else "FAIL",
    }


EXPECTED_DIR = "build/verification/expected"


def record_expected(model: dict, result: dict) -> str:
    """Writes the expected identity set of one category from the run that just produced it.

    Only from a run that was otherwise clean. A set recorded from a run with a failure, an unapproved
    skip or a taken-down child would bake that state in as the expectation, and every later run would
    then agree with it.

    The set comes from the test platform's own result file. Nothing in a test declares that it ran.
    """
    category = result["category"]
    blocking = [finding for finding in result["findings"]
                if not finding.startswith("no expected identity set")]
    if blocking:
        raise VerificationError(
            f"category '{category}' cannot record an expected set from this run: {'; '.join(blocking)}")

    target = REPO_ROOT / EXPECTED_DIR / f"{category}.txt"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(
        f"# The identities category '{category}' is expected to execute.\n"
        "# Generated by tools/ci/verify.py --record-expected from a clean run of that category.\n"
        "# Never edited by hand: it is the result evidence of the test platform, not a wish list.\n"
        + "".join(f"{identity}\n" for identity in sorted(result["executed"])),
        encoding="utf-8")

    relative = target.relative_to(REPO_ROOT).as_posix()
    for capability in model.get("capabilities", []):
        for run in capability.get("runs", []):
            if run.get("category") == category:
                run["expectedIdentities"] = relative
    MODEL_FILE.write_text(json.dumps(model, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    return relative


def digest(path: Path) -> str | None:
    if not path.is_file():
        return None

    return hashlib.sha256(path.read_bytes()).hexdigest()


# -- the receipt ---------------------------------------------------------------------------------

def receipt_findings(receipt: dict, selection: str, commit: str, tree: str, model_hash: str,
             expected_categories: list[str]) -> list[str]:
    """Everything wrong with this receipt, as sentences. Empty means it may be believed."""
    problems: list[str] = []

    if receipt.get("kind") != RECEIPT_KIND:
        problems.append(f"this is not a verification receipt: kind is {receipt.get('kind')!r}")
        return problems
    if receipt.get("schemaVersion") != RECEIPT_SCHEMA_VERSION:
        problems.append(f"the receipt speaks schema version {receipt.get('schemaVersion')!r}, this "
                        f"reader speaks {RECEIPT_SCHEMA_VERSION}")
        return problems

    if receipt.get("selection") != selection:
        problems.append(
            f"the receipt is about selection '{receipt.get('selection')}' and was presented for "
            f"'{selection}'. A narrower run can never satisfy a wider one")
    if commit and receipt.get("commit") != commit:
        problems.append(f"the receipt was produced against commit {receipt.get('commit')}, and this is "
                        f"{commit}")
    if tree and receipt.get("tree") != tree:
        problems.append(f"the receipt was produced against tree {receipt.get('tree')}, and this is {tree}")
    if receipt.get("worktreeClean") is not True:
        problems.append("the receipt was produced from a working tree that was not clean, so it is not "
                        "about the committed state it names")
    if model_hash and receipt.get("verificationModelSha256") != model_hash:
        problems.append("the receipt was produced against a different verification model than the one "
                        "in this working tree")

    resolved = receipt.get("resolvedCategories")
    if resolved != expected_categories:
        problems.append(
            f"the receipt resolves '{selection}' to {resolved}, and this model resolves it to "
            f"{expected_categories}")

    reported = [entry.get("category") for entry in receipt.get("categories", [])]
    if sorted(filter(None, reported)) != sorted(expected_categories):
        problems.append(f"the receipt reports on {sorted(filter(None, reported))} while the selection "
                        f"is {sorted(expected_categories)}")

    for entry in receipt.get("categories", []):
        category = entry.get("category", "<unnamed>")
        if not entry.get("expectedRecorded"):
            problems.append(f"category '{category}' has no recorded expected identity set, so the "
                            "receipt proves no completeness for it")
        for field in ("missing", "unexpected", "duplicate", "failed", "unapprovedNotExecuted",
                      "resultsOmitted", "survivingOwnedProcesses"):
            if entry.get(field):
                problems.append(f"category '{category}' reports {len(entry[field])} {field}")
        if entry.get("timedOut"):
            problems.append(f"category '{category}' was taken down when its budget expired")
        if entry.get("childExitCode") not in (0, None):
            problems.append(f"category '{category}' exited with {entry.get('childExitCode')}")
        if entry.get("terminal") != "PASS":
            problems.append(f"category '{category}' is not a pass")

    if receipt.get("terminal") != "PASS":
        problems.append(f"the receipt's own terminal result is {receipt.get('terminal')!r}")

    return problems


def git(*arguments: str) -> str:
    result = subprocess.run(["git", *arguments], cwd=REPO_ROOT, capture_output=True, text=True,
                            check=False)

    return result.stdout.strip() if result.returncode == 0 else ""


def write_receipt(receipt: dict, run_root: Path, evidence_parent: Path) -> list[Path]:
    """The receipt goes under the run's own root and beside the caller's evidence."""
    body = json.dumps(receipt, indent=2, ensure_ascii=False, sort_keys=False) + "\n"
    written = []
    for target in (run_root / RECEIPT_NAME, evidence_parent / RECEIPT_NAME):
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(body, encoding="utf-8")
        written.append(target)

    return written


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    try:
        model, model_hash = load_model()
        categories = resolve_selection(model, args.selection)
    except VerificationError as error:
        print(f"FAIL verify {args.selection}: {error}", file=sys.stderr)
        return 2

    session_root, _ = run_ownership.mint_run_root(RAW_RUN_OUTPUT_DIR)
    # A caller directory is a parent, never the file: two runs pointed at the same one would otherwise
    # overwrite each other's receipt, which is the one artefact that must not be shared.
    evidence_parent = (args.evidence_dir if args.evidence_dir.is_absolute()
                       else REPO_ROOT / args.evidence_dir) / session_root.name
    started = datetime.now(timezone.utc)

    print(f"verify --selection {args.selection} -> {', '.join(categories)}", flush=True)

    results = []
    try:
        for category in categories:
            result = verify_category(declared_run(model, category), evidence_parent)
            if args.record_expected:
                path = record_expected(model, result)
                print(f"recorded {len(result['executed'])} expected identities in {path}")
                result["findings"] = [finding for finding in result["findings"]
                                      if not finding.startswith("no expected identity set")]
                result["expectedRecorded"] = True
                result["expected"] = sorted(result["executed"])
                result["terminal"] = "PASS" if not result["findings"] else "FAIL"
            results.append(result)
    except VerificationError as error:
        print(f"FAIL verify {args.selection}: {error}", file=sys.stderr)
        results.append({"category": "<aborted>", "findings": [str(error)], "terminal": "FAIL"})

    receipt = {
        "schemaVersion": RECEIPT_SCHEMA_VERSION,
        "kind": RECEIPT_KIND,
        "runId": session_root.name,
        "commit": git("rev-parse", "HEAD"),
        "tree": git("rev-parse", "HEAD^{tree}"),
        "worktreeClean": git("status", "--porcelain") == "",
        "verificationModelSha256": model_hash,
        "selection": args.selection,
        "resolvedCategories": categories,
        "startedUtc": started.isoformat(),
        "finishedUtc": datetime.now(timezone.utc).isoformat(),
        "categories": results,
        "terminal": "PASS" if all(entry["terminal"] == "PASS" for entry in results) else "FAIL",
    }
    written = write_receipt(receipt, session_root, evidence_parent)

    # The producer holds itself to the same rules the standalone reader applies, so a receipt this
    # command wrote can never be one that command would refuse. The dirty-worktree rule is left to the
    # reader: a developer verifying while editing is normal, and the reader is what CI and a handover
    # go through.
    self_check = [problem for problem in receipt_findings(
        receipt, args.selection, receipt["commit"], receipt["tree"], model_hash, categories)
        if "working tree" not in problem]

    for entry in results:
        state = entry["terminal"]
        detail = "; ".join(entry["findings"]) if entry["findings"] else "exact"
        print(f"{state} {entry['category']}: {detail}")
    receipt_path = written[-1]
    print(f"receipt {receipt_path.relative_to(REPO_ROOT).as_posix() if receipt_path.is_relative_to(REPO_ROOT) else receipt_path}")

    if receipt["terminal"] != "PASS" or self_check:
        for problem in self_check:
            print(f"FAIL receipt {problem}", file=sys.stderr)
        print(f"FAIL verify {args.selection}", file=sys.stderr)
        return 1

    print(f"PASS verify {args.selection} categories={len(categories)}")

    return 0


def build_parser() -> argparse.ArgumentParser:
    """The command line of the canonical entry point, and a closed one.

    Exactly one option with a value. Everything else that decides what runs - the categories, the
    projects, the brokers, the budgets, the expected identities - lives in the model, so a workflow
    step can be compared against one allowed shape instead of being parsed as a shell program.
    """
    parser = argparse.ArgumentParser(
        description=__doc__,
        epilog="What a selection contains is declared in build/verification/VERIFICATION_MODEL.json.")
    parser.add_argument("--selection", required=True,
                        help="The named scope to verify, as the verification model declares it.")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/verification"),
                        help="Parent directory for the receipt. Each run gets its own child of it.")
    parser.add_argument("--record-expected", action="store_true",
                        help="Write the expected identity set of every category in the selection from "
                             "this run, which has to be clean. Not part of a verifying run: the "
                             "workflow never passes it.")

    return parser


if __name__ == "__main__":
    raise SystemExit(main())

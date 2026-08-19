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

What authorises a pass is the exit status of this command inside the required check. The receipt every
run writes is derived audit evidence: it records what happened, bound to the commit, the tree and the
model it was produced against, and tools/ci/validate_receipt.py checks that it is internally
consistent and that it agrees with the native result files where those are still beside it. A receipt
presented without them is a consistency checked record and nothing more - it cannot show by itself
that a test process ever started, and it does not claim to.

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
from verification import fixture as fixtures  # noqa: E402
from verification import process_tree, receipt as receipts, run_scope, trx  # noqa: E402
import run_test_category  # noqa: E402
from verification import model as verification_model  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parents[2]
MODEL_FILE = REPO_ROOT / "build/verification/VERIFICATION_MODEL.json"
RAW_RUN_OUTPUT_DIR = REPO_ROOT / "artifacts" / "run-output"
# Re-exported for the callers and cases that name them: what a receipt is lives in
# verification/receipt.py, and there is one definition of it.
RECEIPT_NAME = receipts.RECEIPT_NAME
RECEIPT_KIND = receipts.RECEIPT_KIND
RECEIPT_SCHEMA_VERSION = receipts.RECEIPT_SCHEMA_VERSION



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


def read_identity_file(path: Path) -> list[str]:
    """One identity per line, as the model's own reader gives them."""
    return verification_model.read_identity_file(path)


def expected_identities(run: dict) -> list[str] | None:
    """What this category is expected to execute, bound to this repository."""
    try:
        return verification_model.expected_identities(run, REPO_ROOT)
    except verification_model.ModelError as error:
        raise VerificationError(str(error)) from error


def declared_run(model: dict, category: str) -> dict:
    """The one run of this category, with the entry point's own refusal on top of the model's."""
    try:
        return verification_model.declared_run(model, category)
    except verification_model.ModelError as error:
        raise VerificationError(str(error)) from error


def child_command(run: dict, evidence_dir: Path) -> list[str]:
    """The exact command for this category, bound to this repository."""
    return verification_model.child_command(run, REPO_ROOT, evidence_dir)


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


# -- expected identities -------------------------------------------------------------------------

# -- the native result file ------------------------------------------------------------------------

# -- running one category ------------------------------------------------------------------------

def verify_category(run: dict, evidence_parent: Path) -> dict[str, object]:
    """Runs one category under its own root and budget, and compares its result set with the model."""
    category = run["category"]
    run_root, token = run_scope.mint_run_root(RAW_RUN_OUTPUT_DIR)
    environment = run_scope.handover(run_root, token, dict(os.environ))
    command = child_command(run, evidence_parent / category)

    print(f"verify {category}: {run_root.name}, budget {run['budgetSeconds']} s", flush=True)
    child = process_tree.run_child(command, environment, float(run["budgetSeconds"]))
    sys.stdout.write(child["stdout"])
    sys.stderr.write(child["stderr"])

    result = trx.parse_result_file(run_root / f"{category}.trx")
    fixture = fixtures.fixture_evidence(run_root, run)
    expected = expected_identities(run)

    entry = {
        "category": category,
        "project": run["project"],
        "runRoot": run_root.relative_to(REPO_ROOT).as_posix(),
        "command": command,
        "budgetSeconds": run["budgetSeconds"],
        "brokers": list(run.get("brokers") or []),
        "allowBrokerOutage": run.get("allowBrokerOutage"),
        "childExitCode": child["exitCode"],
        "timedOut": child["timedOut"],
        "escalatedToKill": child["escalatedToKill"],
        "survivingOwnedProcesses": child["survivingOwnedProcesses"],
        "expected": sorted(expected) if expected is not None else None,
        "executed": sorted(result["executed"]),
        "failed": sorted(result["failed"]),
        "skipped": sorted(result["skipped"]),
        "approvedNotExecuted": sorted(verification_model.permitted_not_executed(run)),
        "resultsOmitted": result["omitted"],
        "resultCounters": result["counters"],
        "resultFindings": result["findings"],
        "rawResultSha256": result["sha256"],
        "fixtureRecorded": fixture["recorded"],
        "fixtureFindings": fixture["findings"],
        "fixtureRecordSha256": fixture["sha256"],
        "brokerLogSha256": fixture["logs"],
    }
    entry.update(receipts.derived_fields(entry))
    entry["findings"] = receipts.category_findings(entry)
    entry["terminal"] = "PASS" if not entry["findings"] else "FAIL"

    return entry


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

    session_root, _ = run_scope.mint_run_root(RAW_RUN_OUTPUT_DIR)
    # A caller directory is a parent, never the file: two runs pointed at the same one would otherwise
    # overwrite each other's receipt, which is the one artefact that must not be shared.
    evidence_parent = (args.evidence_dir if args.evidence_dir.is_absolute()
                       else REPO_ROOT / args.evidence_dir) / session_root.name
    started = datetime.now(timezone.utc)

    print(f"verify --selection {args.selection} -> {', '.join(categories)}", flush=True)

    results = []
    aborted = None
    try:
        for category in categories:
            results.append(verify_category(declared_run(model, category), evidence_parent))
    except VerificationError as error:
        print(f"FAIL verify {args.selection}: {error}", file=sys.stderr)
        aborted = str(error)

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
        "terminal": ("PASS" if aborted is None
                     and all(entry["terminal"] == "PASS" for entry in results) else "FAIL"),
    }
    written = write_receipt(receipt, session_root, evidence_parent)

    # The producer holds itself to the same rules the standalone reader applies, so a receipt this
    # command wrote can never be one that command would refuse. The dirty-worktree rule is left to the
    # reader: a developer verifying while editing is normal, and the reader is what CI and a handover
    # go through.
    self_check = [problem for problem in receipts.receipt_findings(
        receipt, args.selection, receipt["commit"], receipt["tree"], model_hash, categories, model,
        REPO_ROOT) if "working tree" not in problem]

    for entry in results:
        state = entry["terminal"]
        detail = "; ".join(entry["findings"]) if entry["findings"] else "exact"
        print(f"{state} {entry['category']}: {detail}")
    receipt_path = written[-1]
    print("receipt " + (receipt_path.relative_to(REPO_ROOT).as_posix()
                        if receipt_path.is_relative_to(REPO_ROOT) else str(receipt_path)))

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

    There is no option here that writes an expected set. This command reads them; the command that
    writes them is tools/ci/record_expected.py, and they are two commands because they were one: a run
    in which a test had disappeared recorded the smaller set as the new expectation and printed PASS.
    """
    parser = argparse.ArgumentParser(
        description=__doc__,
        epilog="What a selection contains is declared in build/verification/VERIFICATION_MODEL.json.")
    parser.add_argument("--selection", required=True,
                        help="The named scope to verify, as the verification model declares it.")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/verification"),
                        help="Parent directory for the receipt. Each run gets its own child of it.")

    return parser


if __name__ == "__main__":
    raise SystemExit(main())

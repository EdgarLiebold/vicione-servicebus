#!/usr/bin/env python3
"""Records what a category is expected to execute. This is maintenance, and it is not verification.

    python3 tools/ci/record_expected.py --category core
    python3 tools/ci/record_expected.py --category core --approval build/verification/removal.json

The expected identity set is the truth a verifying run is measured against, so the command that
measures may not also be the command that rewrites. It was: tools/ci/verify.py carried a
--record-expected option, and a run in which a test had disappeared recorded the smaller set as the new
expectation and printed PASS for it. The measurement and the thing measured were one command.

They are two now. tools/ci/verify.py reads the expected sets and writes nothing but its own receipt.
This command writes them and never prints a verification result: what it prints is RECORDED, with the
exact identities that came and went.

An identity that appears is a test somebody added. An identity that disappears is coverage this
repository had and no longer has, so it needs an approval that names exactly that set - bound to it by
the digest of the set itself, so an approval for one removal cannot authorise another. What the
approval is worth is a matter of review and branch protection; nothing here can make unrestricted write
access to a repository harmless, and nothing here pretends to.

Standard library only.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import verify  # noqa: E402  (repository local, resolved from this file's folder)

REPO_ROOT = verify.REPO_ROOT
EXPECTED_DIR = "build/verification/expected"

APPROVAL_KIND = "SERVICEBUS_EXPECTED_REMOVAL_APPROVAL"
APPROVAL_SCHEMA_VERSION = 1
APPROVAL_FIELDS = ("schemaVersion", "kind", "category", "reference", "referenceSha256",
                   "removedIdentities", "removedIdentitiesSha256")

HEADER = ("# The identities category '{category}' is expected to execute.\n"
          "# Written by tools/ci/record_expected.py from a clean run of that category, never by hand:\n"
          "# it is the result evidence of the test platform, not a wish list. tools/ci/verify.py reads\n"
          "# this file and does not write it.\n")


class MaintenanceError(RuntimeError):
    """Raised when the expected set may not be written from this run, in this tree, for this diff."""


def removal_digest(identities: list[str]) -> str:
    """The digest of an exact set of identities, which is what an approval is bound to."""
    return hashlib.sha256("".join(f"{identity}\n" for identity in sorted(set(identities)))
                          .encode("utf-8")).hexdigest()


def approval_findings(approval: dict, category: str, removed: list[str]) -> list[str]:
    """Everything that stops this document from being an approval of exactly these removals."""
    findings = verify.shape_findings(approval, {
        "schemaVersion": (verify.number, "a number"),
        "kind": (verify.text, "a string"),
        "category": (verify.text, "a string"),
        "reference": (verify.text, "a string"),
        "referenceSha256": (verify.text, "a string"),
        "removedIdentities": (verify.identity_list, "a list of identities"),
        "removedIdentitiesSha256": (verify.text, "a string"),
    }, "the approval")
    if findings:
        return findings

    if approval["kind"] != APPROVAL_KIND:
        findings.append(f"the document is of kind {approval['kind']!r} and not a removal approval")
    if approval["schemaVersion"] != APPROVAL_SCHEMA_VERSION:
        findings.append(f"the approval speaks schema version {approval['schemaVersion']!r}, this "
                        f"reader speaks {APPROVAL_SCHEMA_VERSION}")
    if approval["category"] != category:
        findings.append(f"the approval is about category '{approval['category']}' and this is "
                        f"'{category}'")
    # Two questions, and they are not the same one. The first is whether this document says the same
    # thing twice: an approval whose digest is not the digest of the list it carries is a document
    # somebody edited on one side, and a reader has no way to tell which side. The second is whether
    # the thing it says is this removal.
    if approval["removedIdentitiesSha256"] != removal_digest(approval["removedIdentities"]):
        findings.append("the approval's digest is not the digest of the identities it names, so the "
                        "document disagrees with itself about what was approved")
    elif approval["removedIdentitiesSha256"] != removal_digest(removed):
        findings.append(f"the approval is bound to {len(set(approval['removedIdentities']))} removed "
                        f"identity/identities and this run removes {len(set(removed))}, so it approves "
                        "a different removal")
    if not approval["reference"].strip():
        findings.append("the approval names no decision it comes from")
    if len(approval["referenceSha256"]) != 64 or not all(
            character in "0123456789abcdef" for character in approval["referenceSha256"]):
        findings.append("the approval's reference is not bound by a digest, so nothing says which "
                        "document was approved")

    return findings


def read_approval(path: Path | None, category: str, removed: list[str]) -> None:
    """Refuses unless this exact set of removals is approved, and says what an approval would be."""
    if not removed:
        return

    lost = "\n  ".join(sorted(removed))
    if path is None:
        raise MaintenanceError(
            f"this run removes {len(removed)} identity/identities from the expected set of "
            f"'{category}':\n  {lost}\n"
            f"An identity that disappears is coverage this repository had and no longer has. Recording "
            f"it needs --approval naming exactly this set, with removedIdentitiesSha256 "
            f"{removal_digest(removed)} and the decision it comes from.")
    if not path.is_file():
        raise MaintenanceError(f"the approval '{path}' is not there")
    try:
        approval = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        raise MaintenanceError(f"the approval '{path}' is not readable: {error}") from error
    if not isinstance(approval, dict):
        raise MaintenanceError(f"the approval '{path}' does not hold a document")

    findings = approval_findings(approval, category, removed)
    if findings:
        raise MaintenanceError(f"the approval '{path}' does not approve this removal: "
                               + "; ".join(findings))


def blocking_findings(result: dict) -> list[str]:
    """Everything in this run that is a defect rather than the difference recording resolves.

    A difference from the previous expectation is exactly what this command is for, and a test that was
    legitimately added or removed lands there. A defect of the run is something else: a case that
    failed, a skip nobody approved, a result the file never reported, a child that was taken down or a
    process that outlived it. Recording from one of those would write that state in as the expectation,
    and every later run would then agree with it.
    """
    difference = ("no expected identity set", "missing identity", "unexpected identity",
                  "duplicate identity")

    return [finding for finding in result["findings"]
            if not any(finding.startswith(kind) or kind in finding for kind in difference)]


def write_expected(category: str, identities: list[str]) -> Path:
    target = REPO_ROOT / EXPECTED_DIR / f"{category}.txt"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(HEADER.format(category=category)
                      + "".join(f"{identity}\n" for identity in sorted(identities)), encoding="utf-8")

    return target


def bind_to_model(model: dict, category: str, relative: str, count: int) -> None:
    """The manifest and the floor are one statement, so they are written together.

    A floor that disagreed with the manifest it was recorded beside would be a second, weaker claim
    about the same category, and the policy validator refuses that.
    """
    for capability in model.get("capabilities", []):
        for run in capability.get("runs", []):
            if run.get("category") == category:
                run["expectedIdentities"] = relative
                run["minimumExecutedCases"] = count
    verify.MODEL_FILE.write_text(json.dumps(model, indent=2, ensure_ascii=False) + "\n",
                                 encoding="utf-8")


def renamed_within_a_fixture(added: list[str], removed: list[str]) -> list[tuple[str, str]]:
    """Removals and additions that are the only change inside one fixture, paired.

    A pairing and not a fact: nothing in an identity string says that a case was renamed rather than
    deleted and another written. It is reported because a reviewer reads a rename differently from a
    deletion, and it changes nothing about the approval - a rename removes an identity, so the removal
    still has to be approved.
    """
    pairs = []
    for fixture in sorted({identity.rpartition(".")[0] for identity in removed}):
        gone = [identity for identity in removed if identity.rpartition(".")[0] == fixture]
        came = [identity for identity in added if identity.rpartition(".")[0] == fixture]
        if len(gone) == 1 and len(came) == 1:
            pairs.append((gone[0], came[0]))

    return pairs


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--category", required=True,
                        help="The category whose expected identity set is recorded.")
    parser.add_argument("--approval", type=Path, default=None,
                        help="A document approving the exact identities this run removes.")
    parser.add_argument("--evidence-dir", type=Path, default=Path("artifacts/record-expected"),
                        help="Parent directory for the run this records from.")
    args = parser.parse_args(argv)

    try:
        # A tree with uncommitted work is a tree in which nobody can say what the recorded set is the
        # set of. The command documented this and did not check it.
        dirty = verify.git("status", "--porcelain")
        if dirty:
            raise MaintenanceError(
                f"the working tree carries {len(dirty.splitlines())} uncommitted change(s), so an "
                "expected set recorded now would not be the set of any committed state")

        model, _ = verify.load_model()
        run = verify.declared_run(model, args.category)
        previous = verify.expected_identities(run) or []

        result = verify.verify_category(run, (args.evidence_dir if args.evidence_dir.is_absolute()
                                              else REPO_ROOT / args.evidence_dir))
        blocking = blocking_findings(result)
        if blocking:
            raise MaintenanceError(f"this run cannot be recorded from: {'; '.join(blocking)}")

        executed = sorted(result["executed"])
        added = sorted(set(executed) - set(previous))
        removed = sorted(set(previous) - set(executed))
        read_approval(args.approval, args.category, removed)

        relative = write_expected(args.category, executed).relative_to(REPO_ROOT).as_posix()
        bind_to_model(model, args.category, relative, len(executed))
    except (MaintenanceError, verify.VerificationError) as error:
        print(f"REFUSED record-expected {args.category}: {error}", file=sys.stderr)

        return 1

    for identity in added:
        print(f"  + {identity}")
    for identity in removed:
        print(f"  - {identity}")
    for gone, came in renamed_within_a_fixture(added, removed):
        print(f"  ~ {gone}\n    {came}")
    print(f"RECORDED {args.category}: {len(executed)} identities in {relative}, "
          f"{len(added)} added, {len(removed)} removed")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""What a verification receipt is, and whether one may be believed.

A receipt is the machine readable record of a run. What authorises a pass is the exit status of the
canonical verifier inside the required check; this is the narrower question of whether the record is
worth reading at all - and it was not being asked. Every derived number was read back exactly as the
caller wrote it, so a document claiming three expected identities, one executed identity and nothing
missing was accepted against the right commit, the right tree and the right model hash.

So every derived field is recomputed from the primary facts the record states, the shape is closed in
both directions, and what the record says it ran is compared with the model. Where the native result
files are still beside it they are parsed again; that part, and only that part, is evidence from
outside the receipt.

Standard library only.
"""

from __future__ import annotations

from collections import Counter
from pathlib import Path

from verification import model as verification_model
from verification import trx

# The repository this reader belongs to, used when a caller names no other. A caller that binds its own
# root - the suite of this module does - passes it, because a reader that reached past its argument
# into a module global would be reading a different repository than the one it was asked about.
REPO_ROOT = Path(__file__).resolve().parents[3]

RECEIPT_NAME = "verification-receipt.json"
RECEIPT_KIND = "SERVICEBUS_VERIFICATION_RECEIPT"
RECEIPT_SCHEMA_VERSION = 2


def category_findings(entry: dict) -> list[str]:
    """Everything wrong with one category, read from what that category itself reports.

    The single place a category's verdict is decided. The run that produces a receipt and the reader
    that is handed one both come through here, so a receipt can never assert a conclusion its own
    numbers do not carry: the reader recomputes this from the entry's primary facts and compares it
    with what the entry claims.
    """
    findings: list[str] = []

    if not entry["expectedRecorded"]:
        findings.append("no expected identity set is recorded for this category, so its completeness "
                        "is not proven by this run")
    if entry["timedOut"]:
        findings.append(f"the child did not finish within {entry['budgetSeconds']} s and its tree was "
                        "taken down")
    if entry["survivingOwnedProcesses"]:
        findings.append(f"{len(entry['survivingOwnedProcesses'])} process(es) of this run survived it")
    if entry["childExitCode"] is None:
        findings.append("the child never reported an exit status, so nothing shows that it ran")
    elif entry["childExitCode"] != 0:
        findings.append(f"the child exited with {entry['childExitCode']}")
    for name in ("missing", "unexpected", "duplicate"):
        if entry[name]:
            findings.append(f"{len(entry[name])} {name} identity/identities")
    if entry["failed"]:
        findings.append(f"{len(entry['failed'])} failed identity/identities")
    if entry["unapprovedNotExecuted"]:
        findings.append(f"{len(entry['unapprovedNotExecuted'])} unapproved not-executed "
                        "identity/identities")
    if entry["resultsOmitted"]:
        findings.append(f"{len(entry['resultsOmitted'])} defined case(s) the result file reports no "
                        "result for")
    for problem in entry["resultFindings"]:
        findings.append(f"result file: {problem}")
    for problem in entry["fixtureFindings"]:
        findings.append(f"fixture: {problem}")

    return findings


def derived_fields(entry: dict) -> dict[str, object]:
    """The fields of a category record that are functions of its primary facts, recomputed.

    passed, missing, unexpected, duplicate and unapprovedNotExecuted are named in the receipt so that a
    reader does not have to subtract one list from another. Naming them also makes them assertable, and
    an assertion nobody recomputes is where a fabricated receipt lives: expected three identities,
    executed one, missing none.
    """
    expected, executed = entry["expected"], entry["executed"]
    comparison = ({"missing": [], "unexpected": [], "duplicate": []} if expected is None
                  else trx.compare_identities(expected, executed))

    return {
        "passed": sorted((Counter(executed) - Counter(entry["failed"])).elements()),
        "unapprovedNotExecuted": sorted(
            (Counter(entry["skipped"]) - Counter(entry["approvedNotExecuted"])).elements()),
        "expectedRecorded": expected is not None,
        "missing": comparison["missing"],
        "unexpected": comparison["unexpected"],
        "duplicate": comparison["duplicate"],
    }


# -- the shape of a receipt --------------------------------------------------------------------

def identity_list(value: object) -> bool:
    return isinstance(value, list) and all(isinstance(entry, str) for entry in value)


def optional_identity_list(value: object) -> bool:
    return value is None or identity_list(value)


def text(value: object) -> bool:
    return isinstance(value, str)


def optional_text(value: object) -> bool:
    return value is None or isinstance(value, str)


def flag(value: object) -> bool:
    return isinstance(value, bool)


def number(value: object) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool)


def optional_number(value: object) -> bool:
    return value is None or number(value)


def counter_map(value: object) -> bool:
    return isinstance(value, dict) and all(
        isinstance(name, str) and isinstance(count, int) and not isinstance(count, bool)
        for name, count in value.items())


def digest_map(value: object) -> bool:
    return isinstance(value, dict) and all(
        isinstance(name, str) and optional_text(entry) for name, entry in value.items())


def record_list(value: object) -> bool:
    return isinstance(value, list) and all(isinstance(entry, dict) for entry in value)


RECEIPT_FIELDS = {
    "schemaVersion": (number, "a number"),
    "kind": (text, "a string"),
    "runId": (text, "a string"),
    "commit": (text, "a string"),
    "tree": (text, "a string"),
    "worktreeClean": (flag, "true or false"),
    "verificationModelSha256": (text, "a string"),
    "selection": (text, "a string"),
    "resolvedCategories": (identity_list, "a list of category names"),
    "startedUtc": (text, "a string"),
    "finishedUtc": (text, "a string"),
    "categories": (record_list, "a list of category records"),
    "terminal": (text, "a string"),
}

CATEGORY_FIELDS = {
    "category": (text, "a string"),
    "project": (text, "a string"),
    "runRoot": (text, "a string"),
    "command": (identity_list, "a list of command arguments"),
    "budgetSeconds": (number, "a number"),
    "brokers": (identity_list, "a list of broker names"),
    "allowBrokerOutage": (optional_text, "a broker name or null"),
    "childExitCode": (optional_number, "a number or null"),
    "timedOut": (flag, "true or false"),
    "escalatedToKill": (flag, "true or false"),
    "survivingOwnedProcesses": (identity_list, "a list of survivors"),
    "expectedRecorded": (flag, "true or false"),
    "expected": (optional_identity_list, "a list of identities or null"),
    "executed": (identity_list, "a list of identities"),
    "passed": (identity_list, "a list of identities"),
    "failed": (identity_list, "a list of identities"),
    "skipped": (identity_list, "a list of identities"),
    "approvedNotExecuted": (identity_list, "a list of identities"),
    "unapprovedNotExecuted": (identity_list, "a list of identities"),
    "missing": (identity_list, "a list of identities"),
    "unexpected": (identity_list, "a list of identities"),
    "duplicate": (identity_list, "a list of identities"),
    "resultsOmitted": (identity_list, "a list of identities"),
    "resultCounters": (counter_map, "a map of counter names to whole numbers"),
    "resultFindings": (identity_list, "a list of sentences"),
    "rawResultSha256": (optional_text, "a digest or null"),
    "fixtureRecorded": (flag, "true or false"),
    "fixtureFindings": (identity_list, "a list of sentences"),
    "fixtureRecordSha256": (optional_text, "a digest or null"),
    "brokerLogSha256": (digest_map, "a map of broker names to digests"),
    "findings": (identity_list, "a list of sentences"),
    "terminal": (text, "a string"),
}

DERIVED_FIELDS = ("duplicate", "expectedRecorded", "missing", "passed", "unapprovedNotExecuted",
                  "unexpected")


def shape_findings(mapping: dict, fields: dict, where: str) -> list[str]:
    """A closed shape: every declared field present and of its kind, and no field beyond them.

    Closed in both directions on purpose. A missing field cannot be checked at all, and an unknown one
    is either a document of another schema being read as this one or a field somebody added to carry a
    claim this reader does not check. Neither may pass quietly.
    """
    problems = []
    for name, (accepts, description) in sorted(fields.items()):
        if name not in mapping:
            problems.append(f"{where} has no '{name}'")
        elif not accepts(mapping[name]):
            problems.append(f"{where} field '{name}' is not {description}")
    for name in sorted(mapping):
        if name not in fields:
            problems.append(f"{where} carries an unknown field '{name}'")

    return problems


def summarise(value: object) -> str:
    if isinstance(value, list) and len(value) > 3:
        return f"{len(value)} entries"

    return str(value)


def derivation_findings(entry: dict) -> list[str]:
    """Where a category record disagrees with itself.

    This is the whole reason a receipt is worth reading. Every field a reader would otherwise take on
    trust is a function of the primary ones, so it is recomputed and compared: a record claiming three
    expected identities, one executed identity and nothing missing is refused here by arithmetic rather
    than believed by shape.
    """
    category = entry["category"]
    problems = []

    recomputed = derived_fields(entry)
    for name in DERIVED_FIELDS:
        if entry[name] != recomputed[name]:
            problems.append(f"category '{category}' declares {name}={summarise(entry[name])} and its "
                            f"own expected, executed, failed and skipped sets give "
                            f"{summarise(recomputed[name])}")

    derived = category_findings({**entry, **recomputed})
    if entry["findings"] != derived:
        problems.append(f"category '{category}' declares {len(entry['findings'])} finding(s) and its "
                        f"own facts give {len(derived)}")
    terminal = "PASS" if not derived else "FAIL"
    if entry["terminal"] != terminal:
        problems.append(f"category '{category}' calls itself {entry['terminal']!r} and its own facts "
                        f"make it {terminal}")
    for finding in derived:
        problems.append(f"category '{category}': {finding}")

    return problems


def declaration_findings(entry: dict, run: dict, repo_root: Path) -> list[str]:
    """Where a category record disagrees with the model it claims to have run."""
    category = entry["category"]
    problems = []

    if entry["project"] != run.get("project"):
        problems.append(f"category '{category}' names project '{entry['project']}' and the model "
                        f"declares '{run.get('project')}'")
    if sorted(entry["brokers"]) != sorted(run.get("brokers") or []):
        problems.append(f"category '{category}' names brokers {sorted(entry['brokers'])} and the model "
                        f"declares {sorted(run.get('brokers') or [])}")
    if entry["allowBrokerOutage"] != run.get("allowBrokerOutage"):
        problems.append(f"category '{category}' names outage permission "
                        f"{entry['allowBrokerOutage']!r} and the model declares "
                        f"{run.get('allowBrokerOutage')!r}")
    if entry["brokers"] and not entry["fixtureRecorded"]:
        problems.append(f"category '{category}' runs against a fixture and reports no fixture record, "
                        "so it carries no fixture proof")
    problems.extend(f"category '{category}': {problem}"
                    for problem in verification_model.command_findings(entry["command"], run, repo_root))

    return problems


def reparse_findings(entry: dict, repo_root: Path) -> tuple[list[str], bool]:
    """The record checked against the native evidence, where that evidence is still beside the receipt.

    This is the only part of reading a receipt that is not a consistency check. Where the result file
    is still there it is hashed and parsed again and the record has to agree with it. Where it is not,
    this returns no finding and says so, because a receipt on its own records what a run reported and
    cannot show that the run happened.
    """
    category = entry["category"]
    trx_path = repo_root / entry["runRoot"] / f"{category}.trx"
    if not trx_path.is_file():
        return [], False

    if trx.digest(trx_path) != entry["rawResultSha256"]:
        return [f"category '{category}' names a result file whose bytes have changed since the receipt "
                "was written"], True

    problems = []
    parsed = trx.parse_result_file(trx_path)
    for name in ("executed", "failed", "skipped"):
        if sorted(parsed[name]) != sorted(entry[name]):
            problems.append(f"category '{category}' reports {len(entry[name])} {name} "
                            f"identity/identities and its result file carries {len(parsed[name])}")
    if sorted(parsed["omitted"]) != sorted(entry["resultsOmitted"]):
        problems.append(f"category '{category}' reports {len(entry['resultsOmitted'])} omitted "
                        f"result(s) and its result file carries {len(parsed['omitted'])}")
    if sorted(parsed["findings"]) != sorted(entry["resultFindings"]):
        problems.append(f"category '{category}' reports {len(entry['resultFindings'])} finding(s) "
                        f"about its result file and reading it again gives {len(parsed['findings'])}")

    return problems, True


def receipt_findings(receipt: dict, selection: str, commit: str, tree: str, model_hash: str,
                     expected_categories: list[str], model: dict | None = None,
                     repo_root: Path | None = None) -> list[str]:
    """Everything wrong with this receipt, as sentences. Empty means it is internally sound.

    Internally sound is what this can decide. Every derived number is recomputed from the primary facts
    the receipt states, every field is checked against a closed shape, and every claim about what ran
    is checked against the model. Where the native result files are still beside the receipt they are
    hashed and parsed again as well; that part, and only that part, is evidence from outside the
    receipt.
    """
    problems: list[str] = []

    if receipt.get("kind") != RECEIPT_KIND:
        problems.append(f"this is not a verification receipt: kind is {receipt.get('kind')!r}")
        return problems
    if receipt.get("schemaVersion") != RECEIPT_SCHEMA_VERSION:
        problems.append(f"the receipt speaks schema version {receipt.get('schemaVersion')!r}, this "
                        f"reader speaks {RECEIPT_SCHEMA_VERSION}")
        return problems

    problems.extend(shape_findings(receipt, RECEIPT_FIELDS, "the receipt"))
    if problems:
        return problems

    if receipt["selection"] != selection:
        problems.append(
            f"the receipt is about selection '{receipt['selection']}' and was presented for "
            f"'{selection}'. A narrower run can never satisfy a wider one")
    if commit and receipt["commit"] != commit:
        problems.append(f"the receipt was produced against commit {receipt['commit']}, and this is "
                        f"{commit}")
    if tree and receipt["tree"] != tree:
        problems.append(f"the receipt was produced against tree {receipt['tree']}, and this is {tree}")
    if receipt["worktreeClean"] is not True:
        problems.append("the receipt was produced from a working tree that was not clean, so it is not "
                        "about the committed state it names")
    if model_hash and receipt["verificationModelSha256"] != model_hash:
        problems.append("the receipt was produced against a different verification model than the one "
                        "in this working tree")

    if receipt["resolvedCategories"] != expected_categories:
        problems.append(f"the receipt resolves '{selection}' to {receipt['resolvedCategories']}, and "
                        f"this model resolves it to {expected_categories}")

    reported = [entry.get("category") for entry in receipt["categories"]]
    if sorted(str(name) for name in reported) != sorted(expected_categories):
        problems.append(f"the receipt reports on {sorted(str(name) for name in reported)} while the "
                        f"selection is {sorted(expected_categories)}")

    for entry in receipt["categories"]:
        where = f"category '{entry.get('category', '<unnamed>')}'"
        shape = shape_findings(entry, CATEGORY_FIELDS, where)
        if shape:
            problems.extend(shape)
            continue
        problems.extend(derivation_findings(entry))
        if model is not None:
            try:
                problems.extend(declaration_findings(
                    entry, verification_model.declared_run(model, entry["category"]),
                    repo_root or REPO_ROOT))
            except verification_model.ModelError as error:
                problems.append(str(error))
        reparsed, _ = reparse_findings(entry, repo_root or REPO_ROOT)
        problems.extend(reparsed)

    if receipt["terminal"] != "PASS":
        problems.append(f"the receipt's own terminal result is {receipt['terminal']!r}")

    return problems


def accompanying_evidence(receipt: dict, repo_root: Path | None = None) -> dict[str, int]:
    """How much of this receipt could be read against the files it was produced from.

    A receipt whose result files are gone is still worth checking, and is worth exactly what it is: a
    consistency checked record of what a run reported. This number is what tells a reader which of the
    two they are holding, so it is printed rather than left to be assumed.
    """
    present = 0
    for entry in receipt.get("categories", []):
        category, run_root = entry.get("category"), entry.get("runRoot")
        if isinstance(category, str) and isinstance(run_root, str):
            if ((repo_root or REPO_ROOT) / run_root / f"{category}.trx").is_file():
                present += 1

    return {"categories": len(receipt.get("categories", [])), "withNativeResult": present}

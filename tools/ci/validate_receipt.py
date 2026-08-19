#!/usr/bin/env python3
"""Reads one verification receipt and refuses it unless it really is one.

    python3 tools/ci/validate_receipt.py --receipt <file> --selection all

A receipt is the machine-readable record of a verification run. What authorises a pass is the exit
status of the canonical verifier inside the required check; this reader decides something narrower and
still worth deciding: whether the record may be believed at all. It has to be about this commit, this
tree, this verification model and the selection that was asked for, its shape has to be exactly the
shape of a receipt, and every number it derives has to follow from the facts it states. A stale
receipt, a partial one, one produced for a narrower selection and presented as a wider one, and one
whose own arithmetic does not add up are all refused here rather than read as a pass.

Where the native result files this receipt was produced from are still beside it, they are hashed and
parsed again and the record has to agree with them. That is the only part of this that is evidence
from outside the receipt, and the output says how much of it there was. A receipt handed over without
those files is a consistency checked record of what a run reported; it cannot show that the run
happened, and this reader does not pretend otherwise.

Standard library only.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import verify  # noqa: E402  (repository local, resolved from this file's folder)
from verification import receipt as receipts  # noqa: E402

# Read at the moment it is used rather than captured here: a module level copy taken at
# import time is a different repository from the one a caller later binds.


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--receipt", required=True, type=Path)
    parser.add_argument("--selection", required=True)
    args = parser.parse_args(argv)

    if not args.receipt.is_file():
        print(f"FAIL receipt: {args.receipt} is not there", file=sys.stderr)
        return 1
    try:
        receipt = json.loads(args.receipt.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        print(f"FAIL receipt: {args.receipt} is not readable: {error}", file=sys.stderr)
        return 1
    if not isinstance(receipt, dict):
        print(f"FAIL receipt: {args.receipt} does not hold a receipt", file=sys.stderr)
        return 1

    try:
        model, model_hash = verify.load_model()
        categories = verify.resolve_selection(model, args.selection)
    except verify.VerificationError as error:
        print(f"FAIL receipt: {error}", file=sys.stderr)
        return 1

    problems = receipts.receipt_findings(receipt, args.selection, verify.git("rev-parse", "HEAD"),
                                         verify.git("rev-parse", "HEAD^{tree}"), model_hash,
                                         categories, model, verify.REPO_ROOT)
    if problems:
        for problem in problems:
            print(f"FAIL receipt {problem}", file=sys.stderr)
        return 1

    # Said out loud rather than left to a reader's assumption. The two states are not the same claim,
    # and a record that was only checked against itself must not be reported as if a result file had
    # been read again.
    evidence = receipts.accompanying_evidence(receipt, verify.REPO_ROOT)
    read_again = evidence["withNativeResult"]
    standing = ("consistent and reparsed against every native result file"
                if read_again == evidence["categories"] and read_again
                else f"consistent; {read_again}/{evidence['categories']} native result file(s) were "
                     "still there to be read again")
    print(f"PASS receipt {args.receipt.name} selection={args.selection} "
          f"categories={len(categories)} commit={receipt.get('commit', '')[:12]}: {standing}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

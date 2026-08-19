#!/usr/bin/env python3
"""Reads one verification receipt and refuses it unless it really is one.

    python3 tools/ci/validate_receipt.py --receipt <file> --selection all

A receipt is the machine-readable claim that a scope was verified. This is the small reader that
decides whether the claim may be believed: it has to be about this commit, this tree, this
verification model and the selection that was asked for, and its own numbers have to satisfy the
exact-set rules. A stale receipt, a partial one, one produced for a narrower selection and presented
as a wider one, or one written by hand is rejected here rather than read as a pass.

It proves engineering completeness. It does not, and cannot, make somebody with administrative rights
over this repository harmless, and it does not claim to.

Standard library only.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import verify  # noqa: E402  (repository local, resolved from this file's folder)

REPO_ROOT = verify.REPO_ROOT


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

    try:
        model, model_hash = verify.load_model()
        categories = verify.resolve_selection(model, args.selection)
    except verify.VerificationError as error:
        print(f"FAIL receipt: {error}", file=sys.stderr)
        return 1

    problems = verify.receipt_findings(receipt, args.selection, verify.git("rev-parse", "HEAD"),
                                       verify.git("rev-parse", "HEAD^{tree}"), model_hash, categories)
    if problems:
        for problem in problems:
            print(f"FAIL receipt {problem}", file=sys.stderr)
        return 1

    print(f"PASS receipt {args.receipt.name} selection={args.selection} "
          f"categories={len(categories)} commit={receipt.get('commit', '')[:12]}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

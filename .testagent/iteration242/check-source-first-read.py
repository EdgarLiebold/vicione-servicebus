#!/usr/bin/env python3
"""Read-only, current-byte audit of the ServiceBus src first-read convention.

This is a progress control, not a proof of manual review or A+ acceptance.
"""

from __future__ import annotations

import csv
import hashlib
import json
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BASELINE = "e01a5e5eb3411412229221bd58b170b583ce6caa"
EXTRA_LEDGERS = (
    Path(".testagent/iteration242/remaining-csharp-source-read.tsv"),
    Path(".testagent/iteration242/noncsharp-source-read.tsv"),
)


def git_paths(*arguments: str) -> set[str]:
    result = subprocess.run(
        ["git", *arguments], cwd=ROOT, check=True, capture_output=True
    )
    return {path.decode("utf-8") for path in result.stdout.split(b"\0") if path}


def main() -> None:
    tracked = git_paths("ls-files", "-z", "--", "src")
    untracked = git_paths("ls-files", "--others", "--exclude-standard", "-z", "--", "src")
    changed = git_paths(
        "log", "--format=", "--name-only", "-z", f"{BASELINE}^..HEAD", "--", "src"
    )
    changed |= git_paths("diff", "--name-only", "-z", "HEAD", "--", "src")
    # An untracked source file has no Git edit history and needs direct reading.
    raw_remainder = (tracked - changed) | untracked

    snapshot = set(
        (ROOT / ".testagent/source-read-remainder.txt").read_text(encoding="utf-8").splitlines()
    )
    ledgers = sorted((ROOT / ".testagent").glob("iteration*/source-admission.tsv"))
    ledgers.extend(ROOT / ledger for ledger in EXTRA_LEDGERS)
    attested: set[str] = set()
    hashes = {
        path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
        for path in raw_remainder
    }
    for ledger in ledgers:
        if not ledger.is_file():
            continue
        with ledger.open(encoding="utf-8", newline="") as stream:
            for row in csv.DictReader(stream, delimiter="\t"):
                path = row.get("path")
                if path in hashes and row.get("sha256") == hashes[path]:
                    attested.add(path)

    report = {
        "baseline_inclusive": BASELINE,
        "current_src_paths": len(tracked | untracked),
        "tracked_src_paths": len(tracked),
        "untracked_src_paths": sorted(untracked),
        "git_convention_read_paths": len(tracked & changed),
        "raw_remainder_paths": len(raw_remainder),
        "raw_remainder_csharp": sum(path.endswith(".cs") for path in raw_remainder),
        "current_byte_attested_raw_paths": len(attested),
        "first_read_unproven_paths": sorted(raw_remainder - attested),
        "committed_snapshot_matches_current_raw_remainder": raw_remainder == snapshot,
    }
    print(json.dumps(report, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()

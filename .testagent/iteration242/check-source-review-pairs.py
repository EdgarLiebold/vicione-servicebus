#!/usr/bin/env python3
"""Count current-byte source admission/disposition pairs, not A+ acceptances.

This intentionally reports only machine-joinable evidence packets. Older
per-iteration reports may contain valid review work that is not normalized into
paired TSVs; absence here is not evidence that the source was never reviewed.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def git_paths(*arguments: str) -> set[str]:
    result = subprocess.run(
        ["git", *arguments], cwd=ROOT, check=True, capture_output=True
    )
    return {path.decode("utf-8") for path in result.stdout.split(b"\0") if path}


def rows(path: Path) -> dict[str, dict[str, str]]:
    with path.open(encoding="utf-8", newline="") as stream:
        parsed = list(csv.DictReader(stream, delimiter="\t"))
    result = {row["path"]: row for row in parsed}
    if len(result) != len(parsed):
        raise ValueError(f"duplicate paths in {path.relative_to(ROOT)}")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--list-unpaired", action="store_true", help="print all paths without a current-byte pair"
    )
    args = parser.parse_args()

    current = git_paths("ls-files", "-z", "--", "src")
    current |= git_paths("ls-files", "--others", "--exclude-standard", "-z", "--", "src")
    tracked_evidence = git_paths("ls-files", "-z", "--", ".testagent")
    current_hashes = {
        path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
        for path in current
    }

    pair_files: list[str] = []
    untracked_pair_files: list[str] = []
    admitted_current: set[str] = set()
    dispositioned_current: set[str] = set()
    stale_paths: set[str] = set()
    unpaired_rows: list[dict[str, object]] = []

    for admission in sorted((ROOT / ".testagent").glob("iteration*/*source-admission.tsv")):
        disposition = admission.with_name(
            admission.name.replace("source-admission.tsv", "source-dispositions.tsv")
        )
        if not disposition.is_file():
            continue
        admission_name = str(admission.relative_to(ROOT))
        disposition_name = str(disposition.relative_to(ROOT))
        pair_files.append(admission_name)
        for evidence_name in (admission_name, disposition_name):
            if evidence_name not in tracked_evidence:
                untracked_pair_files.append(evidence_name)
        admission_rows = rows(admission)
        disposition_rows = rows(disposition)
        admission_paths = set(admission_rows)
        disposition_paths = set(disposition_rows)
        if admission_paths != disposition_paths:
            unpaired_rows.append(
                {
                    "admission": str(admission.relative_to(ROOT)),
                    "missing_dispositions": len(admission_paths - disposition_paths),
                    "orphan_dispositions": len(disposition_paths - admission_paths),
                }
            )
        for path in admission_paths & current:
            if admission_rows[path].get("sha256") != current_hashes[path]:
                stale_paths.add(path)
                continue
            admitted_current.add(path)
            disposition_row = disposition_rows.get(path)
            if disposition_row and disposition_row.get("disposition") and disposition_row.get("reason"):
                dispositioned_current.add(path)

    stale_paths -= dispositioned_current
    historical_current: set[str] = set()
    historical_stale: set[str] = set()
    historical_untracked_files: list[str] = []
    for name in ("historical-review-early.tsv", "historical-review-late.tsv"):
        historical = ROOT / ".testagent" / "iteration242" / name
        if not historical.is_file():
            continue
        historical_name = str(historical.relative_to(ROOT))
        if historical_name not in tracked_evidence:
            historical_untracked_files.append(historical_name)
        for path, row in rows(historical).items():
            if not (row.get("disposition") or row.get("explicit_disposition")) or not row.get("confidence"):
                raise ValueError(f"incomplete historical review record: {name}: {path}")
            evidence = row.get("historical_evidence_path", row.get("historical_evidence", ""))
            if not evidence or any(not (ROOT / item.strip()).is_file() for item in evidence.split(";")):
                raise ValueError(f"missing historical review evidence: {name}: {path}")
            if path in current:
                if row.get("current_sha256") == current_hashes[path]:
                    if path in historical_current:
                        raise ValueError(f"duplicate historical review path: {path}")
                    historical_current.add(path)
                else:
                    historical_stale.add(path)

    unpaired = sorted(current - dispositioned_current)
    reviewed_union = dispositioned_current | historical_current
    report = {
        "meaning": "lower-bound current-byte disposition records; not A+ approvals",
        "current_src_paths": len(current),
        "current_csharp_paths": sum(path.endswith(".cs") for path in current),
        "paired_tsv_packets": pair_files,
        "untracked_pair_evidence_files": untracked_pair_files,
        "current_byte_admitted_in_paired_packets": len(admitted_current),
        "current_byte_admitted_and_dispositioned": len(dispositioned_current),
        "current_byte_unpaired_paths": len(unpaired),
        "current_byte_historical_file_specific_records": len(historical_current),
        "historical_record_overlap_with_pairs": len(historical_current & dispositioned_current),
        "current_byte_review_record_union": len(reviewed_union),
        "current_byte_without_these_review_formats": len(current - reviewed_union),
        "historical_stale_path_list": sorted(historical_stale),
        "untracked_historical_evidence_files": historical_untracked_files,
        "stale_admission_paths": len(stale_paths),
        "stale_admission_path_list": sorted(stale_paths),
        "pair_path_mismatches": unpaired_rows,
        "unpaired_sample": unpaired[:10],
    }
    print(json.dumps(report, indent=2, ensure_ascii=False))
    if args.list_unpaired:
        print("\n".join(unpaired))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Create and validate the complete non-scratch deliverable manifest."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from collections import Counter
from pathlib import Path


SCRATCH_PREFIX = ".testagent/"


def git(root: Path, *args: str) -> str:
    return subprocess.run(
        ["git", *args],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout


def sha256(path: Path) -> str | None:
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def read_json(path: Path) -> dict[str, object] | list[dict[str, object]]:
    return json.loads(path.read_text(encoding="utf-8"))


def baseline_file_records(mapping: list[dict[str, object]]) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for item in mapping:
        target_path = str(item["targetPath"])
        path_changed = item["pathDisposition"] == "RENAMED"
        content_changed = item.get("baselineSha256") != item.get("targetSha256")
        git_status = "R" if path_changed else ("M" if content_changed else "UNCHANGED")
        records.append(
            {
                "gitStatus": git_status,
                "deliverableStatus": {
                    "R": "BASELINE_RENAMED",
                    "M": "BASELINE_MODIFIED",
                    "UNCHANGED": "BASELINE_UNCHANGED",
                }[git_status],
                "baselineKey": item["baselineKey"],
                "baselineCommit": item["baselineCommit"],
                "baselinePathSha256": item["baselinePathSha256"],
                "baselineGitBlobOid": item["baselineGitBlobOid"],
                "baselineGitMode": item["baselineGitMode"],
                "targetPath": target_path,
                "baselineSha256": item.get("baselineSha256"),
                "targetSha256": item.get("targetSha256"),
                "pathChanged": path_changed,
                "contentChanged": content_changed,
            }
        )
    return records


def current_nonignored_files(root: Path) -> set[str]:
    candidates = set(git(root, "ls-files").splitlines())
    candidates.update(git(root, "ls-files", "--others", "--exclude-standard").splitlines())
    return {
        path
        for path in candidates
        if path
        and ((root / path).is_file() or (root / path).is_symlink())
    }


def new_file_records(
    root: Path,
    baseline_targets: set[str],
    output_relative: str,
) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for path in sorted(current_nonignored_files(root) - baseline_targets):
        record: dict[str, object] = {
            "gitStatus": "A",
            "deliverableStatus": "NEW_DELIVERABLE",
            "baselineKey": None,
            "targetPath": path,
            "targetSha256": sha256(root / path),
        }
        if path == output_relative:
            record["targetSha256"] = None
            record["hashDisposition"] = "SELF_REFERENTIAL_MANIFEST"
        records.append(record)
    return records


def derived_counts(files: list[dict[str, object]]) -> dict[str, object]:
    statuses = Counter(str(item.get("gitStatus")) for item in files)
    baseline_total = sum(item.get("baselineKey") is not None for item in files)
    new_total = statuses.get("A", 0)
    return {
        "baselineTotal": baseline_total,
        "baselineChanged": statuses.get("R", 0) + statuses.get("M", 0),
        "baselineUnchanged": statuses.get("UNCHANGED", 0),
        "baselineRenamed": statuses.get("R", 0),
        "baselineModified": statuses.get("M", 0),
        "newDeliverables": new_total,
        "fullDeliverableTotal": baseline_total + new_total,
        "statusKinds": dict(sorted(statuses.items())),
    }


def validate_manifest(
    manifest: dict[str, object],
    expected_baseline_keys: set[str] | None = None,
    expected_new_paths: set[str] | None = None,
) -> list[str]:
    findings: list[str] = []
    files = manifest.get("files", [])
    if not isinstance(files, list):
        return ["files must be a list"]
    records = [item for item in files if isinstance(item, dict)]
    if len(records) != len(files):
        findings.append("every file record must be an object")

    target_paths = [str(item.get("targetPath") or "") for item in records]
    baseline_keys = [
        str(item["baselineKey"]) for item in records if item.get("baselineKey") is not None
    ]
    new_paths = {
        str(item.get("targetPath")) for item in records if item.get("gitStatus") == "A"
    }
    if any(path.startswith(SCRATCH_PREFIX) for path in target_paths):
        findings.append("scratch .testagent path included in deliverable")
    if len(target_paths) != len(set(target_paths)):
        findings.append("duplicate target path in deliverable")
    if len(baseline_keys) != len(set(baseline_keys)):
        findings.append("duplicate baseline key in deliverable")
    if any(not path or Path(path).is_absolute() or ".." in Path(path).parts for path in target_paths):
        findings.append("invalid repository-relative target path")
    for item in records:
        status = item.get("gitStatus")
        baseline_key = item.get("baselineKey")
        if status == "A" and baseline_key is not None:
            findings.append("A record must have null baselineKey")
        if status != "A" and baseline_key is None:
            findings.append(f"{status} record must have non-null baselineKey")

    if expected_baseline_keys is not None and set(baseline_keys) != expected_baseline_keys:
        missing = sorted(expected_baseline_keys - set(baseline_keys))
        extra = sorted(set(baseline_keys) - expected_baseline_keys)
        findings.append(f"baseline key set mismatch: missing={missing} extra={extra}")
    if expected_new_paths is not None and new_paths != expected_new_paths:
        missing = sorted(expected_new_paths - new_paths)
        extra = sorted(new_paths - expected_new_paths)
        findings.append(f"new deliverable path set mismatch: missing={missing} extra={extra}")

    actual_counts = derived_counts(records)
    if manifest.get("counts") != actual_counts:
        findings.append("manifest count drift from file records")
    if actual_counts["baselineChanged"] + actual_counts["baselineUnchanged"] != actual_counts["baselineTotal"]:
        findings.append("changed plus unchanged baseline count does not equal baseline total")
    if actual_counts["baselineTotal"] + actual_counts["newDeliverables"] != actual_counts["fullDeliverableTotal"]:
        findings.append("baseline plus new count does not equal full deliverable total")

    if "status" in manifest:
        findings.append("ambiguous generic status is forbidden in freeze manifest")
    gate_summary = manifest.get("gateSummary", {})
    if not isinstance(gate_summary, dict):
        findings.append("gateSummary binding must be an object")
    elif manifest.get("overallStatus") != gate_summary.get("overallStatus"):
        findings.append("overallStatus differs from bound GATE_SUMMARY")
    if manifest.get("overallStatus") == "INCOMPLETE_NOT_GREEN" and manifest.get("manifestIntegrityStatus") not in {
        None,
        "PASS",
    }:
        findings.append("manifest integrity value is invalid")
    return findings


def generate(root: Path, output_path: Path, evidence: Path) -> dict[str, object]:
    mapping_value = read_json(evidence / "BASELINE_TO_TARGET_PATHS.json")
    source_value = read_json(evidence / "SOURCE_IDENTITY_GATE.json")
    summary_value = read_json(evidence / "GATE_SUMMARY.json")
    if not isinstance(mapping_value, list) or not isinstance(source_value, dict) or not isinstance(summary_value, dict):
        raise ValueError("canonical evidence shapes are invalid")

    output_relative = output_path.resolve().relative_to(root).as_posix()
    baseline_records = baseline_file_records(mapping_value)
    baseline_targets = {str(item["targetPath"]) for item in baseline_records}
    new_records = new_file_records(root, baseline_targets, output_relative)
    files = sorted(
        baseline_records + new_records,
        key=lambda item: (str(item["targetPath"]), str(item["gitStatus"])),
    )
    counts = derived_counts(files)
    canonical_counts = summary_value.get("canonicalBaselineCounts", {})
    gate_summary_sha = sha256(evidence / "GATE_SUMMARY.json")
    result: dict[str, object] = {
        "manifestIntegrityStatus": None,
        "overallStatus": summary_value.get("status"),
        "freezeKind": "UNCOMMITTED_COMPLETE_DELIVERABLE_NO_GIT_OBJECT_CREATED",
        "baselineHead": git(root, "rev-parse", "HEAD").strip(),
        "baselineTree": git(root, "rev-parse", "HEAD^{tree}").strip(),
        "gateSummary": {
            "path": (evidence / "GATE_SUMMARY.json").relative_to(root).as_posix(),
            "sha256": gate_summary_sha,
            "overallStatus": summary_value.get("status"),
        },
        "canonicalCountBinding": {
            "sourceIdentityGate": source_value.get("counts"),
            "gateSummary": canonical_counts,
        },
        "counts": counts,
        "deliverableStatistics": {
            "description": "Canonical complete deliverable census; raw Git delete-only diffstat is intentionally not used.",
            "baselineMappings": counts["baselineTotal"],
            "newNonIgnoredPaths": counts["newDeliverables"],
            "completePaths": counts["fullDeliverableTotal"],
        },
        "scratchBoundary": {
            "prefix": SCRATCH_PREFIX,
            "includedInDeliverable": 0,
            "retention": "LOCAL_READ_ONLY_REVIEW_SCRATCH_NOT_FOR_COMMIT",
        },
        "findings": [],
        "files": files,
    }
    expected_baseline = {str(item["baselineKey"]) for item in mapping_value}
    expected_new = {str(item["targetPath"]) for item in new_records}
    findings = validate_manifest(result, expected_baseline, expected_new)
    if counts.get("baselineTotal") != canonical_counts.get("baselineTotal"):
        findings.append("manifest baseline total differs from GATE_SUMMARY canonical count")
    if counts.get("baselineChanged") != canonical_counts.get("baselineChanged"):
        findings.append("manifest changed baseline count differs from GATE_SUMMARY canonical count")
    if counts.get("baselineUnchanged") != canonical_counts.get("baselineUnchanged"):
        findings.append("manifest unchanged baseline count differs from GATE_SUMMARY canonical count")
    source_counts = source_value.get("counts", {})
    if isinstance(source_counts, dict) and counts.get("baselineChanged") != source_counts.get("changedBaselineFiles"):
        findings.append("manifest changed baseline count differs from SOURCE_IDENTITY_GATE")

    result["findings"] = findings
    result["manifestIntegrityStatus"] = "PASS" if not findings else "FAIL"
    fingerprint_payload = {
        "baselineHead": result["baselineHead"],
        "baselineTree": result["baselineTree"],
        "gateSummary": result["gateSummary"],
        "overallStatus": result["overallStatus"],
        "counts": counts,
        "files": files,
    }
    canonical = json.dumps(fingerprint_payload, separators=(",", ":"), sort_keys=True).encode("utf-8")
    result["deliverableFingerprintSha256"] = hashlib.sha256(canonical).hexdigest()
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--evidence", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    evidence = (args.evidence or root / "evidence" / "WP-F2-SERVICEBUS-IDENTITY").resolve(strict=True)
    output = args.output.resolve()
    result = generate(root, output, evidence)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "manifestIntegrityStatus": result["manifestIntegrityStatus"],
                "overallStatus": result["overallStatus"],
                "counts": result["counts"],
            },
            indent=2,
        )
    )
    return 0 if result["manifestIntegrityStatus"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())

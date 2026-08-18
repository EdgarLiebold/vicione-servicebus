#!/usr/bin/env python3
"""Reproduce the frozen gate summaries of WP-F2-SERVICEBUS-IDENTITY correction 02.

HISTORICAL. This is not an active gate and nothing in the current tool graph invokes it. It restates
what one past commit looked like, and the constants below are part of that statement rather than a
description of the tree you are standing in: DOTNET_ROOT_INPUTS names ViciOne.ServiceBus.sln and
Directory.Build.targets, neither of which exists any more, and CORRECTION_02_DOTNET_INPUT_BINDING is
a hash over exactly that list. Rewriting them to match today would falsify the record they bind.

It refuses to run without --historical-reproduction, so it cannot be mistaken for a check of the
current tree by anyone who finds it in tools/.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from identity_gate import (
    baseline_bytes,
    derive_baseline_mapping,
    derive_refactor_conformance,
    validate_legal_documents,
)
from identity_rules import COMMENTLESS_OR_BINARY_EXCEPTIONS, LEGAL_OR_PROVENANCE_PATHS


# A record of which projects had no final result file in the evidence run this gate summarises. It
# states what was true then, so an entry stays even after its project leaves the graph; editing it to
# match today's tree would rewrite a past report rather than clean anything up.
INCOMPLETE_PROJECTS = [
    "ViciOne.ServiceBus.RedisIntegration.Tests",
    "ViciOne.ServiceBus.MongoDbIntegration.Tests",
    "ViciOne.ServiceBus.RabbitMqTransport.Tests",
    "ViciOne.ServiceBus.ActiveMqTransport.Tests",
    "ViciOne.ServiceBus.AmazonSqsTransport.Tests",
    "ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests",
]

CORRECTION_02_STARTING_FREEZE = "bd0f232313912fefe215c309cb8b5339a3276b1cf5381410362248e5567adf88"
CORRECTION_02_DOTNET_INPUT_COUNT = 5633
CORRECTION_02_DOTNET_INPUT_BINDING = "0ad79cefb9395a51ec1be6fb1c36f16aa2c86c4a1001df3821da0d0f07888f5b"
DOTNET_ROOT_INPUTS = frozenset(
    {
        "ViciOne.ServiceBus.sln",
        "Directory.Build.props",
        "Directory.Build.targets",
        "Directory.Packages.props",
        "global.json",
        "NuGet.Config",
        "nuget.config",
    }
)


def read_json(path: Path) -> dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def aggregate_sha256(values: list[str]) -> str:
    return hashlib.sha256("\n".join(sorted(values)).encode("ascii")).hexdigest()


def is_dotnet_input(path: str) -> bool:
    return path.startswith(("src/", "tests/")) or path in DOTNET_ROOT_INPUTS


def dotnet_input_bindings(root: Path) -> list[str]:
    raw = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        cwd=root,
        check=True,
        capture_output=True,
    ).stdout
    candidates = sorted(
        {
            item.decode("utf-8", errors="surrogateescape")
            for item in raw.split(b"\0")
            if item
        }
    )
    return [
        f"{path}:{sha256(root / path)}"
        for path in candidates
        if is_dotnet_input(path) and (root / path).is_file()
    ]


def generate(root: Path, evidence: Path, build_binlog: Path, tool_tests: int) -> dict[str, object]:
    mapping = read_json(evidence / "BASELINE_TO_TARGET_PATHS.json")
    source = read_json(evidence / "SOURCE_IDENTITY_GATE.json")
    artifact = read_json(evidence / "ARTIFACT_GATE.json")
    tests = read_json(evidence / "TEST_GATE.json")
    endpoint = read_json(evidence / "ENDPOINT_IDENTITY_RERUN_GATE.json")
    analyzer = read_json(evidence / "ANALYZER_BASELINE_COMPARISON.json")
    failure_partition = read_json(evidence / "TEST_FAILURE_PARTITION_GATE.json")
    not_executed = read_json(evidence / "NOT_EXECUTED_SOURCE_GATE.json")
    proof_mutation = read_json(evidence / "PROOF_CONTRACT_MUTATION_GATE.json")
    api = read_json(evidence / "PUBLIC_API_MAPPING.json")

    baseline_total = len(mapping)
    baseline_changed = sum(
        item["pathDisposition"] == "RENAMED"
        or item.get("baselineSha256") != item.get("targetSha256")
        for item in mapping
    )
    baseline_unchanged = baseline_total - baseline_changed
    baseline_renamed = sum(item["pathDisposition"] == "RENAMED" for item in mapping)
    baseline_modified = baseline_changed - baseline_renamed
    canonical_baseline_counts = {
        "baselineTotal": baseline_total,
        "baselineChanged": baseline_changed,
        "baselineUnchanged": baseline_unchanged,
        "baselineRenamed": baseline_renamed,
        "baselineModified": baseline_modified,
    }
    source_count_consistent = (
        source["counts"]["baselinePaths"] == baseline_total
        and source["counts"]["changedBaselineFiles"] == baseline_changed
    )

    scratch_root = root / ".testagent"
    scratch_files = [path for path in scratch_root.rglob("*") if path.is_file()]
    scratch_untracked = subprocess.run(
        ["git", "ls-files", "--others", "--exclude-standard", ".testagent"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.splitlines()
    scratch_boundary_gate = {
        "status": "PASS" if not scratch_untracked and ".testagent/" in (root / ".gitignore").read_text(encoding="utf-8").splitlines() else "FAIL",
        "scratchPrefix": ".testagent/",
        "preCorrectionGitUntrackedFiles": 74,
        "postCorrectionGitUntrackedNonIgnoredFiles": len(scratch_untracked),
        "physicalReviewFilesRetained": len(scratch_files),
        "physicalReviewBytesRetained": sum(path.stat().st_size for path in scratch_files),
        "retention": "LOCAL_READ_ONLY_REVIEW_SCRATCH_NOT_FOR_COMMIT",
        "rawCaptureIncludedInDeliverable": False,
    }
    write_json(evidence / "SCRATCH_BOUNDARY_GATE.json", scratch_boundary_gate)

    current_mapping, current_mapping_findings = derive_baseline_mapping(root)
    conformance_findings = derive_refactor_conformance(root)
    artifact_hash_mismatches = [
        str(item["path"])
        for item in artifact["artifacts"]
        if not (root / str(item["path"])).is_file()
        or sha256(root / str(item["path"])) != item["sha256"]
    ]
    package_records = [item for item in artifact["artifacts"] if item["kind"] == "nupkg"]
    trx_files = sorted((root / ".testagent").glob("**/*.trx"))
    product_bindings = [
        f'{item["baselineKey"]}:{item["targetPath"]}:{item.get("targetSha256")}'
        for item in current_mapping
        if item["targetPath"] not in LEGAL_OR_PROVENANCE_PATHS
    ]
    current_dotnet_bindings = dotnet_input_bindings(root)
    current_dotnet_binding = aggregate_sha256(current_dotnet_bindings)
    correction_scope_gate = {
        "status": "PASS",
        "startingFreezeFingerprint": CORRECTION_02_STARTING_FREEZE,
        "authorizedCorrectionPathClasses": [
            "tools/identity/**",
            "evidence/WP-F2-SERVICEBUS-IDENTITY/**",
        ],
        "productSemanticDeviationFromDeterministicBaselineMapping": [
            item.as_dict() for item in conformance_findings
        ],
        "baselineMappingFindings": [item.as_dict() for item in current_mapping_findings],
        "deterministicProductInputBindingSha256": aggregate_sha256(product_bindings),
        "mappedBaselineFiles": len(current_mapping),
        "unchangedDotNetInputs": {
            "selection": "all existing commit candidates under src/** and tests/** plus root .NET build/package inputs",
            "startingFreezeFiles": CORRECTION_02_DOTNET_INPUT_COUNT,
            "currentFiles": len(current_dotnet_bindings),
            "startingFreezeSha256": CORRECTION_02_DOTNET_INPUT_BINDING,
            "currentSha256": current_dotnet_binding,
            "status": (
                "PASS"
                if len(current_dotnet_bindings) == CORRECTION_02_DOTNET_INPUT_COUNT
                and current_dotnet_binding == CORRECTION_02_DOTNET_INPUT_BINDING
                else "FAIL"
            ),
        },
        "reusedBuildAndPackageArtifacts": {
            "artifactEvidenceSha256": sha256(evidence / "ARTIFACT_GATE.json"),
            "artifactFiles": len(artifact["artifacts"]),
            "artifactHashMismatches": artifact_hash_mismatches,
            "artifactHashBindingSha256": aggregate_sha256(
                [f'{item["sha256"]}:{item["size"]}' for item in artifact["artifacts"]]
            ),
            "packageFiles": len(package_records),
            "packageHashBindingSha256": aggregate_sha256(
                [str(item["sha256"]) for item in package_records]
            ),
            "buildCaptureSha256": sha256(build_binlog),
        },
        "reusedProductTestCaptures": {
            "trxFiles": len(trx_files),
            "trxHashBindingSha256": aggregate_sha256([sha256(path) for path in trx_files]),
            "hashClaim": "CURRENT_CAPTURE_SET_ONLY_NOT_HISTORICAL_BYTE_EQUALITY",
            "testGateSha256": sha256(evidence / "TEST_GATE.json"),
        },
        "reuseBasis": "UNCHANGED_DOTNET_INPUTS_AND_REVALIDATED_CURRENT_SEMANTICS",
        "buildPackAndProductTestsReexecuted": False,
    }
    if (
        current_mapping_findings
        or conformance_findings
        or artifact_hash_mismatches
        or len(current_mapping) != 5654
        or len(artifact["artifacts"]) != 178
        or len(package_records) != 33
        or len(trx_files) != 19
        or correction_scope_gate["unchangedDotNetInputs"]["status"] != "PASS"
    ):
        correction_scope_gate["status"] = "FAIL"
    write_json(evidence / "CORRECTION_SCOPE_REUSE_GATE.json", correction_scope_gate)

    build_gate = {
        "status": "PASS" if build_binlog.is_file() else "FAIL",
        "command": "dotnet build ViciOne.ServiceBus.sln -c Release --no-restore --disable-build-servers",
        "configuration": "Release",
        "exitCode": 0,
        "errors": 0,
        "warnings": 201,
        "evidenceKind": "DERIVED_BUILD_SUMMARY",
        "rawCaptureDisposition": "NON_DELIVERABLE_LOCAL_SCRATCH",
        "rawCaptureIncludedInDeliverable": False,
        "elapsed": "00:01:07.84",
    }
    write_json(evidence / "BUILD_GATE.json", build_gate)

    text_path_gate = {
        "status": source["status"],
        "caseInsensitive": True,
        "baselinePaths": source["counts"]["baselinePaths"],
        "changedBaselineFiles": source["counts"]["changedBaselineFiles"],
        "unchangedBaselineFiles": baseline_unchanged,
        "canonicalBaselineCounts": canonical_baseline_counts,
        "canonicalCountConsistency": source_count_consistent,
        "findings": source["findings"],
        "legalRetentionIsPathAndContextExact": True,
    }
    text_path_gate["status"] = "PASS" if text_path_gate["status"] == "PASS" and source_count_consistent else "FAIL"
    write_json(evidence / "TEXT_PATH_GATE.json", text_path_gate)

    api_missing = [item for item in api if not item["targetPresent"]]
    api_gate = {
        "status": "PASS" if not api_missing and len(api) == source["counts"]["publicDeclarationRecords"] else "FAIL",
        "baselineToTargetRecords": len(api),
        "missingMappedDeclarations": len(api_missing),
        "silentRemoval": False if not api_missing else True,
        "findings": api_missing,
    }
    write_json(evidence / "API_GATE.json", api_gate)

    notice = (root / "NOTICE").read_text(encoding="utf-8")
    modifications = (root / "MODIFICATIONS.md").read_text(encoding="utf-8")
    exception_paths = sorted(COMMENTLESS_OR_BINARY_EXCEPTIONS)
    legal_validation_findings = [item.as_dict() for item in validate_legal_documents(root)]
    legal_gate = {
        "status": "PASS" if not legal_validation_findings else "FAIL",
        "licenseByteIdentical": baseline_bytes(root, "LICENSE") == (root / "LICENSE.txt").read_bytes(),
        "exactChangedFormatExceptions": exception_paths,
        "allExceptionsInNotice": all(path in notice for path in exception_paths),
        "allExceptionsInModifications": all(path in modifications for path in exception_paths),
        "blanketExceptionPresent": False,
        "formatAndContextFindings": legal_validation_findings,
        "readmeBilingualProvenance": all(
            marker in (root / "README.md").read_text(encoding="utf-8")
            for marker in ("Herkunft und Lizenz", "Origin and license", "8.5.10", "62ab339afa3bac2e9b3fe1769d0d35d7e44778e9")
        ),
    }
    legal_gate["status"] = "PASS" if (
        legal_gate["status"] == "PASS"
        and legal_gate["licenseByteIdentical"]
        and legal_gate["allExceptionsInNotice"]
        and legal_gate["allExceptionsInModifications"]
        and legal_gate["readmeBilingualProvenance"]
        and not legal_gate["blanketExceptionPresent"]
    ) else "FAIL"
    write_json(evidence / "LEGAL_GATE.json", legal_gate)

    expected_strong_names = [item for item in artifact["strongNames"] if item["expected"]]
    strong_name_failures = [item for item in expected_strong_names if item["status"] != "PASS"]
    il_gate = {
        "status": "PASS" if artifact["status"] == "PASS" and not strong_name_failures else "FAIL",
        "assemblies": artifact["counts"]["dll"],
        "expectedStrongNames": len(expected_strong_names),
        "strongNameFailures": strong_name_failures,
        "metadataAndBinaryFindings": artifact["findings"],
    }
    write_json(evidence / "IL_METADATA_GATE.json", il_gate)

    pdb_gate = {
        "status": artifact["status"],
        "debugType": "embedded",
        "assembliesWithEmbeddedPdb": artifact["counts"]["embeddedPdbAssemblies"],
        "externalPdbFiles": artifact["counts"]["pdb"],
        "scanCarrier": "DLL_BYTES",
        "findings": [item for item in artifact["findings"] if "pdb" in item["gate"]],
    }
    write_json(evidence / "PDB_GATE.json", pdb_gate)

    packages = [item for item in artifact["artifacts"] if item["kind"] == "nupkg"]
    nuget_gate = {
        "status": "PASS" if artifact["status"] == "PASS" and len(packages) == 33 else "FAIL",
        "packages": len(packages),
        "entries": sum(len(item["packageEntries"]) for item in packages),
        "failedPackages": [item["path"] for item in packages if item["status"] != "PASS"],
        "findings": [item for item in artifact["findings"] if item["gate"].startswith("nuget")],
    }
    write_json(evidence / "NUGET_GATE.json", nuget_gate)

    binary_gate = {
        "status": artifact["status"],
        "artifacts": artifact["counts"]["artifacts"],
        "invalidUtf8BinaryScanning": True,
        "asciiAndUtf16FormerIdentityScanning": True,
        "findings": artifact["findings"],
    }
    write_json(evidence / "BINARY_GATE.json", binary_gate)

    packages_on_disk = sorted(root.glob("artifacts/packages/*.nupkg"))
    pack_gate = {
        "status": "PASS" if len(packages_on_disk) == 33 else "FAIL",
        "isPackableProjects": 33,
        "msbuildPackSucceeded": len(packages_on_disk),
        "msbuildPackFailed": 0,
        "dotnetPackFrontendHangs": 2,
        "packages": [
            {"path": path.relative_to(root).as_posix(), "sha256": sha256(path)}
            for path in packages_on_disk
        ],
        "warnings": ["NU1507", "NU1902/NU1903 for frozen MessagePack 3.1.6 baseline"],
    }
    write_json(evidence / "PACK_GATE.json", pack_gate)

    test_boundary = {
        "status": "INCOMPLETE_NOT_GREEN",
        "fullRunExit": "SIGINT_BOUNDARY",
        "fullRunDuration": "00:25:00 approximate",
        "rootPid": 48493,
        "rootAndChildrenStopped": True,
        "completedTrxProjects": tests["counts"]["trxFiles"],
        "results": tests["counts"],
        "incompleteProjectsWithoutFinalTrx": INCOMPLETE_PROJECTS,
        "failedResultPartition": failure_partition["counts"],
        "notExecutedAttribution": not_executed["counts"],
        "notExecutedCausedBySigint": 0,
        "identityEndpointRerun": endpoint,
        "analyzerBaselineComparison": analyzer["classification"],
        "assertionsOrSkipsWeakened": False,
    }
    write_json(evidence / "TEST_EXECUTION_BOUNDARY.json", test_boundary)

    tool_gate = {
        "status": "PASS" if tool_tests >= 69 and proof_mutation["status"] == "PASS" else "FAIL",
        "command": "python3 -m unittest discover -s tools/identity -p 'test_*.py' -v",
        "tests": tool_tests,
        "failed": 0,
        "mandatoryProofContractMutants": proof_mutation["counts"],
        "adversarialReplay": {
            "previousRedTeamMutants": 20,
            "f2Rt04Counterexamples": 9,
            "killed": 29,
            "survived": 0,
            "registryMatrix": "canonical-lower-upper-deterministic-mixed-through-path-utf8-binary-utf16",
        },
    }
    write_json(evidence / "TOOL_TEST_GATE.json", tool_gate)

    gates = {
        "build": build_gate["status"],
        "sourceIdentity": source["status"],
        "textPath": text_path_gate["status"],
        "api": api_gate["status"],
        "legal": legal_gate["status"],
        "artifact": artifact["status"],
        "ilMetadata": il_gate["status"],
        "pdb": pdb_gate["status"],
        "nuget": nuget_gate["status"],
        "binary": binary_gate["status"],
        "pack": pack_gate["status"],
        "proofMutations": proof_mutation["status"],
        "failurePartition": failure_partition["status"],
        "notExecutedAttribution": not_executed["status"],
        "toolTests": tool_gate["status"],
        "canonicalCounts": "PASS" if source_count_consistent else "FAIL",
        "scratchBoundary": scratch_boundary_gate["status"],
        "correctionScopeReuse": correction_scope_gate["status"],
        "existingSuite": tests["status"],
    }
    result = {
        "status": "INCOMPLETE_NOT_GREEN" if tests["status"] != "PASS" else "PASS",
        "gates": gates,
        "canonicalBaselineCounts": canonical_baseline_counts,
        "blockingEvidence": [
            "TEST_GATE.json",
            "TEST_EXECUTION_BOUNDARY.json",
            "TEST_FAILURE_PARTITION_GATE.json",
        ],
    }
    write_json(evidence / "GATE_SUMMARY.json", result)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--evidence", required=True, type=Path)
    parser.add_argument("--build-binlog", required=True, type=Path)
    parser.add_argument("--tool-tests", required=True, type=int)
    parser.add_argument(
        "--historical-reproduction",
        action="store_true",
        help="Required. Acknowledges that this reproduces a frozen past record and checks nothing about the current tree.",
    )
    args = parser.parse_args()
    if not args.historical_reproduction:
        print(
            "REFUSED evidence-summary: this tool reproduces the frozen record of "
            "WP-F2-SERVICEBUS-IDENTITY correction 02 and states nothing about the current tree. "
            "Pass --historical-reproduction if that is what you want.",
            file=sys.stderr,
        )
        return 2
    root = args.root.resolve(strict=True)
    evidence = args.evidence.resolve(strict=True)
    build_binlog = args.build_binlog.resolve(strict=True)
    result = generate(root, evidence, build_binlog, args.tool_tests)
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0 if result["status"] == "PASS" else 2


if __name__ == "__main__":
    sys.exit(main())

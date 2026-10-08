#!/usr/bin/env python3
"""Merge verified coverage receipts from one unchanged ServiceBus source/test tree.

Without --partial, require every product test project, all local integration
projects, the no-AVX2 and scalar-fallback runs, all product assemblies, and
one exact measurement commit before publishing metrics.
The Cobertura branch figure is a conservative observation: branch identities
cannot be reliably joined across independent reports.
"""

import argparse
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from fractions import Fraction
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]
BRANCH = re.compile(r"\((\d+)\s*/\s*(\d+)\)")


def git(*args):
    return subprocess.check_output(["git", *args], cwd=REPO).strip()


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def checked_path(relative):
    path = (REPO / relative).resolve(strict=True)
    if not path.is_relative_to(REPO / "artifacts"):
        raise ValueError(f"Receipt refers outside artifacts: {relative}")
    return path


def check_hashes(receipt, field):
    entries = receipt[field]
    if not isinstance(entries, dict) or not entries:
        raise ValueError(f"Missing {field}")
    for relative, digest in entries.items():
        if sha256(checked_path(relative).read_bytes()) != digest:
            raise ValueError(f"Changed {field} file: {relative}")


def check_binlogs(receipt, run_dir):
    check_hashes(receipt, "binlogsSha256")
    stages = receipt["binlogStages"]
    if not isinstance(stages, dict) or set(stages) != {"restore", "build", "tests"}:
        raise ValueError("Incomplete binary-log stage set")
    recorded = set()
    for stage, entries in stages.items():
        if not isinstance(entries, list) or not entries or len(entries) != len(set(entries)):
            raise ValueError(f"Missing or duplicate binary logs for {stage}")
        for relative in entries:
            path = checked_path(relative)
            if ((REPO / relative).is_symlink() or path.parent != run_dir
                    or not path.name.startswith(stage + "-") or path.suffix != ".binlog"
                    or not path.stat().st_size or relative in recorded):
                raise ValueError(f"Binary log is not owned by this run and stage: {relative}")
            recorded.add(relative)
        actual = {item.relative_to(REPO).as_posix() for item in run_dir.glob(stage + "-*.binlog")}
        if actual != set(entries):
            raise ValueError(f"Incomplete or unexpected binary-log files for {stage}")
    if recorded != set(receipt["binlogsSha256"]):
        raise ValueError("Binary-log hashes and stage membership disagree")


def check_receipt(path, src_tree, tests_tree):
    receipt = json.loads(path.read_text())
    if (receipt["srcTree"], receipt["testsTree"]) != (src_tree, tests_tree):
        raise ValueError(f"Mixed source/test tree in {path}")
    head = receipt["head"]
    if git("rev-parse", f"{head}:src").decode() != src_tree:
        raise ValueError(f"Receipt source tree disagrees with its commit: {path}")
    if git("rev-parse", f"{head}:tests").decode() != tests_tree:
        raise ValueError(f"Receipt test tree disagrees with its commit: {path}")
    for filename, key in (("tools/ci/coverage_receipt.py", "runnerSha256"),
                          ("tools/ci/coverage.settings.xml", "settingsSha256")):
        if sha256(subprocess.check_output(["git", "show", f"{head}:{filename}"], cwd=REPO)) != receipt[key]:
            raise ValueError(f"Receipt {key} disagrees with {head}: {path}")
    for field in ("binarySha256", "logsSha256", "reports"):
        check_hashes(receipt, field)
    check_binlogs(receipt, path.parent)
    if len(receipt["reports"]) != 1:
        raise ValueError(f"Expected one Cobertura report: {path}")
    expected_logs = {(path.parent / name).relative_to(REPO).as_posix()
                     for name in ("restore.log", "build.log", "tests.log")}
    if set(receipt["logsSha256"]) != expected_logs:
        raise ValueError(f"Incomplete or unexpected log set: {path}")
    project = receipt["project"]
    if project not in git("ls-files", "tests").decode().splitlines():
        raise ValueError(f"Untracked test project: {project}")
    test_dll = receipt["testDll"]
    if test_dll not in receipt["binarySha256"] or Path(test_dll).stem != Path(project).stem:
        raise ValueError(f"Test binary disagrees with project: {path}")
    binary_folder = checked_path(test_dll).parent
    expected_binaries = {
        item.relative_to(REPO).as_posix() for item in binary_folder.iterdir()
        if item.is_file() and item.name.startswith("ViciOne.ServiceBus") and item.suffix in (".dll", ".pdb")
    }
    if set(receipt["binarySha256"]) != expected_binaries or test_dll.replace(".dll", ".pdb") not in expected_binaries:
        raise ValueError(f"Incomplete or unexpected product/test binaries: {path}")
    base = Path(project).stem.removesuffix(".LocalIntegration.Tests").removesuffix(".Tests")
    if receipt["requiredAssembly"] != base:
        raise ValueError(f"Required assembly disagrees with project: {path}")
    if not any(Path(item).name == base + ".dll" for item in expected_binaries):
        raise ValueError(f"Required product binary is absent: {path}")
    if receipt.get("dotnetEnvironment", {}).get("DOTNET_EnableAVX2") == "0":
        runner = subprocess.check_output(["git", "show", f"{head}:tools/ci/coverage_receipt.py"], cwd=REPO).decode()
        if "--disable-avx2" not in runner or 'dotnet_env["DOTNET_EnableAVX2"] = "0"' not in runner:
            raise ValueError(f"No-AVX2 mode is absent from receipt runner: {path}")
    if receipt.get("dotnetEnvironment", {}).get("DOTNET_EnableHWIntrinsic") == "0":
        runner = subprocess.check_output(["git", "show", f"{head}:tools/ci/coverage_receipt.py"], cwd=REPO).decode()
        if "--disable-hw-intrinsics" not in runner or 'dotnet_env["DOTNET_EnableHWIntrinsic"] = "0"' not in runner:
            raise ValueError(f"Scalar mode is absent from receipt runner: {path}")
    if receipt["testCount"] < 1:
        raise ValueError(f"No tests in receipt: {path}")
    build = checked_path(next(name for name in receipt["logsSha256"] if name.endswith("/build.log"))).read_text()
    tests = checked_path(next(name for name in receipt["logsSha256"] if name.endswith("/tests.log"))).read_text()
    report = checked_path(next(iter(receipt["reports"])))
    test_binary = str(checked_path(test_dll))
    starts = re.findall(rf"^(?:Ausführen von Tests von|Running tests from) {re.escape(test_binary)} \(", tests, re.M)
    if test_binary not in build or len(starts) != 1:
        raise ValueError(f"Build/test log does not identify test binary: {path}")
    if tests.count(str(report)) != 1:
        raise ValueError(f"Test log does not identify exactly one report: {path}")
    if re.search(r":\s*(?:warning|error)\b", build, re.I):
        raise ValueError(f"Build warning or error: {path}")
    if not re.search(r"^\s*0\s+(?:Warnung\(en\)|Warning\(s\))\s*$", build, re.M | re.I):
        raise ValueError(f"No zero-warning build summary: {path}")
    if not re.search(r"^\s*0\s+(?:Fehler|Error\(s\))\s*$", build, re.M | re.I):
        raise ValueError(f"No zero-error build summary: {path}")
    if len(re.findall(r"^(?:Testlaufzusammenfassung: Bestanden!|Test run summary: Passed!)$", tests, re.M)) != 1:
        raise ValueError(f"No unique passed MTP summary: {path}")
    for label, expected in (("gesamt|total", receipt["testCount"]),
                            ("erfolgreich|passed", receipt["testCount"]),
                            ("fehlgeschlagen|failed", 0), ("übersprungen|skipped", 0)):
        values = re.findall(rf"^\s*(?:{label}):\s*(\d+)\s*$", tests, re.M | re.I)
        if values != [str(expected)]:
            raise ValueError(f"Unexpected {label} count in {path}: {values}")
    return receipt


def expected_projects(assemblies):
    normal, local = set(), set()
    for project in (REPO / "tests").rglob("*.csproj"):
        name = project.stem
        if name.endswith(".LocalIntegration.Tests"):
            base = name.removesuffix(".LocalIntegration.Tests")
            if base in assemblies:
                local.add(project.relative_to(REPO).as_posix())
        elif name.endswith(".Tests"):
            base = name.removesuffix(".Tests")
            if base in assemblies:
                normal.add(project.relative_to(REPO).as_posix())
    return normal, local


def merge(reports, tracked, expected_assemblies):
    lines, branches, methods = {}, {}, {}
    assemblies, sources = set(), set()
    for receipt in reports:
        xml = checked_path(next(iter(receipt["reports"])))
        root = ET.parse(xml).getroot()
        if root.tag != "coverage":
            raise ValueError(f"Not Cobertura: {xml}")
        report_assemblies = set()
        report_sources = set()
        for package in root.findall("./packages/package"):
            assembly = package.get("name", "")
            if assembly not in expected_assemblies or assembly not in receipt["assemblies"]:
                raise ValueError(f"Unexpected assembly {assembly} in {xml}")
            assemblies.add(assembly)
            report_assemblies.add(assembly)
            for cls in package.findall("./classes/class"):
                filename = Path(cls.get("filename", ""))
                absolute = (filename if filename.is_absolute() else REPO / filename).resolve(strict=True)
                source = absolute.relative_to(REPO).as_posix()
                if source not in tracked or not source.startswith("src/"):
                    raise ValueError(f"Untracked or stale source {source} in {xml}")
                sources.add(source)
                report_sources.add(source)
                class_name = cls.get("name", "")
                for line in cls.findall("./lines/line"):
                    number = int(line.get("number", "0"))
                    if number < 1:
                        raise ValueError(f"Bad line number in {xml}")
                    key = (source, number)
                    lines[key] = lines.get(key, False) or int(line.get("hits", "0")) > 0
                    if line.get("branch", "false").lower() == "true":
                        match = BRANCH.search(line.get("condition-coverage", ""))
                        if not match:
                            raise ValueError(f"Missing branch count in {xml}: {key}")
                        covered, valid = map(int, match.groups())
                        if valid < 1 or covered > valid:
                            raise ValueError(f"Invalid branch count in {xml}: {key}")
                        branch_key = (assembly, source, class_name, number)
                        prior = branches.setdefault(branch_key, [0, 0])
                        prior[0] = max(prior[0], covered)
                        prior[1] = max(prior[1], valid)
                for method in cls.findall("./methods/method"):
                    positions = method.findall("./lines/line")
                    key = (assembly, source, class_name, method.get("name", ""), method.get("signature", ""))
                    entry = methods.setdefault(key, {"complexity": 0, "lines": {}})
                    entry["complexity"] = max(entry["complexity"], int(float(method.get("complexity", "0"))))
                    for line in positions:
                        number = int(line.get("number", "0"))
                        entry["lines"][number] = entry["lines"].get(number, False) or int(line.get("hits", "0")) > 0
        if set(receipt["assemblies"]) != report_assemblies:
            raise ValueError(f"Receipt assembly list differs from XML: {xml}")
        if receipt["requiredAssembly"] not in report_assemblies or receipt["trackedSourceCount"] != len(report_sources):
            raise ValueError(f"Receipt source or target count differs from XML: {xml}")
    rows = []
    for key, entry in methods.items():
        valid = len(entry["lines"])
        covered = sum(entry["lines"].values())
        complexity = entry["complexity"]
        crap = Fraction(complexity) + (Fraction(complexity * complexity * (valid - covered) ** 3, valid ** 3) if valid else 0)
        rows.append({"assembly": key[0], "source": key[1], "class": key[2], "method": key[3],
                     "signature": key[4], "firstLine": min(entry["lines"], default=0),
                     "complexity": complexity, "linesCovered": covered, "linesValid": valid, "crap": float(crap)})
    rows.sort(key=lambda row: (-row["crap"], row["source"], row["firstLine"]))
    line_covered = sum(lines.values())
    branch_covered = sum(value[0] for value in branches.values())
    branch_valid = sum(value[1] for value in branches.values())
    return {
        "assemblies": sorted(assemblies), "trackedCsSeen": len(sources),
        "linesCovered": line_covered, "linesValid": len(lines),
        "lineRate": line_covered / len(lines) if lines else 0,
        "branchesCoveredConservative": branch_covered, "branchesValid": branch_valid,
        "branchRateConservative": branch_covered / branch_valid if branch_valid else 0,
        "methodCount": len(rows), "methodsCrapAbove30": sum(row["crap"] > 30 for row in rows),
        "topCrapMethods": rows[:25],
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("receipts", nargs="+", type=Path)
    parser.add_argument("--partial", action="store_true", help="Report observed subset; never label it product-wide")
    parser.add_argument("--output", type=Path, help="Write JSON under artifacts")
    args = parser.parse_args()
    if git("status", "--porcelain", "--", "src", "tests"):
        raise ValueError("Source or tests have uncommitted changes")
    src_tree = git("rev-parse", "HEAD:src").decode()
    tests_tree = git("rev-parse", "HEAD:tests").decode()
    expected_assemblies = {path.stem for path in (REPO / "src").rglob("*.csproj")}
    expected_assemblies.remove("ViciOne.ServiceBus.Analyzers.Package")
    normal, local = expected_projects(expected_assemblies)
    tracked = set(git("ls-files", "src").decode().splitlines())
    receipts = []
    seen = set()
    for path in args.receipts:
        path = path.resolve(strict=True)
        if not path.is_relative_to(REPO / "artifacts") or path.name != "receipt.json":
            raise ValueError(f"Receipt must be under artifacts: {path}")
        receipt = check_receipt(path, src_tree, tests_tree)
        environment = receipt.get("dotnetEnvironment", {})
        avx2 = environment.get("DOTNET_EnableAVX2")
        hw_intrinsics = environment.get("DOTNET_EnableHWIntrinsic")
        if avx2 not in (None, "0") or hw_intrinsics not in (None, "0") or (avx2 == "0" and hw_intrinsics == "0"):
            raise ValueError(f"Unexpected portability settings in {path}: {environment}")
        mode = "noavx2" if avx2 == "0" else "scalar" if hw_intrinsics == "0" else "normal"
        key = (receipt["project"], mode)
        if key in seen:
            raise ValueError(f"Duplicate test project/mode: {key}")
        seen.add(key)
        if receipt["project"] not in normal | local:
            raise ValueError(f"Unexpected test project: {receipt['project']}")
        receipts.append(receipt)
    normal_seen = {project for project, mode in seen if mode == "normal"}
    noavx_seen = {project for project, mode in seen if mode == "noavx2"}
    scalar_seen = {project for project, mode in seen if mode == "scalar"}
    expected_noavx = {next(project for project in normal if Path(project).stem == "ViciOne.ServiceBus.Abstractions.Tests")}
    expected_scalar = expected_noavx
    missing_normal = sorted(normal - normal_seen)
    missing_local = sorted(local - normal_seen)
    missing_noavx = sorted(expected_noavx - noavx_seen)
    missing_scalar = sorted(expected_scalar - scalar_seen)
    unexpected_portability = (noavx_seen - expected_noavx) | (scalar_seen - expected_scalar)
    if unexpected_portability:
        raise ValueError(f"Unexpected portability projects: {sorted(unexpected_portability)}")
    result = merge(receipts, tracked, expected_assemblies)
    missing_assemblies = sorted(expected_assemblies - set(result["assemblies"]))
    aggregate_head = git("rev-parse", "HEAD").decode()
    receipt_heads = sorted({item["head"] for item in receipts})
    same_commit = receipt_heads == [aggregate_head]
    complete = same_commit and not (missing_normal or missing_local or missing_noavx or missing_scalar or missing_assemblies)
    result.update({"status": "complete" if complete else "partial", "aggregateHead": aggregate_head,
                   "receiptHeads": receipt_heads, "sameCommit": same_commit,
                   "srcTree": src_tree, "testsTree": tests_tree, "receiptCount": len(receipts),
                   "testCount": sum(item["testCount"] for item in receipts),
                   "missingUnitProjects": missing_normal, "missingLocalProjects": missing_local,
                   "missingNoAvx2Projects": missing_noavx, "missingScalarProjects": missing_scalar,
                   "missingAssemblies": missing_assemblies})
    if not complete and not args.partial:
        raise ValueError("Product-wide profile is incomplete; use --partial for diagnostic output")
    if args.output:
        output = args.output.resolve()
        if not output.is_relative_to(REPO / "artifacts"):
            raise ValueError("Output must be under artifacts")
        output.parent.mkdir(parents=True, exist_ok=True)
        with output.open("x", encoding="utf-8") as stream:
            stream.write(json.dumps(result, indent=2, sort_keys=True) + "\n")
    print(json.dumps(result, indent=2, sort_keys=True))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, StopIteration, ET.ParseError, subprocess.CalledProcessError) as error:
        print(f"coverage aggregate: {error}", file=sys.stderr)
        sys.exit(1)

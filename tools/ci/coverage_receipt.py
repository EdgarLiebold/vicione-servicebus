#!/usr/bin/env python3
"""Build and test one product MTP project from clean Git trees in a fresh artifact directory.

The target product assembly is inferred from a .Tests or .LocalIntegration.Tests
project name. Pass --required-assembly when that inferred product name is wrong.
Test-only infrastructure projects without a product assembly are outside this
coverage runner's scope.
"""

import argparse
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path


REPO = Path(__file__).resolve().parents[2]


def git(*args):
    return subprocess.check_output(["git", *args], cwd=REPO, text=True).strip()


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def relative(path):
    return path.relative_to(REPO).as_posix()


def clean_git_state():
    tracked_diff = subprocess.run(["git", "diff", "--quiet", "HEAD", "--"], cwd=REPO, check=False)
    if tracked_diff.returncode != 0:
        raise ValueError("Tracked repository files differ from HEAD")
    if git("ls-files", "--others", "--exclude-standard", "--", "src", "tests"):
        raise ValueError("Untracked src or tests files exist")
    return {
        "head": git("rev-parse", "HEAD"),
        "srcTree": git("rev-parse", "HEAD:src"),
        "testsTree": git("rev-parse", "HEAD:tests"),
    }


def run_and_log(command, log):
    with log.open("x", encoding="utf-8") as stream:
        result = subprocess.run(command, cwd=REPO, stdout=stream, stderr=subprocess.STDOUT, check=False)
    if result.returncode:
        raise ValueError(f"Command exited {result.returncode}; see {log}")


def binaries_in(test_dll):
    folder = test_dll.parent
    files = sorted(
        path for path in folder.iterdir()
        if path.is_file() and path.name.startswith("ViciOne.ServiceBus") and path.suffix in (".dll", ".pdb")
    )
    if test_dll not in files or test_dll.with_suffix(".pdb") not in files:
        raise ValueError("Test DLL or its PDB is absent")
    if not any(path.suffix == ".dll" and path != test_dll for path in files):
        raise ValueError("No product or support DLL was found beside the test DLL")
    return {relative(path): sha256(path) for path in files}


def test_count(log, test_dll, report, minimum):
    content = log.read_text(encoding="utf-8")
    starts = re.findall(rf"^(?:Ausführen von Tests von|Running tests from) {re.escape(str(test_dll))} \(", content, re.M)
    if len(starts) != 1:
        raise ValueError("Test log does not have exactly one start for the expected DLL")
    summaries = re.findall(r"^(?:Testlaufzusammenfassung: Bestanden!|Test run summary: Passed!)$", content, re.M)
    if content.count(str(report)) != 1 or len(summaries) != 1:
        raise ValueError("Test log does not have exactly one report and passing MTP summary")

    def count(*labels):
        names = "|".join(re.escape(label) for label in labels)
        values = re.findall(rf"^\s*(?:{names}):\s*(\d+)\s*$", content, re.M | re.I)
        if len(values) != 1:
            raise ValueError(f"Test log has no unique {labels} count")
        return int(values[0])

    total = count("gesamt", "total")
    passed = count("erfolgreich", "passed")
    failed = count("fehlgeschlagen", "failed")
    skipped = count("übersprungen", "skipped")
    if total < minimum or passed != total or failed or skipped:
        raise ValueError(f"Invalid test result: {passed}/{total}, failed={failed}, skipped={skipped}")
    return total


def report_sources(report, built_binaries, required_assembly):
    root = ET.parse(report).getroot()
    if root.tag != "coverage":
        raise ValueError("Report is not a Cobertura coverage document")
    tracked = set(git("ls-files", "src").splitlines())
    assemblies = set()
    sources = set()
    expected = {
        Path(path).stem for path in built_binaries
        if path.endswith(".dll") and ".Tests" not in Path(path).stem
    }
    for package in root.findall("./packages/package"):
        assembly = package.get("name", "")
        if not assembly or assembly not in expected:
            raise ValueError(f"Unexpected report assembly: {assembly!r}")
        for cls in package.findall("./classes/class"):
            filename = Path(cls.get("filename", ""))
            if not str(filename):
                raise ValueError("Report class has no source filename")
            try:
                absolute = filename if filename.is_absolute() else REPO / filename
                source = absolute.resolve(strict=True).relative_to(REPO / "src")
            except (OSError, ValueError) as error:
                raise ValueError(f"Stale source in report: {filename}") from error
            source = f"src/{source.as_posix()}"
            if source not in tracked:
                raise ValueError(f"Untracked source in report: {source}")
            assemblies.add(assembly)
            sources.add(source)
    if required_assembly not in expected:
        raise ValueError(f"Required product assembly {required_assembly} is not in the fresh build")
    if required_assembly not in assemblies or not sources:
        raise ValueError(f"Report does not contain the required product assembly {required_assembly}")
    return sorted(assemblies), len(sources)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True)
    parser.add_argument("--run-dir", required=True)
    parser.add_argument("--minimum-expected-tests", type=int, default=1)
    parser.add_argument("--required-assembly", help="Product assembly that must be covered; defaults to the test project name without its test suffix")
    args = parser.parse_args()
    if args.minimum_expected_tests < 1:
        raise ValueError("Minimum expected tests must be positive")

    project = Path(args.project).resolve(strict=True)
    run_dir = Path(args.run_dir).resolve()
    if not project.is_relative_to(REPO / "tests") or project.suffix != ".csproj":
        raise ValueError("Project must be a repository test project")
    if not run_dir.is_relative_to(REPO / "artifacts") or run_dir.exists():
        raise ValueError("Run directory must be a new path under artifacts")

    start_state = clean_git_state()
    run_dir.mkdir(parents=True)
    sdk = run_dir / "sdk"
    report = run_dir / "coverage.cobertura.xml"
    settings = REPO / "tools/ci/coverage.settings.xml"
    settings_hash = sha256(settings)
    runner_hash = sha256(Path(__file__))
    run_and_log(["dotnet", "restore", str(project), "--locked-mode", "--artifacts-path", str(sdk), "-v:minimal"], run_dir / "restore.log")
    run_and_log(["dotnet", "build", str(project), "--no-restore", "--configuration", "Release", "--artifacts-path", str(sdk), "-v:minimal"], run_dir / "build.log")
    build_log = (run_dir / "build.log").read_text(encoding="utf-8")
    if re.search(r":\s*(?:warning|error)\b", build_log, re.I):
        raise ValueError("Build log contains warnings or errors")
    warnings = re.findall(r"^\s*(\d+)\s+(?:Warnung\(en\)|Warning\(s\))\s*$", build_log, re.M | re.I)
    errors = re.findall(r"^\s*(\d+)\s+(?:Fehler|Error\(s\))\s*$", build_log, re.M | re.I)
    if warnings != ["0"] or errors != ["0"]:
        raise ValueError("Build summary does not prove zero warnings and errors")
    candidates = list((sdk / "bin").glob(f"*/release/{project.stem}.dll"))
    if len(candidates) != 1:
        raise ValueError(f"Expected one freshly built test DLL, found {len(candidates)}")
    test_dll = candidates[0]
    if str(test_dll) not in build_log:
        raise ValueError("Build log does not name the test DLL")
    built_binaries = binaries_in(test_dll)
    if clean_git_state() != start_state:
        raise ValueError("Git tree changed during restore/build")
    run_and_log([
        "dotnet", "test", "--project", str(project), "--configuration", "Release", "--no-build", "--no-restore",
        "--artifacts-path", str(sdk), "--coverage", "--coverage-output-format", "cobertura",
        "--coverage-settings", str(settings), "--coverage-output", str(report),
        "--minimum-expected-tests", str(args.minimum_expected_tests), "--progress", "off",
    ], run_dir / "tests.log")
    if not report.is_file() or not report.stat().st_size:
        raise ValueError("Coverage report is missing or empty")
    total = test_count(run_dir / "tests.log", test_dll, report, args.minimum_expected_tests)
    if binaries_in(test_dll) != built_binaries:
        raise ValueError("Test or product DLL/PDB bytes changed during tests")
    if clean_git_state() != start_state or sha256(settings) != settings_hash or sha256(Path(__file__)) != runner_hash:
        raise ValueError("Git tree, coverage settings, or runner changed during the run")
    required_assembly = args.required_assembly
    if required_assembly is None:
        required_assembly = project.stem
        for suffix in (".LocalIntegration.Tests", ".Tests"):
            if required_assembly.endswith(suffix):
                required_assembly = required_assembly[:-len(suffix)]
                break
    assemblies, source_count = report_sources(report, built_binaries, required_assembly)
    receipt = {
        **start_state,
        "completedUtc": datetime.now(timezone.utc).isoformat(),
        "project": relative(project),
        "testDll": relative(test_dll),
        "binarySha256": built_binaries,
        "settingsSha256": settings_hash,
        "runnerSha256": runner_hash,
        "testCount": total,
        "assemblies": assemblies,
        "requiredAssembly": required_assembly,
        "trackedSourceCount": source_count,
        "reports": {relative(report): sha256(report)},
        "logsSha256": {relative(path): sha256(path) for path in sorted(run_dir.glob("*.log"))},
    }
    with (run_dir / "receipt.json").open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(f"Verified {total} tests, {len(built_binaries)} unchanged binaries, {source_count} tracked product sources")
    print(run_dir / "receipt.json")


if __name__ == "__main__":
    try:
        main()
    except (OSError, subprocess.CalledProcessError, ValueError, ET.ParseError) as error:
        print(f"coverage receipt: {error}", file=sys.stderr)
        sys.exit(1)

#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Explain every NotExecuted TRX record from source declarations or an exact runtime reason."""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from pathlib import Path


EXPLICIT_ATTRIBUTE = re.compile(r"\[[^\]]*\bExplicit\b[^\]]*\]", re.MULTILINE)
CLASS_DECLARATION = re.compile(r"\b(?:class|record|struct)\s+([A-Za-z_]\w*)")
DECLARATION = re.compile(
    r"\b(?P<class_kind>class|record|struct)\s+(?P<class_name>[A-Za-z_]\w*)"
    r"|\b(?:public|protected|private|internal)\s+"
    r"(?:(?:static|virtual|override|abstract|sealed|async|new|partial|extern|unsafe)\s+)*"
    r"(?:[A-Za-z_]\w*(?:[.<>,?\[\]]|\s+(?=[.<>,?\[\]]))*)\s+"
    r"(?P<method_name>[A-Za-z_]\w*)\s*\(",
    re.MULTILINE,
)


def project_name(assembly: str) -> str:
    normalized = assembly.replace("\\", "/")
    match = re.search(r"/tests/([^/]+)/bin/", normalized, re.IGNORECASE)
    return match.group(1) if match else ""


def explicit_declarations(project: Path, repository: Path) -> list[dict[str, object]]:
    declarations: list[dict[str, object]] = []
    for source in sorted(project.rglob("*.cs")):
        text = source.read_text(encoding="utf-8-sig")
        for attribute in EXPLICIT_ATTRIBUTE.finditer(text):
            declaration = DECLARATION.search(text, attribute.end(), min(len(text), attribute.end() + 1200))
            if declaration is None:
                continue
            name = declaration.group("class_name") or declaration.group("method_name")
            preceding_classes = list(CLASS_DECLARATION.finditer(text, 0, attribute.start()))
            declarations.append(
                {
                    "kind": "class" if declaration.group("class_name") else "method",
                    "name": name,
                    "path": source.relative_to(repository).as_posix(),
                    "line": text.count("\n", 0, attribute.start()) + 1,
                    "containingClass": preceding_classes[-1].group(1) if preceding_classes else None,
                }
            )
    return declarations


def analyze(report_path: Path, repository: Path) -> dict[str, object]:
    report = json.loads(report_path.read_text(encoding="utf-8"))
    records = [record for record in report.get("results", []) if record.get("outcome") == "NotExecuted"]
    project_directories = {
        directory.name.casefold(): directory
        for directory in (repository / "tests").iterdir()
        if directory.is_dir()
    }
    cache: dict[str, list[dict[str, object]]] = {}
    results: list[dict[str, object]] = []
    findings: list[dict[str, str]] = []

    for record in records:
        message = str(record.get("messageFirstLine") or "")
        evidence: list[dict[str, object]] = []
        if message == "Only supported on WIN":
            category = "RUNTIME_PLATFORM_SKIP"
            reason = "exact NUnit skip reason: Only supported on WIN"
        else:
            project = project_name(str(record.get("assembly", "")))
            directory = project_directories.get(project.casefold())
            if directory is None:
                declarations = []
            else:
                declarations = cache.setdefault(
                    project.casefold(), explicit_declarations(directory, repository)
                )
            method = str(record.get("methodName", "")).split("(", 1)[0]
            class_identifiers = set(re.findall(r"[A-Za-z_]\w*", str(record.get("className", ""))))
            evidence = [
                declaration
                for declaration in declarations
                if (
                    declaration["kind"] == "method" and declaration["name"] == method
                ) or (
                    declaration["kind"] == "class" and declaration["name"] in class_identifiers
                )
            ]
            explicit_classes = [
                declaration for declaration in evidence if declaration["kind"] == "class"
            ]
            if explicit_classes:
                evidence = explicit_classes
            contextual_methods = [
                declaration
                for declaration in evidence
                if declaration["kind"] == "method"
                and declaration.get("containingClass") in class_identifiers
            ]
            if contextual_methods:
                evidence = contextual_methods
            if len(evidence) > 1:
                lowered_identifiers = {identifier.casefold() for identifier in class_identifiers}

                def path_score(declaration: dict[str, object]) -> int:
                    parts = re.findall(r"[A-Za-z_]\w*", str(declaration["path"]))
                    return sum(part.casefold() in lowered_identifiers for part in parts)

                best_score = max(path_score(declaration) for declaration in evidence)
                evidence = [
                    declaration for declaration in evidence if path_score(declaration) == best_score
                ]
            if len(evidence) > 1:
                minimum_depth = min(len(Path(str(declaration["path"])).parts) for declaration in evidence)
                evidence = [
                    declaration
                    for declaration in evidence
                    if len(Path(str(declaration["path"])).parts) == minimum_depth
                ]
            category = "SOURCE_DECLARED_EXPLICIT" if evidence else "UNEXPLAINED_NOT_EXECUTED"
            reason = "matched NUnit Explicit declaration" if evidence else "no matching Explicit declaration"

        result = {
            "assemblyProject": project_name(str(record.get("assembly", ""))),
            "className": record.get("className"),
            "methodName": record.get("methodName"),
            "messageFirstLine": record.get("messageFirstLine"),
            "duration": record.get("duration"),
            "category": category,
            "reason": reason,
            "sourceEvidence": evidence,
        }
        results.append(result)
        if category == "UNEXPLAINED_NOT_EXECUTED":
            findings.append(
                {
                    "className": str(record.get("className", "")),
                    "methodName": str(record.get("methodName", "")),
                    "reason": reason,
                }
            )

    categories = Counter(str(result["category"]) for result in results)
    return {
        "status": "PASS" if records and not findings else "FAIL",
        "counts": {
            "notExecuted": len(records),
            "categories": dict(sorted(categories.items())),
            "projects": dict(
                sorted(Counter(str(result["assemblyProject"]) for result in results).items())
            ),
        },
        "findings": findings,
        "results": results,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("--repository", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = analyze(args.report, args.repository.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "counts": result["counts"]}, indent=2))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())

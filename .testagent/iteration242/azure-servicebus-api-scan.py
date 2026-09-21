#!/usr/bin/env python3
"""Read-only, lexical Azure Service Bus API/test inventory; no .NET execution."""

import hashlib
import argparse
import json
import re
import subprocess
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCOPE = ROOT / "src/Transports/ViciOne.ServiceBus.AzureServiceBus"
TEST_DIRS = [
    ROOT / "tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests",
    ROOT / "tests/Transports/ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests",
]


def paths(base, globs):
    cmd = ["rg", "--files", "--hidden", "--no-ignore", str(base)]
    for glob in globs:
        cmd.extend(["-g", glob])
    for excluded in ("review", "TestResults", "obj", "bin"):
        cmd.extend(["-g", f"!**/{excluded}/**"])
    result = subprocess.run(cmd, check=True, capture_output=True, text=True)
    return sorted(Path(line) for line in result.stdout.splitlines())


def clean(text):
    """Blank comments and literals while preserving offsets and newlines."""
    out = list(text)
    i = 0
    state = "code"
    while i < len(text):
        c = text[i]
        n = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if c == "/" and n == "/":
                state = "line"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == "/" and n == "*":
                state = "block"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c in ('"', "'"):
                state = c
                out[i] = " "
                i += 1
                continue
        elif state == "line":
            if c == "\n":
                state = "code"
            else:
                out[i] = " "
            i += 1
            continue
        elif state == "block":
            if c == "*" and n == "/":
                out[i] = out[i + 1] = " "
                i += 2
                state = "code"
                continue
            if c != "\n":
                out[i] = " "
            i += 1
            continue
        else:
            if c == "\\":
                out[i] = " "
                if i + 1 < len(text):
                    out[i + 1] = " "
                i += 2
                continue
            if c == state:
                state = "code"
            if c != "\n":
                out[i] = " "
            i += 1
            continue
        i += 1
    return "".join(out)


def depths(lines):
    result = []
    depth = 0
    for line in lines:
        result.append(depth)
        depth += line.count("{") - line.count("}")
    return result


TYPE_RE = re.compile(
    r"^\s*(?:(?:public|protected|internal|private|static|sealed|abstract|partial|readonly|ref|record|class|struct)\s+)*"
    r"\b(class|interface|struct|enum|delegate|record)\s+(?:class\s+|struct\s+)?([A-Za-z_]\w*)"
)
VIS_RE = re.compile(r"^\s*(public|protected|internal|private)\b")
IDENT_RE = re.compile(r"[A-Za-z_]\w*")


def signature(lines, sanitized, i):
    parts = []
    for j in range(i, min(i + 35, len(lines))):
        fragment = sanitized[j]
        stop = len(fragment)
        for marker in ("{", ";", "=>"):
            pos = fragment.find(marker)
            if pos >= 0:
                stop = min(stop, pos)
        parts.append(lines[j][:stop].strip())
        if stop < len(fragment):
            break
    return " ".join(part for part in parts if part).strip()


def slots_from(sig, kind):
    if kind == "type" and "delegate" not in sig:
        return []
    pair = None
    if "this[" in sig:
        pair = (sig.index("[", sig.index("this[")), "[", "]")
    elif "(" in sig:
        pair = (sig.index("("), "(", ")")
    if pair is None:
        return []
    start, op, cl = pair
    depth = 0
    end = None
    for pos in range(start, len(sig)):
        if sig[pos] == op:
            depth += 1
        elif sig[pos] == cl:
            depth -= 1
            if depth == 0:
                end = pos
                break
    if end is None:
        return []
    body = sig[start + 1:end]
    if not body.strip():
        return []
    parts = []
    buf = []
    nesting = 0
    for char in body:
        if char in "<([{":
            nesting += 1
        elif char in ">)]}":
            nesting -= 1
        if char == "," and nesting == 0:
            parts.append("".join(buf).strip())
            buf = []
        else:
            buf.append(char)
    parts.append("".join(buf).strip())
    return [dict(ordinal=k + 1, declaration=part) for k, part in enumerate(parts)]


def method_name(before_parenthesis):
    before = before_parenthesis.rstrip()
    if before.endswith(">"):
        nesting = 0
        for pos in range(len(before) - 1, -1, -1):
            if before[pos] == ">":
                nesting += 1
            elif before[pos] == "<":
                nesting -= 1
                if nesting == 0:
                    before = before[:pos].rstrip()
                    break
    names = IDENT_RE.findall(before)
    return names[-1] if names else "<unknown>"


def parse_source(path, text):
    original = text.splitlines()
    sanitized = clean(text).splitlines()
    dep = depths(sanitized)
    namespace_match = re.search(r"^\s*namespace\s+([\w.]+)\s*;", text, re.M)
    namespace = namespace_match.group(1) if namespace_match else "<unknown>"
    types = []
    for i, line in enumerate(sanitized):
        match = TYPE_RE.match(line)
        if not match or not VIS_RE.match(line):
            continue
        visibility = VIS_RE.match(line).group(1)
        if visibility not in ("public", "protected"):
            continue
        opening = next((j for j in range(i, min(i + 35, len(sanitized))) if "{" in sanitized[j]), None)
        kind, name = match.group(1, 2)
        if kind == "delegate":
            opening = None
        if opening is None and kind != "delegate":
            continue
        closing = None
        if opening is not None:
            for j in range(opening + 1, len(sanitized)):
                if "}" in sanitized[j] and dep[j] + sanitized[j].count("{") - sanitized[j].count("}") == dep[opening]:
                    closing = j
                    break
        enclosing = [item for item in types if item["open_index"] is not None and item["open_index"] < i < item["close_index"]]
        parent = enclosing[-1] if enclosing else None
        external = parent is None or parent["external"]
        item = dict(path=str(path.relative_to(ROOT)), line=i + 1, namespace=namespace, name=name,
                    fullName=namespace + "." + (parent["name"] + "." if parent else "") + name,
                    kind=kind, visibility=visibility, external=external,
                    declaration=signature(original, sanitized, i),
                    open_index=opening, close_index=closing, open_depth=dep[opening] if opening is not None else None)
        item["parameterSlots"] = slots_from(item["declaration"], "type")
        types.append(item)
    members = []
    for typ in types:
        if not typ["external"] or typ["open_index"] is None or typ["close_index"] is None:
            continue
        for i in range(typ["open_index"] + 1, typ["close_index"]):
            line = sanitized[i]
            if dep[i] != typ["open_depth"] + 1 or not line.strip() or line.lstrip().startswith(("[", ":")):
                continue
            if TYPE_RE.match(line):
                continue
            if typ["kind"] == "enum":
                token_match = IDENT_RE.match(line.strip())
                if token_match:
                    name = token_match.group(0)
                    members.append(dict(path=typ["path"], line=i + 1, type=typ["fullName"], name=name,
                                        kind="enum_value", visibility="public", declaration=line.strip().rstrip(","), parameterSlots=[]))
                continue
            vis = VIS_RE.match(line)
            if vis:
                if vis.group(1) not in ("public", "protected"):
                    continue
            elif typ["kind"] != "interface":
                probe = signature(original, sanitized, i)
                if not re.search(r"\b[A-Za-z_]\w*(?:<[^()]*>)?\.[A-Za-z_]\w*(?:<[^()]*>)?\s*(?:\(|$)", probe.split("=>", 1)[0]):
                    continue
            if line.lstrip().startswith(("where ", "get;", "set;", "init;", "add;", "remove;")):
                continue
            sig = signature(original, sanitized, i)
            if not sig or sig.startswith("#"):
                continue
            explicit = vis is None and typ["kind"] != "interface"
            if " operator " in " " + sig + " ":
                name = "operator " + (re.search(r"\boperator\s+([^\s(]+)", sig).group(1) if re.search(r"\boperator\s+([^\s(]+)", sig) else "?")
                kind = "operator"
            elif "this[" in sig:
                name, kind = "this[]", "indexer"
            elif "(" in sig:
                before = sig.split("(", 1)[0]
                name = method_name(before)
                kind = "constructor" if name == typ["name"] else ("explicit_interface_method" if explicit else "method")
            else:
                names = IDENT_RE.findall(sig)
                name = names[-1] if names else "<unknown>"
                kind = "event" if re.search(r"\bevent\b", sig) else ("explicit_interface_property" if explicit else "property_or_field")
            members.append(dict(path=typ["path"], line=i + 1, type=typ["fullName"], name=name,
                                kind=kind, visibility=vis.group(1) if vis else ("explicit-interface" if explicit else "interface-implicit-public"),
                                declaration=sig, parameterSlots=slots_from(sig, "member")))
    for typ in types:
        for key in ("open_index", "close_index", "open_depth"):
            del typ[key]
    return [item for item in types if item["external"]], members


TEST_RE = re.compile(r"\b(?:public|internal)\s+(?:(?:static|async|override|virtual)\s+)*[\w<>,?\[\].]+\s+([A-Za-z_]\w*)\s*\(")
REQ_RE = re.compile(r'RequirementCoverage\("([^"]+)",\s*"([^"]+)"\)')


def parse_tests(path, text):
    original = text.splitlines()
    sanitized = clean(text).splitlines()
    dep = depths(sanitized)
    class_match = re.search(r"\bclass\s+(\w+)", text)
    class_name = class_match.group(1) if class_match else path.stem
    namespace_match = re.search(r"^\s*namespace\s+([\w.]+)\s*;", text, re.M)
    namespace = namespace_match.group(1) if namespace_match else "<unknown>"
    tests = []
    for i, line in enumerate(sanitized):
        match = TEST_RE.search(line)
        if not match:
            continue
        attributes = []
        for preceding in range(i - 1, max(-1, i - 24), -1):
            stripped = original[preceding].strip()
            if not stripped:
                continue
            if stripped.startswith("["):
                attributes.append(stripped)
                continue
            break
        prelude = "\n".join(reversed(attributes))
        if not re.search(r"\[(?:Fact|Theory|Test)\b", prelude):
            continue
        coverage = REQ_RE.findall(prelude)
        opening = next((j for j in range(i, min(i + 8, len(sanitized))) if "{" in sanitized[j]), None)
        if opening is None:
            continue
        closing = next((j for j in range(opening + 1, len(sanitized)) if dep[j] == dep[opening] + 1 and "}" in sanitized[j]), None)
        if closing is None:
            continue
        tests.append(dict(path=str(path.relative_to(ROOT)), line=i + 1,
                          testType=namespace + "." + class_name, testMethod=match.group(1),
                          requirements=[dict(requirementId=a, variantKey=b) for a, b in coverage],
                          body="\n".join(original[opening:closing + 1])))
    return tests


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--section", choices=["summary", "manifest", "testmanifest", "types", "members", "slots", "tests", "requirements", "all"], default="all")
    parser.add_argument("--part", type=int, default=0)
    parser.add_argument("--part-size", type=int, default=100)
    args = parser.parse_args()
    source_paths = paths(SCOPE, ["*.cs", "*.csproj", "packages.lock.json"])
    test_paths = []
    for directory in TEST_DIRS:
        test_paths.extend(paths(directory, ["*.cs", "*.csproj", "packages.lock.json", "*.json"]))
    manifest = []
    types, members = [], []
    for path in source_paths:
        data = path.read_bytes()
        manifest.append(dict(path=str(path.relative_to(ROOT)), sha256=hashlib.sha256(data).hexdigest(), bytes=len(data)))
        if path.suffix == ".cs":
            t, m = parse_source(path, data.decode("utf-8-sig"))
            types.extend(t)
            members.extend(m)
    tests = []
    test_manifest = []
    for path in test_paths:
        data = path.read_bytes()
        test_manifest.append(dict(path=str(path.relative_to(ROOT)), sha256=hashlib.sha256(data).hexdigest(), bytes=len(data)))
        if path.suffix == ".cs":
            tests.extend(parse_tests(path, data.decode("utf-8-sig")))
    requirement_rows = []
    for directory in TEST_DIRS:
        for path in paths(directory / "Requirements", ["*.json"]):
            requirement_rows.extend(json.loads(path.read_text(encoding="utf-8-sig")))
    test_by_key = {(item["testType"], item["testMethod"]): item for item in tests}
    for number, item in enumerate(tests, 1):
        item["testId"] = number
    for row in requirement_rows:
        row["testFound"] = (row["testType"], row["testMethod"]) in test_by_key
        row["testId"] = test_by_key.get((row["testType"], row["testMethod"]), {}).get("testId")
        row["attributeFound"] = any(r["requirementId"] == row["requirementId"] and r["variantKey"] == row["variantKey"]
                                    for r in test_by_key.get((row["testType"], row["testMethod"]), {}).get("requirements", []))
    for number, member in enumerate(members, 1):
        member["memberId"] = number
        token = member["name"]
        if member["kind"] == "operator":
            pattern = re.compile(r"\b" + re.escape(member["type"].split(".")[-1]) + r"\b")
        elif member["kind"] == "constructor":
            pattern = re.compile(r"\bnew\s+" + re.escape(token) + r"\b")
        elif member["kind"] == "indexer":
            pattern = re.compile(r"\[[^\]]+\]")
        else:
            pattern = re.compile(r"\b" + re.escape(token) + r"\b")
        matches = [item for item in tests if pattern.search(item["body"])]
        member["lexicalTestCandidateIds"] = [item["testId"] for item in matches]
        member["lexicalTestCandidates"] = [item["testType"] + "." + item["testMethod"] for item in matches]
        member["candidateRequirements"] = sorted({r["requirementId"] + "/" + r["variantKey"]
                                                   for item in matches for r in item["requirements"]})
    for number, typ in enumerate(types, 1):
        typ["typeId"] = number
        pattern = re.compile(r"\b" + re.escape(typ["name"]) + r"\b")
        typ["lexicalTestCandidateIds"] = [item["testId"] for item in tests if pattern.search(item["body"])]
    slots = []
    for typ in types:
        for slot in typ["parameterSlots"]:
            slots.append(dict(slotId=len(slots) + 1, ownerKind="type", ownerId=typ["typeId"],
                              path=typ["path"], line=typ["line"], ordinal=slot["ordinal"],
                              declaration=slot["declaration"],
                              inheritedLexicalTestCandidateIds=typ["lexicalTestCandidateIds"],
                              parameterSpecificProof="unverified"))
    for member in members:
        for slot in member["parameterSlots"]:
            slots.append(dict(slotId=len(slots) + 1, ownerKind="member", ownerId=member["memberId"],
                              path=member["path"], line=member["line"], ordinal=slot["ordinal"],
                              declaration=slot["declaration"],
                              inheritedLexicalTestCandidateIds=member["lexicalTestCandidateIds"],
                              parameterSpecificProof="unverified"))
    for item in tests:
        item["lexicalTypeCandidateIds"] = [typ["typeId"] for typ in types if item["testId"] in typ["lexicalTestCandidateIds"]]
        item["lexicalMemberCandidateIds"] = [member["memberId"] for member in members if item["testId"] in member["lexicalTestCandidateIds"]]
        item["inheritedLexicalSlotCandidateIds"] = [slot["slotId"] for slot in slots if item["testId"] in slot["inheritedLexicalTestCandidateIds"]]
        del item["body"]
    result = dict(method="static lexical scan; candidate test matches are not execution or semantic proof",
                  scope=str(SCOPE.relative_to(ROOT)), manifest=manifest, testmanifest=test_manifest,
                  types=types, members=members, slots=slots,
                  tests=tests, requirementProjection=requirement_rows)
    summary = dict(sourceFiles=len(manifest), sourceCSharpFiles=sum(row["path"].endswith(".cs") for row in manifest),
                   testInputFiles=len(test_manifest),
                   sourceBytes=sum(row["bytes"] for row in manifest), publicTypes=len(types),
                   declaredAccessibleMembers=len(members), parameterSlots=len(slots),
                   testMethods=len(tests), requirementProjectionRows=len(requirement_rows),
                   projectionMethodsFound=sum(row["testFound"] for row in requirement_rows),
                   projectionAttributesFound=sum(row["attributeFound"] for row in requirement_rows),
                   membersWithLexicalTestCandidates=sum(bool(row["lexicalTestCandidates"]) for row in members),
                   membersWithoutLexicalTestCandidates=sum(not row["lexicalTestCandidates"] for row in members),
                   memberKindCounts=dict(Counter(row["kind"] for row in members)))
    if args.section == "summary":
        print(json.dumps(summary, indent=2))
    elif args.section == "all":
        print(json.dumps(result, indent=2, ensure_ascii=False))
    else:
        key = {"requirements": "requirementProjection"}.get(args.section, args.section)
        rows = result[key]
        rows = rows[args.part * args.part_size:(args.part + 1) * args.part_size]
        for row in rows:
            if args.section == "members":
                row = {k: v for k, v in row.items() if k not in ("lexicalTestCandidates", "candidateRequirements")}
            print(json.dumps(row, separators=(",", ":"), ensure_ascii=False))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Compare every committed file in ServiceBus main with upstream MassTransit."""

from __future__ import annotations

import argparse
import csv
import os
import subprocess
import sys
import tempfile
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path

BASELINE_TAG = "MassTransit/v8.5.10"
UPSTREAM_COMMIT = "62ab339afa3bac2e9b3fe1769d0d35d7e44778e9"
ROOT = Path(__file__).resolve().parents[1]
GROUPS = ("src", "tests", "samples", "benchmarks", "other")
SUMMARY_NAME = "REPOSITORY_DIFF.md"
DETAILS_NAME = "REPOSITORY_DIFF_DETAILS.md"
STATUSES = ("moved", "modified", "added", "removed", "unchanged")


def in_test_tree(path: str) -> bool:
    return path.startswith("tests/")


def git(*args: str, input_bytes: bytes | None = None,
        env: dict[str, str] | None = None) -> bytes:
    return subprocess.run(
        ["git", "-C", str(ROOT), *args], input=input_bytes, env=env,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True,
    ).stdout


def decoded(value: bytes) -> str:
    return value.decode("utf-8", errors="surrogateescape")


def diff_args(baseline: str, main: str, *options: str) -> list[str]:
    # This identity change exceeds Git's default exhaustive-rename limit.
    return ["-c", "diff.renameLimit=10000", "diff", "--no-ext-diff", "--no-textconv",
            "--find-renames=50%", *options, baseline, main, "--", "."]


def endpoints() -> tuple[str, str, str]:
    baseline = decoded(git("rev-parse", "--verify", f"refs/tags/{BASELINE_TAG}^{{commit}}")).strip()
    if baseline != UPSTREAM_COMMIT:
        raise ValueError(f"{BASELINE_TAG} does not point to the pinned MassTransit commit")
    for main_ref in ("refs/heads/main", "refs/remotes/origin/main"):
        try:
            main = decoded(git("rev-parse", "--verify", f"{main_ref}^{{commit}}")).strip()
            return baseline, main_ref, main
        except subprocess.CalledProcessError:
            continue
    raise ValueError("No local main or origin/main reference is available")


@dataclass(frozen=True)
class Blob:
    mode: str
    oid: str


@dataclass
class FileChange:
    old_path: str | None
    new_path: str | None
    match: str
    group: str = ""
    project: str = ""
    old_project: str = ""
    added_lines: int | None = 0
    deleted_lines: int | None = 0
    mode_changed: bool = False
    blob_changed: bool = False

    @property
    def status(self) -> str:
        if self.old_path is None:
            return "added"
        if self.new_path is None:
            return "removed"
        if self.old_path != self.new_path:
            return "moved"
        return "modified" if self.blob_changed or self.mode_changed else "unchanged"


def tree_files(ref: str) -> dict[str, Blob]:
    files: dict[str, Blob] = {}
    for record in git("ls-tree", "-r", "-z", ref).split(b"\0"):
        if not record:
            continue
        metadata, path = record.split(b"\t", 1)
        mode, kind, oid = metadata.split()
        if kind != b"blob":
            raise ValueError(f"Cannot count lines in non-blob tree entry: {decoded(path)}")
        files[decoded(path)] = Blob(decoded(mode), decoded(oid))
    return files


def project_successors(old_files: dict[str, Blob], new_files: dict[str, Blob]) -> dict[str, str]:
    """Match project identities only when the known naming change is unambiguous."""
    suffixes = (".csproj", ".fsproj", ".vbproj")
    old_projects = (path for path in old_files if path.endswith(suffixes))
    new_projects = (path for path in new_files if path.endswith(suffixes))
    by_current_name: dict[str, list[str]] = defaultdict(list)
    by_old_name: dict[str, list[str]] = defaultdict(list)
    for path in new_projects:
        by_current_name[Path(path).stem].append(path)
    for path in old_projects:
        name = Path(path).stem.replace("MassTransit", "ViciOne.ServiceBus", 1)
        for old, new in (
            ("Azure.ServiceBus.Core", "AzureServiceBus"),
            ("EventHubIntegration", "EventHubs"),
            ("ActiveMqTransport", "ActiveMq"),
            ("AmazonSqsTransport", "AmazonSqs"),
            ("RabbitMqTransport", "RabbitMq"),
            ("Integration", ""),
        ):
            name = name.replace(old, new)
        by_old_name[name].append(path)
    successors = {}
    for name, old_paths in by_old_name.items():
        new_paths = by_current_name[name]
        if len(old_paths) == len(new_paths) == 1 and old_paths[0] != new_paths[0]:
            successors[old_paths[0]] = new_paths[0]
    return successors


def git_renames(baseline: str, main: str) -> list[tuple[str, str]]:
    fields = git(*diff_args(baseline, main, "--name-status", "-z")).split(b"\0")
    if fields[-1] == b"":
        fields.pop()
    pairs: list[tuple[str, str]] = []
    index = 0
    while index < len(fields):
        status = fields[index][:1]
        width = 3 if status == b"R" else 2
        if status not in {b"A", b"M", b"D", b"T", b"R"} or index + width > len(fields):
            raise ValueError("Unexpected or incomplete Git name-status record")
        if status == b"R":
            pairs.append((decoded(fields[index + 1]), decoded(fields[index + 2])))
        index += width
    return pairs


def match_files(old_files: dict[str, Blob], new_files: dict[str, Blob],
                renames: list[tuple[str, str]],
                successors: dict[str, str]) -> tuple[list[FileChange], Counter[str]]:
    old_left, new_left = set(old_files), set(new_files)
    changes: list[FileChange] = []
    methods: Counter[str] = Counter()

    def pair(old: str, new: str, method: str) -> None:
        if in_test_tree(old) or in_test_tree(new):
            raise ValueError(f"Test trees must remain separate snapshots: {old} -> {new}")
        if old not in old_left or new not in new_left:
            raise ValueError(f"Conflicting file match: {old} -> {new}")
        old_left.remove(old)
        new_left.remove(new)
        changes.append(FileChange(old, new, method,
                                  mode_changed=old_files[old].mode != new_files[new].mode,
                                  blob_changed=old_files[old].oid != new_files[new].oid))
        methods[method] += 1

    for old, new in renames:
        if not in_test_tree(old) and not in_test_tree(new):
            pair(old, new, "Git similarity")
    for path in sorted(old_left & new_left):
        if not in_test_tree(path):
            pair(path, path, "same path")
    for old in sorted(old_left):
        new = old.replace("MassTransit", "ViciOne.ServiceBus")
        if old != new and new in new_left and not in_test_tree(old) and not in_test_tree(new):
            pair(old, new, "product path rename")

    # Project identity follows the package naming pattern even when the folder
    # moves into a new area. This does not turn unmatched old files into moves.
    for old_project, new_project in sorted(successors.items()):
        if in_test_tree(old_project) or in_test_tree(new_project):
            continue
        if old_project in old_left and new_project in new_left:
            pair(old_project, new_project, "project file rename")
        old_dir, new_dir = old_project.rsplit("/", 1)[0], new_project.rsplit("/", 1)[0]
        for old in sorted(path for path in old_left if path.startswith(old_dir + "/")):
            new = new_dir + old[len(old_dir):]
            if new in new_left:
                pair(old, new, "successor relative path")

        old_names: dict[str, list[str]] = defaultdict(list)
        new_names: dict[str, list[str]] = defaultdict(list)
        for old in old_left:
            if old.startswith(old_dir + "/") and old.endswith(".cs"):
                old_names[Path(old).name].append(old)
        for new in new_left:
            if new.startswith(new_dir + "/") and new.endswith(".cs"):
                new_names[Path(new).name].append(new)
        for name in sorted(old_names):
            if len(old_names[name]) == len(new_names[name]) == 1:
                pair(old_names[name][0], new_names[name][0], "successor C# filename")

    old_by_name: dict[str, list[str]] = defaultdict(list)
    new_by_name: dict[str, list[str]] = defaultdict(list)
    for path in old_left:
        if path.endswith(".cs") and not in_test_tree(path):
            old_by_name[Path(path).name].append(path)
    for path in new_left:
        if path.endswith(".cs") and not in_test_tree(path):
            new_by_name[Path(path).name].append(path)
    for name in sorted(old_by_name):
        olds, news = old_by_name[name], new_by_name[name]
        if len(olds) != 1 or len(news) != 1:
            continue
        old, new = olds[0], news[0]
        old_root, new_root = old.split("/", 1)[0], new.split("/", 1)[0]
        if old_root == new_root:
            pair(old, new, "unique C# filename")

    changes.extend(FileChange(None, path, "") for path in sorted(new_left))
    changes.extend(FileChange(path, None, "") for path in sorted(old_left))
    return changes, methods


def project_for(path: str, projects: list[tuple[str, str]]) -> str:
    for directory, project in projects:
        if path.startswith(directory + "/"):
            return project
    root = path.split("/", 1)[0]
    return f"{root}/(shared files)" if root in GROUPS[:-1] else "(other repository files)"


def assign_projects(changes: list[FileChange], old_files: dict[str, Blob],
                    new_files: dict[str, Blob], successors: dict[str, str]) -> None:
    suffixes = (".csproj", ".fsproj", ".vbproj")
    old_projects = sorted(((p.rsplit("/", 1)[0], p) for p in old_files if p.endswith(suffixes)),
                          key=lambda item: len(item[0]), reverse=True)
    new_projects = sorted(((p.rsplit("/", 1)[0], p) for p in new_files if p.endswith(suffixes)),
                          key=lambda item: len(item[0]), reverse=True)
    for change in changes:
        if change.old_path:
            change.old_project = project_for(change.old_path, old_projects)
        path = change.new_path or change.old_path
        assert path is not None
        if change.new_path:
            change.project = project_for(change.new_path, new_projects)
        else:
            successor = successors.get(change.old_project)
            old_group = change.old_path.split("/", 1)[0]
            change.project = (successor if successor and successor.startswith(old_group + "/")
                              else change.old_project)
        change.group = path.split("/", 1)[0]
        if change.group not in GROUPS:
            change.group = "other"


def synthetic_tree(changes: list[FileChange], files: dict[str, Blob], side: str,
                   names: list[str], env: dict[str, str]) -> str:
    records = []
    for change, name in zip(changes, names):
        path = change.old_path if side == "old" else change.new_path
        if path is not None:
            blob = files[path]
            records.append(f"{blob.mode} blob {blob.oid}\t{name}\0".encode())
    return decoded(git("mktree", "-z", input_bytes=b"".join(records), env=env)).strip()


def count_lines(changes: list[FileChange], old_files: dict[str, Blob],
                new_files: dict[str, Blob]) -> None:
    # Align paired blobs under one temporary path. Git then counts every file
    # in one diff, including heavily rewritten moves; no repo objects are written.
    names = [f"f{index:06d}{Path(c.new_path or c.old_path or '').suffix}"
             for index, c in enumerate(changes)]
    by_name = dict(zip(names, changes))
    objects = decoded(git("rev-parse", "--path-format=absolute", "--git-path", "objects")).strip()
    with tempfile.TemporaryDirectory(prefix="servicebus-diff-") as directory:
        object_dir = Path(directory) / "objects"
        object_dir.mkdir()
        env = os.environ.copy()
        env["GIT_OBJECT_DIRECTORY"] = str(object_dir)
        alternatives = [objects, env.get("GIT_ALTERNATE_OBJECT_DIRECTORIES", "")]
        env["GIT_ALTERNATE_OBJECT_DIRECTORIES"] = os.pathsep.join(x for x in alternatives if x)
        old_tree = synthetic_tree(changes, old_files, "old", names, env)
        new_tree = synthetic_tree(changes, new_files, "new", names, env)
        output = git("diff", "--no-ext-diff", "--no-textconv", "--no-renames",
                     "--numstat", "-z", old_tree, new_tree, env=env)
    for record in output.split(b"\0"):
        if not record:
            continue
        added, deleted, name = record.split(b"\t", 2)
        change = by_name[decoded(name)]
        change.added_lines = int(added) if added != b"-" else None
        change.deleted_lines = int(deleted) if deleted != b"-" else None


def collect() -> tuple[str, str, str, list[FileChange], Counter[str], dict[str, str]]:
    baseline, main_ref, main = endpoints()
    old_files, new_files = tree_files(baseline), tree_files(main)
    successors = project_successors(old_files, new_files)
    changes, methods = match_files(old_files, new_files, git_renames(baseline, main), successors)
    assign_projects(changes, old_files, new_files, successors)
    count_lines(changes, old_files, new_files)
    return baseline, main_ref, main, changes, methods, successors


def grouped_changes(changes: list[FileChange]) -> dict[str, dict[str, list[FileChange]]]:
    grouped: dict[str, dict[str, list[FileChange]]] = defaultdict(lambda: defaultdict(list))
    for change in changes:
        grouped[change.group][change.project].append(change)
    return grouped


def file_totals(changes: list[FileChange]) -> Counter[str]:
    totals = Counter(change.status for change in changes)
    totals["plus"] = sum(change.added_lines or 0 for change in changes)
    totals["minus"] = sum(change.deleted_lines or 0 for change in changes)
    totals["binary"] = sum(change.added_lines is None for change in changes)
    return totals


def summary(baseline: str, main_ref: str, main: str,
            changes: list[FileChange], methods: Counter[str],
            successors: dict[str, str]) -> str:
    predecessors = {current: old for old, current in successors.items()}
    lines = [
        "# Current repository change overview", "",
        f"Original source: `{BASELINE_TAG}` (`{baseline}`).",
        f"Current main: `{main_ref}` (`{main}`).", "",
        "Every committed file is matched by its path, Git's 50% similarity rule, a known",
        "product path change, a recognized successor project, or a unique C# filename.",
        "Exception: all paths under `tests/` are treated as a complete replacement: every",
        "original test-tree file is removed and every current test-tree file is added.",
        "A matching test project name describes project lineage, not a surviving test file.",
        "Path and filename matches infer continuity; they do not prove identical implementation.",
        "A former project is linked only when its normalized name identifies one current project.",
        "Unmatched old files are grouped under that successor but remain removals. Ambiguous files",
        "remain additions and removals. Changed lines use Git's diff engine on the paired blobs.",
        "Binary changes have no line counts and appear in the Binary column.",
        "Moved files belong to their current project. Removed files belong to a recognized",
        "successor project in the same top-level tree, or to their old project otherwise.",
        "Paths outside the four product trees appear under Other. The worktree and index are excluded.",
        f"Project successors: {len(successors)}. File matches: {methods['Git similarity']} Git, "
        f"{methods['product path rename']} product path, {methods['project file rename']} project file,",
        f"{methods['successor relative path']} successor-relative path, "
        f"{methods['successor C# filename']} successor-local C# name, "
        f"{methods['unique C# filename']} globally unique C# name.",
        f"[Open the file-level report]({DETAILS_NAME}) for every old/new path and its line diff.",
        "Use `--files` for TSV or `--patch` for the complete Git patch; the test portion",
        "of that patch also uses delete/add records.", "",
    ]
    grouped = grouped_changes(changes)
    grand = Counter()
    for group in GROUPS:
        lines.extend((f"## {group}", "",
                      "| Project | Former project | Added files | Modified files | Removed files | Moved files | "
                      "Unchanged files | + lines | - lines | Binary |",
                      "|---|---|---:|---:|---:|---:|---:|---:|---:|---:|"))
        group_total = Counter()
        for project, files in sorted(grouped[group].items()):
            counts = file_totals(files)
            group_total.update(counts)
            lines.append(f"| {markdown_cell(project)} | "
                         f"{markdown_cell(predecessors.get(project))} | "
                         f"{counts['added']} | {counts['modified']} | "
                         f"{counts['removed']} | {counts['moved']} | {counts['unchanged']} | "
                         f"{counts['plus']} | {counts['minus']} | {counts['binary']} |")
        grand.update(group_total)
        lines.append(f"| **{group} total** | — | **{group_total['added']}** | "
                     f"**{group_total['modified']}** | **{group_total['removed']}** | "
                     f"**{group_total['moved']}** | **{group_total['unchanged']}** | "
                     f"**{group_total['plus']}** | **{group_total['minus']}** | "
                     f"**{group_total['binary']}** |")
        lines.append("")
    lines.append(f"**Repository total:** {grand['added']} added, {grand['modified']} modified, "
                 f"{grand['removed']} removed, {grand['moved']} moved, "
                 f"{grand['unchanged']} unchanged files; +{grand['plus']} / -{grand['minus']} lines; "
                 f"{grand['binary']} binary changes.")
    lines.append("")
    return "\n".join(lines)


def markdown_cell(value: str | None) -> str:
    if not value:
        return "—"
    return "`" + value.replace("|", "\\|").replace("`", "\\`") + "`"


def details(baseline: str, main_ref: str, main: str, changes: list[FileChange],
            successors: dict[str, str]) -> str:
    predecessors = {current: old for old, current in successors.items()}
    lines = [
        "# Repository change details", "",
        f"Original source: `{BASELINE_TAG}` (`{baseline}`).",
        f"Current main: `{main_ref}` (`{main}`).", "",
        f"[Open the project summary]({SUMMARY_NAME}).", "",
        "Each file appears once. Moved files are listed under their current project. Removed",
        "files are grouped under a recognized successor in the same top-level tree, or their old",
        "project otherwise. All original `tests/` paths are Removed and all current `tests/`",
        "paths are Added, even where project names or filenames are similar. A former test",
        "project therefore describes package lineage rather than file continuity.",
        "Removed status is preserved. An old project is shown when it differs from",
        "the current one. Git calculates added and removed lines from each matched blob pair.",
        "A binary change has no line count. Matches based on product paths or unique C# names",
        "infer file continuity and can be checked using the displayed old and current paths.", "",
        "Groups: " + " · ".join(f"[{group}](#{group})" for group in GROUPS) + ".", "",
    ]
    grouped = grouped_changes(changes)
    for group in GROUPS:
        group_files = [change for files in grouped[group].values() for change in files]
        group_total = file_totals(group_files)
        lines.extend((f"## {group}", "",
                      f"{len(group_files)} files; +{group_total['plus']} / -{group_total['minus']} "
                      f"lines; {group_total['binary']} binary changes.", ""))
        for project, files in sorted(grouped[group].items()):
            totals = file_totals(files)
            lines.extend((f"### {markdown_cell(project)}", ""))
            if project in predecessors:
                lines.extend((f"Former project: {markdown_cell(predecessors[project])}.", ""))
            lines.extend((f"{len(files)} files; +{totals['plus']} / -{totals['minus']} lines; "
                          f"{totals['binary']} binary changes.", ""))
            for status in STATUSES:
                members = sorted((change for change in files if change.status == status),
                                 key=lambda change: change.new_path or change.old_path or "")
                if not members:
                    continue
                lines.extend((f"#### {status.title()} ({len(members)})", "",
                              "| Old path | Current path | Old project if different | + lines | - lines | Match |",
                              "|---|---|---|---:|---:|---|"))
                for change in members:
                    old_project = (change.old_project if change.old_project != change.project
                                   else None)
                    plus = change.added_lines if change.added_lines is not None else "binary"
                    minus = change.deleted_lines if change.deleted_lines is not None else "binary"
                    lines.append(f"| {markdown_cell(change.old_path)} | "
                                 f"{markdown_cell(change.new_path)} | "
                                 f"{markdown_cell(old_project)} | {plus} | {minus} | "
                                 f"{markdown_cell(change.match)} |")
                lines.append("")
    return "\n".join(lines)


def write_reports(output_dir: Path, summary_text: str, details_text: str) -> tuple[Path, Path]:
    output_dir.mkdir(parents=True, exist_ok=True)
    paths = (output_dir / SUMMARY_NAME, output_dir / DETAILS_NAME)
    # Both complete reports are prepared before either published file changes.
    with tempfile.TemporaryDirectory(prefix="servicebus-reports-", dir=output_dir) as directory:
        temporary = (Path(directory) / SUMMARY_NAME, Path(directory) / DETAILS_NAME)
        for path, content in zip(temporary, (summary_text, details_text)):
            path.write_text(content, encoding="utf-8")
        for source, destination in zip(temporary, paths):
            os.replace(source, destination)
    return paths


def file_rows(changes: list[FileChange]) -> None:
    writer = csv.writer(sys.stdout, dialect="excel-tab", lineterminator="\n")
    writer.writerow(("group", "project", "status", "old_project", "old_path", "new_path",
                     "added_lines", "deleted_lines", "match"))
    for change in sorted(changes, key=lambda c: (GROUPS.index(c.group), c.project,
                                                  c.new_path or c.old_path or "")):
        writer.writerow((change.group, change.project, change.status, change.old_project,
                         change.old_path or "", change.new_path or "",
                         change.added_lines if change.added_lines is not None else "binary",
                         change.deleted_lines if change.deleted_lines is not None else "binary",
                         change.match))


def patch() -> None:
    baseline, _, main = endpoints()
    # Keep the patch consistent with the report's complete test-tree replacement.
    subprocess.run(
        ["git", "-C", str(ROOT), "-c", "diff.renameLimit=10000", "diff",
         "--no-ext-diff", "--no-textconv", "--find-renames=50%", "--binary",
         baseline, main, "--", ".", ":(exclude)tests"], check=True,
    )
    subprocess.run(
        ["git", "-C", str(ROOT), "diff", "--no-ext-diff", "--no-textconv",
         "--no-renames", "--binary", baseline, main, "--", "tests"], check=True,
    )


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, default=Path("artifacts/policy"),
                        help="report directory, relative to the repository root by default")
    choice = parser.add_mutually_exclusive_group()
    choice.add_argument("--files", action="store_true", help="print a tab-separated file inventory")
    choice.add_argument("--patch", action="store_true", help="print the complete committed Git diff")
    args = parser.parse_args()
    try:
        if args.patch:
            patch()
        else:
            baseline, main_ref, main, changes, methods, successors = collect()
            if args.files:
                file_rows(changes)
            else:
                output_dir = args.output_dir if args.output_dir.is_absolute() else ROOT / args.output_dir
                paths = write_reports(output_dir,
                                      summary(baseline, main_ref, main, changes, methods, successors),
                                      details(baseline, main_ref, main, changes, successors))
                for path in paths:
                    print(path)
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Cannot compare repository changes: {error}", file=sys.stderr)
        raise SystemExit(1) from error

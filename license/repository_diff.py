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
                renames: list[tuple[str, str]]) -> tuple[list[FileChange], Counter[str]]:
    old_left, new_left = set(old_files), set(new_files)
    changes: list[FileChange] = []
    methods: Counter[str] = Counter()

    def pair(old: str, new: str, method: str) -> None:
        if old not in old_left or new not in new_left:
            raise ValueError(f"Conflicting file match: {old} -> {new}")
        old_left.remove(old)
        new_left.remove(new)
        changes.append(FileChange(old, new, method,
                                  mode_changed=old_files[old].mode != new_files[new].mode,
                                  blob_changed=old_files[old].oid != new_files[new].oid))
        methods[method] += 1

    for old, new in renames:
        pair(old, new, "Git similarity")
    for path in sorted(old_left & new_left):
        pair(path, path, "same path")
    for old in sorted(old_left):
        new = old.replace("MassTransit", "ViciOne.ServiceBus")
        if old != new and new in new_left:
            pair(old, new, "product path rename")

    old_by_name: dict[str, list[str]] = defaultdict(list)
    new_by_name: dict[str, list[str]] = defaultdict(list)
    for path in old_left:
        if path.endswith(".cs"):
            old_by_name[Path(path).name].append(path)
    for path in new_left:
        if path.endswith(".cs"):
            new_by_name[Path(path).name].append(path)
    for name in sorted(old_by_name):
        olds, news = old_by_name[name], new_by_name[name]
        if len(olds) != 1 or len(news) != 1:
            continue
        old, new = olds[0], news[0]
        old_root, new_root = old.split("/", 1)[0], new.split("/", 1)[0]
        benchmark_move = old.startswith("tests/MassTransit.Benchmark") and new_root == "benchmarks"
        if old_root == new_root or benchmark_move:
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
                    new_files: dict[str, Blob]) -> None:
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
        change.group = path.split("/", 1)[0]
        if change.group not in GROUPS:
            change.group = "other"
        change.project = (project_for(change.new_path, new_projects) if change.new_path
                          else change.old_project)


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


def collect() -> tuple[str, str, str, list[FileChange], Counter[str]]:
    baseline, main_ref, main = endpoints()
    old_files, new_files = tree_files(baseline), tree_files(main)
    changes, methods = match_files(old_files, new_files, git_renames(baseline, main))
    assign_projects(changes, old_files, new_files)
    count_lines(changes, old_files, new_files)
    return baseline, main_ref, main, changes, methods


def summary(baseline: str, main_ref: str, main: str,
            changes: list[FileChange], methods: Counter[str]) -> str:
    lines = [
        "# Current repository change overview", "",
        f"Original source: `{BASELINE_TAG}` (`{baseline}`).",
        f"Current main: `{main_ref}` (`{main}`).", "",
        "Every committed file is matched by its path, Git's 50% similarity rule, the known",
        "product path rename, or a unique C# filename in the same top-level tree. The last two",
        "rules infer continuity; they do not prove identical implementation. Ambiguous files",
        "remain additions and removals. Changed lines use Git's diff engine on the paired blobs.",
        "Binary changes have no line counts and appear in the Binary column.",
        "Moved files belong to their current project; removed files belong to their old project.",
        "Paths outside the four product trees appear under Other. The worktree and index are excluded.",
        f"Matches: {methods['Git similarity']} Git, {methods['product path rename']} product path, "
        f"{methods['unique C# filename']} unique C# filename.",
        "Run `python3 license/repository_diff.py --files` for every old/new file path and its",
        "added/deleted lines; use `--patch` for the complete Git patch.", "",
    ]
    grouped: dict[str, dict[str, list[FileChange]]] = defaultdict(lambda: defaultdict(list))
    for change in changes:
        grouped[change.group][change.project].append(change)
    grand = Counter()
    for group in GROUPS:
        lines.extend((f"## {group}", "",
                      "| Project | Added files | Modified files | Removed files | Moved files | "
                      "Unchanged files | + lines | - lines | Binary |",
                      "|---|---:|---:|---:|---:|---:|---:|---:|---:|"))
        group_total = Counter()
        for project, files in sorted(grouped[group].items()):
            counts = Counter(c.status for c in files)
            counts["plus"] = sum(c.added_lines or 0 for c in files)
            counts["minus"] = sum(c.deleted_lines or 0 for c in files)
            counts["binary"] = sum(c.added_lines is None for c in files)
            group_total.update(counts)
            lines.append(f"| `{project}` | {counts['added']} | {counts['modified']} | "
                         f"{counts['removed']} | {counts['moved']} | {counts['unchanged']} | "
                         f"{counts['plus']} | {counts['minus']} | {counts['binary']} |")
        grand.update(group_total)
        lines.append(f"| **{group} total** | **{group_total['added']}** | "
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
    subprocess.run(["git", "-C", str(ROOT), *diff_args(baseline, main, "--binary")], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    choice = parser.add_mutually_exclusive_group()
    choice.add_argument("--files", action="store_true", help="print a tab-separated file inventory")
    choice.add_argument("--patch", action="store_true", help="print the complete committed Git diff")
    args = parser.parse_args()
    try:
        if args.patch:
            patch()
        else:
            baseline, main_ref, main, changes, methods = collect()
            if args.files:
                file_rows(changes)
            else:
                sys.stdout.write(summary(baseline, main_ref, main, changes, methods))
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Cannot compare repository changes: {error}", file=sys.stderr)
        raise SystemExit(1) from error

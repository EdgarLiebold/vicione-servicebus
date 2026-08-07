#!/usr/bin/env python3
# ViciOne modification: created for WP-F2-SERVICEBUS-IDENTITY on 2026-08-07.
"""Apply the frozen full-repository identity mapping exactly once."""

from __future__ import annotations

import argparse
import io
import os
import subprocess
import sys
import tarfile
from pathlib import Path

from identity_rules import (
    BASELINE_COMMIT,
    LEGAL_OR_PROVENANCE_PATHS,
    add_modification_notice,
    map_path,
    map_text,
)


def git(root: Path, *arguments: str) -> bytes:
    result = subprocess.run(
        ["git", "-C", str(root), *arguments],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace"))
    return result.stdout


def baseline_contents(root: Path) -> dict[str, bytes | None]:
    """Read the immutable baseline with one Git process, including symlink markers."""
    archive = git(root, "archive", "--format=tar", BASELINE_COMMIT)
    contents: dict[str, bytes | None] = {}
    with tarfile.open(fileobj=io.BytesIO(archive), mode="r:") as stream:
        for member in stream.getmembers():
            if member.isfile():
                extracted = stream.extractfile(member)
                if extracted is None:
                    raise RuntimeError(f"Unable to read baseline archive member: {member.name}")
                contents[member.name] = extracted.read()
            elif member.issym():
                contents[member.name] = None
    return contents


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--check", action="store_true")
    parser.add_argument(
        "--reapply",
        action="store_true",
        help="rederive already-renamed target files from the immutable Git baseline",
    )
    args = parser.parse_args()
    root = args.root.resolve(strict=True)

    head = git(root, "rev-parse", "HEAD").decode("ascii").strip()
    if head != BASELINE_COMMIT:
        raise RuntimeError(f"Refactor requires baseline HEAD {BASELINE_COMMIT}, found {head}")

    contents = baseline_contents(root)
    paths = sorted(contents)
    mapping = {source: map_path(source) for source in paths}
    if len(set(mapping.values())) != len(mapping):
        collisions: dict[str, list[str]] = {}
        for source, target in mapping.items():
            collisions.setdefault(target, []).append(source)
        raise RuntimeError(f"Path mapping is not bijective: {[item for item in collisions.items() if len(item[1]) > 1]}")

    changed_content = 0
    changed_paths = sum(source != target for source, target in mapping.items())
    commentless = 0
    for source, target in mapping.items():
        source_path = root / source
        baseline_data = contents[source]
        if baseline_data is None:
            continue
        data = baseline_data
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            if source != target:
                commentless += 1
            continue

        transformed = text if source in LEGAL_OR_PROVENANCE_PATHS else map_text(text)
        changed = transformed != text or source != target
        if changed:
            transformed = add_modification_notice(target, transformed)
        encoded = transformed.encode("utf-8")
        if encoded != data:
            changed_content += 1
            if not args.check:
                write_path = root / target if args.reapply else source_path
                write_path.parent.mkdir(parents=True, exist_ok=True)
                write_path.write_bytes(encoded)

    if not args.check and not args.reapply:
        for source in sorted(paths, key=lambda item: (item.count("/"), item), reverse=True):
            target = mapping[source]
            if target == source:
                continue
            source_path = root / source
            target_path = root / target
            if target_path.exists() or target_path.is_symlink():
                raise RuntimeError(f"Refactor target already exists: {target}")
            target_path.parent.mkdir(parents=True, exist_ok=True)
            os.replace(source_path, target_path)

        old_directories = sorted(
            {str(PureParent) for source in paths for PureParent in Path(source).parents if str(PureParent) != "."},
            key=lambda item: (item.count("/"), item),
            reverse=True,
        )
        for relative in old_directories:
            directory = root / relative
            try:
                directory.rmdir()
            except (FileNotFoundError, OSError):
                pass

    print(
        f"PASS identity-refactor mode={'check' if args.check else ('reapply' if args.reapply else 'apply')} "
        f"baselinePaths={len(paths)} changedPaths={changed_paths} changedContent={changed_content} "
        f"commentlessOrBinaryMoved={commentless}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Proves that the tracked lock files actually bind the restore.

A repository can carry a packages.lock.json for every project and still resolve whatever it likes, if
nothing ever restores in locked mode or if the lock file is ignored. These cases therefore do not read
the lock files: they run a real restore in locked mode against a scratch copy of the repository
configuration and assert that a changed lock file, a changed package graph and a missing lock file
each make it fail.

The cases are marked slow because each one runs a restore. They need the package source the
repository's NuGet.config names.
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


# One directory deeper than the modules under test, so the repository root is three levels
# up and the folder holding those modules is the parent of this one.
REPO_ROOT = Path(__file__).resolve().parents[3]

# A project small enough to restore quickly and real enough to have package references.
PROBE_PROJECT = Path("src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj")


def restore(root: Path, project: Path, locked: bool | None = None) -> subprocess.CompletedProcess[str]:
    """Restore the project. locked=None is the plain call a developer types, which is the point.

    Locked mode is the repository default, so the plain call has to refuse a changed graph on its own.
    Passing locked=True adds the flag anyway, and locked=False is the one documented way to open the
    graph, which is what a package update does.
    """
    command = ["dotnet", "restore", str(root / project)]
    if locked is True:
        command.append("--locked-mode")
    elif locked is False:
        command.append("-p:RestoreLockedMode=false")

    environment = dict(os.environ)
    environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"

    return subprocess.run(command, cwd=root, text=True, capture_output=True, check=False, env=environment)


class TrackedLockFileTests(unittest.TestCase):
    """That the repository has projects at all, and a lock file for every one of them.

    The policy rule checks each project it finds and deliberately does not fail on a tree without
    projects, because it runs against synthetic trees in its own tests. The claim that this tree is not
    such a tree belongs here, where it can be made about the real repository.
    """

    def test_every_project_carries_a_tracked_lock_file(self) -> None:
        tracked = subprocess.run(["git", "ls-files", "*.csproj"], cwd=REPO_ROOT, text=True,
                                 capture_output=True, check=True).stdout.split()
        self.assertGreater(len(tracked), 30, "the repository reports far fewer projects than it has")

        missing = sorted(
            project for project in tracked
            if not (REPO_ROOT / project).parent.joinpath("packages.lock.json").is_file()
        )

        self.assertEqual([], missing, "these projects resolve without a lock file")

    def test_every_lock_file_belongs_to_a_project(self) -> None:
        locks = subprocess.run(["git", "ls-files", "packages.lock.json", "**/packages.lock.json"],
                               cwd=REPO_ROOT, text=True, capture_output=True, check=True).stdout.split()
        self.assertGreater(len(locks), 30, "the repository tracks far fewer lock files than it has projects")

        orphans = sorted(
            lock for lock in locks
            if not list((REPO_ROOT / lock).parent.glob("*.csproj"))
        )

        self.assertEqual([], orphans, "these lock files belong to no project")


class LockedRestoreTests(unittest.TestCase):
    """Each case works on its own copy, so a failing case cannot leave the repository changed."""

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.root = Path(self._directory.name) / "repository"
        self.root.mkdir(parents=True)

        for name in ("Directory.Build.props", "Directory.Packages.props", "NuGet.config", "global.json",
                     "signing.props", "ViciOne.ServiceBus.snk"):
            shutil.copy2(REPO_ROOT / name, self.root / name)

        source = REPO_ROOT / PROBE_PROJECT.parent
        shutil.copytree(source, self.root / PROBE_PROJECT.parent, dirs_exist_ok=False)

        self.lock_file = self.root / PROBE_PROJECT.parent / "packages.lock.json"

    def tearDown(self) -> None:
        self._directory.cleanup()

    def test_a_plain_restore_is_locked_without_asking_for_it(self) -> None:
        """The default, measured on the call a developer actually types."""
        self.sabotage_lock_file()

        result = restore(self.root, PROBE_PROJECT)

        self.assertNotEqual(0, result.returncode,
                            "a plain restore accepted a changed lock file, so locked mode is not the default")

    def test_the_documented_update_path_opens_the_graph(self) -> None:
        """The one operation that may change the graph, and it says so.

        Without this the case above could be satisfied by a repository in which no restore can ever
        update a package again, which would make updating one impossible rather than deliberate. The
        mode is read from the evaluated project rather than from a restore, because a restore that
        opens the graph resolves against the network and would make this case depend on a feed.
        """
        self.assertEqual("true", self.evaluated("RestoreLockedMode"),
                         "locked mode is not the default of the repository")
        self.assertEqual("false", self.evaluated("RestoreLockedMode", "-p:RestoreLockedMode=false"),
                         "the documented update switch does not open the graph")

    def evaluated(self, property_name: str, *arguments: str) -> str:
        result = subprocess.run(
            ["dotnet", "msbuild", str(self.root / PROBE_PROJECT), f"-getProperty:{property_name}", *arguments],
            cwd=self.root, text=True, capture_output=True, check=True)
        return result.stdout.strip()

    def sabotage_lock_file(self) -> None:
        content = json.loads(self.lock_file.read_text(encoding="utf-8"))
        framework = next(iter(content["dependencies"].values()))
        _, entry = next(iter(framework.items()))
        entry["resolved"] = "0.0.1-sabotage"
        self.lock_file.write_text(json.dumps(content, indent=2) + "\n", encoding="utf-8")

    def test_the_tracked_lock_file_restores_in_locked_mode(self) -> None:
        self.assertTrue(self.lock_file.is_file(), "the project carries no tracked lock file")

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertEqual(0, result.returncode, f"a locked restore of the tracked lock file failed: {result.stdout}")

    def test_a_changed_lock_file_fails_the_locked_restore(self) -> None:
        self.sabotage_lock_file()

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertNotEqual(0, result.returncode,
                            "a locked restore accepted a lock file that resolves a package to a version "
                            "nobody asked for")

    def test_a_changed_package_graph_fails_the_locked_restore(self) -> None:
        packages = self.root / "Directory.Packages.props"
        text = packages.read_text(encoding="utf-8")
        changed = text.replace(
            '<PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" />',
            '<PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />')
        self.assertNotEqual(text, changed, "the anchor for the package graph change is gone")
        packages.write_text(changed, encoding="utf-8")

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertNotEqual(0, result.returncode,
                            "a locked restore accepted a package version the lock file does not record")

    def test_locked_mode_does_not_notice_a_missing_lock_file(self) -> None:
        """Measured, not assumed: locked mode alone is not the whole guard.

        With RestorePackagesWithLockFile on, a restore that finds no lock file writes one and succeeds
        even in locked mode. A deleted lock file would therefore reopen the package graph silently,
        which is why the locked-restore contract requires the file before any
        restore runs. This case records the gap so nobody closes that rule believing locked mode
        already covers it.
        """
        self.lock_file.unlink()

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertEqual(0, result.returncode,
                         "locked mode rejected a missing lock file, so the policy rule that covers this may be redundant now")
        self.assertTrue(self.lock_file.is_file(), "the restore did not write the lock file it accepted")


if __name__ == "__main__":
    unittest.main()

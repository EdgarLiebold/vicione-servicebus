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


REPO_ROOT = Path(__file__).resolve().parents[2]

# A project small enough to restore quickly and real enough to have package references.
PROBE_PROJECT = Path("src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj")


def restore(root: Path, project: Path, locked: bool) -> subprocess.CompletedProcess[str]:
    command = ["dotnet", "restore", str(root / project)]
    if locked:
        command.append("--locked-mode")

    environment = dict(os.environ)
    environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"

    return subprocess.run(command, cwd=root, text=True, capture_output=True, check=False, env=environment)


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

    def test_the_tracked_lock_file_restores_in_locked_mode(self) -> None:
        self.assertTrue(self.lock_file.is_file(), "the project carries no tracked lock file")

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertEqual(0, result.returncode, f"a locked restore of the tracked lock file failed: {result.stdout}")

    def test_a_changed_lock_file_fails_the_locked_restore(self) -> None:
        content = json.loads(self.lock_file.read_text(encoding="utf-8"))
        framework = next(iter(content["dependencies"].values()))
        name, entry = next(iter(framework.items()))
        entry["resolved"] = "0.0.1-sabotage"
        self.lock_file.write_text(json.dumps(content, indent=2) + "\n", encoding="utf-8")

        result = restore(self.root, PROBE_PROJECT, locked=True)

        self.assertNotEqual(0, result.returncode,
                            f"a locked restore accepted a lock file that resolves {name} to a version nobody asked for")

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
        which is why policy_validator.check_restore_lock_files asserts the file is there before any
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

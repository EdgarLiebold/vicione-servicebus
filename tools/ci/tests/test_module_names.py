#!/usr/bin/env python3
"""Every module under tools/ resolves every name it uses.

An extraction breaks code by leaving a name behind: the function moves to another module and the call
site still says the bare name, which is a NameError on the one path that reaches it. A suite finds the
paths it covers. This finds the rest, because it does not run anything - it asks Python's own symbol
tables which names a module reads without defining, importing or inheriting from the builtins.

Written while moving the runners into packages, where it found four such names in code that was green:
three inside f-strings, which this Python tokenises as a single string, and one in a branch no case
reaches.

Standard library only.
"""

from __future__ import annotations

import builtins
import symtable
import sys
import unittest
from pathlib import Path

# One directory deeper than the modules under test, so the repository root is three levels
# up and the folder holding those modules is the parent of this one.
REPO_ROOT = Path(__file__).resolve().parents[3]
BUILTINS = set(dir(builtins)) | {"__file__", "__name__", "__doc__", "__builtins__", "__spec__"}


def unresolved_names(path: Path) -> list[str]:
    """Names this module reads and never binds, as 'name (line unknown)' is no use - so name and scope."""
    source = path.read_text(encoding="utf-8")
    table = symtable.symtable(source, str(path), "exec")
    top = {symbol.get_name() for symbol in table.get_symbols()
           if symbol.is_assigned() or symbol.is_imported() or symbol.is_parameter()}
    # A name bound anywhere at module level counts, including one bound only inside a try or an if.
    top |= {symbol.get_name() for symbol in table.get_symbols() if symbol.is_local()}

    missing = []

    def walk(scope: symtable.SymbolTable, path_of_scope: str) -> None:
        for symbol in scope.get_symbols():
            name = symbol.get_name()
            if not symbol.is_referenced():
                continue
            bound_here = symbol.is_assigned() or symbol.is_parameter() or symbol.is_imported()
            if bound_here or symbol.is_local() or symbol.is_free():
                continue
            if name in top or name in BUILTINS:
                continue
            missing.append(f"{name} in {path_of_scope}")
        for child in scope.get_children():
            walk(child, f"{path_of_scope}.{child.get_name()}")

    walk(table, path.stem)

    return sorted(set(missing))


class Every_module_of_this_repository_resolves_its_names(unittest.TestCase):
    """A name a module reads and nobody gives it is a NameError waiting for the path that reaches it."""

    def test_no_module_under_tools_reads_a_name_nobody_gives_it(self) -> None:
        found = {}
        for path in sorted(REPO_ROOT.glob("tools/**/*.py")):
            missing = unresolved_names(path)
            if missing:
                found[path.relative_to(REPO_ROOT).as_posix()] = missing

        self.assertEqual({}, found,
                         "these modules read names that are neither defined, imported nor built in, "
                         "which is what an extraction leaves behind")

    def test_a_module_that_calls_something_it_never_imported_is_reported(self) -> None:
        """The guard of the case above: a check that found nothing would satisfy it."""
        broken = REPO_ROOT / "artifacts" / "unresolved-name-probe.py"
        broken.parent.mkdir(parents=True, exist_ok=True)
        broken.write_text("def use():\n    return compose_fixture.start()\n", encoding="utf-8")
        try:
            self.assertEqual(["compose_fixture in unresolved-name-probe.use"],
                             unresolved_names(broken))
        finally:
            broken.unlink()

    def test_a_module_that_imports_what_it_calls_is_not_reported(self) -> None:
        clean = REPO_ROOT / "artifacts" / "resolved-name-probe.py"
        clean.parent.mkdir(parents=True, exist_ok=True)
        clean.write_text("import json\n\n\ndef use():\n    return json.dumps({})\n", encoding="utf-8")
        try:
            self.assertEqual([], unresolved_names(clean))
        finally:
            clean.unlink()


if __name__ == "__main__":
    unittest.main()

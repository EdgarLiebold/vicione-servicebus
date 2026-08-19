#!/usr/bin/env python3
"""The one place this repository states what a required run may not do, and refuses it before one runs.

    python3 tools/ci/policy_validator.py

Every rule lives under policies/, one module per thing a rule is about: workflow.py how the required
profile starts what the model declares, model.py the verification model and the expected truth it
points at, msbuild.py what every project is bound by and the three that are excepted, fixtures.py the
pinned containers, their ports and their credentials. This composes them and runs every rule they have.

Found rather than listed. The list of rules used to be written by hand beside them, so a rule could be
defined and never called - written, reviewed as written, and looking at nothing. A method whose name
begins with check_ is a rule, and being one is what makes it run.

Standard library only, so it runs before any restore.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from policies import PolicyBase  # noqa: E402  (repository local, resolved from this file's folder)
from policies.fixtures import FixturesPolicy  # noqa: E402
from policies.model import ModelPolicy  # noqa: E402
from policies.msbuild import MsbuildPolicy  # noqa: E402
from policies.workflow import WorkflowPolicy  # noqa: E402

# Re-exported for the suite of this validator, which builds a repository and asks this what is wrong
# with it. There is one definition of each of these, in the module the rule that reads it lives in.
from policies import effective_commands  # noqa: E402,F401
from policies import ANALYZER_PACKAGE_PROJECT, ROSLYN_COMPONENT_PROJECTS  # noqa: E402,F401


class Policy(WorkflowPolicy, ModelPolicy, MsbuildPolicy, FixturesPolicy, PolicyBase):
    """Every rule this repository has, in one object, over one repository."""

    def rules(self) -> list[str]:
        """Every rule this validator has, found rather than listed.

        The list this replaces was written by hand, and a rule that was defined and never added to it
        ran nowhere: it was written, it passed review as written, and it looked at nothing. That is the
        same shape as the loop over an empty dictionary this suite already found once, so the state is
        removed instead of corrected - a method whose name begins with check_ is a rule, and being one
        is what makes it run.
        """
        return sorted(name for name in dir(self)
                      if name.startswith("check_") and callable(getattr(self, name)))

    def run(self) -> int:
        for name in self.rules():
            getattr(self, name)()

        if self.failures:
            for failure in self.failures:
                print(f"FAIL ci-policy {failure}", file=sys.stderr)
            return 1

        print("PASS ci-policy all invariants hold")
        return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2],
                        help="Repository root to validate.")
    args = parser.parse_args(argv)
    return Policy(args.root).run()


if __name__ == "__main__":
    raise SystemExit(main())

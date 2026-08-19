#!/usr/bin/env python3
"""The verification model and the expected truth it points at.

One capability per project, one run per category, one expected identity set per run,
and a floor that agrees with it. A case that does not execute is named here with the
reason it does not, because a silent exclusion is a category that shrank.

Standard library only.
"""

from __future__ import annotations

import json
import re
from pathlib import Path

from policies import (EXPECTED_IDENTITY_DIRECTORY, PolicyBase, VERIFICATION_MODEL,
                      strip_comments)
from verification import model as verification_model


class ModelPolicy(PolicyBase):
    """The verification model and the expected truth it points at."""

    def check_no_required_test_masking(self) -> None:
        """The required path may not filter, skip or ignore its way to green.

        This used to match a quoted category predicate. That was one spelling of one selector: the
        same exclusion written with single quotes, or written against FullyQualifiedName, Name or
        the NUnit selector, walked straight past it. A required category is defined by running
        everything it contains, so no selector is admissible here at all and the rule matches the
        switch rather than the predicate behind it.
        """
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        for pattern, detail in (
            (r"--filter\b", "the required path selects a subset of its tests"),
            (r"NUnit\.Where", "the required path applies an NUnit selector"),
            (r"--settings\b", "the required path loads a run settings file, which can carry a filter"),
            (r"TestCaseFilter", "the required path applies a test case filter"),
            (r"--blame-hang", "the required path masks hangs instead of failing"),
            (r"VSTEST_.*SKIP", "the required path sets a skip switch"),
        ):
            if re.search(pattern, body):
                self.fail("test-masking", f"{detail}: pattern '{pattern}'")

    def check_no_silent_test_exclusion(self) -> None:
        """Every test the required categories do not execute is named in one inventory.

        A category that reports a positive count and zero failures says nothing about the tests it
        never started. NUnit offers two ways to leave one out, and they are treated differently on
        purpose:

          [Ignore]    never appears in this repository and is rejected outright. It disables a case
                      without a run time decision, which is exactly the regression this rule exists
                      for.
          [Explicit]  is inherited from the imported suite for cases that genuinely cannot run in a
                      required category. Each of them is listed in the inventory with its reason, and
                      the count per project is bound, so a new one cannot appear unnoticed.

        The run time half of this rule is in run_test_category.py, which rejects any not executed
        case the inventory does not name. This half catches the same mutation before a run.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} is missing; "
                                        "the required categories would have no binding list of what they skip")
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError as error:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} is not readable: {error}")
            return

        categories = {run["category"]: run for run in verification_model.runs(data) if run.get("category")}
        if not categories:
            self.fail("test-exclusion", f"{VERIFICATION_MODEL} declares no required run")
            return

        for name, category in sorted(categories.items()):
            project = str(category.get("testProjectDirectory", ""))
            directory = self.root / project
            if not project or not directory.is_dir():
                self.fail("test-exclusion", f"category '{name}' names no existing test project: '{project}'")
                continue

            sources = [
                source for source in sorted(directory.rglob("*.cs"))
                if "/bin/" not in source.as_posix() and "/obj/" not in source.as_posix()
            ]
            ignored = 0
            explicit = 0
            for source in sources:
                for number, line in enumerate(source.read_text(encoding="utf-8-sig", errors="replace").splitlines(), 1):
                    if line.lstrip().startswith("//"):
                        continue
                    if "[Ignore" in line:
                        ignored += 1
                        self.fail("test-exclusion",
                                  f"{source.relative_to(self.root).as_posix()}:{number} uses [Ignore]; "
                                  "a required project may not silence a case without a run time decision")
                    explicit += line.count("[Explicit")

            declared = category.get("explicitAttributeCount")
            if not isinstance(declared, int) or declared != explicit:
                self.fail("test-exclusion",
                          f"category '{name}' declares {declared} [Explicit] attributes but {project} carries "
                          f"{explicit}; a new exclusion has to be classified in {VERIFICATION_MODEL}")

            cases = category.get("notExecuted")
            if not isinstance(cases, list):
                self.fail("test-exclusion", f"category '{name}' has no case list")
                continue
            for case in cases:
                if not isinstance(case, dict) or not {"identity", "fixture", "test", "mechanism", "dueness", "reason"} <= set(case):
                    self.fail("test-exclusion", f"category '{name}' has an incomplete case entry: {case}")
                    continue
                # The identity carries the namespace, the class including parameterised fixture
                # arguments and the case name. A short form would let one entry authorise every
                # case that happens to share it, which is how two fixtures of the same name in
                # different namespaces once shared a single permission.
                identity = str(case.get("identity", ""))
                suffix = f".{case.get('fixture')}.{case.get('test')}"
                # Ends exactly on .fixture.test, and something has to stand in front of it. The first
                # version of this rule asked whether the identity merely contained the two names, so
                # the bare short form Fixture.Test passed as a full identity and a name inserted
                # between fixture and test passed as well. Both are the collision this contract
                # exists to prevent, so the check is a suffix and a non empty namespace.
                namespace = identity[:-len(suffix)] if identity.endswith(suffix) else ""
                if not namespace:
                    self.fail("test-exclusion",
                              f"category '{name}' names the case {case.get('fixture')}.{case.get('test')} with the "
                              f"identity '{identity}', which is not a namespaced identity ending on "
                              f"'{suffix}'")
                if str(case.get("dueness", "")).startswith("DUE"):
                    self.fail("test-exclusion",
                              f"category '{name}' lists a due case as not executed: "
                              f"{case.get('fixture')}.{case.get('test')}. A due case is run, never inventoried.")
            _ = ignored

    def check_verification_model(self) -> None:
        """The active capability truth has to hold before anything reads it."""
        for problem in verification_model.findings(self.root):
            self.fail("verification-model", problem)

    def check_dueness_classes(self) -> None:
        """A case may not claim missing infrastructure that the category's own fixture provides.

        Nine cases were excluded as NOT_DUE_EXTERNAL_INFRASTRUCTURE while the pinned broker they need
        is exactly what the required profile starts. The class was the whole reason nobody looked at
        them again, so a category with a fixture may not use it. Every category in the matrix whose
        capability is verified against a pinned fixture is such a category.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError:
            return

        try:
            matrix = verification_model.load(self.root)
        except verification_model.ModelError:
            return

        with_a_fixture = {
            run["category"]
            for capability in matrix.get("capabilities", [])
            if capability.get("class") == "PINNED_FIXTURE_REQUIRED_RUN"
            for run in capability.get("runs", [])
            if run.get("category")
        }

        for run in sorted(verification_model.runs(data), key=lambda r: r.get("category") or ""):
            name = run.get("category")
            if name not in with_a_fixture:
                continue
            for case in run.get("notExecuted", []):
                if not isinstance(case, dict):
                    continue
                if case.get("dueness") == "NOT_DUE_EXTERNAL_INFRASTRUCTURE":
                    self.fail("dueness-class",
                              f"category '{name}' runs against a pinned fixture, so "
                              f"'{case.get('identity', '<unnamed>')}' may not be excluded as needing external "
                              "infrastructure")

    def check_executed_floor(self) -> None:
        """Every category that a required job runs records the count it must not fall below.

        This rule read a top level 'categories' object, which the verification model has not had since
        it replaced the two files before it. The loop therefore ran over nothing at all and the check
        passed for every repository, including one with no floor anywhere. Found while working the
        anchors of the same model; it is the same failure as the ones the directive names - a control
        that reports success without looking at anything.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            return
        try:
            data = json.loads(inventory)
        except json.JSONDecodeError:
            return

        declared = verification_model.runs(data)
        if not declared:
            self.fail("executed-floor", f"{VERIFICATION_MODEL} declares no run, so no category has a floor")
            return

        for run in sorted(declared, key=lambda entry: str(entry.get("category"))):
            floor = run.get("minimumExecutedCases")
            if not isinstance(floor, int) or floor <= 0:
                self.fail("executed-floor",
                          f"category '{run.get('category')}' records no minimumExecutedCases, so a category "
                          "that shrinks would still report green")

    def check_expected_identity_manifests(self) -> None:
        """The expected sets are the truth a run is measured against, so they are files and not claims.

        Each one is exactly build/verification/expected/<category>.txt, a regular file that is part of
        this repository, sorted and free of repetition, and its size is the floor the model records for
        the same category. A manifest that is a link is a second name somebody can repoint; one that
        nobody committed is not the expected truth of anything; one whose floor disagrees with it is
        two claims about one category.
        """
        inventory = self.read(VERIFICATION_MODEL)
        if inventory is None:
            return
        try:
            model = json.loads(inventory)
        except json.JSONDecodeError:
            return

        directory = self.root / EXPECTED_IDENTITY_DIRECTORY
        declared: set[str] = set()

        for run in sorted(verification_model.runs(model), key=lambda entry: str(entry.get("category"))):
            category = run.get("category")
            named = run.get("expectedIdentities")
            if not named:
                self.fail("expected-identities",
                          f"category '{category}' records no expected identity set, so nothing states "
                          "which cases it is complete with")
                continue

            expected_path = f"{EXPECTED_IDENTITY_DIRECTORY}/{category}.txt"
            if named != expected_path:
                self.fail("expected-identities",
                          f"category '{category}' names '{named}' and the expected set of a category "
                          f"is '{expected_path}'")
                continue

            declared.add(f"{category}.txt")
            path = self.root / named
            if path.is_symlink():
                self.fail("expected-identities",
                          f"{named} is a symbolic link. The expected truth is the file itself, because "
                          "a link is a second name somebody can repoint")
                continue
            if not path.is_file():
                self.fail("expected-identities", f"{named} is not there, so this category is measured "
                                                 "against nothing")
                continue
            if not self.tracked(named):
                self.fail("expected-identities",
                          f"{named} is not part of this repository, so it is not the expected truth of "
                          "any committed state")

            identities = [line.strip() for line in path.read_text(encoding="utf-8").splitlines()
                          if line.strip() and not line.strip().startswith("#")]
            if identities != sorted(identities):
                self.fail("expected-identities",
                          f"{named} is not sorted, so two recordings of one set are two different files")
            if len(set(identities)) != len(identities):
                repeated = sorted({entry for entry in identities if identities.count(entry) > 1})
                self.fail("expected-identities",
                          f"{named} names {repeated[0]} more than once, so its size is not the number "
                          "of cases it holds")
            floor = run.get("minimumExecutedCases")
            if isinstance(floor, int) and floor != len(identities):
                self.fail("expected-identities",
                          f"category '{category}' records the floor {floor} and its expected set holds "
                          f"{len(identities)} identities, so the model makes two claims about one "
                          "category")

        if directory.is_dir():
            for path in sorted(directory.iterdir()):
                if path.name not in declared:
                    self.fail("expected-identities",
                              f"{EXPECTED_IDENTITY_DIRECTORY}/{path.name} belongs to no category of "
                              "this model, so nothing is measured against it and nothing maintains it")

    def check_no_raw_run_artifacts_in_evidence(self) -> None:
        """evidence/ is the durable record. A raw run artifact is not durable record material.

        Measured before this rule existed: 526 MiB of tracked evidence, 434 of it in 139 TRX files and
        23 collected broker logs, with 45 files that were a byte identical repeat of another, and not
        one outbox record binding a TRX. What a record binds and a reader reads is the compact category
        summary the runner writes beside the TRX; the TRX itself is reproducible by rerunning, and the
        digests of the removed ones are in evidence/RAW_RUN_ARTIFACT_MANIFEST.json.

        A run may of course still produce them - it has to, the counters are parsed out of them. They
        belong under artifacts/, which git ignores wholesale, not under the tree that is kept.
        """
        evidence = self.root / "evidence"
        if not evidence.is_dir():
            return

        for artifact in sorted(evidence.rglob("*")):
            if artifact.suffix in (".trx", ".log") and artifact.is_file():
                relative = artifact.relative_to(self.root).as_posix()
                self.fail("raw-artifact",
                          f"{relative} is a raw run artifact under evidence/; keep the category summary "
                          "and the digest instead, and write the run output under artifacts/")

    def check_every_tool_test_module_runs(self) -> None:
        """A test module nobody starts is an inventory entry, not a proof.

        This repository holds fifteen Python test modules under tools/. Exactly one of them was named
        by a required job; the other fourteen, 94 cases across the identity gates and the two CI
        runners, were green and never started by anything but a developer who remembered them. That is
        the same defect the not-executed inventory exists to prevent, one layer down.

        A job satisfies this either by naming the module path or by discovering its directory. The
        check reads the workflow text, so it proves that the command is written, not that it passed -
        the run itself proves that.
        """
        text = "\n".join((self.root / name).read_text(encoding="utf-8") for name in self.workflow_files())

        for module in sorted(self.root.glob("tools/**/test_*.py")):
            relative = module.relative_to(self.root).as_posix()
            directory = module.parent.relative_to(self.root).as_posix()
            if relative in text or any(f"unittest discover -s {reached}" in text
                                       for reached in self.discovering(module.parent)):
                continue
            self.fail("tool-test-not-run",
                      f"{relative} is never started by a required job; name it or discover "
                      f"{directory}, or the module is an inventory entry rather than a proof")

    def discovering(self, directory: Path) -> list[str]:
        """Every directory whose discovery reaches this one, as unittest really walks them.

        Discovery descends from the directory it was given into each subdirectory that is a package,
        so a module under tools/ci/tests is started by `discover -s tools/ci` - the rule read only the
        module's own directory and would have demanded a second command for a suite that already runs.
        The chain has to be unbroken: a directory without an __init__.py stops the descent, and every
        module below it with it.
        """
        reached = [directory.relative_to(self.root).as_posix()]
        walking = directory
        while (walking / "__init__.py").is_file() and walking != self.root:
            walking = walking.parent
            reached.append(walking.relative_to(self.root).as_posix())

        return reached

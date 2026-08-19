#!/usr/bin/env python3
"""What every project of this repository is bound by, and the three that are not.

The central contract applies to every project: no project takes itself out of it and
no project grants itself an exception. The netstandard2.0 exception belongs to two
Roslyn components and one package surface by path, and it is checked in both
directions - nobody else may have it, and those three have to keep being what it was
granted for.

Standard library only.
"""

from __future__ import annotations

import xml.etree.ElementTree as ElementTree

from policies import (ANALYZER_PACKAGE_PROJECT, CENTRAL_BUILD_FILES, CREDENTIAL_ELEMENT,
                      CREDENTIAL_KEYS, PolicyBase, REQUIRED_SOURCE_KEY, REQUIRED_SOURCE_VALUE,
                      RESERVED_CENTRAL_PROPERTIES, RESTORE_CONFIG, RETIRED_SELF_MARKERS,
                      ROSLYN_COMPONENT_PROJECTS, msbuild_properties)


class MsbuildPolicy(PolicyBase):
    """What every project of this repository is bound by, and the three that are not."""

    def check_analyzer_release_tracking(self) -> None:
        for name in ("AnalyzerReleases.Shipped.md", "AnalyzerReleases.Unshipped.md"):
            body = self.read(f"src/ViciOne.ServiceBus.Analyzers/{name}")
            if body is None:
                self.fail("analyzer-tracking", f"{name} is missing")
                continue
            for line in body.lstrip("﻿").splitlines():
                if not line.strip():
                    continue
                if line.lstrip().startswith(";"):
                    continue  # the release tracking parser accepts ';' comments
                if not line.startswith("##"):
                    self.fail("analyzer-tracking",
                              f"{name} starts with '{line.strip()[:60]}' instead of a release header; "
                              "this is exactly what raises RS2007")
                break

    def check_every_project_belongs_to_a_solution(self) -> None:
        """A project no solution references is not built, and a reference to nothing is not a project.

        Both directions, and the solution is read as the XML it is. A regular expression over
        Path="..." finds a reference inside a comment and misses one written with single quotes, which
        is not a gate, it is a guess that usually agrees.

        The first direction is not hypothetical: a stray test project reappeared twice, once when a
        disallowed history rewrite resurrected it and once when a routine `git add -A` picked the
        untracked file up again. Nothing referenced it and nothing built it. The second direction
        catches the opposite mistake - a project moved or removed while a solution still names it,
        which fails the build for everyone rather than silently.
        """
        referenced: dict[str, str] = {}

        for solution in sorted(self.root.glob("*.slnx")):
            name = solution.relative_to(self.root).as_posix()
            try:
                tree = ElementTree.parse(solution)
            except ElementTree.ParseError as error:
                self.fail("solution", f"{name} is not parsable as XML: {error}")
                continue

            for element in tree.getroot().iter("Project"):
                path = element.get("Path")
                if not path:
                    self.fail("solution", f"{name} carries a Project element without a Path")
                    continue

                relative = path.replace("\\", "/")
                if not (self.root / relative).is_file():
                    self.fail("solution", f"{name} references '{relative}', which is not a file")
                referenced[relative] = name

        for project in sorted(self.root.rglob("*.csproj")):
            relative = project.relative_to(self.root).as_posix()
            if relative.startswith("artifacts/"):
                continue
            if relative not in referenced:
                self.fail("orphan-project",
                          f"{relative} is referenced by no solution, so nothing builds it and nothing "
                          "verifies it")

    def check_central_build_targets_are_effective(self) -> None:
        """Directory.Build.targets has to assert something, not merely exist.

        A decorative file is worse than no file: it looks like a central contract and enforces
        nothing, so the next reader assumes the promises in it are held. What makes this one effective
        is that every gate raises an Error rather than a Warning - a warning is a note nobody reads in
        a repository that does not fail on them - and that each gate hangs off a real build target, so
        it is evaluated rather than only defined.
        """
        relative = "Directory.Build.targets"
        text = self.read(relative)
        if text is None:
            self.fail("build-targets", f"{relative} is missing, so the repository has no late central "
                                       "build contract at all")
            return

        try:
            root = ElementTree.fromstring(text)
        except ElementTree.ParseError as error:
            self.fail("build-targets", f"{relative} is not parsable as XML: {error}")
            return

        targets = [element for element in root if element.tag == "Target"]
        if not targets:
            self.fail("build-targets", f"{relative} declares no target, so it asserts nothing")
            return

        for target in targets:
            name = target.get("Name", "<unnamed>")
            if not (target.get("BeforeTargets") or target.get("AfterTargets") or target.get("DependsOnTargets")):
                self.fail("build-targets",
                          f"target '{name}' in {relative} hangs off no build target, so it is defined "
                          "and never evaluated")
            if not [child for child in target if child.tag == "Error"]:
                self.fail("build-targets",
                          f"target '{name}' in {relative} raises no Error; a warning in a central "
                          "contract is a note nobody reads")

    def check_framework_exceptions_belong_to_named_projects(self) -> None:
        """The netstandard2.0 exception is granted by path, not claimed by the project that wants one.

        A marker a project sets about itself is not a control: any project can set it, so a copied,
        renamed or newly added project could hand itself the exception and the central rule would agree.
        Three exact repository relative paths have it, the same three Directory.Build.targets names, and
        this rule catches a fourth before a build rather than during one.
        """
        for path in sorted(self.build_files()):
            relative = path.relative_to(self.root).as_posix()
            if not relative.endswith(".csproj"):
                continue
            try:
                root = ElementTree.fromstring(path.read_text(encoding="utf-8-sig", errors="replace"))
            except ElementTree.ParseError:
                continue

            properties = dict(msbuild_properties(root))
            framework = properties.get("targetframework", "")
            allowed = relative in ROSLYN_COMPONENT_PROJECTS or relative == ANALYZER_PACKAGE_PROJECT

            if framework == "netstandard2.0" and not allowed:
                self.fail("framework-exception",
                          f"{relative} targets netstandard2.0. That exception belongs to "
                          f"{', '.join(ROSLYN_COMPONENT_PROJECTS)} and {ANALYZER_PACKAGE_PROJECT}, and it "
                          "is granted by path rather than by a property a project sets about itself")

            if properties.get("isroslyncomponent", "").lower() == "true" \
                    and relative not in ROSLYN_COMPONENT_PROJECTS:
                self.fail("framework-exception",
                          f"{relative} declares IsRoslynComponent and is not one of the two Roslyn "
                          "components of this repository. A copy of an analyzer project is a new project "
                          "and gets no exception by carrying the marker of an old one")

            for marker in RETIRED_SELF_MARKERS:
                if marker in properties:
                    self.fail("framework-exception",
                              f"{relative} declares {marker}, which is retired. A project cannot grant "
                              "itself the framework exception; the projects that have one are named by "
                              "path in Directory.Build.targets")

            if relative == ANALYZER_PACKAGE_PROJECT:
                for meaningless in ("langversion", "warninglevel"):
                    if meaningless in properties:
                        self.fail("framework-exception",
                                  f"{relative} sets {meaningless} and compiles no source at all "
                                  "(EnableDefaultCompileItems is off), so it states something about a "
                                  "compilation that never happens")

        self.check_the_named_projects_still_hold_their_exception()

    def check_the_named_projects_still_hold_their_exception(self) -> None:
        """The other direction: an exception nobody keeps is an exception nobody needs.

        The rule above says that no project outside the three named ones may have the netstandard2.0
        exception. It says nothing about the three, so all of them could quietly stop being what the
        exception was granted for and every check would still pass - the analyzers could retarget, the
        package project could start compiling source, and the reason the exception exists would be gone
        while the exception stayed.

        What each of them has to remain is what it is for. A Roslyn component runs inside the compiler,
        which is why it targets netstandard2.0 and says IsRoslynComponent. The package project ships the
        two assemblies as the one package they always were, which is why it compiles nothing, carries no
        build output of its own and is a development dependency.
        """
        for relative in ROSLYN_COMPONENT_PROJECTS:
            properties = self.msbuild_properties_of(relative)
            if properties is None:
                self.fail("framework-exception",
                          f"{relative} is one of the two Roslyn components of this repository and is "
                          "not there")
                continue
            if properties.get("targetframework") != "netstandard2.0":
                self.fail("framework-exception",
                          f"{relative} is a Roslyn component and targets "
                          f"'{properties.get('targetframework')}'. A component the compiler loads is "
                          "netstandard2.0, and that is the whole reason this project has an exception")
            if properties.get("isroslyncomponent", "").lower() != "true":
                self.fail("framework-exception",
                          f"{relative} holds the framework exception of a Roslyn component and no "
                          "longer declares IsRoslynComponent")

        properties = self.msbuild_properties_of(ANALYZER_PACKAGE_PROJECT)
        if properties is None:
            self.fail("framework-exception",
                      f"{ANALYZER_PACKAGE_PROJECT} carries the analyzer package surface and is not there")
            return
        if properties.get("targetframework") != "netstandard2.0":
            self.fail("framework-exception",
                      f"{ANALYZER_PACKAGE_PROJECT} targets '{properties.get('targetframework')}'. An "
                      "analyzer package carries no lib folder, so the framework group of its nuspec is "
                      "what decides which projects may reference it")
        for name, wanted, why in (
                ("enabledefaultcompileitems", "false",
                 "it would start compiling source, and it exists to carry none"),
                ("includebuildoutput", "false",
                 "its own assembly would ship in the package beside the two that are the package"),
                ("developmentdependency", "true",
                 "it would flow to the consumers of a consumer as a runtime dependency")):
            if properties.get(name, "").lower() != wanted:
                self.fail("framework-exception",
                          f"{ANALYZER_PACKAGE_PROJECT} sets {name}='{properties.get(name)}' and the "
                          f"package surface needs '{wanted}': {why}")

    def check_no_project_leaves_the_central_contract(self) -> None:
        """A project may not take itself out of the repository's build contract.

        The properties below are reserved for the two root files. Each of them decides whether the
        central contract applies at all, so a project that writes one has left it whatever value it
        wrote: ImportDirectoryBuildTargets skips the late gates, DirectoryBuildTargetsPath and the two
        CustomBefore/AfterMicrosoftCommonTargets hooks replace them with another file, RestoreLockedMode
        resolves past a lock file the project still carries, and RestoreLockedModeFromCommandLine is the
        flag the lock gate uses to tell a documented update apart from an escape - a project that sets
        it hands itself the exception.

        This searched exact XML text before, and three executed counterexamples walked past it:
        RestoreLockedMode written as False, a conditional ImportDirectoryBuildTargets, and a conditional
        DirectoryBuildTargetsPath redirect. The text search saw a capital letter and an attribute, not a
        property. The XML is parsed instead, every PropertyGroup is read wherever it stands - including
        inside a Choose or a Target - and the name is compared without case, whitespace, condition or
        value entering into it.

        Project-local props and targets are read as well, because an import is only one line away, and a
        property in the imported file has exactly the effect it would have had in the project. The two
        root files remain the sole owner. The documented package update stays possible because it passes
        its property on the command line, where the whole run and the diff of the change see it.
        """
        for path in sorted(self.build_files()):
            relative = path.relative_to(self.root).as_posix()
            try:
                root = ElementTree.fromstring(path.read_text(encoding="utf-8-sig", errors="replace"))
            except ElementTree.ParseError as error:
                self.fail("central-contract", f"{relative} is not parsable as MSBuild XML: {error}")
                continue

            for name, value in msbuild_properties(root):
                if name not in RESERVED_CENTRAL_PROPERTIES:
                    continue
                self.fail("central-contract",
                          f"{relative} declares the reserved central property {name} as '{value}'. "
                          f"{RESERVED_CENTRAL_PROPERTIES[name]} Only {' and '.join(CENTRAL_BUILD_FILES)} "
                          "may set it, and a package update passes its property on the command line, "
                          "where the whole run and the diff of the change see it")

    def check_restore_lock_files(self) -> None:
        """Every project resolves against a tracked lock file.

        Locked mode alone does not carry this. Measured: a restore with --locked-mode and no lock file
        present writes one and succeeds, because RestorePackagesWithLockFile is on. A deleted lock file
        would therefore reopen the package graph without anything reporting it, which is precisely what
        the lock files exist to prevent, so the presence of the file is checked here, before any
        restore runs.
        """
        # No vacuity guard here: this rule runs against synthetic trees in its own tests as well, and a
        # tree without projects is not a policy violation. That the real repository has projects at all,
        # and a lock file for every one of them, is asserted in tools/ci/test_locked_restore.py.
        for project in sorted(self.root.rglob("*.csproj")):
            relative = project.relative_to(self.root)
            if relative.parts[0] in {"artifacts", "obj", "bin"}:
                continue

            lock_file = project.parent / "packages.lock.json"
            if not lock_file.is_file():
                self.fail("restore-lock", f"{relative.as_posix()} has no packages.lock.json, so its restore is not bound")

    def check_restore_sources(self) -> None:
        """The repository names its own restore source, so a restore cannot inherit machine state.

        Order matters: <clear /> has to stand before the source it keeps, otherwise it wipes it again.
        The mapping has to claim every pattern, because an unclaimed pattern is exactly the case
        NU1507 warns about and the case where a package could arrive from somewhere unnamed.
        """
        body = self.read(RESTORE_CONFIG)
        if body is None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} is missing, so a restore inherits the machine's sources")
            return

        try:
            root = ElementTree.fromstring(body)
        except ElementTree.ParseError as error:
            self.fail("restore-sources", f"{RESTORE_CONFIG} is not parsable: {error}")
            return

        sources = root.find("packageSources")
        if sources is None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} declares no packageSources")
            return

        children = list(sources)
        if not any(child.tag == "clear" for child in children):
            self.fail("restore-sources", "packageSources does not clear the inherited sources")
        else:
            first_add = next((index for index, child in enumerate(children) if child.tag == "add"), len(children))
            last_clear = max(index for index, child in enumerate(children) if child.tag == "clear")
            if last_clear > first_add:
                self.fail("restore-sources", "a clear stands after the source it should keep, which removes it again")

        added = [child for child in children if child.tag == "add"]
        if len(added) != 1:
            self.fail("restore-sources",
                      f"packageSources declares {len(added)} sources; exactly one is allowed")
        else:
            key, value = added[0].get("key"), added[0].get("value")
            if key != REQUIRED_SOURCE_KEY or value != REQUIRED_SOURCE_VALUE:
                self.fail("restore-sources", f"the only source must be {REQUIRED_SOURCE_KEY} at "
                                             f"{REQUIRED_SOURCE_VALUE}, found '{key}' at '{value}'")

        mapping = root.find("packageSourceMapping")
        if mapping is None:
            self.fail("restore-sources", "no packageSourceMapping, so central package management cannot "
                                         "decide which source a package comes from")
        else:
            mapped = {entry.get("key"): [package.get("pattern") for package in entry.findall("package")]
                      for entry in mapping.findall("packageSource")}
            if set(mapped) != {REQUIRED_SOURCE_KEY}:
                self.fail("restore-sources",
                          f"the mapping must name exactly {REQUIRED_SOURCE_KEY}, found {sorted(mapped)}")
            elif "*" not in mapped[REQUIRED_SOURCE_KEY]:
                self.fail("restore-sources", "the mapping does not claim the pattern *, so some package "
                                             "pattern stays unmapped")

        # Clearing the inherited sources is only half of the host independence. A machine level
        # disabledPackageSources entry can switch off the one source that is left, and the restore then
        # has none at all. The file states that it clears them; the gate has to hold that statement.
        disabled = root.findall("disabledPackageSources")
        if len(disabled) != 1:
            self.fail("restore-sources",
                      f"{RESTORE_CONFIG} declares {len(disabled)} disabledPackageSources sections; "
                      "exactly one is required so a host entry cannot disable the only source")
        else:
            children = list(disabled[0])
            if not any(child.tag == "clear" for child in children):
                self.fail("restore-sources", "disabledPackageSources does not clear what the host disabled")
            else:
                first_clear = next(index for index, child in enumerate(children) if child.tag == "clear")
                if any(child.tag != "clear" for child in children[:first_clear]):
                    self.fail("restore-sources",
                              "an operation stands before the clear in disabledPackageSources, so it "
                              "survives the clear")
            for child in children:
                if child.tag == "add" and child.get("key") == REQUIRED_SOURCE_KEY:
                    self.fail("restore-sources",
                              f"disabledPackageSources disables {REQUIRED_SOURCE_KEY}, which is the only "
                              "source the restore has")

        if root.find(CREDENTIAL_ELEMENT) is not None:
            self.fail("restore-sources", f"{RESTORE_CONFIG} carries {CREDENTIAL_ELEMENT}; the single source "
                                         "is public and no credential belongs in the repository")
        for element in root.iter("add"):
            if element.get("key") in CREDENTIAL_KEYS:
                self.fail("restore-sources", f"{RESTORE_CONFIG} carries a '{element.get('key')}' entry")

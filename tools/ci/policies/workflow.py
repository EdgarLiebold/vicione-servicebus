#!/usr/bin/env python3
"""How the required profile starts what the model declares.

The workflow names a selection and nothing else: what runs, against which project,
with which broker and for how long is the model's. Everything here is that boundary -
the one canonical invocation, a step that may not mask its own outcome, a runner and
an SDK that are pinned, and a pack that depends on every gate it claims to follow.

Standard library only.
"""

from __future__ import annotations

from pathlib import Path
from verification import model as verification_model
import re
import shlex

from policies import (APPROVED_SDK_FILE, CANONICAL_ENTRY_POINT, CANONICAL_INVOCATION,
                      CANONICAL_RUNNER, PolicyBase, REQUIRED_CATEGORIES, REQUIRED_JOB_CONDITION,
                      REQUIRED_RUNNER, WorkflowScriptError, effective_commands, masked_outcomes,
                      run_scripts, strip_comments, verify)


class WorkflowPolicy(PolicyBase):
    """How the required profile starts what the model declares."""

    def check_canonical_runner(self) -> None:
        """A category with a fixture is started by the runner that owns that fixture.

        This routing used to be written into the workflow, where it was a second copy of what the model
        already said and could disagree with it. It is now a decision of the canonical entry point, so
        it is checked where it is made: for every category the model declares brokers for, the command
        the entry point builds has to be the broker runner, and it has to name every one of those
        brokers. A category that started its own Compose fixture would have to guess the ephemeral
        ports, which is how fourteen green tests once measured a foreign broker.
        """
        try:
            model = verification_model.load(self.root)
        except verification_model.ModelError as error:
            self.fail("canonical-runner", str(error))
            return

        body = self.read(".github/workflows/build.yml")
        if body is not None:
            for pattern in (r"docker\s+compose", r"docker\s+run", r"docker\s+build"):
                if re.search(pattern, strip_comments(body)):
                    self.fail("canonical-runner",
                              f"the required profile runs docker itself ('{pattern}'). The fixture "
                              "belongs to the runner that resolves its ephemeral ports; a job that "
                              "starts one has to guess them")

        for run in sorted(verification_model.runs(model), key=lambda entry: str(entry.get("category"))):
            brokers = run.get("brokers") or []
            command = verify.child_command(run, Path("evidence"))
            names = [command[index + 1] for index, token in enumerate(command) if token == "--broker"]
            if not brokers:
                if CANONICAL_RUNNER in " ".join(command):
                    self.fail("canonical-runner",
                              f"category '{run.get('category')}' declares no broker and is still routed "
                              "through the broker runner")
                continue
            if CANONICAL_RUNNER not in " ".join(command):
                self.fail("canonical-runner",
                          f"category '{run.get('category')}' needs {', '.join(brokers)} and does not go "
                          f"through {CANONICAL_RUNNER}; the fixture endpoints would have to be guessed")
            if names != brokers:
                self.fail("canonical-runner",
                          f"category '{run.get('category')}' declares the brokers {brokers} and would be "
                          f"started with {names}")

    def check_required_profile(self) -> None:
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            self.fail("required-profile", ".github/workflows/build.yml is missing")
            return
        body = strip_comments(workflow)
        for category in REQUIRED_CATEGORIES:
            marker = f"\n  {category}:"
            if marker not in body:
                self.fail("required-profile", f"required category '{category}' is absent from the required profile")
                continue
            # Present is not the same as reachable. The three literals below are the guards that
            # actually came back once, but any job level condition can hide a category just as well,
            # so the shape is rejected rather than the three spellings.
            if REQUIRED_JOB_CONDITION.search(self.job_section(body, category)):
                self.fail("required-profile",
                          f"required category '{category}' carries a job level condition; a required "
                          "category runs unconditionally or it is not required")
        for guard in ("refs/heads/master", "refs/heads/develop", "ViciOne.ServiceBus/ViciOne.ServiceBus"):
            if guard in body:
                self.fail("required-profile", f"an upstream repository or branch guard returned: {guard}")
        if CANONICAL_ENTRY_POINT not in body:
            self.fail("required-profile",
                      f"no required job calls {CANONICAL_ENTRY_POINT}, so nothing in this profile "
                      "verifies anything")

        # A job level condition was rejected while the trigger above it was never read. A path or
        # branch filter on the workflow itself removes every required job at once and for every change
        # outside the list, which is the same effect with a wider blast radius. The invariant this file
        # states is that a required job runs on every push and pull request, so the trigger is held to
        # it too.
        trigger = body.split("\njobs:", 1)[0]
        for keyword in ("paths:", "paths-ignore:", "branches-ignore:"):
            if re.search(rf"^\s+{re.escape(keyword)}", trigger, re.M):
                self.fail("required-profile",
                          f"the required profile filters its own trigger with '{keyword}'; a change outside that "
                          "list would run no required job at all")

    def check_required_steps_are_the_canonical_invocation(self) -> None:
        """A required job runs one exact command, and it is compared rather than parsed.

        Directive 0096 replaced the parser design: there is no general reader for arbitrary shell
        programs here, because there is nothing arbitrary left to read. A verifying job's step is
        exactly 'python3 tools/ci/verify.py --selection <name>' and the selection has to be the one the
        model gives that job. Everything a step could otherwise do - echo instead of the runner, a
        wrapper, a second command, a pipe, a redirection, a command substitution, '|| true' - produces
        a token vector that is not this one, so all of it is refused by the same comparison.

        The reader is still needed to say what "the command" is at all: a step is text until its line
        continuations are joined, which is how the RabbitMQ job once lost its --one-refusal-per-vhost
        and could not run while every expected token was still present.
        """
        body = self.read(".github/workflows/build.yml")
        if body is None:
            return
        try:
            model = verification_model.load(self.root)
        except verification_model.ModelError as error:
            self.fail("canonical-invocation", str(error))
            return

        job_of_selection = model.get("jobs") or {}
        try:
            jobs = verification_model.workflow_jobs(self.root)
        except verification_model.WorkflowShapeError as error:
            self.fail("canonical-invocation", f"the required workflow cannot be read: {error}")
            return

        for job, selection in sorted(job_of_selection.items()):
            section = jobs.get(job)
            if section is None:
                continue
            try:
                commands = [command for _, script in run_scripts(section)
                            for command in effective_commands(script)]
            except WorkflowScriptError as error:
                self.fail("canonical-invocation", f"job '{job}': {error}")
                continue

            if len(commands) != 1:
                self.fail("canonical-invocation",
                          f"job '{job}' runs {len(commands)} command(s); a verifying job runs exactly "
                          f"one: {' '.join(CANONICAL_INVOCATION)} {selection}")
                continue

            expected = [*CANONICAL_INVOCATION, selection]
            if commands[0] != expected:
                self.fail("canonical-invocation",
                          f"job '{job}' runs '{shlex.join(commands[0])}' and the model gives it the "
                          f"selection '{selection}', so its step has to be '{shlex.join(expected)}'")

    def check_no_step_masks_its_own_outcome(self) -> None:
        """No step of the required profile may turn a failure into a success.

        '|| true' after a required command is the shortest way to make a red gate green, and it leaves
        every other rule satisfied. The build and pack jobs legitimately chain commands, so this is
        checked for the masking itself rather than for the presence of an operator.
        """
        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            try:
                scripts = run_scripts(body)
            except WorkflowScriptError as error:
                self.fail("outcome-masking", f"{relative}: {error}")
                continue
            for line, script in scripts:
                for masking in masked_outcomes(script):
                    self.fail("outcome-masking",
                              f"{relative} line {line}: '{masking}' turns whatever ran before it into a "
                              "success, so the step reports green whatever happened")

    def check_no_command_begins_with_an_option(self) -> None:
        """A command that begins with an option is a line continuation that went missing.

        This is what made the required RabbitMQ job unexecutable: the backslash after its
        --evidence-dir line was gone, so the shell built two commands and the second one's program
        name was '--one-refusal-per-vhost'. Every token the other rules looked for was still there.
        """
        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            try:
                scripts = run_scripts(body)
            except WorkflowScriptError as error:
                self.fail("workflow-command", f"{relative}: {error}")
                continue
            for line, script in scripts:
                try:
                    commands = effective_commands(script)
                except WorkflowScriptError as error:
                    self.fail("workflow-command", f"{relative} line {line}: {error}")
                    continue
                for command in commands:
                    if command[0].startswith("-"):
                        self.fail("workflow-command",
                                  f"{relative} line {line}: the shell builds '{shlex.join(command)}' as a "
                                  "command of its own, so the line before it is missing its continuation "
                                  "and the option never reaches the program it was written for")

    def check_pack(self) -> None:
        workflow = self.read(".github/workflows/build.yml")
        if workflow is None:
            return
        body = strip_comments(workflow)
        pack = body.split("\n  pack:", 1)
        if len(pack) < 2:
            self.fail("pack", "the required profile has no pack job")
            return
        section = pack[1]
        for gate in REQUIRED_CATEGORIES:
            if gate == "pack":
                continue
            if f"- {gate}" not in section.split("steps:", 1)[0]:
                self.fail("pack", f"pack does not depend on the required gate '{gate}'")
        if "upload-artifact" not in section:
            self.fail("pack", "pack uploads no verifiable run artifact")
        if "sha256sum" not in section:
            self.fail("pack", "pack does not hash its packages")
        # "a package is there" has to mean "this run produced it". A local run proved the difference:
        # pack failed and the hash step was still satisfied by what an earlier run had left behind.
        if "rm -rf artifacts/packages" not in section:
            self.fail("pack", "pack does not empty its output directory, so a stale package satisfies its hash step")

    def check_no_publication(self) -> None:
        for relative in self.workflow_files():
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)
            # Matching the two command spellings was too narrow: a workflow publishes just as
            # effectively through an action, and neither 'nuget push' nor 'docker push' appears in
            # that case. The rule now covers the effect rather than one way of writing it.
            for pattern, detail in (
                (r"nuget\s+push", "pushes a package"),
                (r"api\.nuget\.org", "addresses the nuget.org publish endpoint"),
                (r"nuget\.org/api", "addresses the nuget.org publish endpoint"),
                (r"--api-key", "passes a publish credential"),
                (r"NUGET_API_KEY", "passes a publish credential"),
                (r"docker\s+push", "publishes a container image"),
                (r"ghcr\.io", "addresses a container registry"),
                (r"push-action", "uses a publishing action"),
                (r"^\s*push:\s*true", "enables publishing on an action"),
                (r"login-action", "authenticates against a registry"),
            ):
                if re.search(pattern, stripped, re.M):
                    self.fail("publication", f"{relative} {detail}: pattern '{pattern}'")

    def check_required_runner_and_sdk(self) -> None:
        """The required profile runs on one operating system and on the SDK that was approved.

        A windows-latest leg claimed support this product does not have and does not want. A floating
        '10.0.x' resolves to whatever the runner image ships, which global.json then rejects after the
        fact; the version is pinned to the approved one instead.
        """
        approved = self.read(APPROVED_SDK_FILE)
        version = None
        if approved is None:
            self.fail("required-runner", f"{APPROVED_SDK_FILE} is missing, so no SDK is approved")
        else:
            match = re.search(r'"version"\s*:\s*"([^"]+)"', approved)
            if match is None:
                self.fail("required-runner", f"{APPROVED_SDK_FILE} names no SDK version")
            else:
                version = match.group(1)

        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)

            for runner in re.findall(r"runs-on:\s*(\S+)", stripped):
                if runner != REQUIRED_RUNNER:
                    self.fail("required-runner",
                              f"{relative} runs a job on '{runner}'; this product builds and is supported "
                              f"on {REQUIRED_RUNNER} only")

            if re.search(r"windows|macos", stripped, re.I):
                self.fail("required-runner", f"{relative} still names a Windows or macOS runner")

            for declared in re.findall(r"dotnet-version:\s*'?\"?([^'\"\n]+)'?\"?", stripped):
                declared = declared.strip()
                if declared.startswith("${{"):
                    continue
                if version is not None and declared != version:
                    self.fail("required-runner",
                              f"{relative} asks for SDK '{declared}' while {APPROVED_SDK_FILE} approves "
                              f"'{version}'")

            for declared in re.findall(r"DOTNET_VERSION:\s*'?([^'\n]+)'?", stripped):
                declared = declared.strip().strip("'")
                if version is not None and declared != version:
                    self.fail("required-runner",
                              f"{relative} pins DOTNET_VERSION to '{declared}' while {APPROVED_SDK_FILE} "
                              f"approves '{version}'")

    def check_no_selector_without_a_job(self) -> None:
        """A selectable target has to have a job that runs it.

        A workflow that offers four targets and defines a job for one of them ends green for the other
        three on an inventory print alone. That is not a skipped gate, it is a gate that reports success
        without running.
        """
        for relative in sorted(self.workflow_files()):
            body = self.read(relative)
            if body is None:
                continue
            stripped = strip_comments(body)

            options = re.search(r"options:\s*\n((?:\s+-\s+\S+\n)+)", stripped)
            if options is None:
                continue

            targets = [line.strip().lstrip("- ").strip("'\"")
                       for line in options.group(1).splitlines() if line.strip()]
            for target in targets:
                if not re.search(rf"inputs\.target\s*==\s*['\"]{re.escape(target)}['\"]", stripped):
                    self.fail("selector-binding",
                              f"{relative} offers the target '{target}' but no job runs it, so selecting it "
                              "ends green without executing anything")

    def check_pack_depends_on_every_gate(self) -> None:
        """Pack may only run after every required gate, and may not resolve the graph on its own."""
        body = self.read(".github/workflows/build.yml")
        if body is None:
            return
        stripped = strip_comments(body)
        section = self.job_section(stripped, "pack")

        needs = re.search(r"needs:\s*\n((?:\s+-\s+\S+\n)+)", section)
        declared = ({line.strip().lstrip("- ").strip() for line in needs.group(1).splitlines() if line.strip()}
                    if needs else set())

        for gate in REQUIRED_CATEGORIES:
            if gate == "pack":
                continue
            if gate not in declared:
                self.fail("pack", f"pack does not depend on the required gate '{gate}'")

        if "--locked-mode" not in section:
            self.fail("pack", "the pack job does not restore in locked mode, so it resolves the graph unbound")
        if "--no-restore" not in section:
            self.fail("pack", "the pack job packs without --no-restore, so it restores a second time unbound")

    def check_every_dotnet_command_names_its_target(self) -> None:
        """A dotnet command in a required job has to say which solution it means.

        This repository holds two solutions at its root: the product one and the benchmark one. An
        unqualified 'dotnet restore --locked-mode' or 'dotnet build -c Release' does not fall back to
        one of them, it exits with MSB1011 and does nothing at all. The build job and the whole pack
        chain stood in the workflow that way, so both would have failed on the runner the first time
        anything triggered them.

        The rule is unconditional rather than conditional on the number of solutions: a job that names
        its target cannot become ambiguous by someone adding a second solution later.
        """
        for relative in self.workflow_files():
            workflow = self.read(relative)
            if workflow is None:
                continue
            for number, line in enumerate(workflow.splitlines(), 1):
                stripped = line.strip()
                if stripped.startswith("#"):
                    continue
                for verb in self.DOTNET_VERBS_THAT_NEED_A_TARGET:
                    if verb not in stripped:
                        continue
                    target = stripped.split(verb, 1)[1]
                    if any(token.endswith((".slnx", ".sln", ".csproj")) for token in target.split()):
                        continue
                    self.fail("dotnet-target",
                              f"{relative}:{number} runs '{verb}' without naming a solution or project; "
                              "this repository holds two solutions, so the command exits with MSB1011")

    def check_workflow_inputs_are_pinned(self) -> None:
        """Nothing a required run consumes may be a moving reference.

        A tag and a runner label are both names somebody else can repoint. The SDK is pinned, the
        images are pinned by digest, and an action pinned to v5 would have been the one input left
        where a change outside this repository changes what runs inside it. ubuntu-latest is the same
        thing for the machine underneath: it moves to the next LTS when GitHub decides, and the build
        that meets it is not the build that was verified.
        """
        for relative in self.workflow_files():
            workflow = self.read(relative)
            if workflow is None:
                continue

            for number, line in enumerate(workflow.splitlines(), 1):
                stripped = line.strip()
                if stripped.startswith("#"):
                    continue

                if stripped.startswith("- uses:") or stripped.startswith("uses:"):
                    reference = stripped.split("uses:", 1)[1].strip().split()[0]
                    if "@" not in reference or not re.fullmatch(r"[0-9a-f]{40}", reference.split("@", 1)[1]):
                        self.fail("moving-reference",
                                  f"{relative}:{number} uses '{reference}', which is a tag rather than a "
                                  "commit; whoever can move that tag decides what runs here")

                if stripped.startswith("runs-on:") and stripped.endswith("-latest"):
                    self.fail("moving-reference",
                              f"{relative}:{number} runs on '{stripped.split(':', 1)[1].strip()}', which "
                              "moves when GitHub repoints it; name the image this repository verified against")

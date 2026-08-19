#!/usr/bin/env python3
"""Self-tests for the CI policy validator.

Each test builds a minimal repository that satisfies every rule, applies exactly one mutation, and
asserts that the validator turns red for that mutation and only for it. A validator that cannot be
made to fail proves nothing, so the negative cases carry the weight here.

Standard library only, and no dependency on the real repository state: these tests must keep working
when the repository legitimately changes.
"""

from __future__ import annotations

import json
import re
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from policy_validator import Policy, effective_commands  # noqa: E402


NUGET_CONFIG = """\
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"""

COMPOSE = """\
services:
  rabbitmq:
    build:
      context: ./rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: ${VICIONE_SERVICEBUS_RMQ_USER:?run credentials are missing}
      RABBITMQ_DEFAULT_PASS: ${VICIONE_SERVICEBUS_RMQ_PASS:?run credentials are missing}
    ports:
      - "127.0.0.1::5672"
      - "127.0.0.1::15672"
  activemq:
    build:
      context: ./activemq
    environment:
      ACTIVEMQ_CONNECTION_USER: ${VICIONE_SERVICEBUS_AMQ_USER:?run credentials are missing}
      ACTIVEMQ_CONNECTION_PASSWORD: ${VICIONE_SERVICEBUS_AMQ_PASS:?run credentials are missing}
    ports:
      - "127.0.0.1::61616"
      - "127.0.0.1::8161"
"""

RABBIT_DOCKERFILE = """\
FROM rabbitmq:4.2-management@sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d924002773933ab881da49c073b85d
ADD --checksum=sha256:f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1 \\
    https://example.invalid/plugin.ez /opt/rabbitmq/plugins/plugin.ez
"""

ACTIVEMQ_DOCKERFILE = """\
FROM apache/activemq-classic:6.2.0@sha256:992bc29a8459f6772ff14d168a8b011c9cde7a769ec01d72788c9cc4e9229564
COPY activemq.xml /opt/apache-activemq/conf/activemq.xml
"""

LOCK = {
    "baseImages": {
        "rabbitmq": {"digest": "sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d924002773933ab881da49c073b85d"},
        "activemq": {"digest": "sha256:992bc29a8459f6772ff14d168a8b011c9cde7a769ec01d72788c9cc4e9229564"},
    },
    "plugins": {
        "rabbitmq_delayed_message_exchange": {
            "sha256": "f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1"
        }
    },
}

RABBITMQ_TEST_PROJECT = "tests/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests"

# The one fixture the fixture repository leans on indirectly. It runs: an anchor on a case that is
# never executed proves as little as an anchor on a comment.
ANCHOR_FIXTURE = "ViciOne.ServiceBus.RabbitMqTransport.Tests.Delivering_a_message"

APPROVED_SDK = "10.0.302"

GLOBAL_JSON = """\
{
  "sdk": {
    "version": "%s",
    "allowPrerelease": false,
    "rollForward": "disable"
  }
}
""" % APPROVED_SDK

# One list of what the fixture runs, so its workflow and its model cannot disagree by construction.
# That is the same rule the model exists to enforce, applied to the fixture that tests it.
BROKER_OF = {"activemq": "activemq", "rabbitmq": "rabbitmq", "sql-transport": "postgres",
             "entity-framework-core": "postgres"}

FIXTURE_RUNS = [
    ("analyzer", "analyzer", "tests/Analyzer.Tests/Analyzer.Tests.csproj"),
    ("core-unit", "core", "tests/Core.Tests/Core.Tests.csproj"),
    ("signalr", "signalr", "tests/SignalR.Tests/SignalR.Tests.csproj"),
    ("quartz", "quartz", "tests/Quartz.Tests/Quartz.Tests.csproj"),
    ("activemq", "activemq", "tests/ActiveMq.Tests/ActiveMq.Tests.csproj"),
    ("sql-transport", "sql-transport", "tests/Sql.Tests/Sql.Tests.csproj"),
    ("benchmarks", "benchmarks", "tests/Benchmarks.Tests/Benchmarks.Tests.csproj"),
    ("rabbitmq", "rabbitmq", f"{RABBITMQ_TEST_PROJECT}/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj"),
    ("entity-framework", "entity-framework-core", "tests/Ef.Tests/Ef.Tests.csproj"),
]


def fixture_model() -> dict:
    capabilities = []
    for index, (job, category, project) in enumerate(FIXTURE_RUNS):
        capabilities.append({
            "id": f"capability-{category}",
            "class": "PINNED_FIXTURE_REQUIRED_RUN" if category in BROKER_OF else "LOCAL_REQUIRED_RUN",
            "sourceProjects": ["src/ViciOne.ServiceBus"] if index == 0 else [],
            "testProjects": [project.rsplit("/", 1)[0]],
            "supportProjects": [],
            "toolProjects": [],
            "runs": [{
                "job": job,
                "category": category,
                "project": project,
                "testProjectDirectory": RABBITMQ_TEST_PROJECT,
                "minimumExecutedCases": 1,
                "budgetSeconds": 60,
                "brokers": [BROKER_OF[category]] if category in BROKER_OF else [],
                "allowBrokerOutage": None,
                "oneRefusalPerVhost": None,
                "expectedIdentities": None,
                "explicitAttributeCount": 1,
                "notExecuted": [
                    {
                        "identity": "ViciOne.ServiceBus.RabbitMqTransport.Tests.Watching_by_hand.Should_be_watched_by_a_human",
                        "fixture": "Watching_by_hand",
                        "test": "Should_be_watched_by_a_human",
                        "mechanism": "EXPLICIT",
                        "dueness": "NOT_DUE_MANUAL_OBSERVATION",
                        "reason": "Asserts nothing a gate could evaluate.",
                    }
                ] if category == "rabbitmq" else [],
            }],
        })

    capabilities.append({
        "id": "capability-analyzers-source", "class": "LOCAL_REQUIRED_RUN",
        "sourceProjects": ["src/ViciOne.ServiceBus.Analyzers"], "testProjects": [],
        "supportProjects": [], "toolProjects": [],
        # Through the one fixture capability whose test project really holds sources, so the anchor
        # check has something to find. The rest of the fixture projects are bare stubs.
        "verifiedThroughCapability": "capability-rabbitmq",
        "testAnchors": [{"category": "rabbitmq", "fixture": ANCHOR_FIXTURE}],
    })

    return {
        "schemaVersion": 1,
        "kind": "SERVICEBUS_VERIFICATION_MODEL",
        "selections": {category: {"members": [category]} for _, category, _ in FIXTURE_RUNS},
        "jobs": dict(JOB_SELECTION),
        "verificationClasses": {
            "LOCAL_REQUIRED_RUN": "runs locally in the required profile",
            "PINNED_FIXTURE_REQUIRED_RUN": "runs against a pinned fixture in the required profile",
            "REAL_EPHEMERAL_CLOUD": "needs a real cloud resource and is not executed here",
        },
        "capabilities": capabilities,
    }


# Which required job of the fixture verifies which selection. One line per job, and nothing about
# categories, projects or brokers: that is the model's business now.
JOB_SELECTION = {job: category for job, category, _ in FIXTURE_RUNS}


def runner_step(selection: str) -> str:
    """The one canonical invocation, as one line.

    Written once here because the workflow of the fixture and the cases that mutate it have to say the
    same thing; two spellings of it would let a case mutate a step the workflow never carried.
    """
    return f"python3 tools/ci/verify.py --selection {selection}"


JOB = """\
  %s:
    runs-on: ubuntu-24.04
    steps:
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: '{sdk}'
      - run: %s
"""

BUILD_WORKFLOW = ("""\
name: Required CI

on:
  push:
    branches:
      - '**'
  pull_request:
  workflow_dispatch:

env:
  DOTNET_VERSION: '{sdk}'

jobs:
"""
    + JOB % ("policy", "python3 -m unittest discover -s tools/ci -p 'test_*.py'")
    + JOB % ("build", "dotnet build ViciOne.ServiceBus.slnx -c Release")
    + "".join(JOB % (job, runner_step(category)) for job, category, _ in FIXTURE_RUNS)
    + """\
  pack:
    runs-on: ubuntu-24.04
    needs:
      - policy
      - build
      - analyzer
      - core-unit
      - signalr
      - quartz
      - activemq
      - sql-transport
      - benchmarks
      - rabbitmq
      - entity-framework
    steps:
      - uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: '{sdk}'
      - run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode
      - run: rm -rf artifacts/packages
      - run: dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore -o artifacts/packages
      - run: sha256sum artifacts/packages/*.nupkg
      - uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
""").format(sdk=APPROVED_SDK)

SHIPPED = "## Release 1.0\n\n### New Rules\nRule ID | Category | Severity | Notes\n"
UNSHIPPED = ""

SET_UP_FIXTURE = """\
using NUnit.Framework;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    [SetUpFixture]
    public class RabbitMqTestSetUpFixture
    {
        [OneTimeSetUp]
        public void Before_any()
        {
            RequireRunScopedCredentials();
        }

        static void RequireRunScopedCredentials()
        {
            foreach (var variable in new[]
                     {
                         RabbitMqTestHarness.UsernameVariable, RabbitMqTestHarness.PasswordVariable,
                         RabbitMqTestHarness.HostVariable, RabbitMqTestHarness.PortVariable,
                         RabbitMqTestHarness.ManagementPortVariable
                     })
            {
                Assert.Fail(variable);
            }
        }
    }
}
"""

MANUAL_SPEC = """\
using NUnit.Framework;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    [TestFixture]
    public class Watching_by_hand
    {
        [Test]
        [Explicit]
        public void Should_be_watched_by_a_human()
        {
        }
    }
}
"""

REQUIRED_SPEC = """\
using NUnit.Framework;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    [TestFixture]
    public class Delivering_a_message
    {
        [Test]
        public void Should_arrive()
        {
        }
    }
}
"""



RABBITMQ_PROJECT_FILE = f"{RABBITMQ_TEST_PROJECT}/ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj"

# The canonical multi line form of the step whose missing continuation made the required RabbitMQ job
# unexecutable. Written out rather than generated, because what these cases mutate is its text.
CONTINUED_RABBITMQ_STEP = f"""\
      - run: |
          python3 tools/ci/run_broker_category.py \\
            --broker rabbitmq \\
            --category rabbitmq \\
            --project {RABBITMQ_PROJECT_FILE} \\
            --evidence-dir artifacts/required/rabbitmq \\
            --one-refusal-per-vhost 'test-exclusive-*'
"""


class PolicyTestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)

        infra = self.root / "build/test-infrastructure"
        (infra / "rabbitmq").mkdir(parents=True)
        (infra / "activemq").mkdir(parents=True)
        (infra / "compose.yaml").write_text(COMPOSE, encoding="utf-8")
        (infra / "images.lock.json").write_text(json.dumps(LOCK, indent=2), encoding="utf-8")
        (infra / "rabbitmq/Dockerfile").write_text(RABBIT_DOCKERFILE, encoding="utf-8")
        (infra / "activemq/Dockerfile").write_text(ACTIVEMQ_DOCKERFILE, encoding="utf-8")
        (self.root / "build/verification").mkdir(parents=True, exist_ok=True)
        (self.root / "build/verification/VERIFICATION_MODEL.json").write_text(
            json.dumps(fixture_model(), indent=2), encoding="utf-8"
        )

        specs = self.root / RABBITMQ_TEST_PROJECT
        specs.mkdir(parents=True)
        (specs / "RabbitMqTestSetUpFixture.cs").write_text(SET_UP_FIXTURE, encoding="utf-8")
        (specs / "ManualWatch_Specs.cs").write_text(MANUAL_SPEC, encoding="utf-8")
        (specs / "Delivery_Specs.cs").write_text(REQUIRED_SPEC, encoding="utf-8")

        workflows = self.root / ".github/workflows"
        workflows.mkdir(parents=True)
        (workflows / "build.yml").write_text(BUILD_WORKFLOW, encoding="utf-8")

        (self.root / "NuGet.config").write_text(NUGET_CONFIG, encoding="utf-8")
        (self.root / "Directory.Build.targets").write_text(
            '<Project>\n  <Target Name="AssertSomething" BeforeTargets="BeforeBuild">\n'
            '    <Error Text="a gate that fires" Condition=" \'$(Broken)\' == \'true\' " />\n'
            '  </Target>\n</Project>\n', encoding="utf-8")
        self.write_solution()
        (self.root / "global.json").write_text(GLOBAL_JSON, encoding="utf-8")


        # The matrix names projects, and a project that is not there is a finding of its own, so the
        # fixture carries the ones it claims.
        (self.root / "src/ViciOne.ServiceBus").mkdir(parents=True)
        (self.root / "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj").write_text(
            "<Project />\n", encoding="utf-8")
        (specs / "ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj").write_text(
            "<Project />\n", encoding="utf-8")

        analyzers = self.root / "src/ViciOne.ServiceBus.Analyzers"
        analyzers.mkdir(parents=True)
        (analyzers / "AnalyzerReleases.Shipped.md").write_text(SHIPPED, encoding="utf-8")
        (analyzers / "AnalyzerReleases.Unshipped.md").write_text(UNSHIPPED, encoding="utf-8")
        (analyzers / "ViciOne.ServiceBus.Analyzers.csproj").write_text("<Project />\n", encoding="utf-8")

        # Every project the lock file rule sees has to carry one, or that rule fires instead of the
        # one a case is about.
        for _, _, project in FIXTURE_RUNS:
            target = self.root / project
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists():
                target.write_text("<Project />\n", encoding="utf-8")

        for project in sorted(self.root.rglob("*.csproj")):
            (project.parent / "packages.lock.json").write_text("{}\n", encoding="utf-8")

    def write_solution(self) -> None:
        """Names every project the fixture holds, so the orphan rule has nothing to complain about."""
        projects = sorted(p.relative_to(self.root).as_posix() for p in self.root.rglob("*.csproj"))
        body = "<Solution>\n" + "".join(f'  <Project Path="{path}" />\n' for path in projects) + "</Solution>\n"
        (self.root / "ViciOne.ServiceBus.slnx").write_text(body, encoding="utf-8")

    def failures(self, regenerate_solution: bool = True) -> list[str]:
        """Every finding of the validator against this fixture.

        The solution is regenerated first so that a case which adds a project does not also trip the
        orphan rule. A case about the solution file itself asks for that to be left alone, or its own
        mutation would be written over before the validator ever saw it.
        """
        if regenerate_solution:
            self.write_solution()
        policy = Policy(self.root)
        policy.run()
        return policy.failures

    def assert_rejected(self, rule: str, regenerate_solution: bool = True) -> None:
        found = self.failures(regenerate_solution)
        self.assertTrue(found, f"the mutation was accepted; rule '{rule}' never fired")
        self.assertTrue(
            any(failure.startswith(rule) for failure in found),
            f"expected rule '{rule}' to fire, got: {found}",
        )

    def compose(self) -> Path:
        return self.root / "build/test-infrastructure/compose.yaml"

    def inventory(self) -> Path:
        return self.root / "build/verification/VERIFICATION_MODEL.json"

    def matrix(self) -> Path:
        return self.root / "build/verification/VERIFICATION_MODEL.json"

    def workflow(self) -> Path:
        return self.root / ".github/workflows/build.yml"

    def inventory(self) -> Path:
        return self.root / "build/verification/VERIFICATION_MODEL.json"

    def rabbitmq_spec(self, name: str) -> Path:
        return self.root / RABBITMQ_TEST_PROJECT / name

    # -- positive ------------------------------------------------------------------------------

    def test_accepts_a_conforming_repository(self) -> None:
        self.assertEqual([], self.failures())

    # -- the one command a required job runs ------------------------------------------------------
    #
    # Directive 0096 replaced the parser design: a verifying job's step is compared against one allowed
    # shape rather than read as a shell program. Both counterexamples the Lead executed are here - echo
    # instead of the runner, and '|| true' after it - together with the shapes that would have walked
    # past a token check.

    def rewrite_step(self, job: str, script: str) -> None:
        """Replaces the canonical step of one job, and proves the anchor was there."""
        one_line = "      - run: " + runner_step(JOB_SELECTION[job]) + "\n"
        body = self.workflow().read_text(encoding="utf-8")
        self.assertIn(one_line, body, f"the fixture no longer carries the step of job '{job}'")
        self.workflow().write_text(body.replace(one_line, script), encoding="utf-8")

    def test_accepts_the_canonical_invocation(self) -> None:
        self.assertEqual([], self.failures())

    def test_rejects_echo_instead_of_the_runner(self) -> None:
        self.rewrite_step("signalr", "      - run: echo tools/ci/verify.py --selection signalr\n")

        self.assert_rejected("canonical-invocation")

    def test_rejects_a_masked_outcome_after_the_runner(self) -> None:
        self.rewrite_step("signalr",
                          "      - run: python3 tools/ci/verify.py --selection signalr || true\n")

        self.assert_rejected("outcome-masking")

    def test_rejects_a_masked_outcome_anywhere_in_the_profile(self) -> None:
        """The build job chains commands legitimately, so the masking itself is what is refused."""
        body = self.workflow().read_text(encoding="utf-8")
        self.workflow().write_text(
            body.replace("      - run: dotnet build ViciOne.ServiceBus.slnx -c Release\n",
                         "      - run: dotnet build ViciOne.ServiceBus.slnx -c Release || true\n"),
            encoding="utf-8")

        self.assert_rejected("outcome-masking")

    def test_rejects_a_wrapper_around_the_runner(self) -> None:
        self.rewrite_step("quartz",
                          "      - run: sh -c 'python3 tools/ci/verify.py --selection quartz'\n")

        self.assert_rejected("canonical-invocation")

    def test_rejects_a_redirection_that_hides_what_it_said(self) -> None:
        self.rewrite_step("quartz",
                          "      - run: python3 tools/ci/verify.py --selection quartz > /dev/null\n")

        self.assert_rejected("canonical-invocation")

    def test_rejects_a_second_command_in_the_same_step(self) -> None:
        self.rewrite_step("quartz",
                          "      - run: python3 tools/ci/verify.py --selection quartz; echo done\n")

        self.assert_rejected("canonical-invocation")

    def test_rejects_a_selection_the_model_does_not_give_that_job(self) -> None:
        """A job that verifies a narrower scope than the model says it does."""
        self.rewrite_step("core-unit", "      - run: python3 tools/ci/verify.py --selection signalr\n")

        self.assert_rejected("canonical-invocation")

    def test_accepts_the_canonical_call_written_across_two_lines(self) -> None:
        self.rewrite_step("rabbitmq",
                          "      - run: |\n"
                          "          python3 tools/ci/verify.py \\\n"
                          "            --selection rabbitmq\n")

        self.assertEqual([], self.failures())

    def test_rejects_a_continuation_that_went_missing(self) -> None:
        """The defect that made the required RabbitMQ job unexecutable, in its new shape."""
        self.rewrite_step("rabbitmq",
                          "      - run: |\n"
                          "          python3 tools/ci/verify.py\n"
                          "            --selection rabbitmq\n")

        self.assert_rejected("workflow-command")

    def test_rejects_a_step_with_an_unbalanced_quote(self) -> None:
        self.rewrite_step("quartz", "      - run: python3 tools/ci/verify.py --selection 'quartz\n")

        self.assert_rejected("canonical-invocation")

    def test_rejects_a_folded_script_the_reader_cannot_reconstruct(self) -> None:
        """A folded scalar joins its lines by rules this reader does not implement, so it is refused
        rather than read as something it may not be."""
        self.rewrite_step("quartz",
                          "      - run: >\n          python3 tools/ci/verify.py --selection quartz\n")

        self.assert_rejected("canonical-invocation")

    # -- rules that had no case at all ------------------------------------------------------------
    #
    # Found by the meta case at the end of this file, not by a reviewer. One of these had a case and
    # lost it when this file was restructured: a suite stays green when a test disappears, so nothing
    # said so, and the rule went back to being one nobody had proven can report.

    def test_rejects_a_run_without_an_executed_floor(self) -> None:
        """This rule read a top level 'categories' object the model has not had since it replaced the
        two files before it, so its loop ran over nothing and it passed for every repository."""
        model = fixture_model()
        for capability in model["capabilities"]:
            for run in capability.get("runs", []):
                run.pop("minimumExecutedCases", None)
        self.inventory().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("executed-floor")

    def test_rejects_a_project_without_a_lock_file(self) -> None:
        """Locked mode alone does not carry this: a restore with --locked-mode and no lock file writes
        one and succeeds, so a deleted lock file would reopen the package graph silently."""
        for lock in self.root.rglob("packages.lock.json"):
            lock.unlink()

        self.assert_rejected("restore-lock")

    def test_rejects_a_solution_that_names_a_project_which_is_not_there(self) -> None:
        body = (self.root / "ViciOne.ServiceBus.slnx").read_text(encoding="utf-8")
        (self.root / "ViciOne.ServiceBus.slnx").write_text(
            body.replace("</Solution>", '  <Project Path="src/Gone/Gone.csproj" />\n</Solution>'),
            encoding="utf-8")

        self.assert_rejected("solution", regenerate_solution=False)

    def test_rejects_a_solution_that_is_not_parsable(self) -> None:
        (self.root / "ViciOne.ServiceBus.slnx").write_text("<Solution>", encoding="utf-8")

        self.assert_rejected("solution", regenerate_solution=False)

    # -- who may leave the product's target framework ---------------------------------------------
    #
    # The exception used to be a property a project set about itself, so any project could hand itself
    # one: a copy of an analyzer project carried the marker with it and the central rule agreed. It is
    # granted by exact repository relative path now, in Directory.Build.targets and here.

    ROSLYN_PROJECT = "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj"
    PACKAGE_PROJECT = "src/ViciOne.ServiceBus.Analyzers.Package/ViciOne.ServiceBus.Analyzers.Package.csproj"

    def add_project(self, relative: str, body: str) -> None:
        """A project of the fixture repository, in a solution, with a lock file, so only this case's
        mutation is what a rule can find."""
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f'<Project Sdk="Microsoft.NET.Sdk">\n{body}</Project>\n', encoding="utf-8")
        (path.parent / "packages.lock.json").write_text("{}\n", encoding="utf-8")
        directory = relative.rsplit("/", 1)[0]
        model = json.loads(self.inventory().read_text(encoding="utf-8"))
        classified = {project for capability in model["capabilities"]
                      for key in ("sourceProjects", "testProjects", "supportProjects", "toolProjects")
                      for project in capability.get(key, [])}
        if directory not in classified:
            model["capabilities"][0].setdefault("supportProjects", []).append(directory)
            self.inventory().write_text(json.dumps(model, indent=2), encoding="utf-8")

    def test_accepts_the_two_roslyn_components_on_netstandard(self) -> None:
        self.add_project(self.ROSLYN_PROJECT,
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <IsRoslynComponent>true</IsRoslynComponent>\n"
                         "  </PropertyGroup>\n")

        self.assertEqual([], self.failures())

    def test_rejects_a_fourth_project_on_netstandard(self) -> None:
        self.add_project("src/ViciOne.ServiceBus.Somewhere/ViciOne.ServiceBus.Somewhere.csproj",
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    def test_rejects_a_copy_of_an_analyzer_project_that_carries_the_marker(self) -> None:
        """A copy is a new project, and it gets no exception by carrying the marker of an old one."""
        self.add_project("src/ViciOne.ServiceBus.Analyzers.Copy/ViciOne.ServiceBus.Analyzers.Copy.csproj",
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <IsRoslynComponent>true</IsRoslynComponent>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    def test_rejects_a_renamed_analyzer_project(self) -> None:
        """The path is the identity, so moving the project is losing the exception."""
        self.add_project("src/Analyzers/ViciOne.ServiceBus.Analyzers.csproj",
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <IsRoslynComponent>true</IsRoslynComponent>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    def test_rejects_the_retired_self_marker(self) -> None:
        self.add_project(self.PACKAGE_PROJECT,
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <ViciOneAnalyzerPackageSurface>true</ViciOneAnalyzerPackageSurface>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    def test_rejects_the_older_self_marker_too(self) -> None:
        self.add_project(self.PACKAGE_PROJECT,
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <ViciOneCompilerHost>true</ViciOneCompilerHost>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    def test_rejects_a_compiler_setting_on_the_project_that_compiles_nothing(self) -> None:
        self.add_project(self.PACKAGE_PROJECT,
                         "  <PropertyGroup>\n"
                         "    <TargetFramework>netstandard2.0</TargetFramework>\n"
                         "    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n"
                         "    <LangVersion>14.0</LangVersion>\n"
                         "  </PropertyGroup>\n")

        self.assert_rejected("framework-exception")

    # -- the central build contract a project may not leave ---------------------------------------
    #
    # This rule searched exact XML text. Three executed counterexamples walked past it: RestoreLockedMode
    # written as False, a conditional ImportDirectoryBuildTargets and a conditional
    # DirectoryBuildTargetsPath redirect. What is compared now is a parsed property name, so a capital
    # letter, an attribute, whitespace and the value itself no longer decide whether it is seen.

    def project_with(self, body: str, name: str = "ViciOne.ServiceBus.RabbitMqTransport.Tests.csproj") -> None:
        (self.root / RABBITMQ_TEST_PROJECT / name).write_text(
            f"<Project Sdk=\"Microsoft.NET.Sdk\">\n{body}</Project>\n", encoding="utf-8")

    def test_rejects_a_locked_mode_escape_written_in_another_case(self) -> None:
        self.project_with(
            "  <PropertyGroup>\n"
            "    <RestoreLockedMode>False</RestoreLockedMode>\n"
            "    <RestoreLockedModeFromCommandLine>true</RestoreLockedModeFromCommandLine>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_a_project_that_hands_itself_the_command_line_exception(self) -> None:
        """The flag the lock gate reads to tell a documented update apart from an escape. A project
        that sets it has granted itself the exception the command line exists to make visible."""
        self.project_with(
            "  <PropertyGroup>\n"
            "    <RestoreLockedModeFromCommandLine>true</RestoreLockedModeFromCommandLine>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_a_conditional_import_of_the_central_targets(self) -> None:
        self.project_with(
            "  <PropertyGroup>\n"
            "    <ImportDirectoryBuildTargets Condition=\" '$(OS)' != 'Windows_NT' \">False"
            "</ImportDirectoryBuildTargets>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_a_conditional_redirect_of_the_central_targets(self) -> None:
        self.project_with(
            "  <PropertyGroup>\n"
            "    <DirectoryBuildTargetsPath Condition=\" '$(Configuration)' == 'Release' \">"
            "build/Elsewhere.targets</DirectoryBuildTargetsPath>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_a_hook_placed_before_the_common_targets(self) -> None:
        self.project_with(
            "  <PropertyGroup>\n"
            "    <CustomBeforeMicrosoftCommonTargets>build/Elsewhere.targets"
            "</CustomBeforeMicrosoftCommonTargets>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_a_hook_placed_after_the_common_targets(self) -> None:
        self.project_with(
            "  <PropertyGroup>\n"
            "    <CustomAfterMicrosoftCommonTargets>build/Elsewhere.targets"
            "</CustomAfterMicrosoftCommonTargets>\n"
            "  </PropertyGroup>\n")

        self.assert_rejected("central-contract")

    def test_rejects_the_property_when_it_hides_inside_a_choose(self) -> None:
        """A PropertyGroup stands wherever MSBuild allows one. Reading only the top level would miss
        exactly the places a property is put to avoid being seen."""
        self.project_with(
            "  <Choose>\n    <When Condition=\" '$(Configuration)' == 'Release' \">\n"
            "      <PropertyGroup>\n"
            "        <RestoreLockedMode>false</RestoreLockedMode>\n"
            "      </PropertyGroup>\n"
            "    </When>\n  </Choose>\n")

        self.assert_rejected("central-contract")

    def test_rejects_the_property_when_it_hides_inside_a_target(self) -> None:
        self.project_with(
            "  <Target Name=\"Sneak\" BeforeTargets=\"BeforeBuild\">\n"
            "    <PropertyGroup>\n"
            "      <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>\n"
            "    </PropertyGroup>\n"
            "  </Target>\n")

        self.assert_rejected("central-contract")

    def test_rejects_the_property_in_a_props_file_the_project_imports(self) -> None:
        """An import is one line away, and the property has the same effect from there."""
        (self.root / RABBITMQ_TEST_PROJECT / "Local.props").write_text(
            "<Project>\n  <PropertyGroup>\n"
            "    <RestoreLockedMode>false</RestoreLockedMode>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")
        self.project_with("  <Import Project=\"Local.props\" />\n")

        self.assert_rejected("central-contract")

    def test_rejects_the_property_in_a_directory_build_props_further_down_the_tree(self) -> None:
        """The two root files are the contract. One added below them changes it for a folder."""
        (self.root / RABBITMQ_TEST_PROJECT / "Directory.Build.props").write_text(
            "<Project>\n  <PropertyGroup>\n"
            "    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")

        self.assert_rejected("central-contract")

    def test_accepts_the_reserved_properties_in_the_root_contract_itself(self) -> None:
        """The root files are where these belong. A rule that rejected them there would be a rule
        against the contract rather than for it."""
        (self.root / "Directory.Build.props").write_text(
            "<Project>\n  <PropertyGroup>\n"
            "    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>\n"
            "    <RestoreLockedMode>true</RestoreLockedMode>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")

        self.assertEqual([], self.failures())

    def test_accepts_a_project_that_names_none_of_them(self) -> None:
        self.project_with("  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n")

        self.assertEqual([], self.failures())

    # -- broker images -------------------------------------------------------------------------

    def test_rejects_returning_rabbitmq_image(self) -> None:
        self.compose().write_text(COMPOSE.replace("build:\n      context: ./rabbitmq",
                                                  "image: vicione-servicebus/rabbitmq:latest"), encoding="utf-8")
        self.assert_rejected("broker-image")

    def test_rejects_returning_activemq_image(self) -> None:
        self.compose().write_text(COMPOSE.replace("build:\n      context: ./activemq",
                                                  "image: vicione-servicebus/activemq:latest"), encoding="utf-8")
        self.assert_rejected("broker-image")

    def test_accepts_a_forbidden_image_named_only_in_a_comment(self) -> None:
        self.compose().write_text("# once referenced vicione-servicebus/rabbitmq:latest\n" + COMPOSE, encoding="utf-8")
        self.assertEqual([], self.failures())

    # -- pinning -------------------------------------------------------------------------------

    def test_rejects_missing_base_digest(self) -> None:
        path = self.root / "build/test-infrastructure/rabbitmq/Dockerfile"
        path.write_text(RABBIT_DOCKERFILE.replace(
            "rabbitmq:4.2-management@sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d924002773933ab881da49c073b85d",
            "rabbitmq:4.2-management"), encoding="utf-8")
        self.assert_rejected("pinning")

    def test_rejects_moving_latest_tag(self) -> None:
        path = self.root / "build/test-infrastructure/activemq/Dockerfile"
        path.write_text(ACTIVEMQ_DOCKERFILE.replace(
            "apache/activemq-classic:6.2.0@sha256:992bc29a8459f6772ff14d168a8b011c9cde7a769ec01d72788c9cc4e9229564",
            "apache/activemq-classic:latest"), encoding="utf-8")
        self.assert_rejected("pinning")

    def test_rejects_missing_plugin_checksum(self) -> None:
        path = self.root / "build/test-infrastructure/rabbitmq/Dockerfile"
        path.write_text(RABBIT_DOCKERFILE.replace(
            "--checksum=sha256:f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1 \\\n    ", ""),
            encoding="utf-8")
        self.assert_rejected("pinning")

    def test_rejects_lock_without_plugin_checksum(self) -> None:
        lock = json.loads(json.dumps(LOCK))
        lock["plugins"]["rabbitmq_delayed_message_exchange"]["sha256"] = "not-a-checksum"
        (self.root / "build/test-infrastructure/images.lock.json").write_text(json.dumps(lock), encoding="utf-8")
        self.assert_rejected("pinning")

    # -- host binding, required by lead directives 0005 and 0007 --------------------------------

    def test_rejects_bare_host_port_mapping(self) -> None:
        self.compose().write_text(COMPOSE.replace('"127.0.0.1::5672"', '"5672:5672"'), encoding="utf-8")
        self.assert_rejected("host-binding")

    def test_rejects_binding_to_all_interfaces(self) -> None:
        self.compose().write_text(COMPOSE.replace('"127.0.0.1::15672"', '"0.0.0.0:15672:15672"'), encoding="utf-8")
        self.assert_rejected("host-binding")

    def test_rejects_bare_mapping_on_the_jolokia_port(self) -> None:
        self.compose().write_text(COMPOSE.replace('"127.0.0.1::8161"', '"8161:8161"'), encoding="utf-8")
        self.assert_rejected("host-binding")

    def test_rejects_a_fixed_loopback_host_port(self) -> None:
        # A fixed port collides with whatever already listens on the developer machine; the whole
        # point of directive 0007 is that Docker assigns the host port per run.
        self.compose().write_text(COMPOSE.replace('"127.0.0.1::5672"', '"127.0.0.1:5672:5672"'), encoding="utf-8")
        self.assert_rejected("host-binding")

    # -- canonical runner and masking, required by lead directives 0007 and 0009 -----------------

    def test_rejects_a_job_that_starts_the_fixture_itself(self) -> None:
        """The fixture belongs to the runner that resolves its ephemeral ports. A job that starts one
        has to guess them, which is how fourteen green tests once measured a foreign broker."""
        step = "      - run: " + runner_step("rabbitmq") + "\n"
        self.workflow().write_text(
            BUILD_WORKFLOW.replace(step, "      - run: docker compose up -d rabbitmq\n" + step),
            encoding="utf-8")

        self.assert_rejected("canonical-runner")

    # -- hardcoded broker endpoints in specs ---------------------------------------------------

    def spec(self, name: str, body: str) -> Path:
        directory = self.root / "tests/Transports/ViciOne.ServiceBus.ActiveMqTransport.Tests"
        directory.mkdir(parents=True, exist_ok=True)
        path = directory / name
        path.write_text(body, encoding="utf-8")
        return path

    def test_rejects_a_spec_that_hardcodes_the_openwire_port(self) -> None:
        self.spec("Bound_Specs.cs", 'var address = new Uri("activemq://localhost:61616");\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_a_spec_that_hardcodes_the_fixed_artemis_port(self) -> None:
        self.spec("Artemis_Specs.cs", 'var address = new Uri("amqp://localhost:61618");\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_a_spec_that_hardcodes_a_loopback_broker_address(self) -> None:
        self.spec("Loopback_Specs.cs", 'var address = new Uri("rabbitmq://127.0.0.1:5672/test");\n')
        self.assert_rejected("test-endpoint")

    def test_accepts_an_address_spec_using_a_fictional_host(self) -> None:
        # Address parsing specs carry such literals as data. They open no connection and therefore
        # cannot reach a broker that happens to listen on the developer machine.
        self.spec("Address_Specs.cs", 'var address = new Uri("rabbitmq://remote-host:5672/input-queue");\n')
        self.assertEqual([], self.failures())

    def test_accepts_a_hardcoded_port_named_only_in_a_comment(self) -> None:
        self.spec("Commented_Specs.cs", '        // once used activemq://localhost:61616 before it was pinned\n')
        self.assertEqual([], self.failures())

    def test_rejects_a_spec_that_hardcodes_the_local_sql_server_endpoint(self) -> None:
        self.spec("Db_Specs.cs", 'var c = "Server=tcp:localhost,1433;User ID=sa;";\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_a_spec_that_hardcodes_the_local_postgres_endpoint(self) -> None:
        self.spec("Pg_Specs.cs", 'var c = "host=localhost;port=5432;database=x;";\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_the_well_known_database_secret(self) -> None:
        self.spec("Secret_Specs.cs", 'var c = "user id=postgres;password=Password12!;";\n')
        self.assert_rejected("test-endpoint")

    def test_accepts_a_database_endpoint_that_is_commented_out(self) -> None:
        # C# comments describe the shape that used to be there; they open no connection.
        self.spec("Commented_Db_Specs.cs", '        // builder.UseNpgsql("host=localhost;port=5432;password=Password12!;");\n')
        self.assertEqual([], self.failures())

    def test_accepts_the_run_scoped_database_helper_itself(self) -> None:
        self.spec("RunScopedDatabase.cs", 'var fallback = "Server=tcp:localhost,1433;";\n')
        self.assertEqual([], self.failures())

    def test_accepts_the_run_scoped_broker_helper_itself(self) -> None:
        self.spec("RunScopedBroker.cs", 'var fallback = new Uri("activemq://localhost:61616");\n')
        self.assertEqual([], self.failures())

    def test_rejects_effective_known_credentials(self) -> None:
        self.compose().write_text(
            COMPOSE.replace("${VICIONE_SERVICEBUS_AMQ_PASS:?run credentials are missing}", "admin"),
            encoding="utf-8")
        self.assert_rejected("known-credentials")

    def test_rejects_filtering_a_category_out_of_the_required_path(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--selection rabbitmq", '--selection rabbitmq -- --filter "Category!=Flaky"'),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    # The six mutations below all survived the first version of these rules. They are the reason the
    # rules now match the switch and the shape rather than one spelling of one predicate.

    def test_rejects_a_single_quoted_category_filter(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--selection rabbitmq", "--selection rabbitmq -- --filter 'Category!=Flaky'"),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    def test_rejects_a_fully_qualified_name_filter(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--selection rabbitmq",
                                   '--selection rabbitmq -- --filter "FullyQualifiedName!~KillSwitch"'),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    def test_rejects_an_nunit_selector(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--selection rabbitmq",
                                   '--selection rabbitmq -- -- NUnit.Where="cat != Flaky"'),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    def test_rejects_ignoring_a_required_case(self) -> None:
        self.rabbitmq_spec("Delivery_Specs.cs").write_text(
            REQUIRED_SPEC.replace("        [Test]", '        [Test]\n        [Ignore("temporarily disabled")]'),
            encoding="utf-8")
        self.assert_rejected("test-exclusion")

    def test_rejects_a_new_explicit_case_that_is_not_inventoried(self) -> None:
        self.rabbitmq_spec("Delivery_Specs.cs").write_text(
            REQUIRED_SPEC.replace("        [Test]", "        [Test]\n        [Explicit]"),
            encoding="utf-8")
        self.assert_rejected("test-exclusion")

    def test_rejects_inventorying_a_due_case_as_not_executed(self) -> None:
        self.with_case(dueness="DUE_OPEN_DEFECT")

        self.assert_rejected("test-exclusion")

    def with_case(self, **changes: object) -> None:
        """Rewrite the single inventoried case of the fixture repository."""
        model = fixture_model()
        run = next(r for capability in model["capabilities"] for r in capability.get("runs", [])
                   if r["category"] == "rabbitmq")
        case = run["notExecuted"][0]
        for key, value in changes.items():
            if value is None:
                case.pop(key, None)
            else:
                case[key] = value
        self.inventory().write_text(json.dumps(model, indent=2), encoding="utf-8")

    def test_accepts_a_namespaced_full_identity(self) -> None:
        # The baseline entry already carries one; stating it as its own case keeps the positive side
        # of the rule visible next to the four rejections.
        self.assertEqual([], self.failures())

    def test_accepts_a_parameterised_fixture_identity(self) -> None:
        # Parameterised fixture arguments are part of the fixture name and have to survive the rule.
        self.with_case(
            fixture='Reconnecting_Specs("rabbitmq")',
            test="Should_fault_nicely",
            identity='ViciOne.ServiceBus.RabbitMqTransport.Tests.Reconnecting_Specs("rabbitmq").Should_fault_nicely',
        )
        self.assertEqual([], self.failures())

    def test_rejects_an_entry_without_a_full_identity(self) -> None:
        self.with_case(identity=None)
        self.assert_rejected("test-exclusion")

    def test_rejects_a_short_form_identity(self) -> None:
        # Fixture.Test names no namespace, so it would authorise a fixture of that name anywhere.
        self.with_case(identity="Watching_by_hand.Should_be_watched_by_a_human")
        self.assert_rejected("test-exclusion")

    def test_rejects_an_identity_that_does_not_name_its_own_case(self) -> None:
        self.with_case(
            identity="ViciOne.ServiceBus.RabbitMqTransport.Tests.Watching_by_hand.Elsewhere"
                     ".Should_be_watched_by_a_human")
        self.assert_rejected("test-exclusion")

    def test_rejects_a_missing_not_executed_inventory(self) -> None:
        self.inventory().unlink()
        self.assert_rejected("test-exclusion")

    def test_rejects_publishing_through_an_action(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW + "      - uses: docker/build-push-action@v6\n        with:\n          push: true\n",
            encoding="utf-8")
        self.assert_rejected("publication")

    def test_rejects_a_job_level_condition_on_a_required_category(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  rabbitmq:\n", "  rabbitmq:\n    if: github.repository == 'nobody/nothing'\n"),
            encoding="utf-8")
        self.assert_rejected("required-profile")

    def test_rejects_removing_the_run_scoped_credential_guard(self) -> None:
        self.rabbitmq_spec("RabbitMqTestSetUpFixture.cs").write_text(
            SET_UP_FIXTURE.replace("            RequireRunScopedCredentials();\n", ""),
            encoding="utf-8")
        self.assert_rejected("run-scoped-guard")

    def test_rejects_a_guard_that_no_longer_covers_every_variable(self) -> None:
        self.rabbitmq_spec("RabbitMqTestSetUpFixture.cs").write_text(
            SET_UP_FIXTURE.replace("RabbitMqTestHarness.ManagementPortVariable", "null"),
            encoding="utf-8")
        self.assert_rejected("run-scoped-guard")

    def test_rejects_a_guard_that_only_warns(self) -> None:
        self.rabbitmq_spec("RabbitMqTestSetUpFixture.cs").write_text(
            SET_UP_FIXTURE.replace("Assert.Fail(variable)", "TestContext.Out.WriteLine(variable)"),
            encoding="utf-8")
        self.assert_rejected("run-scoped-guard")

    def test_rejects_a_path_filter_on_the_required_trigger(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  pull_request:\n",
                                   "    paths:\n      - 'src/**'\n  pull_request:\n"),
            encoding="utf-8")
        self.assert_rejected("required-profile")

    def test_rejects_a_paths_ignore_filter_on_the_required_trigger(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  pull_request:\n",
                                   "    paths-ignore:\n      - 'docs/**'\n  pull_request:\n"),
            encoding="utf-8")
        self.assert_rejected("required-profile")

    def test_rejects_a_broker_endpoint_that_carries_credentials(self) -> None:
        self.spec("Credentialed_Specs.cs",
                  'var host = new Uri("amqp://guest:guest@localhost:5672");\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_a_credentialed_endpoint_on_the_loopback_address(self) -> None:
        self.spec("Loopback_Specs.cs",
                  'var host = new Uri("rabbitmq://guest:guest@127.0.0.1:5672/test");\n')
        self.assert_rejected("test-endpoint")

    def test_rejects_blame_hang_masking(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("--selection core", "--selection core -- --blame-hang-timeout 5m"),
                                   encoding="utf-8")
        self.assert_rejected("test-masking")

    # -- credentials ---------------------------------------------------------------------------

    def test_rejects_weakened_run_credential_requirement(self) -> None:
        self.compose().write_text(
            COMPOSE.replace("${VICIONE_SERVICEBUS_RMQ_PASS:?run credentials are missing}",
                            "${VICIONE_SERVICEBUS_RMQ_PASS}"), encoding="utf-8")
        self.assert_rejected("credentials")

    def test_rejects_guest_account(self) -> None:
        self.compose().write_text(
            COMPOSE.replace("${VICIONE_SERVICEBUS_RMQ_USER:?run credentials are missing}", "guest"), encoding="utf-8")
        self.assert_rejected("credentials")

    def test_rejects_relaxed_loopback_users(self) -> None:
        (self.root / "build/test-infrastructure/rabbitmq/rabbitmq.conf").write_text(
            "loopback_users = none\n", encoding="utf-8")
        self.assert_rejected("credentials")

    # -- required profile ----------------------------------------------------------------------

    def test_rejects_removing_a_required_category(self) -> None:
        job, category, project = next(entry for entry in FIXTURE_RUNS if entry[1] == "rabbitmq")
        del project
        removed = (JOB % (job, runner_step(category))).format(sdk=APPROVED_SDK)
        self.assertIn(removed, BUILD_WORKFLOW, "the anchor for the removed job is gone")
        body = BUILD_WORKFLOW.replace(removed, "")
        self.workflow().write_text(body, encoding="utf-8")
        self.assert_rejected("required-profile")

    # -- operating system, approved SDK, selector binding, pack chain, capability matrix ---------

    def test_rejects_a_windows_runner(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n    runs-on: ubuntu-24.04",
                                   "  build:\n    runs-on: windows-2022", 1), encoding="utf-8")
        self.assert_rejected("required-runner")

    def test_rejects_a_macos_runner(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n    runs-on: ubuntu-24.04",
                                   "  build:\n    runs-on: macos-14", 1), encoding="utf-8")
        self.assert_rejected("required-runner")

    def test_rejects_a_floating_sdk_version(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace(f"DOTNET_VERSION: '{APPROVED_SDK}'", "DOTNET_VERSION: '10.0.x'"),
            encoding="utf-8")
        self.assert_rejected("required-runner")

    def test_rejects_an_sdk_the_repository_did_not_approve(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace(f"dotnet-version: '{APPROVED_SDK}'", "dotnet-version: '10.0.100'"),
            encoding="utf-8")
        self.assert_rejected("required-runner")

    def test_rejects_a_selectable_target_without_a_job(self) -> None:
        (self.root / ".github/workflows/extended.yml").write_text(
            "name: Extended\n"
            "on:\n"
            "  workflow_dispatch:\n"
            "    inputs:\n"
            "      target:\n"
            "        type: choice\n"
            "        options:\n"
            "          - activemq\n"
            "          - amazon-sqs\n"
            "jobs:\n"
            "  activemq:\n"
            "    runs-on: ubuntu-latest\n"
            "    if: inputs.target == 'activemq'\n"
            "    steps:\n"
            "      - run: echo run\n", encoding="utf-8")
        self.assert_rejected("selector-binding")

    def test_rejects_pack_without_every_gate(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - sql-transport\n", "", 1), encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_pack_that_restores_unbound(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("      - run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode\n", "", 1), encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_pack_that_restores_a_second_time(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore -o artifacts/packages",
                                   "dotnet pack ViciOne.ServiceBus.slnx -c Release -o artifacts/packages"), encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_a_removed_capability_in_the_matrix(self) -> None:
        matrix = fixture_model()
        matrix["capabilities"].append({
            "id": "kafka", "class": "REAL_EPHEMERAL_CLOUD",
            "sourceProjects": ["src/Transports/ViciOne.ServiceBus.KafkaIntegration"], "testProjects": [],
            "supportProjects": [], "toolProjects": [],
        })
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("verification-model")

    def test_rejects_one_category_declared_by_two_runs(self) -> None:
        model = fixture_model()
        first = model["capabilities"][0]["runs"][0]
        model["capabilities"].append({
            "id": "capability-again", "class": "LOCAL_REQUIRED_RUN", "sourceProjects": [],
            "testProjects": [], "supportProjects": [], "toolProjects": [],
            "runs": [dict(first)],
        })
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_capability_verified_through_itself(self) -> None:
        model = fixture_model()
        model["capabilities"][0].pop("runs")
        model["capabilities"][0]["verifiedThroughCapability"] = model["capabilities"][0]["id"]
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_cycle_of_two_capabilities(self) -> None:
        model = fixture_model()
        first, second = model["capabilities"][0], model["capabilities"][1]
        for capability, other in ((first, second), (second, first)):
            capability.pop("runs", None)
            capability["verifiedThroughCapability"] = other["id"]
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_leaning_on_a_run_without_naming_an_anchor(self) -> None:
        model = fixture_model()
        model["capabilities"].append({
            "id": "capability-leaning", "class": "LOCAL_REQUIRED_RUN", "sourceProjects": [],
            "testProjects": [], "supportProjects": [], "toolProjects": [],
            "verifiedThroughCapability": model["capabilities"][0]["id"],
        })
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_an_anchor_that_is_not_in_that_test_project(self) -> None:
        model = fixture_model()
        rabbit = next(c for c in model["capabilities"] if c["runs"][0]["category"] == "rabbitmq")
        model["capabilities"].append({
            "id": "capability-anchored", "class": "LOCAL_REQUIRED_RUN", "sourceProjects": [],
            "testProjects": [], "supportProjects": [], "toolProjects": [],
            "verifiedThroughCapability": rabbit["id"],
            "testAnchors": ["Somewhere.Else.Nothing_Like_This"],
        })
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_workflow_shape_the_reader_was_not_written_for(self) -> None:
        """A reader that guesses agrees with the model for the wrong reason."""
        self.workflow().write_text(
            BUILD_WORKFLOW.format(sdk=APPROVED_SDK).replace("jobs:\n", "jobs: {}\n", 1), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_job_map_entry_the_workflow_does_not_have(self) -> None:
        """The model may not give a selection to a job nobody runs."""
        model = fixture_model()
        model["jobs"]["a-job-that-does-not-exist"] = "rabbitmq"
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_category_no_required_job_reaches(self) -> None:
        """A declared category that no job's selection contains is a category nothing runs."""
        model = fixture_model()
        del model["jobs"]["quartz"]
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_run_that_names_the_wrong_project(self) -> None:
        model = fixture_model()
        model["capabilities"][0]["runs"][0]["project"] = "tests/Somewhere.Else/Somewhere.Else.csproj"
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_workflow_job_the_model_does_not_explain(self) -> None:
        """The other direction: a required job that belongs to no capability."""
        self.workflow().write_text(
            BUILD_WORKFLOW.format(sdk=APPROVED_SDK)
            + (JOB % ("kafka", "python3 tools/ci/run_test_category.py --category kafka --project x.csproj")
               ).format(sdk=APPROVED_SDK),
            encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_lowered_executed_floor(self) -> None:
        """A floor that may fall is not a floor. Zero is the shape a lowering takes."""
        model = fixture_model()
        model["capabilities"][0]["runs"][0]["minimumExecutedCases"] = 0
        self.matrix().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_a_project_claimed_twice(self) -> None:
        matrix = fixture_model()
        matrix["capabilities"].append({
            "id": "core-again", "class": "LOCAL_REQUIRED_RUN",
            "sourceProjects": ["src/ViciOne.ServiceBus"], "testProjects": [], "supportProjects": [],
            "toolProjects": [], "verifiedThroughCapability": "capability-core",
        })
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("verification-model")

    def test_rejects_a_pack_that_keeps_an_earlier_run_output(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace("      - run: rm -rf artifacts/packages\n", "", 1),
            encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_a_solution_reference_to_nothing(self) -> None:
        self.write_solution()
        solution = self.root / "ViciOne.ServiceBus.slnx"
        solution.write_text(
            solution.read_text(encoding="utf-8").replace(
                "</Solution>", '  <Project Path="src/Gone/Gone.csproj" />\n</Solution>'),
            encoding="utf-8")

        policy = Policy(self.root)
        policy.run()

        self.assertTrue(any(failure.startswith("solution") for failure in policy.failures),
                        f"a reference to a project that does not exist was accepted: {policy.failures}")

    def test_reads_a_reference_the_regex_would_have_missed(self) -> None:
        """Single quotes are valid XML and a Path= regex does not see them."""
        projects = sorted(p.relative_to(self.root).as_posix() for p in self.root.rglob("*.csproj"))
        body = "<Solution>\n" + "".join(f"  <Project Path='{path}' />\n" for path in projects) + "</Solution>\n"
        (self.root / "ViciOne.ServiceBus.slnx").write_text(body, encoding="utf-8")

        policy = Policy(self.root)
        policy.run()

        self.assertFalse(any(failure.startswith("orphan-project") for failure in policy.failures),
                         f"a project named with single quotes was read as unreferenced: {policy.failures}")

    def test_rejects_an_action_pinned_to_a_tag(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace(
                "actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68", "actions/setup-dotnet@v5"),
            encoding="utf-8")

        self.assert_rejected("moving-reference")

    def test_rejects_a_runner_label_that_moves(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace("ubuntu-24.04", "ubuntu-latest", 1),
            encoding="utf-8")

        self.assert_rejected("moving-reference")

    def test_rejects_a_project_that_skips_the_build_targets(self) -> None:
        project = next(self.root.rglob("*.csproj"))
        project.write_text(
            "<Project>\n  <PropertyGroup>\n    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")

        self.assert_rejected("central-contract")

    def test_rejects_a_project_that_turns_locked_mode_off_in_itself(self) -> None:
        project = next(self.root.rglob("*.csproj"))
        project.write_text(
            "<Project>\n  <PropertyGroup>\n    <RestoreLockedMode>false</RestoreLockedMode>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")

        self.assert_rejected("central-contract")

    def test_rejects_a_project_that_points_the_build_path_elsewhere(self) -> None:
        project = next(self.root.rglob("*.csproj"))
        project.write_text(
            "<Project>\n  <PropertyGroup>\n"
            "    <CustomBeforeMicrosoftCommonTargets>other.targets</CustomBeforeMicrosoftCommonTargets>\n"
            "  </PropertyGroup>\n</Project>\n", encoding="utf-8")

        self.assert_rejected("central-contract")

    def test_rejects_a_decorative_build_targets_file(self) -> None:
        (self.root / "Directory.Build.targets").write_text("<Project />\n", encoding="utf-8")

        self.assert_rejected("build-targets")

    def test_rejects_a_gate_that_only_warns(self) -> None:
        (self.root / "Directory.Build.targets").write_text(
            '<Project>\n  <Target Name="Check" BeforeTargets="BeforeBuild">\n'
            '    <Warning Text="something" />\n  </Target>\n</Project>\n', encoding="utf-8")

        self.assert_rejected("build-targets")

    def test_rejects_a_gate_that_hangs_off_nothing(self) -> None:
        (self.root / "Directory.Build.targets").write_text(
            '<Project>\n  <Target Name="Check">\n    <Error Text="something" />\n  </Target>\n</Project>\n',
            encoding="utf-8")

        self.assert_rejected("build-targets")

    def test_rejects_a_project_no_solution_names(self) -> None:
        stray = self.root / "tests/ViciOne.ServiceBus.Stray.Tests"
        stray.mkdir(parents=True)
        (stray / "ViciOne.ServiceBus.Stray.Tests.csproj").write_text("<Project />\n", encoding="utf-8")
        (stray / "packages.lock.json").write_text("{}\n", encoding="utf-8")

        # Written after the fixture solution, and the solution is not rewritten for this one case.
        policy = Policy(self.root)
        policy.run()

        self.assertTrue(any(failure.startswith("orphan-project") for failure in policy.failures),
                        f"a project no solution names was accepted: {policy.failures}")

    def test_rejects_a_dotnet_command_without_a_target(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace(
                "dotnet build ViciOne.ServiceBus.slnx -c Release", "dotnet build -c Release"),
            encoding="utf-8")
        self.assert_rejected("dotnet-target")

    def test_rejects_a_pack_command_without_a_target(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace(
                "dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore",
                "dotnet pack -c Release --no-build --no-restore"),
            encoding="utf-8")
        self.assert_rejected("dotnet-target")

    def test_accepts_a_dotnet_command_that_names_a_project(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace(
                "dotnet build ViciOne.ServiceBus.slnx -c Release",
                "dotnet build build/Tooling/Tooling.csproj -c Release"),
            encoding="utf-8")
        self.assertEqual([], self.failures())

    def test_rejects_a_tool_test_module_no_job_starts(self) -> None:
        module = self.root / "tools/identity/test_identity_gate.py"
        module.parent.mkdir(parents=True, exist_ok=True)
        module.write_text("# a proof nobody starts\n", encoding="utf-8")
        self.assert_rejected("tool-test-not-run")

    def test_accepts_a_tool_test_module_whose_directory_is_discovered(self) -> None:
        module = self.root / "tools/ci/test_something_else.py"
        module.parent.mkdir(parents=True, exist_ok=True)
        module.write_text("# discovered by the policy job\n", encoding="utf-8")
        self.assertEqual([], self.failures())

    def test_accepts_a_tool_test_module_a_job_names(self) -> None:
        module = self.root / "tools/identity/test_identity_gate.py"
        module.parent.mkdir(parents=True, exist_ok=True)
        module.write_text("# named by a job\n", encoding="utf-8")
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace(
                "python3 -m unittest discover -s tools/ci -p 'test_*.py'",
                "python3 -m unittest discover -s tools/ci -p 'test_*.py' && "
                "python3 tools/identity/test_identity_gate.py"),
            encoding="utf-8")
        self.assertEqual([], self.failures())

    def test_rejects_a_trx_under_evidence(self) -> None:
        artifact = self.root / "evidence/some-record/core-unit.trx"
        artifact.parent.mkdir(parents=True)
        artifact.write_text("<TestRun />\n", encoding="utf-8")
        self.assert_rejected("raw-artifact")

    def test_rejects_a_collected_broker_log_under_evidence(self) -> None:
        artifact = self.root / "evidence/some-record/rabbitmq-broker.log"
        artifact.parent.mkdir(parents=True)
        artifact.write_text("connection accepted\n", encoding="utf-8")
        self.assert_rejected("raw-artifact")

    def test_accepts_the_category_summary_the_runner_writes_beside_it(self) -> None:
        # The summary is what a record binds, so the rule has to leave it alone; a rule that rejected
        # every file under evidence/ would pass the two cases above for the wrong reason.
        summary = self.root / "evidence/some-record/core-unit.json"
        summary.parent.mkdir(parents=True)
        summary.write_text(json.dumps({"executed": 1872}), encoding="utf-8")
        self.assertEqual([], self.failures())

    def test_rejects_a_retained_capability_nobody_classifies(self) -> None:
        unclassified = self.root / "src/ViciOne.ServiceBus.Unclassified"
        unclassified.mkdir(parents=True)
        (unclassified / "ViciOne.ServiceBus.Unclassified.csproj").write_text("<Project />\n", encoding="utf-8")
        (unclassified / "packages.lock.json").write_text("{}\n", encoding="utf-8")
        self.assert_rejected("verification-model")

    def test_rejects_a_required_capability_without_a_job(self) -> None:
        matrix = fixture_model()
        for capability in matrix["capabilities"]:
            if capability["id"] == "capability-core":
                capability.pop("runs")
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("verification-model")

    def test_rejects_an_unknown_verification_class(self) -> None:
        matrix = fixture_model()
        matrix["capabilities"][0]["class"] = "SOMEHOW_VERIFIED"
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("verification-model")

    def test_rejects_a_missing_capability_matrix(self) -> None:
        self.matrix().unlink()
        self.assert_rejected("verification-model")

    def test_rejects_external_infrastructure_for_a_category_with_a_fixture(self) -> None:
        inventory = fixture_model()
        run = next(r for capability in inventory["capabilities"] for r in capability.get("runs", [])
                   if r["category"] == "rabbitmq")
        run["notExecuted"].append({
            "identity": "ViciOne.ServiceBus.RabbitMqTransport.Tests.Some_Specs.Should_do_something",
            "dueness": "NOT_DUE_EXTERNAL_INFRASTRUCTURE",
            "reason": "claims a fixture that the required profile starts",
        })
        self.inventory().write_text(json.dumps(inventory, indent=2), encoding="utf-8")
        self.assert_rejected("dueness-class")

    def test_rejects_a_category_without_an_executed_floor(self) -> None:
        model = fixture_model()
        for capability in model["capabilities"]:
            for run in capability.get("runs", []):
                run.pop("minimumExecutedCases", None)
        self.inventory().write_text(json.dumps(model, indent=2), encoding="utf-8")

        self.assert_rejected("verification-model")

    def test_rejects_returning_upstream_repository_guard(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n",
                                   "  build:\n    if: github.repository == 'ViciOne.ServiceBus/ViciOne.ServiceBus'\n"),
            encoding="utf-8")
        self.assert_rejected("required-profile")

    def test_rejects_returning_master_branch_guard(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n", "  build:\n    if: github.ref == 'refs/heads/master'\n"),
            encoding="utf-8")
        self.assert_rejected("required-profile")

    def test_rejects_a_profile_that_verifies_nothing(self) -> None:
        """Every required job goes through the one entry point, or the profile proves nothing."""
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("python3 tools/ci/verify.py --selection", "dotnet test --filter Category!="),
            encoding="utf-8")

        self.assert_rejected("required-profile")

    # -- pack ----------------------------------------------------------------------------------

    def test_rejects_pack_without_required_gates(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - rabbitmq\n", ""), encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_pack_without_run_artifact(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1\n", ""),
                                   encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_pack_without_package_hashes(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - run: sha256sum artifacts/packages/*.nupkg\n", ""),
                                   encoding="utf-8")
        self.assert_rejected("pack")

    # -- publication ---------------------------------------------------------------------------

    def test_rejects_returning_nuget_push(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW + "      - run: dotnet nuget push * -s https://api.nuget.org/v3/index.json\n",
            encoding="utf-8")
        self.assert_rejected("publication")

    def test_rejects_container_registry_push(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW + "      - run: docker push ghcr.io/example/image\n",
                                   encoding="utf-8")
        self.assert_rejected("publication")

    # -- analyzer release tracking -------------------------------------------------------------

    def test_rejects_invalid_analyzer_release_header(self) -> None:
        (self.root / "src/ViciOne.ServiceBus.Analyzers/AnalyzerReleases.Shipped.md").write_text(
            "<!-- ViciOne modification -->\n" + SHIPPED, encoding="utf-8")
        self.assert_rejected("analyzer-tracking")

    def test_accepts_semicolon_provenance_comment(self) -> None:
        (self.root / "src/ViciOne.ServiceBus.Analyzers/AnalyzerReleases.Shipped.md").write_text(
            "; ViciOne modification\n" + SHIPPED, encoding="utf-8")
        self.assertEqual([], self.failures())


    # -- the ownership rule ------------------------------------------------------------------------

    CHANNEL_CONTEXT = "src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqChannelContext.cs"

    def write_channel_context(self, publish_body: str) -> None:
        path = self.root / self.CHANNEL_CONTEXT
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            "namespace ViciOne.ServiceBus.RabbitMqTransport\n"
            "{\n"
            "    public class RabbitMqChannelContext\n"
            "    {\n"
            "        public Task BasicPublishAsync(string exchange)\n"
            "        {\n"
            f"{publish_body}\n"
            "        }\n"
            "\n"
            "        public Task NotifyFaulted(Exception exception)\n"
            "        {\n"
            "            return Task.CompletedTask;\n"
            "        }\n"
            "    }\n"
            "}\n",
            encoding="utf-8")

    def test_accepts_an_operation_that_takes_a_lease(self) -> None:
        self.write_channel_context("            using var lease = Lease();\n            return Task.CompletedTask;")
        self.assertEqual([], self.failures())

    def test_rejects_an_operation_that_calls_the_broker_without_a_lease(self) -> None:
        self.write_channel_context("            return _channel.BasicPublishAsync(exchange);")
        self.assert_rejected("transport-lease")

    def test_rejects_a_name_that_merely_contains_lease(self) -> None:
        # 'NoLease()' contains the word and must not satisfy the rule: the earlier substring form of
        # this check accepted exactly that and would have accepted the defect it exists to catch.
        self.write_channel_context("            using var lease = NoLease();\n            return Task.CompletedTask;")
        self.assert_rejected("transport-lease")


    # -- restore sources -----------------------------------------------------------------------

    def nuget_config(self) -> Path:
        return self.root / "NuGet.config"

    def test_accepts_the_isolated_restore_configuration(self) -> None:
        self.assertEqual([], self.failures())

    def test_rejects_a_missing_configuration(self) -> None:
        self.nuget_config().unlink()
        self.assert_rejected("restore-sources")

    def test_rejects_a_configuration_that_does_not_clear_inherited_sources(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace("    <clear />\n", "", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_clear_that_stands_after_the_source(self) -> None:
        # A clear below the add wipes the source it was meant to keep, so the restore falls back to
        # nothing at all. The element is present either way, which is why position has to be checked.
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            '    <clear />\n    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />\n',
            '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />\n    <clear />\n',
            1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_second_source(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  </packageSources>",
            '    <add key="JFrogViciOne" value="https://jfrog.invalid/artifactory/api/nuget/v3/index.json" />\n'
            "  </packageSources>", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_renamed_source(self) -> None:
        self.nuget_config().write_text(
            NUGET_CONFIG.replace('value="https://api.nuget.org/v3/index.json"',
                                 'value="https://jfrog.invalid/artifactory/api/nuget/v3/index.json"', 1),
            encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_missing_mapping(self) -> None:
        body = NUGET_CONFIG.split("  <packageSourceMapping>")[0] + "</configuration>\n"
        self.nuget_config().write_text(body, encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_mapping_that_leaves_patterns_unclaimed(self) -> None:
        self.nuget_config().write_text(
            NUGET_CONFIG.replace('<package pattern="*" />', '<package pattern="ViciOne.*" />', 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_mapping_for_an_undeclared_source(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "    </packageSource>",
            "    </packageSource>\n"
            '    <packageSource key="JFrogViciOne">\n'
            '      <package pattern="ViciOne.*" />\n'
            "    </packageSource>", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_missing_disabled_sources_section(self) -> None:
        # The real sabotage the review used: drop the block entirely. The single source can then be
        # switched off by whatever the host has configured, and the restore has nothing left.
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  <disabledPackageSources>\n    <clear />\n  </disabledPackageSources>\n", "", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_disabled_sources_section_without_a_clear(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  <disabledPackageSources>\n    <clear />\n  </disabledPackageSources>\n",
            "  <disabledPackageSources>\n  </disabledPackageSources>\n", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_clear_that_stands_after_a_disable(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  <disabledPackageSources>\n    <clear />\n  </disabledPackageSources>\n",
            '  <disabledPackageSources>\n    <add key="other" value="true" />\n    <clear />\n'
            "  </disabledPackageSources>\n", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_disabling_the_only_source(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  <disabledPackageSources>\n    <clear />\n  </disabledPackageSources>\n",
            '  <disabledPackageSources>\n    <clear />\n    <add key="nuget.org" value="true" />\n'
            "  </disabledPackageSources>\n", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_a_second_disabled_sources_section(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "  </disabledPackageSources>\n",
            "  </disabledPackageSources>\n  <disabledPackageSources>\n  </disabledPackageSources>\n", 1),
            encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_credentials_in_the_repository(self) -> None:
        self.nuget_config().write_text(NUGET_CONFIG.replace(
            "</configuration>",
            "  <packageSourceCredentials>\n"
            '    <nuget.org>\n'
            '      <add key="Username" value="build" />\n'
            '      <add key="ClearTextPassword" value="secret" />\n'
            "    </nuget.org>\n"
            "  </packageSourceCredentials>\n"
            "</configuration>", 1), encoding="utf-8")
        self.assert_rejected("restore-sources")

    def test_rejects_an_unparsable_configuration(self) -> None:
        self.nuget_config().write_text("<configuration><packageSources>", encoding="utf-8")
        self.assert_rejected("restore-sources")



class Every_rule_this_validator_can_report(unittest.TestCase):
    """A rule nobody has made fire is a rule nobody has proven works.

    The dead executed-floor loop was exactly that shape: it read a key the model has not had since it
    replaced the two files before it, so it passed for every repository, and no case would have noticed.
    A second one is subtler - a rule can have a case and then lose it, because a suite stays green when
    a test disappears. This case is the thing that says so.
    """

    def test_has_a_case_that_makes_it_report(self) -> None:
        here = Path(__file__).resolve().parent
        reported = set(re.findall(r'self\.fail\(\s*"([a-z0-9-]+)"',
                                  (here / "policy_validator.py").read_text(encoding="utf-8")))
        cases = (here / "test_policy_validator.py").read_text(encoding="utf-8")
        proven = set(re.findall(r'assert_rejected\(\s*"([a-z0-9-]+)"', cases))
        proven |= set(re.findall(r'failure\.startswith\("([a-z0-9-]+)"\)', cases))

        self.assertEqual(set(), reported - proven,
                         "these rules can report something and no case in this file ever makes them "
                         "do it, so nothing has shown that they work")


class Turning_a_step_into_the_commands_a_shell_would_run(unittest.TestCase):
    """The reader itself, on the two shapes that decide whether it can be trusted at all."""

    def test_a_quoted_separator_is_not_a_command_boundary(self) -> None:
        """The logger argument of dotnet test carries a semicolon inside quotes. Splitting on the raw
        character would cut it in half and report a command nobody wrote."""
        commands = effective_commands('dotnet test x.csproj --logger "trx;LogFileName=core.trx"')

        self.assertEqual([["dotnet", "test", "x.csproj", "--logger", "trx;LogFileName=core.trx"]], commands)

    def test_an_escaped_backslash_at_the_end_of_a_line_does_not_continue_it(self) -> None:
        """An even number of trailing backslashes ends the line, which is why they are counted rather
        than tested for one."""
        commands = effective_commands("echo one\\\\\nfalse\n")

        self.assertEqual([["echo", "one\\"], ["false"]], commands)

    def test_a_continuation_joins_the_next_line_into_one_command(self) -> None:
        commands = effective_commands("python3 x.py \\\n  --one 1 \\\n  --two 2\n")

        self.assertEqual([["python3", "x.py", "--one", "1", "--two", "2"]], commands)


if __name__ == "__main__":
    unittest.main(verbosity=2)

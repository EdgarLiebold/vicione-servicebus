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
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from policy_validator import Policy  # noqa: E402


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

CAPABILITY_MATRIX = {
    "schemaVersion": 1,
    "kind": "SERVICEBUS_CAPABILITY_MATRIX",
    "verificationClasses": {
        "LOCAL_REQUIRED_RUN": "runs locally in the required profile",
        "PINNED_FIXTURE_REQUIRED_RUN": "runs against a pinned fixture in the required profile",
        "REAL_EPHEMERAL_CLOUD": "needs a real cloud resource and is not executed here",
    },
    "capabilities": [
        {"id": "core", "class": "LOCAL_REQUIRED_RUN",
         "sourceProjects": ["src/ViciOne.ServiceBus"], "testProjects": [], "requiredJobs": ["core-unit"]},
        {"id": "analyzers", "class": "LOCAL_REQUIRED_RUN",
         "sourceProjects": ["src/ViciOne.ServiceBus.Analyzers"], "testProjects": [],
         "requiredJobs": ["analyzer"]},
        {"id": "transport-rabbitmq", "class": "PINNED_FIXTURE_REQUIRED_RUN",
         "sourceProjects": [], "testProjects": [RABBITMQ_TEST_PROJECT], "requiredJobs": ["rabbitmq"]},
    ],
}

JOB = """\
  %s:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/setup-dotnet@v5
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
    + JOB % ("analyzer", "python3 tools/ci/run_test_category.py --category analyzer")
    + JOB % ("core-unit", "python3 tools/ci/run_test_category.py --category core")
    + JOB % ("signalr", "python3 tools/ci/run_test_category.py --category signalr")
    + JOB % ("quartz", "python3 tools/ci/run_test_category.py --category quartz")
    + JOB % ("activemq", "python3 tools/ci/run_broker_category.py --broker activemq --category activemq")
    + JOB % ("sql-transport", "python3 tools/ci/run_broker_category.py --broker postgres --category sql-transport")
    + JOB % ("benchmarks", "python3 tools/ci/run_test_category.py --category benchmarks")
    + JOB % ("rabbitmq", "python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq")
    + JOB % ("entity-framework", "python3 tools/ci/run_test_category.py --category entity-framework-core")
    + """\
  pack:
    runs-on: ubuntu-latest
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
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '{sdk}'
      - run: dotnet restore ViciOne.ServiceBus.slnx --locked-mode
      - run: rm -rf artifacts/packages
      - run: dotnet pack ViciOne.ServiceBus.slnx -c Release --no-build --no-restore -o artifacts/packages
      - run: sha256sum artifacts/packages/*.nupkg
      - uses: actions/upload-artifact@v4
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

NOT_EXECUTED_INVENTORY = {
    "schemaVersion": 1,
    "kind": "NOT_EXECUTED_INVENTORY",
    "categories": {
        "rabbitmq": {
            "project": RABBITMQ_TEST_PROJECT,
            "explicitAttributeCount": 1,
            "minimumExecutedCases": 1,
            "cases": [
                {
                    "identity": "ViciOne.ServiceBus.RabbitMqTransport.Tests.Watching_by_hand.Should_be_watched_by_a_human",
                    "fixture": "Watching_by_hand",
                    "test": "Should_be_watched_by_a_human",
                    "mechanism": "EXPLICIT",
                    "dueness": "NOT_DUE_MANUAL_OBSERVATION",
                    "reason": "Asserts nothing a gate could evaluate.",
                }
            ],
        }
    },
}


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
        (infra / "not-executed-inventory.json").write_text(
            json.dumps(NOT_EXECUTED_INVENTORY, indent=2), encoding="utf-8"
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
        self.write_solution()
        (self.root / "global.json").write_text(GLOBAL_JSON, encoding="utf-8")
        (infra / "capability-matrix.json").write_text(
            json.dumps(CAPABILITY_MATRIX, indent=2), encoding="utf-8"
        )

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
        for project in sorted(self.root.rglob("*.csproj")):
            (project.parent / "packages.lock.json").write_text("{}\n", encoding="utf-8")

    def write_solution(self) -> None:
        """Names every project the fixture holds, so the orphan rule has nothing to complain about."""
        projects = sorted(p.relative_to(self.root).as_posix() for p in self.root.rglob("*.csproj"))
        body = "<Solution>\n" + "".join(f'  <Project Path="{path}" />\n' for path in projects) + "</Solution>\n"
        (self.root / "ViciOne.ServiceBus.slnx").write_text(body, encoding="utf-8")

    def failures(self) -> list[str]:
        self.write_solution()
        policy = Policy(self.root)
        policy.run()
        return policy.failures

    def assert_rejected(self, rule: str) -> None:
        found = self.failures()
        self.assertTrue(found, f"the mutation was accepted; rule '{rule}' never fired")
        self.assertTrue(
            any(failure.startswith(rule) for failure in found),
            f"expected rule '{rule}' to fire, got: {found}",
        )

    def compose(self) -> Path:
        return self.root / "build/test-infrastructure/compose.yaml"

    def inventory(self) -> Path:
        return self.root / "build/test-infrastructure/not-executed-inventory.json"

    def matrix(self) -> Path:
        return self.root / "build/test-infrastructure/capability-matrix.json"

    def workflow(self) -> Path:
        return self.root / ".github/workflows/build.yml"

    def inventory(self) -> Path:
        return self.root / "build/test-infrastructure/not-executed-inventory.json"

    def rabbitmq_spec(self, name: str) -> Path:
        return self.root / RABBITMQ_TEST_PROJECT / name

    # -- positive ------------------------------------------------------------------------------

    def test_accepts_a_conforming_repository(self) -> None:
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

    def test_rejects_bypassing_the_canonical_broker_runner(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq",
                                   "docker compose up -d rabbitmq && python3 tools/ci/run_test_category.py --category rabbitmq"),
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
            BUILD_WORKFLOW.replace("--category rabbitmq", '--category rabbitmq -- --filter "Category!=Flaky"'),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    # The six mutations below all survived the first version of these rules. They are the reason the
    # rules now match the switch and the shape rather than one spelling of one predicate.

    def test_rejects_a_single_quoted_category_filter(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--category rabbitmq", "--category rabbitmq -- --filter 'Category!=Flaky'"),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    def test_rejects_a_fully_qualified_name_filter(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--category rabbitmq",
                                   '--category rabbitmq -- --filter "FullyQualifiedName!~KillSwitch"'),
            encoding="utf-8")
        self.assert_rejected("test-masking")

    def test_rejects_an_nunit_selector(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("--category rabbitmq",
                                   '--category rabbitmq -- -- NUnit.Where="cat != Flaky"'),
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
        inventory = json.loads(json.dumps(NOT_EXECUTED_INVENTORY))
        inventory["categories"]["rabbitmq"]["cases"][0]["dueness"] = "DUE_OPEN_DEFECT"
        self.inventory().write_text(json.dumps(inventory, indent=2), encoding="utf-8")
        self.assert_rejected("test-exclusion")

    def with_case(self, **changes: object) -> None:
        """Rewrite the single inventoried case of the fixture repository."""
        inventory = json.loads(json.dumps(NOT_EXECUTED_INVENTORY))
        case = inventory["categories"]["rabbitmq"]["cases"][0]
        for key, value in changes.items():
            if value is None:
                case.pop(key, None)
            else:
                case[key] = value
        self.inventory().write_text(json.dumps(inventory, indent=2), encoding="utf-8")

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
        self.workflow().write_text(BUILD_WORKFLOW.replace("--category core", "--category core -- --blame-hang-timeout 5m"),
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
        removed = (JOB % ("rabbitmq",
                          "python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq")
                   ).format(sdk=APPROVED_SDK)
        self.assertIn(removed, BUILD_WORKFLOW, "the anchor for the removed job is gone")
        body = BUILD_WORKFLOW.replace(removed, "")
        self.workflow().write_text(body, encoding="utf-8")
        self.assert_rejected("required-profile")

    # -- operating system, approved SDK, selector binding, pack chain, capability matrix ---------

    def test_rejects_a_windows_runner(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n    runs-on: ubuntu-latest",
                                   "  build:\n    runs-on: windows-latest", 1), encoding="utf-8")
        self.assert_rejected("required-runner")

    def test_rejects_a_macos_runner(self) -> None:
        self.workflow().write_text(
            BUILD_WORKFLOW.replace("  build:\n    runs-on: ubuntu-latest",
                                   "  build:\n    runs-on: macos-latest", 1), encoding="utf-8")
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
        matrix = json.loads(self.matrix().read_text(encoding="utf-8"))
        matrix["capabilities"].append({
            "id": "kafka", "class": "REAL_EPHEMERAL_CLOUD",
            "sourceProjects": ["src/Transports/ViciOne.ServiceBus.KafkaIntegration"], "testProjects": [],
        })
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("capability-matrix")

    def test_rejects_a_capability_claimed_twice(self) -> None:
        matrix = json.loads(self.matrix().read_text(encoding="utf-8"))
        matrix["capabilities"].append({
            "id": "core-again", "class": "LOCAL_REQUIRED_RUN",
            "sourceProjects": ["src/ViciOne.ServiceBus"], "testProjects": [], "requiredJobs": ["core-unit"],
        })
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("capability-matrix")

    def test_rejects_a_pack_that_keeps_an_earlier_run_output(self) -> None:
        workflow = self.workflow()
        workflow.write_text(
            workflow.read_text(encoding="utf-8").replace("      - run: rm -rf artifacts/packages\n", "", 1),
            encoding="utf-8")
        self.assert_rejected("pack")

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
        self.assert_rejected("capability-matrix")

    def test_rejects_a_required_capability_without_a_job(self) -> None:
        matrix = json.loads(self.matrix().read_text(encoding="utf-8"))
        for capability in matrix["capabilities"]:
            if capability["id"] == "core":
                capability.pop("requiredJobs")
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("capability-matrix")

    def test_rejects_an_unknown_verification_class(self) -> None:
        matrix = json.loads(self.matrix().read_text(encoding="utf-8"))
        matrix["capabilities"][0]["class"] = "SOMEHOW_VERIFIED"
        self.matrix().write_text(json.dumps(matrix, indent=2), encoding="utf-8")
        self.assert_rejected("capability-matrix")

    def test_rejects_a_missing_capability_matrix(self) -> None:
        self.matrix().unlink()
        self.assert_rejected("capability-matrix")

    def test_rejects_external_infrastructure_for_a_category_with_a_fixture(self) -> None:
        inventory = json.loads(self.inventory().read_text(encoding="utf-8"))
        inventory["categories"]["rabbitmq"]["cases"].append({
            "identity": "ViciOne.ServiceBus.RabbitMqTransport.Tests.Some_Specs.Should_do_something",
            "dueness": "NOT_DUE_EXTERNAL_INFRASTRUCTURE",
            "reason": "claims a fixture that the required profile starts",
        })
        self.inventory().write_text(json.dumps(inventory, indent=2), encoding="utf-8")
        self.assert_rejected("dueness-class")

    def test_rejects_a_category_without_an_executed_floor(self) -> None:
        inventory = json.loads(self.inventory().read_text(encoding="utf-8"))
        inventory["categories"]["rabbitmq"].pop("minimumExecutedCases", None)
        self.inventory().write_text(json.dumps(inventory, indent=2), encoding="utf-8")
        self.assert_rejected("executed-floor")

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

    def test_rejects_bypassing_the_test_count_gate(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("python3 tools/ci/run_test_category.py --category",
                                                          "dotnet test --filter Category!="), encoding="utf-8")
        self.assert_rejected("required-profile")

    # -- pack ----------------------------------------------------------------------------------

    def test_rejects_pack_without_required_gates(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - rabbitmq\n", ""), encoding="utf-8")
        self.assert_rejected("pack")

    def test_rejects_pack_without_run_artifact(self) -> None:
        self.workflow().write_text(BUILD_WORKFLOW.replace("      - uses: actions/upload-artifact@v4\n", ""),
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


if __name__ == "__main__":
    unittest.main(verbosity=2)

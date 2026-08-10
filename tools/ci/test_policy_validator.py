#!/usr/bin/env python3
"""Self-tests for the CI policy validator.

ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.

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

BUILD_WORKFLOW = """\
name: Required CI
jobs:
  policy:
    steps:
      - run: python3 tools/ci/policy_validator.py
  build:
    steps:
      - run: dotnet build -c Release
  analyzer:
    steps:
      - run: python3 tools/ci/run_test_category.py --category analyzer
  core-unit:
    steps:
      - run: python3 tools/ci/run_test_category.py --category core
  rabbitmq:
    steps:
      - run: python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq
  entity-framework:
    steps:
      - run: python3 tools/ci/run_test_category.py --category entity-framework-core
  pack:
    needs:
      - policy
      - build
      - analyzer
      - core-unit
      - rabbitmq
      - entity-framework
    steps:
      - run: dotnet pack -c Release -o artifacts/packages
      - run: sha256sum artifacts/packages/*.nupkg
      - uses: actions/upload-artifact@v4
"""

SHIPPED = "## Release 1.0\n\n### New Rules\nRule ID | Category | Severity | Notes\n"
UNSHIPPED = ""

RABBITMQ_TEST_PROJECT = "tests/ViciOne.ServiceBus.RabbitMqTransport.Tests"

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
            "cases": [
                {
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

        analyzers = self.root / "src/ViciOne.ServiceBus.Analyzers"
        analyzers.mkdir(parents=True)
        (analyzers / "AnalyzerReleases.Shipped.md").write_text(SHIPPED, encoding="utf-8")
        (analyzers / "AnalyzerReleases.Unshipped.md").write_text(UNSHIPPED, encoding="utf-8")

    def failures(self) -> list[str]:
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
        directory = self.root / "tests/ViciOne.ServiceBus.ActiveMqTransport.Tests"
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
        body = BUILD_WORKFLOW.replace(
            "  rabbitmq:\n    steps:\n"
            "      - run: python3 tools/ci/run_broker_category.py --broker rabbitmq --category rabbitmq\n", "")
        self.workflow().write_text(body, encoding="utf-8")
        self.assert_rejected("required-profile")

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


if __name__ == "__main__":
    unittest.main(verbosity=2)

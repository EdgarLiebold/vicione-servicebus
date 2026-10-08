"""Known fixture credentials must be removed before log persistence or console output."""
from contextlib import redirect_stdout, redirect_stderr
import importlib.util
import io
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


REPOSITORY = next(parent for parent in Path(__file__).resolve().parents
                  if (parent / "tools/ci/fixtures/broker_logs.py").is_file())
sys.path.insert(0, str(REPOSITORY / "tools/ci"))
paired = Path(__file__).with_name("broker_logs.py")
SUBJECT = Path(os.environ.get("VICIONE_REVIEW_BROKER_LOG_SUBJECT",
                             str(paired if paired.is_file() else REPOSITORY / "tools/ci/fixtures/broker_logs.py")))
spec = importlib.util.spec_from_file_location("review_broker_logs", SUBJECT)
logs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(logs)


class BrokerLogRedactionContracts(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="servicebus-log-redaction-contract-")
        self.root = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def capture(self, text, credentials=None, *, error=None, brokers=("servicebus",)):
        environment = {"VICIONE_SERVICEBUS_RUN_ROOT": str(self.root), **(credentials or {})}
        original = dict(environment)
        result = subprocess.CompletedProcess(["fake-compose-log-result"], 0 if error is None else 17,
                                             stdout=text, stderr=error or "")
        output, errors = io.StringIO(), io.StringIO()
        with patch.object(logs.compose_fixture, "compose", return_value=result) as compose, \
                redirect_stdout(output), redirect_stderr(errors):
            logs.capture_logs(list(brokers), environment)
        self.assertEqual(original, environment)
        self.assertEqual(len(brokers), compose.call_count)
        for call in compose.call_args_list:
            self.assertEqual(environment, call.kwargs["environment"])
        return output.getvalue(), errors.getvalue()

    def test_all_known_fixture_secret_variables_and_case_aliases_are_masked(self):
        # Independent explicit contract list, not an iteration over the implementation whitelist.
        names = ("VICIONE_SERVICEBUS_RMQ_PASS", "VICIONE_SERVICEBUS_AMQ_PASS",
                 "VICIONE_SERVICEBUS_ARTEMIS_PASS", "VICIONE_SERVICEBUS_PG_PASS",
                 "VICIONE_SERVICEBUS_AZURITE_KEY", "VICIONE_SERVICEBUS_MSSQL_PASS",
                 "VICIONE_SERVICEBUS_SERVICEBUS_KEY", "AWS_ACCESS_KEY_ID",
                 "AWS_SECRET_ACCESS_KEY", "AWS_SESSION_TOKEN", "VICIONE_SERVICEBUS_RUN_TOKEN")
        for name in names:
            for alias in (name, name.lower()):
                with self.subTest(variable=alias):
                    value = "LOCAL_TEST_CREDENTIAL.+[x]$"
                    self.capture("provider fact; credential=" + value + "; terminal fact\n", {alias: value})
                    self.assertEqual("provider fact; credential=[REDACTED]; terminal fact\n",
                                     (self.root / "servicebus-broker.log").read_text())

    def test_failed_collection_masks_console_credentials_and_does_not_write_a_log(self):
        value = "LOCAL_TEST_SQL_CREDENTIAL"
        output, errors = self.capture("unused", {"VICIONE_SERVICEBUS_MSSQL_PASS": value},
                                      error="provider denied credentials: " + value)
        self.assertEqual("", output)
        self.assertIn("WARN broker-log servicebus: not collected", errors)
        self.assertIn("[REDACTED]", errors)
        self.assertNotIn(value, errors)
        self.assertFalse((self.root / "servicebus-broker.log").exists())

    def test_redaction_precedes_console_whitespace_trimming(self):
        value = " LOCAL_TEST_SECRET_WITH_SPACES "
        _, errors = self.capture("unused", {"VICIONE_SERVICEBUS_RMQ_PASS": value}, error=value)
        self.assertIn("([REDACTED])", errors)
        self.assertNotIn(value.strip(), errors)

    def test_overlapping_values_and_regex_characters_are_literal_and_complete(self):
        self.capture("alpha.extra+[x] and alpha; unrelated alphaX\n",
                     {"VICIONE_SERVICEBUS_RMQ_PASS": "alpha",
                      "VICIONE_SERVICEBUS_PG_PASS": "alpha.extra+[x]"})
        self.assertEqual("[REDACTED] and [REDACTED]; unrelated [REDACTED]X\n",
                         (self.root / "servicebus-broker.log").read_text())

    def test_empty_and_unrelated_environment_values_preserve_log_bytes(self):
        text = "unmodified provider record\nwith exact whitespace  \n"
        for credentials in ({}, {"VICIONE_SERVICEBUS_RMQ_PASS": "",
                                 "UNRELATED_APPLICATION_VALUE": "provider"}):
            with self.subTest(credentials_present=bool(credentials)):
                self.capture(text, credentials)
                self.assertEqual(text.encode(), (self.root / "servicebus-broker.log").read_bytes())

    def test_every_selected_broker_uses_the_complete_shared_secret_scope(self):
        self.capture("sql=LOCAL_SQL; sas=LOCAL_SAS\n",
                     {"VICIONE_SERVICEBUS_MSSQL_PASS": "LOCAL_SQL",
                      "VICIONE_SERVICEBUS_SERVICEBUS_KEY": "LOCAL_SAS"},
                     brokers=("rabbitmq", "servicebus"))
        for broker in ("rabbitmq", "servicebus"):
            self.assertEqual("sql=[REDACTED]; sas=[REDACTED]\n",
                             (self.root / (broker + "-broker.log")).read_text())

    def test_refusal_evidence_still_accepts_one_and_rejects_duplicate_refusals(self):
        prefix = "Adding vhost 'vsb-contract'\n"
        refusal = "resource_locked queue refusal in vhost 'vsb-contract'\n"
        credentials = {"VICIONE_SERVICEBUS_RMQ_PASS": "LOCAL_TEST_PASSWORD"}
        for repetitions, expected in ((1, True), (2, False)):
            with self.subTest(repetitions=repetitions):
                self.capture(prefix + refusal * repetitions + "auth=LOCAL_TEST_PASSWORD\n", credentials,
                             brokers=("rabbitmq",))
                with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()):
                    actual = logs.assert_one_refusal_per_vhost(self.root / "rabbitmq-broker.log", "vsb-*")
                self.assertEqual(expected, actual)

    def test_success_output_retains_the_actual_persisted_line_count_and_no_credential(self):
        value = "LOCAL_TEST_KEY"
        output, errors = self.capture("one\nkey=" + value + "\nthree\n",
                                      {"VICIONE_SERVICEBUS_SERVICEBUS_KEY": value})
        self.assertEqual("", errors)
        self.assertIn("(3 lines)", output)
        self.assertNotIn(value, output)
        self.assertEqual(3, len((self.root / "servicebus-broker.log").read_text().splitlines()))

    def test_duplicate_secret_values_are_removed_at_every_occurrence(self):
        self.capture("LOCAL_TEST_TOKEN LOCAL_TEST_TOKEN\n",
                     {"VICIONE_SERVICEBUS_RMQ_PASS": "LOCAL_TEST_TOKEN",
                      "VICIONE_SERVICEBUS_RUN_TOKEN": "LOCAL_TEST_TOKEN"})
        self.assertEqual("[REDACTED] [REDACTED]\n", (self.root / "servicebus-broker.log").read_text())


if __name__ == "__main__":
    unittest.main(verbosity=2)

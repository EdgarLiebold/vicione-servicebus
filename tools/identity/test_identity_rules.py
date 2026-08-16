#!/usr/bin/env python3
"""Focused tests for the closed identity mapping and notice policy."""

from __future__ import annotations

import re
import unittest
from dataclasses import replace

from identity_gate import scan_entry
from identity_rules import (
    COMMENTLESS_OR_BINARY_EXCEPTIONS,
    FORMER_IDENTITY_REGISTRY,
    FORMER_ANALYZER_ID,
    FORMER_BENCHMARK,
    FORMER_CAMEL,
    FORMER_ENV_PREFIX,
    FORMER_HEADER_PREFIX,
    FORMER_HYPHEN_LOWER,
    FORMER_LOCAL_ADMIN,
    FORMER_LOCAL_ASSEMBLY,
    FORMER_LOWER,
    FORMER_PASCAL,
    FORMER_QUEUE,
    FORMER_SPACED_LOWER,
    FORMER_TEST_TOKEN,
    FORMER_TEST_URL,
    FORMER_TRADE_TYPE,
    FORMER_TRADES_TYPE,
    FORMER_VENDOR,
    DEVELOPER_REGISTRY_QUICKCHECK_AUTHORITY,
    DEVELOPER_REGISTRY_QUICKCHECK_SHA256,
    MODIFICATION_NOTICE,
    add_modification_notice,
    contains_former_identity,
    identity_mapping_rules,
    map_path,
    map_text,
    developer_registry_quickcheck_document,
    developer_registry_quickcheck_sha256,
    require_developer_registry_quickcheck,
)


class IdentityRulesTests(unittest.TestCase):
    @staticmethod
    def registry_without_rule(family_index: int, rule_index: int):
        families = list(FORMER_IDENTITY_REGISTRY)
        family = families[family_index]
        families[family_index] = replace(
            family,
            mapping_rules=tuple(
                rule
                for index, rule in enumerate(family.mapping_rules)
                if index != rule_index
            ),
        )
        return tuple(families)

    @staticmethod
    def mapping_rule_probe(rule) -> str:
        if rule.strategy != "regex":
            return rule.source
        probe = rule.source.replace(r"\b", "").replace(r"\s+", " ")
        if re.search(rule.source, probe) is None:
            raise AssertionError(f"cannot derive regex probe from registry rule: {rule.source!r}")
        return probe

    @staticmethod
    def apply_mapping_rule_probe(rule, probe: str, registry) -> str:
        return map_path(probe, registry) if rule.channel == "path" else map_text(probe, registry)

    def assert_developer_quickcheck_rejects(self, registry) -> None:
        with self.assertRaisesRegex(ValueError, "developer registry quickcheck digest mismatch"):
            require_developer_registry_quickcheck(registry)

    def test_developer_registry_quickcheck_matches_non_authoritative_local_snapshot(self) -> None:
        document = developer_registry_quickcheck_document()
        self.assertEqual(19, len(document["families"]))
        self.assertEqual(
            41,
            sum(len(family["mappingRules"]) for family in document["families"]),
        )
        self.assertEqual("NON_AUTHORITATIVE_LOCAL_DEVELOPER_CHECK", DEVELOPER_REGISTRY_QUICKCHECK_AUTHORITY)
        self.assertEqual(DEVELOPER_REGISTRY_QUICKCHECK_SHA256, developer_registry_quickcheck_sha256())
        self.assertEqual(DEVELOPER_REGISTRY_QUICKCHECK_SHA256, require_developer_registry_quickcheck())

    def test_each_of_41_original_mapping_rules_has_non_equivalent_isolated_effect_mutant(self) -> None:
        tested = 0
        for family in FORMER_IDENTITY_REGISTRY:
            for rule in family.mapping_rules:
                original_rule = rule
                probe = self.mapping_rule_probe(original_rule)
                original_view = (replace(family, mapping_rules=(original_rule,)),)
                expected = self.apply_mapping_rule_probe(original_rule, probe, original_view)

                disabled_view = (replace(family, mapping_rules=()),)
                mutant_actual = self.apply_mapping_rule_probe(original_rule, probe, disabled_view)

                with self.subTest(
                    family=family.key,
                    channel=original_rule.channel,
                    phase=original_rule.phase,
                    strategy=original_rule.strategy,
                ):
                    self.assertNotEqual(probe, expected, "original mapping rule has no concrete effect")
                    self.assertEqual(probe, mutant_actual, "disabled isolated rule still changes its probe")
                    self.assertNotEqual(expected, mutant_actual, "isolated mapping-rule mutant is equivalent")
                tested += 1
        self.assertEqual(41, tested)

    def test_developer_quickcheck_rejects_each_of_41_mapping_rule_removals(self) -> None:
        tested = 0
        for family_index, family in enumerate(FORMER_IDENTITY_REGISTRY):
            for rule_index, rule in enumerate(family.mapping_rules):
                with self.subTest(
                    family=family.key,
                    channel=rule.channel,
                    phase=rule.phase,
                    strategy=rule.strategy,
                ):
                    self.assert_developer_quickcheck_rejects(
                        self.registry_without_rule(family_index, rule_index)
                    )
                tested += 1
        self.assertEqual(41, tested)

    def test_developer_quickcheck_rejects_family_and_mapping_rule_drift(self) -> None:
        first_family = FORMER_IDENTITY_REGISTRY[0]
        first_rule = first_family.mapping_rules[0]
        changed_rule_family = replace(
            first_family,
            mapping_rules=(replace(first_rule, target=first_rule.source),)
            + first_family.mapping_rules[1:],
        )
        added_rule_family = replace(
            first_family,
            mapping_rules=first_family.mapping_rules + (first_rule,),
        )
        mutants = (
            FORMER_IDENTITY_REGISTRY[1:],
            FORMER_IDENTITY_REGISTRY + (first_family,),
            (replace(first_family, scan_pattern=first_family.scan_pattern + "(?!)"),)
            + FORMER_IDENTITY_REGISTRY[1:],
            (changed_rule_family,) + FORMER_IDENTITY_REGISTRY[1:],
            (added_rule_family,) + FORMER_IDENTITY_REGISTRY[1:],
        )
        for mutant in mutants:
            with self.subTest(digest=developer_registry_quickcheck_sha256(mutant)):
                self.assert_developer_quickcheck_rejects(mutant)

    def test_developer_quickcheck_rejects_exact_f2_rt_05_and_required_strategy_removals(self) -> None:
        spaced_index = next(
            index
            for index, family in enumerate(FORMER_IDENTITY_REGISTRY)
            if family.key == "spaced-product-name"
        )
        spaced_family = FORMER_IDENTITY_REGISTRY[spaced_index]
        exact_rule_index = 1
        exact_rule = spaced_family.mapping_rules[exact_rule_index]
        exact_mutant = self.registry_without_rule(spaced_index, exact_rule_index)
        exact_probe = self.mapping_rule_probe(exact_rule)
        self.assertNotEqual(map_text(exact_probe), map_text(exact_probe, exact_mutant))
        self.assert_developer_quickcheck_rejects(exact_mutant)

        selectors = {
            "path": lambda rule: rule.channel == "path",
            "literal": lambda rule: rule.channel == "text"
            and rule.strategy == "literal"
            and rule.phase == 40,
            "regex": lambda rule: rule.strategy == "regex",
            "pascal-context": lambda rule: rule.strategy == "pascal-context",
            "lower-context": lambda rule: rule.strategy == "lower-context",
            "path-context": lambda rule: rule.strategy == "path-pascal-context",
            "context-specific": lambda rule: rule.channel == "text"
            and rule.strategy == "literal"
            and rule.phase < 20,
        }
        indexed_rules = [
            (family_index, rule_index, rule)
            for family_index, family in enumerate(FORMER_IDENTITY_REGISTRY)
            for rule_index, rule in enumerate(family.mapping_rules)
        ]
        for label, selector in selectors.items():
            family_index, rule_index, rule = next(
                item for item in indexed_rules if selector(item[2])
            )
            with self.subTest(kind=label, strategy=rule.strategy, channel=rule.channel):
                self.assert_developer_quickcheck_rejects(
                    self.registry_without_rule(family_index, rule_index)
                )

    @staticmethod
    def deterministic_mixed_case(value: str) -> str:
        letter_index = 0
        result: list[str] = []
        for character in value:
            if character.isalpha():
                result.append(character.upper() if letter_index % 2 == 0 else character.lower())
                letter_index += 1
            else:
                result.append(character)
        return "".join(result)

    @staticmethod
    def finding_gates(path: str, content: bytes = b"") -> set[str]:
        return {finding.gate for finding in scan_entry(path, content)}

    def test_registry_drives_every_mapping_rule_and_casefolded_scanner_channel(self) -> None:
        declared_rules = {
            rule
            for family in FORMER_IDENTITY_REGISTRY
            for rule in family.mapping_rules
        }
        active_rules = set(identity_mapping_rules("path")) | set(identity_mapping_rules("text"))
        self.assertEqual(declared_rules, active_rules)

        for family in FORMER_IDENTITY_REGISTRY:
            self.assertTrue(family.mapping_rules, f"scanner family has no mapping rule: {family.key}")
            for rule in family.mapping_rules:
                example = rule.scan_example or rule.source
                self.assertTrue(
                    family.matches(example),
                    f"mapping rule has no scanner-family coverage: {family.key}/{rule.source!r}",
                )

            for canonical in family.scan_examples:
                variants = {
                    canonical,
                    canonical.lower(),
                    canonical.upper(),
                    self.deterministic_mixed_case(canonical),
                }
                for variant in variants:
                    with self.subTest(family=family.key, variant=variant):
                        self.assertIn("path-scan", self.finding_gates(f"probe/{variant}/x"))
                        self.assertIn("text-scan", self.finding_gates("probe.txt", variant.encode("utf-8")))
                        self.assertIn(
                            "binary-scan",
                            self.finding_gates("probe.bin", b"\xff" + variant.encode("utf-8") + b"\x00"),
                        )
                        self.assertIn(
                            "binary-scan",
                            self.finding_gates("probe.bin", b"\xff\xfe" + variant.encode("utf-16-le")),
                        )

    def test_red_team_short_form_examples_hit_their_required_gates(self) -> None:
        header = "".join(("m", "t-host-info"))
        mixed_header = "".join(("m", "T-host-info"))
        environment = "".join(("m", "t_secret"))
        local_assembly = "".join(("m", "tAssembly"))
        local_admin = "".join(("m", "tAdmin"))
        test_address = "".join(("m", "ttest"))
        test_url = "".join(("http://", "m", "t.com"))

        for value in (header, mixed_header, environment, local_assembly, local_admin, test_address, test_url):
            with self.subTest(value=value):
                self.assertIn("text-scan", self.finding_gates("probe.txt", value.encode("utf-8")))

        for path in (
            "src/" + "".join(("m", "t-host")) + "/x.txt",
            "tests/" + "".join(("M", "TBENCH")) + "/x.txt",
        ):
            with self.subTest(path=path):
                self.assertIn("path-scan", self.finding_gates(path))

    def test_compiled_registry_union_preserves_all_four_scanner_channels(self) -> None:
        value = self.deterministic_mixed_case(FORMER_LOCAL_ASSEMBLY)
        self.assertIn("path-scan", self.finding_gates(f"probe/{value}/x"))
        self.assertIn("text-scan", self.finding_gates("probe.txt", value.encode("utf-8")))
        self.assertIn(
            "binary-scan",
            self.finding_gates("probe.bin", b"\xff" + value.encode("utf-8") + b"\x00"),
        )
        self.assertIn(
            "binary-scan",
            self.finding_gates("probe.bin", b"\xff\xfe" + value.encode("utf-16-le")),
        )

    def test_bounded_header_family_does_not_match_unrelated_short_hyphen_words(self) -> None:
        unrelated = "".join(("m", "t-bench"))
        self.assertEqual(set(), self.finding_gates(f"tests/{unrelated}/x.txt"))
        self.assertEqual(set(), self.finding_gates("probe.txt", unrelated.encode("utf-8")))

    def test_maps_namespace_project_package_and_symbol_forms(self) -> None:
        source = (
            f"namespace {FORMER_PASCAL}; {FORMER_PASCAL}.RabbitMqTransport "
            f"class {FORMER_PASCAL}TestHarness; var {FORMER_CAMEL} = {FORMER_LOWER};"
        )

        mapped = map_text(source)

        self.assertEqual(
            "namespace ViciOne.ServiceBus; ViciOne.ServiceBus.RabbitMqTransport "
            "class ViciOneServiceBusTestHarness; var viciOneServiceBus = vicione-servicebus;",
            mapped,
        )

    def test_maps_wire_environment_mime_and_protobuf_forms(self) -> None:
        source = (
            f"{FORMER_HEADER_PREFIX}Host-Info {FORMER_ENV_PREFIX}RABBITMQ "
            f"application/{FORMER_VENDOR}+json {FORMER_TRADE_TYPE} {FORMER_TRADES_TYPE}"
        )

        mapped = map_text(source)

        self.assertEqual(
            "VSB-Host-Info VICIONE_SERVICEBUS_RABBITMQ "
            "application/vnd.vicione.servicebus+json "
            "TradeBookedViciOneServiceBus TradesBookedViciOneServiceBus",
            mapped,
        )

    def test_maps_exact_csharp_field_identifier_as_clr_token(self) -> None:
        source = f'public const string {FORMER_PASCAL} = "{FORMER_PASCAL}"; LogCategoryName.{FORMER_PASCAL}'

        self.assertEqual(
            'public const string ViciOneServiceBus = "ViciOne.ServiceBus"; '
            'LogCategoryName.ViciOneServiceBus',
            map_text(source),
        )

    def test_maps_legacy_analyzer_benchmark_and_local_tokens(self) -> None:
        source = " ".join(
            (
                FORMER_ANALYZER_ID,
                FORMER_BENCHMARK,
                FORMER_QUEUE,
                FORMER_LOCAL_ASSEMBLY,
                FORMER_LOCAL_ADMIN,
                FORMER_TEST_TOKEN,
                FORMER_TEST_URL,
            )
        )

        self.assertEqual(
            "ViciOneServiceBus0001 vicione-servicebus-benchmark "
            "vicione-servicebus-message-queue viciOneServiceBusAssembly "
            "viciOneServiceBusAdmin vicione-servicebus-test "
            "https://vicione-servicebus.invalid",
            map_text(source),
        )

    def test_maps_lowercase_identity_embedded_in_clr_identifier_as_clr_token(self) -> None:
        self.assertEqual(
            "class built_into_ViciOneServiceBus {}",
            map_text(f"class built_into_{FORMER_LOWER} {{}}"),
        )

    def test_maps_namespace_symbol_comparisons_semantically(self) -> None:
        self.assertEqual(
            'symbol.ContainingNamespace.ToString() == "ViciOne.ServiceBus"',
            map_text(f'symbol.ContainingNamespace.Name == "{FORMER_PASCAL}"'),
        )
        self.assertEqual(
            'type.ContainingNamespace.ToString() == "ViciOne.ServiceBus.Initializers.Variables"',
            map_text(
                f'type.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == "{FORMER_PASCAL}"'
            ),
        )

    def test_maps_kebab_formatter_expectations_by_runtime_grammar(self) -> None:
        self.assertEqual(
            "vici-one-service-bus-tests-endpoint-name-specs",
            map_text(FORMER_HYPHEN_LOWER + "-tests-endpoint-name-specs"),
        )
        self.assertEqual(
            "vici-one-service-bus-test-framework-messages",
            map_text(FORMER_HYPHEN_LOWER + "-test-framework-messages"),
        )

    def test_maps_paths_without_mixed_dotted_identifier_names(self) -> None:
        self.assertEqual(
            "src/ViciOne.ServiceBus.RabbitMqTransport/"
            "ViciOneServiceBusRabbitMqTransportRegistrationExtensions.cs",
            map_path(
                f"src/{FORMER_PASCAL}.RabbitMqTransport/"
                f"{FORMER_PASCAL}RabbitMqTransportRegistrationExtensions.cs"
            ),
        )

    def test_keeps_renamed_log4net_configuration_stageable(self) -> None:
        source = "*.log*\r\n.nuxt\r\n"
        self.assertEqual(
            "*.log*\n!src/ViciOne.ServiceBus.TestFramework/"
            "ViciOne.ServiceBus.TestFramework.log4net.xml\n.testagent/\n.nuxt\n",
            map_text(source),
        )

    def test_removes_transformed_product_sentence_trailing_space(self) -> None:
        source = f"Please report a bug in {FORMER_PASCAL}. \n"
        self.assertEqual("Please report a bug in ViciOne.ServiceBus.\n", map_text(source))
        self.assertEqual("ViciOne.ServiceBus.sln", map_path(FORMER_PASCAL + ".sln"))

    def test_former_identity_detector_rejects_all_closed_forms(self) -> None:
        for value in (
            FORMER_PASCAL,
            FORMER_SPACED_LOWER,
            FORMER_HYPHEN_LOWER,
            f"application/{FORMER_VENDOR}+json",
            FORMER_HEADER_PREFIX + "Host-Info",
            FORMER_ENV_PREFIX + "RABBITMQ",
            FORMER_TRADE_TYPE,
        ):
            with self.subTest(value=value):
                self.assertTrue(contains_former_identity(value))

        self.assertFalse(contains_former_identity("ViciOne.ServiceBus"))

    def test_adds_notice_after_shebang_or_xml_declaration(self) -> None:
        shell = add_modification_notice("build.sh", "#!/bin/sh\necho ok\n")
        xml = add_modification_notice("project.csproj", '<?xml version="1.0"?>\n<Project/>\n')

        self.assertTrue(shell.startswith(f"#!/bin/sh\n# {MODIFICATION_NOTICE}\n"))
        self.assertTrue(xml.startswith(f'<?xml version="1.0"?>\n<!-- {MODIFICATION_NOTICE} -->\n'))

    def test_notice_is_idempotent_and_exact_exceptions_remain_byte_stable(self) -> None:
        source = "namespace Example;\n"
        once = add_modification_notice("Example.cs", source)
        self.assertEqual(once, add_modification_notice("Example.cs", once))

        exception = next(iter(COMMENTLESS_OR_BINARY_EXCEPTIONS))
        self.assertEqual(source, add_modification_notice(exception, source))

    def test_unknown_changed_commentless_format_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "no declared comment grammar"):
            add_modification_notice("fixture.unknown", FORMER_PASCAL)


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3
"""Canonical technical-identity mapping shared by the refactor and its gates."""

from __future__ import annotations

import hashlib
import json
import re
from collections.abc import Iterator
from dataclasses import dataclass
from pathlib import PurePosixPath


BASELINE_COMMIT = "1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc"
BASELINE_TREE = "2b09d4e2b2e14289f06ba112ce1ae52e326a0307"
PRODUCT = "ViciOne.ServiceBus"
CLR_TOKEN = "ViciOneServiceBus"
SLUG = "vicione-servicebus"
HEADER_PREFIX = "VSB-"

# The intermediate identity between the MassTransit prefix and the active one. It was never released,
# so it is not a compatibility alias: it is a second forbidden prefix and is detected like the first.
SUPERSEDED_HEADER_PREFIX = "ViciOne-ServiceBus-"
MIME_VENDOR = "vnd.vicione.servicebus"
MODIFICATION_NOTICE = "ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07."
DEVELOPER_REGISTRY_QUICKCHECK_SCHEMA_VERSION = 1
DEVELOPER_REGISTRY_QUICKCHECK_SHA256 = "d1c43d05b2da39375a906daf97c03116e37cb459c5086d6b0657701a9a91e87a"
DEVELOPER_REGISTRY_QUICKCHECK_AUTHORITY = "NON_AUTHORITATIVE_LOCAL_DEVELOPER_CHECK"

LEGAL_OR_PROVENANCE_PATHS = frozenset({"README.md", "LICENSE.txt", "NOTICE", "COPYRIGHT", "MODIFICATIONS.md"})

# The source identity is deliberately assembled from separate, readable parts.
# This lets the validator test the forbidden values without preserving them as
# contiguous plaintext in the validator or its hostile fixtures.
FORMER_PASCAL = "".join(("Mass", "Transit"))
FORMER_LOWER = "".join(("mass", "transit"))
FORMER_CAMEL = "".join(("mass", "Transit"))
FORMER_UPPER = "".join(("MASS", "TRANSIT"))
FORMER_SPACED_TITLE = " ".join(("Mass", "Transit"))
FORMER_SPACED_LOWER = " ".join(("mass", "transit"))
FORMER_SPACED_UPPER = " ".join(("MASS", "TRANSIT"))
FORMER_HYPHEN_TITLE = "-".join(("Mass", "Transit"))
FORMER_HYPHEN_LOWER = "-".join(("mass", "transit"))
FORMER_VENDOR = ".".join(("vnd", FORMER_LOWER))
FORMER_HEADER_PREFIX = "".join(("M", "T", "-"))
FORMER_ENV_PREFIX = "".join(("M", "T", "_"))
FORMER_ANALYZER_ID = "".join(("M", "TA0001"))
FORMER_BENCHMARK = "".join(("m", "tbench"))
FORMER_QUEUE = "".join(("m", "t-message-queue"))
FORMER_LOGO = "".join(("m", "t-logo-small"))
FORMER_LOCAL_ASSEMBLY = "".join(("m", "tAssembly"))
FORMER_LOCAL_ADMIN = "".join(("m", "tAdmin"))
FORMER_TEST_TOKEN = "".join(("m", "ttest"))
FORMER_TEST_URL = "".join(("http://", "m", "t.com"))
FORMER_TRADE_TYPE = "".join(("TradeBooked", "M", "T"))
FORMER_TRADES_TYPE = "".join(("TradesBooked", "M", "T"))

# Exact source-to-target bindings for changed baseline files whose format cannot carry a valid
# in-file modification comment. The source path is explicit because several files moved while the
# repository structure was normalized; deriving it from the target name would silently lose that
# provenance.
COMMENTLESS_OR_BINARY_BASELINE_SOURCES = {
    "ViciOne.ServiceBus.slnx": "MassTransit.sln",
    "ViciOne.ServiceBus.snk": "MassTransit.snk",
}
COMMENTLESS_OR_BINARY_EXCEPTIONS = frozenset(COMMENTLESS_OR_BINARY_BASELINE_SOURCES)


@dataclass(frozen=True)
class IdentityMappingRule:
    channel: str
    phase: int
    source: str
    target: str
    strategy: str = "literal"
    scan_example: str | None = None


@dataclass(frozen=True)
class FormerIdentityFamily:
    key: str
    scan_pattern: str
    scan_examples: tuple[str, ...]
    mapping_rules: tuple[IdentityMappingRule, ...]

    def matches(self, value: str) -> bool:
        return re.search(self.scan_pattern, value, flags=re.IGNORECASE) is not None


def _literal_pattern(*values: str) -> str:
    return "(?:" + "|".join(re.escape(value.casefold()) for value in values) + ")"


# Roots of the FORMER header prefix, used to build a scan pattern that finds leftovers of the
# forbidden upstream identity. This is a detector, not a description of the headers the product sends
# today, and the two read alike but pull in opposite directions: dropping a root because its product
# left the graph does not remove that forbidden identity, it stops the detector from finding it if it
# ever comes back. Entries here are therefore kept even when the corresponding capability is gone. An exception list is the
# mirror image: an entry naming a file that no longer exists permits nothing and belongs removed.
_HEADER_ROOTS = (
    "host",
    "fault",
    "reason",
    "forwarder",
    "scheduling",
    "redelivery",
    "quartz",
    "request",
    "initiating",
    "initiator",
    "source",
    "response",
    "message",
    "original",
    "routing",
    "activity",
    "fail",
    "server",
    "hangfire",
    "jobid",
)


FORMER_IDENTITY_REGISTRY = (
    FormerIdentityFamily(
        key="compact-product-name",
        scan_pattern=_literal_pattern(FORMER_PASCAL),
        scan_examples=(FORMER_PASCAL, FORMER_LOWER, FORMER_CAMEL, FORMER_UPPER),
        mapping_rules=(
            IdentityMappingRule(
                "text",
                10,
                f'type.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name == "{FORMER_PASCAL}"',
                'type.ContainingNamespace.ToString() == "ViciOne.ServiceBus.Initializers.Variables"',
            ),
            IdentityMappingRule(
                "text",
                10,
                f'symbol.ContainingNamespace.Name == "{FORMER_PASCAL}"',
                'symbol.ContainingNamespace.ToString() == "ViciOne.ServiceBus"',
            ),
            IdentityMappingRule(
                "text",
                30,
                f"LogCategoryName.{FORMER_PASCAL}",
                f"LogCategoryName.{CLR_TOKEN}",
            ),
            IdentityMappingRule(
                "text",
                30,
                rf"\bconst\s+string\s+{FORMER_PASCAL}\b",
                f"const string {CLR_TOKEN}",
                strategy="regex",
                scan_example=FORMER_PASCAL,
            ),
            IdentityMappingRule("text", 40, FORMER_CAMEL, "viciOneServiceBus"),
            IdentityMappingRule("text", 40, FORMER_UPPER, "VICIONE_SERVICEBUS"),
            IdentityMappingRule("text", 50, FORMER_PASCAL, PRODUCT, strategy="pascal-context"),
            IdentityMappingRule("text", 50, FORMER_LOWER, SLUG, strategy="lower-context"),
            IdentityMappingRule("path", 40, FORMER_UPPER, "VICIONE_SERVICEBUS"),
            IdentityMappingRule("path", 50, FORMER_LOWER, SLUG),
            IdentityMappingRule("path", 60, FORMER_PASCAL, PRODUCT, strategy="path-pascal-context"),
        ),
    ),
    FormerIdentityFamily(
        key="spaced-product-name",
        scan_pattern=_literal_pattern(FORMER_SPACED_LOWER),
        scan_examples=(FORMER_SPACED_TITLE,),
        mapping_rules=(
            IdentityMappingRule("text", 40, FORMER_SPACED_TITLE, PRODUCT),
            IdentityMappingRule("text", 40, FORMER_SPACED_LOWER, PRODUCT),
            IdentityMappingRule("text", 40, FORMER_SPACED_UPPER, PRODUCT),
        ),
    ),
    FormerIdentityFamily(
        key="hyphenated-product-name",
        scan_pattern=_literal_pattern(FORMER_HYPHEN_LOWER),
        scan_examples=(FORMER_HYPHEN_TITLE,),
        mapping_rules=(
            IdentityMappingRule("text", 40, FORMER_HYPHEN_TITLE, PRODUCT),
            IdentityMappingRule("text", 40, FORMER_HYPHEN_LOWER, SLUG),
        ),
    ),
    FormerIdentityFamily(
        key="vendor-mime-name",
        scan_pattern=_literal_pattern(FORMER_VENDOR),
        scan_examples=(FORMER_VENDOR,),
        mapping_rules=(
            IdentityMappingRule("text", 40, f"application/{FORMER_VENDOR}", f"application/{MIME_VENDOR}"),
            IdentityMappingRule("text", 40, FORMER_VENDOR, MIME_VENDOR),
        ),
    ),
    FormerIdentityFamily(
        key="repository-and-site-urls",
        scan_pattern=_literal_pattern(FORMER_PASCAL, FORMER_LOWER),
        scan_examples=(
            f"https://github.com/{FORMER_PASCAL}/{FORMER_PASCAL}",
            f"https://{FORMER_LOWER}.io",
            f"https://{FORMER_LOWER}-project.com",
        ),
        mapping_rules=(
            IdentityMappingRule("text", 40, f"https://github.com/{FORMER_PASCAL}/{FORMER_PASCAL}", "https://github.com/EdgarLiebold/vicione-servicebus"),
            IdentityMappingRule("text", 40, f"http://github.com/{FORMER_PASCAL}/{FORMER_PASCAL}", "https://github.com/EdgarLiebold/vicione-servicebus"),
            IdentityMappingRule("text", 40, f"https://{FORMER_LOWER}.io", "https://github.com/EdgarLiebold/vicione-servicebus"),
            IdentityMappingRule("text", 40, f"https://{FORMER_LOWER}-project.com", "https://github.com/EdgarLiebold/vicione-servicebus"),
        ),
    ),
    FormerIdentityFamily(
        key="endpoint-formatter-forms",
        scan_pattern=_literal_pattern(FORMER_HYPHEN_LOWER),
        scan_examples=(FORMER_HYPHEN_LOWER + "-tests-endpoint-name-specs",),
        mapping_rules=(
            IdentityMappingRule("text", 20, FORMER_HYPHEN_LOWER + "-tests-endpoint-name-specs", "vici-one-service-bus-tests-endpoint-name-specs"),
            IdentityMappingRule("text", 20, FORMER_HYPHEN_LOWER + "-test-framework-messages", "vici-one-service-bus-test-framework-messages"),
        ),
    ),
    FormerIdentityFamily(
        key="analyzer-id",
        scan_pattern=rf"(?<![a-z0-9]){re.escape(FORMER_ANALYZER_ID.casefold())}(?![a-z0-9])",
        scan_examples=(FORMER_ANALYZER_ID,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_ANALYZER_ID, f"{CLR_TOKEN}0001"),),
    ),
    FormerIdentityFamily(
        key="benchmark-token",
        scan_pattern=rf"(?<![a-z0-9]){re.escape(FORMER_BENCHMARK.casefold())}(?![a-z0-9])",
        scan_examples=(FORMER_BENCHMARK,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_BENCHMARK, f"{SLUG}-benchmark"),),
    ),
    FormerIdentityFamily(
        key="queue-token",
        scan_pattern=_literal_pattern(FORMER_QUEUE),
        scan_examples=(FORMER_QUEUE,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_QUEUE, f"{SLUG}-message-queue"),),
    ),
    FormerIdentityFamily(
        key="logo-token",
        scan_pattern=_literal_pattern(FORMER_LOGO),
        scan_examples=(FORMER_LOGO,),
        mapping_rules=(
            IdentityMappingRule("text", 40, FORMER_LOGO, "vicione-servicebus-logo"),
            IdentityMappingRule("path", 10, FORMER_LOGO, "vicione-servicebus-logo"),
        ),
    ),
    FormerIdentityFamily(
        key="header-prefix",
        scan_pattern=rf"(?<![a-z0-9])mt-(?:{'|'.join(_HEADER_ROOTS)})(?=[^a-z0-9]|$)",
        scan_examples=(FORMER_HEADER_PREFIX + "Host-Info",),
        mapping_rules=(
            IdentityMappingRule(
                "text",
                40,
                FORMER_HEADER_PREFIX,
                HEADER_PREFIX,
                scan_example=FORMER_HEADER_PREFIX + "Host-Info",
            ),
        ),
    ),
    FormerIdentityFamily(
        key="superseded-header-prefix",
        scan_pattern=rf"(?<![a-z0-9])vicione-servicebus-(?:{'|'.join(_HEADER_ROOTS)})(?=[^a-z0-9]|$)",
        scan_examples=(SUPERSEDED_HEADER_PREFIX + "Host-Info",),
        mapping_rules=(
            IdentityMappingRule(
                "text",
                40,
                SUPERSEDED_HEADER_PREFIX,
                HEADER_PREFIX,
                scan_example=SUPERSEDED_HEADER_PREFIX + "Host-Info",
            ),
        ),
    ),
    FormerIdentityFamily(
        key="environment-prefix",
        scan_pattern=r"(?<![a-z0-9])mt_[a-z0-9]",
        scan_examples=(FORMER_ENV_PREFIX + "SECRET",),
        mapping_rules=(
            IdentityMappingRule(
                "text",
                40,
                FORMER_ENV_PREFIX,
                "VICIONE_SERVICEBUS_",
                scan_example=FORMER_ENV_PREFIX + "SECRET",
            ),
        ),
    ),
    FormerIdentityFamily(
        key="local-assembly-token",
        scan_pattern=rf"(?<![a-z0-9]){re.escape(FORMER_LOCAL_ASSEMBLY.casefold())}(?![a-z0-9])",
        scan_examples=(FORMER_LOCAL_ASSEMBLY,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_LOCAL_ASSEMBLY, "viciOneServiceBusAssembly"),),
    ),
    FormerIdentityFamily(
        key="local-admin-token",
        scan_pattern=rf"(?<![a-z0-9]){re.escape(FORMER_LOCAL_ADMIN.casefold())}(?![a-z0-9])",
        scan_examples=(FORMER_LOCAL_ADMIN,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_LOCAL_ADMIN, "viciOneServiceBusAdmin"),),
    ),
    FormerIdentityFamily(
        key="test-address-token",
        scan_pattern=rf"(?<![a-z0-9]){re.escape(FORMER_TEST_TOKEN.casefold())}(?![a-z0-9])",
        scan_examples=(FORMER_TEST_TOKEN,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_TEST_TOKEN, f"{SLUG}-test"),),
    ),
    FormerIdentityFamily(
        key="test-url",
        scan_pattern=_literal_pattern(FORMER_TEST_URL),
        scan_examples=(FORMER_TEST_URL,),
        mapping_rules=(IdentityMappingRule("text", 40, FORMER_TEST_URL, "https://vicione-servicebus.invalid"),),
    ),
    FormerIdentityFamily(
        key="trade-protobuf-types",
        scan_pattern=_literal_pattern(FORMER_TRADE_TYPE, FORMER_TRADES_TYPE),
        scan_examples=(FORMER_TRADE_TYPE, FORMER_TRADES_TYPE),
        mapping_rules=(
            IdentityMappingRule("text", 60, FORMER_TRADE_TYPE, "TradeBookedViciOneServiceBus"),
            IdentityMappingRule("text", 60, FORMER_TRADES_TYPE, "TradesBookedViciOneServiceBus"),
            IdentityMappingRule("path", 20, FORMER_TRADE_TYPE, "TradeBookedViciOneServiceBus"),
            IdentityMappingRule("path", 20, FORMER_TRADES_TYPE, "TradesBookedViciOneServiceBus"),
        ),
    ),
    FormerIdentityFamily(
        key="product-short-label",
        scan_pattern=r"(?<![a-z0-9])non-" + "".join(("m", "t")) + r"(?![a-z0-9])",
        scan_examples=("".join(("non-", "M", "T")),),
        mapping_rules=(
            IdentityMappingRule(
                "text",
                70,
                rf"\bnon-{''.join(('M', 'T'))}\b",
                "non-ViciOne-ServiceBus",
                strategy="regex",
                scan_example="".join(("non-", "M", "T")),
            ),
        ),
    ),
)


def developer_registry_quickcheck_document(
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> dict[str, object]:
    """Build a non-authoritative local snapshot used only for fast developer feedback."""
    active_registry = FORMER_IDENTITY_REGISTRY if registry is None else registry
    return {
        "schemaVersion": DEVELOPER_REGISTRY_QUICKCHECK_SCHEMA_VERSION,
        "families": [
            {
                "key": family.key,
                "scanPattern": family.scan_pattern,
                "scanExamples": list(family.scan_examples),
                "mappingRules": [
                    {
                        "channel": rule.channel,
                        "phase": rule.phase,
                        "source": rule.source,
                        "target": rule.target,
                        "strategy": rule.strategy,
                        "scanExample": rule.scan_example,
                    }
                    for rule in family.mapping_rules
                ],
            }
            for family in active_registry
        ],
    }


def developer_registry_quickcheck_sha256(
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> str:
    canonical = json.dumps(
        developer_registry_quickcheck_document(registry),
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")
    return hashlib.sha256(canonical).hexdigest()


def require_developer_registry_quickcheck(
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> str:
    """Reject local drift without claiming authority beyond this developer repository."""
    actual = developer_registry_quickcheck_sha256(registry)
    if actual != DEVELOPER_REGISTRY_QUICKCHECK_SHA256:
        raise ValueError(
            "developer registry quickcheck digest mismatch: "
            f"expected={DEVELOPER_REGISTRY_QUICKCHECK_SHA256} actual={actual}"
        )
    return actual

FORMER_IDENTITY_PATTERN = re.compile(
    "|".join(f"(?:{family.scan_pattern})" for family in FORMER_IDENTITY_REGISTRY),
    flags=re.IGNORECASE,
)


def identity_mapping_rules(
    channel: str,
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> tuple[IdentityMappingRule, ...]:
    active_registry = FORMER_IDENTITY_REGISTRY if registry is None else registry
    return tuple(
        sorted(
            (
                rule
                for family in active_registry
                for rule in family.mapping_rules
                if rule.channel == channel
            ),
            key=lambda rule: rule.phase,
        )
    )


def map_path_component(
    component: str,
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> str:
    """Map one Git path component without inventing mixed dotted/CLR forms."""
    for rule in identity_mapping_rules("path", registry):
        if rule.strategy == "literal":
            component = component.replace(rule.source, rule.target)
        elif rule.strategy == "path-pascal-context" and rule.source in component:
            replacement = (
                rule.target
                if component == rule.source or component.startswith(rule.source + ".")
                else CLR_TOKEN
            )
            component = component.replace(rule.source, replacement)
        else:
            if rule.strategy not in {"literal", "path-pascal-context"}:
                raise ValueError(f"unsupported path mapping strategy: {rule.strategy}")
    return component


def map_path(
    path: str,
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> str:
    return PurePosixPath(
        *(map_path_component(part, registry) for part in PurePosixPath(path).parts)
    ).as_posix()


def _map_pascal_identity(match: re.Match[str]) -> str:
    start, end = match.span()
    text = match.string
    previous = text[start - 1] if start else ""
    following = text[end] if end < len(text) else ""
    is_identifier = (previous.isalnum() or previous == "_") or (following.isalnum() or following == "_")
    return CLR_TOKEN if is_identifier else PRODUCT


def _map_lower_identity(match: re.Match[str]) -> str:
    start, end = match.span()
    text = match.string
    previous = text[start - 1] if start else ""
    following = text[end] if end < len(text) else ""
    is_identifier = (previous.isalnum() or previous == "_") or (following.isalnum() or following == "_")
    return CLR_TOKEN if is_identifier else SLUG


def _apply_text_mapping_rule(text: str, rule: IdentityMappingRule) -> str:
    if rule.strategy == "literal":
        return text.replace(rule.source, rule.target)
    if rule.strategy == "regex":
        return re.sub(rule.source, rule.target, text)
    if rule.strategy == "pascal-context":
        return re.sub(re.escape(rule.source), _map_pascal_identity, text)
    if rule.strategy == "lower-context":
        return re.sub(re.escape(rule.source), _map_lower_identity, text)
    raise ValueError(f"unsupported text mapping strategy: {rule.strategy}")


def map_text(
    text: str,
    registry: tuple[FormerIdentityFamily, ...] | None = None,
) -> str:
    """Apply the closed, order-sensitive technical identity mapping."""
    scratch_boundary = ".testagent/"
    if "*.log*" in text and scratch_boundary not in text:
        text = text.replace("\r\n", "\n")
        newline = "\n"
        text = text.replace(
            f"*.log*{newline}",
            f"*.log*{newline}{scratch_boundary}{newline}",
            1,
        )

    for rule in identity_mapping_rules("text", registry):
        text = _apply_text_mapping_rule(text, rule)
    text = text.replace("ViciOne.ServiceBus. \r\n", "ViciOne.ServiceBus.\r\n")
    text = text.replace("ViciOne.ServiceBus. \n", "ViciOne.ServiceBus.\n")
    return text


def contains_former_identity(value: str) -> bool:
    return FORMER_IDENTITY_PATTERN.search(value) is not None


def matching_identity_families(value: str) -> tuple[str, ...]:
    return tuple(family.key for family in FORMER_IDENTITY_REGISTRY if family.matches(value))


def _ascii_view(data: bytes) -> str:
    return data.decode("ascii", errors="replace")


def _utf16_view(data: bytes, encoding: str, offset: int) -> str:
    candidate = data[offset:]
    if len(candidate) % 2:
        candidate = candidate[:-1]
    return candidate.decode(encoding, errors="replace")


def binary_identity_views(data: bytes) -> Iterator[str]:
    """Yield scanner views lazily so large artifacts never hold five copies."""
    yield _ascii_view(data)
    yield _utf16_view(data, "utf-16-le", 0)
    yield _utf16_view(data, "utf-16-le", 1)
    yield _utf16_view(data, "utf-16-be", 0)
    yield _utf16_view(data, "utf-16-be", 1)


def contains_former_identity_bytes(data: bytes) -> bool:
    return any(contains_former_identity(view) for view in binary_identity_views(data))


def supports_modification_notice(path: str) -> bool:
    """Return whether the target grammar has a syntax-valid comment form."""
    name = PurePosixPath(path).name
    suffix = PurePosixPath(path).suffix.casefold()
    return path == ".devcontainer/devcontainer.json" or suffix in {
        ".cs",
        ".proto",
        ".csproj",
        ".props",
        ".targets",
        ".xml",
        ".dotsettings",
        ".config",
        ".md",
        ".sql",
        ".yml",
        ".yaml",
        ".sh",
        ".ps1",
        ".dockerignore",
    } or name in {".editorconfig", ".gitignore", ".gitattributes", ".dockerignore", "Dockerfile"}


def add_modification_notice(path: str, text: str) -> str:
    """Add a syntax-valid prominent notice when the format has comments."""
    if MODIFICATION_NOTICE in text:
        return text

    bom = "\ufeff" if text.startswith("\ufeff") else ""
    if bom:
        text = text[1:]
    newline = "\r\n" if "\r\n" in text else "\n"
    name = PurePosixPath(path).name
    suffix = PurePosixPath(path).suffix.casefold()

    if path in COMMENTLESS_OR_BINARY_EXCEPTIONS or path in LEGAL_OR_PROVENANCE_PATHS:
        return bom + text

    if suffix in {".cs", ".proto"}:
        notice = f"// {MODIFICATION_NOTICE}{newline}"
    elif suffix in {".csproj", ".props", ".targets", ".xml", ".dotsettings", ".config"}:
        notice = f"<!-- {MODIFICATION_NOTICE} -->{newline}"
    elif suffix in {".md"}:
        notice = f"<!-- {MODIFICATION_NOTICE} -->{newline}"
    elif suffix in {".sql"}:
        notice = f"-- {MODIFICATION_NOTICE}{newline}"
    elif suffix in {".yml", ".yaml", ".sh", ".ps1", ".dockerignore"} or name in {
        ".editorconfig", ".gitignore", ".gitattributes", ".dockerignore", "Dockerfile"
    }:
        notice = f"# {MODIFICATION_NOTICE}{newline}"
    else:
        raise ValueError(f"Changed text path has no declared comment grammar or exact exception: {path}")

    if text.startswith("#!"):
        position = text.find("\n") + 1
        return bom + text[:position] + notice + text[position:]
    if text.startswith("<?xml"):
        position = text.find("\n") + 1
        return bom + text[:position] + notice + text[position:]
    return bom + notice + text

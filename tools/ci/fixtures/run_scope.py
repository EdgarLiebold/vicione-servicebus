#!/usr/bin/env python3
"""Own the output directory of one provider-fixture run.

A run root contains the endpoint projection, outage-control exchange and broker logs for exactly
one native MTP invocation. A caller may hand such a root to a child only together with the secret
minted inside it, and only a direct, non-symlink child of the repository-owned run-output directory
can be adopted. This module orchestrates provider infrastructure; it does not discover, execute or
evaluate tests.
"""

from __future__ import annotations

import os
import re
import secrets
from pathlib import Path

RUN_ROOT_VARIABLE = "VICIONE_SERVICEBUS_RUN_ROOT"
RUN_TOKEN_VARIABLE = "VICIONE_SERVICEBUS_RUN_TOKEN"
RUN_TOKEN_FILE = "run-root.token"
RUN_ROOT_NAME = re.compile(r"^vicione-[0-9a-f]{12}$")


class OwnershipError(RuntimeError):
    """Raised when a run cannot show that the directory it was pointed at is its own."""


def mint_run_root(parent: Path) -> tuple[Path, str]:
    """Create a unique output root and the token proving ownership of that root."""
    root = parent / f"vicione-{secrets.token_hex(6)}"
    root.mkdir(parents=True, exist_ok=False)
    token = secrets.token_hex(16)
    (root / RUN_TOKEN_FILE).write_text(token + "\n", encoding="utf-8")

    return root, token


def claim_run_root(parent: Path, environment: dict[str, str] | None = None) -> Path:
    """Claim a handed root with proof, or mint a new root owned by this run."""
    values = os.environ if environment is None else environment
    handed = (values.get(RUN_ROOT_VARIABLE) or "").strip()
    if not handed:
        root, _ = mint_run_root(parent)

        return root

    return adopt(Path(handed), parent, (values.get(RUN_TOKEN_VARIABLE) or "").strip())


def adopt(handed: Path, parent: Path, token: str) -> Path:
    """Return the handed root only when containment, shape and token all prove ownership."""
    owned = parent.resolve()

    if handed.is_symlink():
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}', which is a symbolic link. A run root is the "
            "directory itself, because a link is a second name somebody else can repoint")

    resolved = handed.resolve()
    if not resolved.is_dir():
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}', which is not a directory that is there")
    if resolved.parent != owned:
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}', which resolves to '{resolved}' and is not a direct "
            f"child of the run area '{owned}'. This run would clean up outside the area this "
            "repository owns")
    if not RUN_ROOT_NAME.match(resolved.name):
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{resolved.name}', which is not the name of a run root minted "
            "by this repository")

    proof = resolved / RUN_TOKEN_FILE
    if proof.is_symlink() or not proof.is_file():
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}', which carries no ownership token file of its own")
    if not token or proof.read_text(encoding="utf-8").strip() != token:
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}' without a valid ownership token, so this run cannot "
            "show that the directory is its own. A run root is handed down with proof or it is not "
            "handed down at all")

    return resolved


def handover(root: Path, token: str, environment: dict[str, str]) -> dict[str, str]:
    """Return the child environment that claims exactly this root and no other."""
    handed = dict(environment)
    handed[RUN_ROOT_VARIABLE] = str(root)
    handed[RUN_TOKEN_VARIABLE] = token

    return handed

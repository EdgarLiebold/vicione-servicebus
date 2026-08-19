#!/usr/bin/env python3
"""Who owns the directory a run writes into, and how that is proved rather than believed.

A run root is the one place everything of a single run lives: its raw result file, its endpoint
projection, its control files and its broker logs. Two runs that share one are two runs deleting each
other's evidence, so the name of that directory cannot be something a run simply reads out of the
environment. Any process on the machine can set an environment variable.

So a root is either minted here, which writes a secret into it, or handed down together with that
secret. Everything else is refused. The secret is a token for ownership, not for secrecy: it says
"the process that created this directory started you", which is exactly the claim that has to hold.

Standard library only, like everything this runner chain uses before a restore.
"""

from __future__ import annotations

import os
import secrets
from pathlib import Path

RUN_ROOT_VARIABLE = "VICIONE_SERVICEBUS_RUN_ROOT"
RUN_TOKEN_VARIABLE = "VICIONE_SERVICEBUS_RUN_TOKEN"
RUN_TOKEN_FILE = "run-root.token"


class OwnershipError(RuntimeError):
    """Raised when a run cannot show that the directory it was pointed at is its own."""


def mint_run_root(parent: Path) -> tuple[Path, str]:
    """A new root nobody else has, and the token that proves it belongs to this run.

    Created with exist_ok=False on purpose: a name collision is a bug worth failing on, not something
    to write into quietly.
    """
    root = parent / f"vicione-{secrets.token_hex(6)}"
    root.mkdir(parents=True, exist_ok=False)
    token = secrets.token_hex(16)
    (root / RUN_TOKEN_FILE).write_text(token + "\n", encoding="utf-8")

    return root, token


def claim_run_root(parent: Path, environment: dict[str, str] | None = None) -> Path:
    """The root this run owns: the one it was handed with proof, or a new one of its own.

    A handed-down root is accepted only when it carries the token file whose content matches the token
    variable. A run that adopted a bare path would write into - and later clean up - a directory
    belonging to somebody else.
    """
    values = os.environ if environment is None else environment
    handed = (values.get(RUN_ROOT_VARIABLE) or "").strip()
    if not handed:
        root, _ = mint_run_root(parent)

        return root

    root = Path(handed)
    token = (values.get(RUN_TOKEN_VARIABLE) or "").strip()
    proof = root / RUN_TOKEN_FILE
    if not token or not proof.is_file() or proof.read_text(encoding="utf-8").strip() != token:
        raise OwnershipError(
            f"{RUN_ROOT_VARIABLE} names '{handed}' without a valid ownership token, so this run cannot "
            "show that the directory is its own. A run root is handed down with proof or it is not "
            "handed down at all.")

    return root


def handover(root: Path, token: str, environment: dict[str, str]) -> dict[str, str]:
    """The environment a child gets so it can claim exactly this root and no other."""
    handed = dict(environment)
    handed[RUN_ROOT_VARIABLE] = str(root)
    handed[RUN_TOKEN_VARIABLE] = token

    return handed

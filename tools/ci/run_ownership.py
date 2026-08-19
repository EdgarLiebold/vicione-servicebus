#!/usr/bin/env python3
"""Who owns the directory a run writes into, and how that is proved rather than believed.

A run root is the one place everything of a single run lives: its raw result file, its endpoint
projection, its control files and its broker logs. Two runs that share one are two runs deleting each
other's evidence, so the name of that directory cannot be something a run simply reads out of the
environment. Any process on the machine can set an environment variable.

So a root is either minted here, which writes a secret into it, or handed down together with that
secret - and the secret is not the whole claim. A token says "the process that created this directory
started you". It says nothing about where the directory is, and where it is decides what a run removes
when it cleans up after itself. A root somewhere else is a run that cleans up somewhere else, so a
handed root has to be a direct child of the area this repository owns, carrying the name this module
mints, reached without a symlink.

Standard library only, like everything this runner chain uses before a restore.
"""

from __future__ import annotations

import os
import re
import secrets
from pathlib import Path

RUN_ROOT_VARIABLE = "VICIONE_SERVICEBUS_RUN_ROOT"
RUN_TOKEN_VARIABLE = "VICIONE_SERVICEBUS_RUN_TOKEN"
RUN_TOKEN_FILE = "run-root.token"

# The name every root of this repository carries. Minted below and required on adoption, so a
# directory that happens to sit in the run area under some other name is not one of these runs.
RUN_ROOT_NAME = re.compile(r"^vicione-[0-9a-f]{12}$")


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
    """The root this run owns: the one it was handed with proof, or a new one of its own."""
    values = os.environ if environment is None else environment
    handed = (values.get(RUN_ROOT_VARIABLE) or "").strip()
    if not handed:
        root, _ = mint_run_root(parent)

        return root

    return adopt(Path(handed), parent, (values.get(RUN_TOKEN_VARIABLE) or "").strip())


def adopt(handed: Path, parent: Path, token: str) -> Path:
    """The handed root, or a refusal naming what is wrong with it.

    Contained before it is authenticated, because the two questions are different and only one of them
    was being asked. A token proves possession of a secret. It does not prove that the directory is one
    this repository owns, and ownership is what decides the blast radius: a run removes what is under
    its root, and a root that resolves anywhere else is a run that removes anywhere else. Reproduced:
    an externally created directory with a matching token was adopted without a word.

    Every alias of the same thing is resolved first, so a path built out of '..' is judged by where it
    lands rather than by how it reads. A symlink is refused instead of resolved: a link is a second
    name that somebody else can repoint after this check has passed.
    """
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
    """The environment a child gets so it can claim exactly this root and no other."""
    handed = dict(environment)
    handed[RUN_ROOT_VARIABLE] = str(root)
    handed[RUN_TOKEN_VARIABLE] = token

    return handed

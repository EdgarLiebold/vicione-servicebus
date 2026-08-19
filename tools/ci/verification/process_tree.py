#!/usr/bin/env python3
"""The process group a run owns, from the moment it is started to the moment nothing of it is left.

A test run is a tree - MSBuild nodes, a vstest console, a test host - and the group is what makes that
tree one owned thing. So the child is started in a session of its own, the whole group is signalled
rather than the process that was started, and the group is what has to be empty before a takedown may
call itself one. A parent that exits is not a group that exited: waiting for the parent alone returned
a clean takedown in a fifth of a second with a member still running, and three such trees once survived
more than thirteen hours on this machine because nothing ever asked them to stop.

Standard library only.
"""

from __future__ import annotations

import os
import signal
import subprocess
import time
from pathlib import PurePosixPath

# How long the whole owned process group is given to end after it was asked to, before the ask becomes
# a kill.
TERMINATION_GRACE_SECONDS = 20

# How long the group is given to empty after it was killed, before whatever is left is reported as a
# survivor. Nothing else can be done to it from here; what is left to do is say so.
KILL_GRACE_SECONDS = 10

# How long a green run's own process group is given to drain before a process still in it counts as a
# survivor. Measured: on a green core run an MSBuild node was still in the group the instant the child
# returned and was gone a moment later, so a census taken at that instant reports a survivor every
# time.
SURVIVOR_GRACE_SECONDS = 15

# The one process the .NET SDK deliberately keeps alive after a build, which is therefore not evidence
# that a run outlived itself. VBCSCompiler is the shared Roslyn compiler server: it idles for minutes
# by design so the next build reuses it, it is shared with every other build on this machine, and it
# holds nothing of this run - no fixture port, no file under the run root, no broker.
#
# Named rather than waited out. Its idle timeout is longer than any grace period this runner could
# sensibly have, so a longer wait would only turn a false finding into a slow false finding. Measured
# here: this control first fired on a leaked pid with no name, then on a green ActiveMQ category whose
# survivor was exactly this process.
#
# Recognised by identity and not by a word in a command line. The substring rule this replaces exempted
# every process whose command line carried the word anywhere: measured with the lead's own example,
# `python hold-port.py --label VBCSCompiler` was running and the census reported nothing at all. The
# census exists to find a process that outlived its run, so a way to be invisible to it must not be a
# way of spelling an argument.
SHARED_COMPILER_FILES = ("VBCSCompiler", "VBCSCompiler.dll")
SHARED_COMPILER_DIRECTORY = ("Roslyn", "bincore")
DOTNET_HOSTS = ("dotnet", "dotnet.exe")


def is_shared_compiler(command: str) -> bool:
    """Whether this command line is the SDK's shared compiler server, decided by what it is.

    Measured on this machine: the SDK 10 apphost runs as
    /usr/local/share/dotnet/sdk/10.0.302/Roslyn/bincore/VBCSCompiler -pipename:<name>. Older SDKs ship
    no apphost and are started through the host as `dotnet exec .../Roslyn/bincore/VBCSCompiler.dll`,
    so both forms are recognised here and nothing else is.

    A command line this cannot split into a path - one carrying spaces, for instance - stays visible.
    That is the direction to be wrong in: a compiler server reported as a survivor is a false finding
    somebody reads, and a survivor hidden by a spelling is one nobody ever sees.
    """
    arguments = command.split()
    if not arguments:
        return False

    candidates = [arguments[0]]
    if PurePosixPath(arguments[0]).name in DOTNET_HOSTS:
        rest = arguments[1:]
        if rest and rest[0] == "exec":
            rest = rest[1:]
        if rest:
            candidates.append(rest[0])

    return any(is_compiler_assembly(candidate) for candidate in candidates)


def is_compiler_assembly(argument: str) -> bool:
    """The exact file the shared compiler server is, in the exact place the SDK keeps it."""
    path = PurePosixPath(argument)
    directory = path.parent

    return (path.name in SHARED_COMPILER_FILES
            and (directory.parent.name, directory.name) == SHARED_COMPILER_DIRECTORY)


def group_members(group: int) -> list[str]:
    """Every live process in one process group, as "pid command", newest census each call.

    The group leader itself is included when it is still there. A process the kernel is holding as a
    zombie is not: it has already ended, it holds no port, no file and no broker, and reporting one as
    a survivor would be reporting an accounting entry.
    """
    listing = subprocess.run(["ps", "-Ao", "pid,pgid,command"], capture_output=True, text=True,
                             check=False)
    members = []
    for line in listing.stdout.splitlines()[1:]:
        parts = line.split(None, 2)
        if len(parts) != 3 or not parts[1].isdigit() or not parts[0].isdigit():
            continue
        if int(parts[1]) != group:
            continue
        if parts[2].rstrip().endswith("<defunct>"):
            continue
        members.append(f"{parts[0]} {parts[2][:200]}")

    return members


def signal_group(group: int, which: int) -> bool:
    """Signals the whole group and says whether there was anything there to signal."""
    try:
        os.killpg(group, which)
    except (ProcessLookupError, PermissionError):
        return False

    return True


def wait_for_group(group: int, seconds: float, child: subprocess.Popen | None = None) -> list[str]:
    """Waits until the group is empty or the time is up, and returns what is still in it.

    The child is polled while waiting so that the process this runner started is reaped rather than
    left as a zombie for the census to trip over.
    """
    deadline = time.monotonic() + seconds
    while True:
        if child is not None:
            child.poll()
        members = group_members(group)
        if not members or time.monotonic() >= deadline:
            return members
        time.sleep(0.2)


def terminate_tree(child: subprocess.Popen) -> dict[str, object]:
    """Asks the whole owned process group to end and does not return while any of it is still there.

    The group is the ownership boundary, which is why the child is started in a session of its own:
    dotnet test is a tree of MSBuild nodes, a vstest console and a test host, and signalling only the
    process that was started leaves the rest of them running. Three such trees survived more than
    thirteen hours on this machine because nothing ever asked them to stop.

    A parent that exits is not a group that exited, and that was the defect: this waited for the child
    alone, so a parent which took the TERM and left returned it in a fifth of a second with the rest of
    its group untouched and SIGKILL never sent. Reproduced with a parent that exits on TERM and a
    grandchild that ignores it - one member left, reported as a clean takedown.
    """
    group = child.pid
    if not signal_group(group, signal.SIGTERM):
        child.poll()

        return {"signalled": False, "escalatedToKill": False, "survivors": group_members(group)}

    remaining = wait_for_group(group, TERMINATION_GRACE_SECONDS, child)
    if not remaining:
        return {"signalled": True, "escalatedToKill": False, "survivors": []}

    escalated = signal_group(group, signal.SIGKILL)

    return {"signalled": True, "escalatedToKill": escalated,
            "survivors": wait_for_group(group, KILL_GRACE_SECONDS, child)}


def surviving_owned_processes(group: int) -> list[str]:
    """Processes still in this run's own process group, read after a grace period, with what they are.

    The grace period is what makes this a finding rather than noise. Measured on a green core run: an
    MSBuild node was still in the group the instant the child returned and was gone shortly after, so
    a census taken at that moment reports a survivor on every successful run.

    The command line is part of the finding, not decoration. A survivor reported as a number alone is
    a mystery by the time anybody reads it - the process is gone and nothing says what leaked, which is
    exactly what happened the first time this control fired.
    """
    deadline = time.monotonic() + SURVIVOR_GRACE_SECONDS
    while True:
        survivors = [member for member in group_members(group)
                     if int(member.split(None, 1)[0]) != group and not is_shared_compiler(
                         member.split(None, 1)[1] if " " in member else "")]
        if not survivors or time.monotonic() >= deadline:
            return survivors
        time.sleep(0.5)


def run_child(command: list[str], environment: dict[str, str], budget: float) -> dict[str, object]:
    """Runs the test process under a finite budget, in a session it alone owns.

    The budget is the point. This runner is the canonical local entry point as well as the one CI
    calls, and a job level timeout in a workflow does nothing for a developer machine. Without it a
    test process that never returns holds the run for as long as the machine stays up.
    """
    started = time.monotonic()
    try:
        child = subprocess.Popen(command, env=environment, text=True, start_new_session=True,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except OSError as error:
        # A child that never started is a result of this run like any other. Letting the exception out
        # ends the process before it writes the one record that says what happened, so the caller would
        # be left with a traceback instead of a red category.
        return {
            "exitCode": None,
            "stdout": "",
            "stderr": f"the child could not be started: {error}\n",
            "timedOut": False,
            "escalatedToKill": False,
            "seconds": round(time.monotonic() - started, 3),
            "survivingOwnedProcesses": [],
        }

    timed_out = False
    killed = False
    try:
        out, err = child.communicate(timeout=budget)
    except subprocess.TimeoutExpired:
        timed_out = True
        takedown = terminate_tree(child)
        killed = bool(takedown["escalatedToKill"])
        try:
            out, err = child.communicate(timeout=TERMINATION_GRACE_SECONDS)
        except subprocess.TimeoutExpired:
            # Something is still holding the pipes after the group was taken down. Killing the parent
            # here is what this used to do and it addresses the one process that has already ended;
            # the group is the thing that can still be holding anything, so the group is signalled
            # again and what is left of it is reported by the census below.
            signal_group(child.pid, signal.SIGKILL)
            try:
                out, err = child.communicate(timeout=KILL_GRACE_SECONDS)
            except subprocess.TimeoutExpired:
                out, err = "", ""

    return {
        "exitCode": child.returncode,
        "stdout": out or "",
        "stderr": err or "",
        "timedOut": timed_out,
        "escalatedToKill": killed,
        "seconds": round(time.monotonic() - started, 3),
        "survivingOwnedProcesses": surviving_owned_processes(child.pid),
    }

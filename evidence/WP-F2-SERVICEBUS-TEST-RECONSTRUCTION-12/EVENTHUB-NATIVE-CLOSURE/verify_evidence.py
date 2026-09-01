#!/usr/bin/env python3
from __future__ import annotations

import gzip
import hashlib
import json
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[2]


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def git(*args: str) -> bytes:
    return subprocess.run(["git", *args], cwd=REPO, check=True, capture_output=True).stdout


def evidence_text(path: Path) -> str:
    if path.suffix == ".gz":
        with gzip.open(path, "rt") as stream:
            return stream.read()
    return path.read_text()


def ctrf_summary(path: Path) -> dict[str, int]:
    return json.loads(path.read_text())["results"]["summary"]


def aggregate_ctrf(directory: Path) -> tuple[int, dict[str, int]]:
    totals = {key: 0 for key in ("tests", "passed", "failed", "pending", "skipped", "other")}
    files = sorted(directory.glob("*.ctrf"))
    for path in files:
        summary = ctrf_summary(path)
        for key in totals:
            totals[key] += summary[key]
    return len(files), totals


def assert_sha_manifest() -> None:
    expected = {}
    for line in (ROOT / "SHA256SUMS").read_text().splitlines():
        digest, relative = line.split("  ", 1)
        expected[relative] = digest
    actual = {
        path.relative_to(ROOT).as_posix(): sha256(path.read_bytes())
        for path in ROOT.rglob("*")
        if path.is_file() and path.name != "SHA256SUMS"
    }
    assert expected == actual, (set(expected) ^ set(actual), {p for p in expected & actual if expected[p] != actual[p]})


RECIPES = {
    "M01": (
        "await Riders.StopRiders(cancellationToken).ConfigureAwait(false);",
        "await Riders.Stop(cancellationToken).ConfigureAwait(false);",
    ),
    "M02": ("_handles.Remove(handle.Key);", "GC.KeepAlive(current);"),
    "M03": (
        "await confirmation.Confirmed.OrCanceled(_cancellationToken).ConfigureAwait(false);",
        "GC.KeepAlive(confirmation);",
    ),
    "M04": (
        "public DateTimeOffset EnqueuedTime => _eventData.EnqueuedTime;",
        "public DateTimeOffset EnqueuedTime => default;",
    ),
    "M05": ("_configureOptions?.Invoke(options);", "GC.KeepAlive(options);"),
    "M06": (
        "sendContexts[i] as EventHubMessageSendContext<T>",
        "sendContexts[0] as EventHubMessageSendContext<T>",
    ),
    "M07": ("PartitionKey = context.PartitionKey", "PartitionKey = null"),
    "M08": (
        """                return _messageFactory.Use(exceptionContext, async (ctx, s) =>
                {
                    var producer = await ctx.GetProducer(ctx, _nameProvider(ctx)).ConfigureAwait(false);

                    await producer.Produce(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
                });""",
        """                GC.KeepAlive(exceptionContext);
                return Task.CompletedTask;""",
    ),
    "M09": (
        """                await _messageFactory.Use(exceptionContext, async (ctx, s) =>
                {
                    var producer = await ctx.GetProducer(ctx, _nameProvider(ctx)).ConfigureAwait(false);

                    await producer.Produce(s.Message, s.Pipe, ctx.CancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);""",
        "                GC.KeepAlive(exceptionContext);",
    ),
    "M10": ("await _partitionClosingHandler(args).ConfigureAwait(false);", "GC.KeepAlive(args);"),
    "M11": ("await _partitionInitializingHandler(args).ConfigureAwait(false);", "GC.KeepAlive(args);"),
    "M12": (
        """configurator.TryAddScoped<IEventHubRider, Bind<TBus, IEventHubProducerProvider>>((rider, provider) =>
                Bind<TBus>.Create(GetCurrentProducerProvider(rider, provider)));""",
        "GC.KeepAlive(configurator);",
    ),
    "M13": (
        """    for broker in dict.fromkeys(brokers):
        result = compose("up", "-d", "--wait", broker, capture=True, environment=environment)
        if result.returncode != 0:
            raise RunnerError(f"the {broker} fixture did not become ready: {result.stderr.strip()}")""",
        """    result = compose("up", "-d", "--wait", *dict.fromkeys(brokers), capture=True, environment=environment)
    if result.returncode != 0:
        raise RunnerError(f"the {', '.join(brokers)} fixture did not become ready: {result.stderr.strip()}")""",
    ),
    "M14": (
        """            IConsumerConvention[] conventions;
            lock (Cached.MutateLock)
                conventions = Cached.Registered.ToArray();

            return conventions.Select(convention => convention.GetConsumerMessageConvention<T>()).ToArray();""",
        "            return Cached.Registered.Select(convention => convention.GetConsumerMessageConvention<T>());",
    ),
}


def main() -> None:
    results = json.loads((ROOT / "FINAL_RESULTS.json").read_text())
    manifest = json.loads((ROOT / "MUTATION_MANIFEST.json").read_text())
    technical = results["technicalCommit"]
    assert git("rev-parse", f"{technical}^{{tree}}").decode().strip() == results["technicalTree"]
    assert git("rev-parse", f"{technical}^").decode().strip() == results["technicalParent"]

    expected_diff = git("diff", "--binary", f"{results['baselineCommit']}..{technical}")
    assert gzip.open(ROOT / "TECHNICAL_DIFF.patch.gz", "rb").read() == expected_diff
    assert not git("ls-tree", "-r", "--name-only", technical, "--", results["retirement"]["oldProjectRoot"])

    for mutation in manifest["mutations"]:
        baseline = git("show", f"{technical}:{mutation['target']}")
        old, new = (part.encode() for part in RECIPES[mutation["id"]])
        assert baseline.count(old) == 1, mutation["id"]
        mutant = baseline.replace(old, new, 1)
        assert sha256(baseline) == mutation["baselineSha256"] == mutation["restoredSha256"]
        assert sha256(mutant) == mutation["mutantSha256"]
        for log_key in ("buildLog", "testLog"):
            if log_key in mutation:
                assert (ROOT / mutation[log_key]).is_file(), (mutation["id"], log_key)

    unit_files, unit = aggregate_ctrf(ROOT / "positive" / "unit")
    local_files, local = aggregate_ctrf(ROOT / "positive" / "local")
    assert unit_files == 21 and unit == {"tests": 2858, "passed": 2858, "failed": 0, "pending": 0, "skipped": 0, "other": 0}
    assert local_files == 10 and local == {"tests": 398, "passed": 398, "failed": 0, "pending": 0, "skipped": 0, "other": 0}
    focused = ctrf_summary(ROOT / "positive" / "eventhub-focused.json")
    assert {key: focused[key] for key in ("tests", "passed", "failed", "skipped")} == {
        "tests": 23, "passed": 23, "failed": 0, "skipped": 0
    }
    for mutation_id in ("M08", "M09", "M14"):
        summary = ctrf_summary(ROOT / "mutations" / f"{mutation_id}.json")
        assert (summary["tests"], summary["passed"], summary["failed"], summary["skipped"]) == (1, 0, 1, 0)

    rows = (ROOT / "R0_DISPOSITIONS.tsv").read_text().splitlines()[1:]
    assert len(rows) == 25
    assert sum("\tREPLACED_EXECUTING\t" in row for row in rows) == 22
    assert sum("\tEXTERNAL_PENDING\t" in row for row in rows) == 3
    assert json.loads((ROOT / "fixture" / "fixture-findings.json").read_text())["findings"] == []
    assert "Ran 262 tests" in evidence_text(ROOT / "positive" / "ci-tool-tests.log.gz")
    identity_log = evidence_text(ROOT / "positive" / "identity-tests.log.gz")
    assert "Ran 148 tests" in identity_log and "OK" in identity_log
    assert (ROOT / "positive" / "engineering-build.binlog").stat().st_size > 1_000_000
    assert_sha_manifest()
    print("PASS Event Hubs native closure evidence")


if __name__ == "__main__":
    main()

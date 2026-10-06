#!/usr/bin/env python3
"""Bind public benchmark contracts to actual rebuilt original and mutant tools."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys

W = Path(__file__).resolve().parent.parent
D = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR'
O = W / 'repositories/vicione-servicebus'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
M = R / 'mutations/source-main'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
P = R / 'repair-consumers/package07/compiled-tools'
BIN = P / 'bin/Release/net10.0'
CONSOLE = 'benchmarks/ViciOne.ServiceBus.BenchmarkConsole/'
LATENCY = 'benchmarks/ViciOne.ServiceBus.Benchmark/'
BATCH = CONSOLE + 'MediatorBatchBenchmark.cs'
JSON_SOURCE = CONSOLE + 'DeserializationBenchmark.cs'
METRICS = LATENCY + 'Latency/MessageMetricCapture.cs'
PROJECTS = {'console': CONSOLE + 'ViciOne.ServiceBus.BenchmarkConsole.csproj',
            'latency': LATENCY + 'ViciOne.ServiceBus.Benchmark.csproj'}
TOOLS = {'console': ('ViciOne.ServiceBus.BenchmarkConsole', 'ViciOne.ServiceBus.BenchmarkConsole.dll'),
         'latency': ('ViciOne.ServiceBus.Benchmark', 'vicione-servicebus-benchmark.dll')}
MODES = ['deserialization', 'mediator-batches', 'unique', 'duplicate', 'concurrent-duplicate',
         'concurrent-unique', 'duplicate-send-observer']


def sha(data):
    return hashlib.sha256(data).hexdigest()


def operation(phase, cwd, command, expected=0):
    phase = 'package07-r2-' + phase
    assert not any(json.loads(line)['phase'] == phase for line in (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines())
    process = subprocess.run([sys.executable, str(D / 'run_operation.py'), phase, str(cwd), '--', *command],
                             stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    receipt = next(json.loads(line) for line in (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()
                   if json.loads(line)['phase'] == phase)
    assert receipt['exit_code'] == process.returncode == expected, (phase, process.stdout)
    raw = Path(receipt['log']).read_bytes()
    assert sha(raw) == receipt['log_sha256']
    print(json.dumps({'phase': phase, 'exit': expected}), flush=True)
    return receipt, raw.decode()


def build(name, family):
    return operation(name, M, ['dotnet', 'build', PROJECTS[family], '-c', 'Release', '--no-restore', '-warnaserror'])[0]


def install(family, name):
    project, dll = TOOLS[family]
    source = M / 'artifacts/sdk/bin' / project / 'release' / dll
    assert source.is_file()
    snapshot = R / 'repair-evidence/package07-benchmark-binaries' / name / dll
    snapshot.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, snapshot)
    shutil.copyfile(source, BIN / dll)
    return {'compiled_tool': str(source), 'snapshot': str(snapshot), 'runtime_tool': str(BIN / dll),
            'sha256': sha(source.read_bytes())}


def run(name, mode, expected):
    receipt, log = operation(name + '-' + mode, P, ['dotnet', str(BIN / 'CompiledBenchmarkContracts.dll'), mode], expected)
    observed = json.loads(log.strip().splitlines()[-1])
    assert observed['mode'] == mode and observed['passed'] == (expected == 0), observed
    assert 'TimeoutException' not in log and 'error CS' not in log, log
    if expected:
        admissible = observed['error'] == 'System.InvalidOperationException' and (
            observed['Message'].startswith('Contract assertion:') or 'more than one element' in observed['Message'])
        if mode == 'mediator-batches':
            admissible = observed['error'] == 'ViciOne.ServiceBus.ConfigurationException' and 'Limits' in log
        if mode == 'deserialization':
            admissible = observed['error'] == 'System.ArgumentNullException' and observed['parameter'] == 'headers'
        assert admissible, observed
    else:
        for family, property_name in [('console', 'consoleDll'), ('latency', 'benchmarkDll')]:
            assert observed[property_name] == sha((BIN / TOOLS[family][1]).read_bytes())
    return {'mode': mode, 'native_receipt': receipt, 'observed': observed}


def replace_once(text, before, after):
    assert text.count(before) == 1, before
    return text.replace(before, after)


def main():
    paths = [BATCH, JSON_SOURCE, METRICS]
    original = {path: (F / path).read_bytes() for path in paths}
    corrected = {path: (O / path).read_bytes() for path in paths}
    assert all((M / path).read_bytes() == original[path] for path in paths)
    oracle_hashes = {str(path): sha(path.read_bytes()) for path in [P / 'Program.cs', P / 'CompiledBenchmarkContracts.csproj',
                     P / 'NuGet.Config', P / 'packages.lock.json', BIN / 'CompiledBenchmarkContracts.dll']}
    owned_runtime = {dll: (BIN / dll).read_bytes() for _, dll in TOOLS.values()}
    result = {'status': 'RUNNING', 'findings': ['F024', 'F025', 'F026'], 'oracle_hashes': oracle_hashes,
              'baseline': [], 'controls': [], 'mutants': [], 'rollback': []}
    mutants = []
    batch = corrected[BATCH].decode()
    needle = '            cfg.Limits(MessageLimits.Conservative);\n'
    assert batch.count(needle) == 2
    starts = [i for i in range(len(batch)) if batch.startswith(needle, i)]
    for number, index in enumerate(starts):
        mutants.append((f'batch-limits-lost-{number}', BATCH, batch[:index] + batch[index + len(needle):], 'console', 'mediator-batches'))
    mutants.append(('json-null-headers', JSON_SOURCE, original[JSON_SOURCE].decode(), 'console', 'deserialization'))
    metrics = corrected[METRICS].decode()
    mutants.append(('consume-original-bag', METRICS, original[METRICS].decode(), 'latency', 'duplicate'))
    mutants.append(('consume-duplicate-counted', METRICS, replace_once(metrics,
        '        if (!_consumedMessages.TryAdd(messageId, new ConsumedMessage(messageId, _clock.ElapsedTicks)))\n            return TaskResults.Completed;',
        '        _consumedMessages.TryAdd(messageId, new ConsumedMessage(messageId, _clock.ElapsedTicks));'), 'latency', 'duplicate'))
    mutants.append(('consume-first-timestamp-overwritten', METRICS, replace_once(metrics,
        '        if (!_consumedMessages.TryAdd(messageId, new ConsumedMessage(messageId, _clock.ElapsedTicks)))\n            return TaskResults.Completed;',
        '        if (!_consumedMessages.TryAdd(messageId, new ConsumedMessage(messageId, _clock.ElapsedTicks)))\n        {\n            _consumedMessages[messageId] = new ConsumedMessage(messageId, _clock.ElapsedTicks);\n            return TaskResults.Completed;\n        }'), 'latency', 'duplicate'))
    mutants.append(('consume-join-duplicated', METRICS, replace_once(metrics,
        '.Join(_consumedMessages.Values,', '.Join(_consumedMessages.Values.Concat(_consumedMessages.Values),'), 'latency', 'unique'))
    mutants.append(('consume-counter-lost', METRICS, replace_once(metrics,
        'var consumed = Interlocked.Increment(ref _consumed);', 'var consumed = Interlocked.Add(ref _consumed, 0);'), 'latency', 'unique'))
    mutants.append(('consume-identity-collapsed', METRICS, replace_once(metrics,
        '_consumedMessages.TryAdd(messageId,', '_consumedMessages.TryAdd(Guid.Empty,'), 'latency', 'concurrent-unique'))
    try:
        for family in PROJECTS:
            operation('isolated-' + family + '-restore', M, ['dotnet', 'restore', PROJECTS[family], '--locked-mode', '--disable-parallel'])
            build('isolated-original-' + family + '-build', family)
            install(family, 'original')
        for mode in MODES:
            result['baseline'].append(run('original', mode, 2 if mode in ['deserialization', 'mediator-batches', 'duplicate', 'concurrent-duplicate'] else 0))
        for path, data in corrected.items(): (M / path).write_bytes(data)
        for family in PROJECTS:
            build('isolated-corrected-' + family + '-build', family)
            install(family, 'corrected')
        for mode in MODES: result['controls'].append(run('isolated-corrected', mode, 0))
        for name, path, code, family, mode in mutants:
            (M / path).write_text(code)
            build_receipt = build('mutant-' + name + '-build', family)
            binary = install(family, name)
            assertion = run('mutant-' + name, mode, 2)
            result['mutants'].append({'name': name, 'source': path, 'source_sha256': sha(code.encode()),
                'source_before_sha256': sha(corrected[path]), 'build_receipt': build_receipt, 'binary': binary,
                'assertion': assertion, 'status': 'KILLED_BY_CAUSAL_PUBLIC_CONTRACT'})
            (M / path).write_bytes(corrected[path])
            (E / 'PACKAGE07_BENCHMARK_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
        for family in PROJECTS:
            build('isolated-rollback-' + family + '-build', family)
            install(family, 'rollback')
        for mode in MODES: result['rollback'].append(run('rollback', mode, 0))
        result['status'] = 'EXECUTED_ALL_9_MUTANTS_KILLED_CORRECTED_ROLLBACK_GREEN'
    finally:
        for path, data in original.items(): (M / path).write_bytes(data)
        for dll, data in owned_runtime.items(): (BIN / dll).write_bytes(data)
        rows = list(csv.DictReader((R / 'FILE_COVERAGE.csv').open()))
        assert len(rows) == 6209
        mismatches = [row['path'] for row in rows if sha((M / row['path']).read_bytes()) != row['sha256']]
        result['original_checkout_restored_files'] = len(rows)
        result['original_checkout_mismatches'] = mismatches
        result['owned_oracle_runtime_restored'] = all((BIN / dll).read_bytes() == data for dll, data in owned_runtime.items())
        result['oracle_hashes_preserved'] = all(sha(Path(path).read_bytes()) == expected for path, expected in oracle_hashes.items())
        (E / 'PACKAGE07_BENCHMARK_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
        assert not mismatches and result['owned_oracle_runtime_restored'] and result['oracle_hashes_preserved']


if __name__ == '__main__':
    main()

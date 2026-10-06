#!/usr/bin/env python3
"""Verify the real public normal Build generation and its settings/options mutants."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

W = Path(__file__).resolve().parent.parent
D = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR'
O = W / 'repositories/vicione-servicebus'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
M = R / 'mutations/source-main'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
BASE = 'src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/Configuration/'
BUILDER = BASE + 'EventHubReceiveEndpointBuilder.cs'
CONFIG = BASE + 'EventHubReceiveEndpointConfigurator.cs'
OWNER = 'tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/'
PROJECT = OWNER + 'ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.csproj'
FIXTURES = [OWNER + 'EventHubIntegration/' + name for name in (
    'EventHubClosingRegressionTests.cs', 'EventHubReceivePolicyRegressionTests.cs',
    'EventHubDeferredCallerCancellationRegressionTests.cs', 'EventHubBuiltContextCapture.cs')]
PROJECTION = OWNER + 'Requirements/EventHubLocalIntegrationRequirements.json'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def operation(phase, command, failures=None):
    expected = 2 if failures else 0
    existing = [row for row in map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()) if row['phase'] == phase]
    if existing:
        assert '--resume-owned-unchanged-inputs' in sys.argv and len(existing) == 1
        receipt = existing[0]; assert receipt['command'] == command and receipt['cwd'] == str(M)
    else:
        output = subprocess.run([sys.executable, str(D / 'run_operation.py'), phase, str(M), '--', *command],
                                stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        receipt = next(row for row in map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()) if row['phase'] == phase)
        assert output.returncode == expected, (phase, output.stdout)
    assert receipt['exit_code'] == expected
    raw = Path(receipt['log']).read_bytes(); assert sha(raw) == receipt['log_sha256']
    log = raw.decode()
    if failures is not None:
        assert 'gesamt: 9' in log and 'übersprungen: 0' in log, (phase, log)
        if failures:
            assert 'Assert.' in log and 'TimeoutException' not in log and 'error CS' not in log, (phase, log)
            assert 'can only be used once' not in log, (phase, log)
            if type(failures) is int: assert f'fehlgeschlagen: {failures}' in log, (phase, log)
        else: assert 'fehlgeschlagen: 0' in log and 'erfolgreich: 9' in log
    print(json.dumps({'phase': phase, 'native_exit': expected, 'cases': 9 if failures is not None else None}), flush=True)
    return receipt


def test(phase, failures):
    return operation(phase, ['env', 'VICIONE_TESTS__Profile=LocalIntegration', 'dotnet', 'test', '--project', PROJECT,
        '-c', 'Release', '--no-restore', '--filter-method',
        '*.BuiltSettings_RemainOneImmutablePolicyAfterLaterConfigurationChangesAsync',
        '*.BuiltClientFactory_UsesOriginalOptionsForCompletedGenerationAsync', '--minimum-expected-tests', '9'], failures)


def main():
    originals = {path: (F / path).read_bytes() for path in [BUILDER, CONFIG, PROJECTION]}
    corrected = {path: (O / path).read_bytes() for path in [BUILDER, CONFIG]}
    regressions = {path: (O / path).read_bytes() for path in FIXTURES}
    assert all((M / path).read_bytes() == data for path, data in originals.items())
    assert all(not (M / path).exists() for path in FIXTURES)
    result = {'status': 'RUNNING', 'findings': ['F016'], 'checks': [], 'mutants': [],
              'regression_hashes': {path: sha(data) for path, data in regressions.items()},
              'proof_limit': 'Actual normal public DI Build observed by a forwarding public SPI adapter, preserving explicit limits payload. No SDK processor startup/cloud delivery or independent standalone Blob-lazy generation claim.'}
    if '--resume-owned-unchanged-inputs' in sys.argv:
        previous = E / 'PACKAGE03_EVENTHUB_SETTINGS_MUTATIONS.json'; prior = json.loads(previous.read_text())
        assert prior['status'] == 'RUNNING' and prior['regression_hashes'] == result['regression_hashes']
        preserved = E / 'PACKAGE03_EVENTHUB_SETTINGS_MUTATIONS_ATTEMPT1.json'; assert not preserved.exists()
        preserved.write_bytes(previous.read_bytes())
        result['resume_evidence'] = str(preserved)
        result['resume_reason'] = 'Boolean failure-count sentinel in the already loaded Python module compared to text True. Actual native case was causal red; no native receipts overwritten, exact fixture hashes reused.'
    mutants = [('live-settings-alias', BUILDER, corrected[BUILDER].decode().replace(
        '_receiveSettings = new SettingsSnapshot(receiveSettings);', '_receiveSettings = receiveSettings;'), 7)]
    for property_name, replacement in [('ConsumerGroup', '"corrupted"'), ('ContainerName', '"corrupted"'),
        ('EventHubName', '"corrupted"'), ('CheckpointMessageLimit', '0'), ('CheckpointMessageCount', '0'),
        ('PrefetchCount', '0'), ('CheckpointInterval', 'TimeSpan.FromMinutes(2)'),
        ('ConcurrentMessageLimit', '0'), ('ConcurrentDeliveryLimit', '0')]:
        code = corrected[BUILDER].decode(); before = f'{property_name} = settings.{property_name};'
        assert code.count(before) == 1
        failures = 9 if property_name in ['ConsumerGroup', 'EventHubName'] else 7
        mutants.append(('wrong-snapshot-' + property_name.lower(), BUILDER, code.replace(before, f'{property_name} = {replacement};'), failures))
    code = corrected[CONFIG].decode(); before = 'configureOptions?.Invoke(options);'; assert code.count(before) == 1
    mutants.append(('live-options-callback', CONFIG, code.replace(before, '_configureOptions?.Invoke(options);'), 1))
    try:
        for path, data in regressions.items(): (M / path).write_bytes(data)
        (M / PROJECTION).write_bytes((O / PROJECTION).read_bytes())
        result['checks'].append(operation('package03-eh-settings-isolated-restore', ['dotnet', 'restore', PROJECT, '--locked-mode', '--disable-parallel']))
        result['checks'].append(test('package03-eh-settings-isolated-original', 8))
        for path, data in corrected.items(): (M / path).write_bytes(data)
        result['checks'].append(test('package03-eh-settings-isolated-corrected', 0))
        for name, path, code, failures in mutants:
            mutant = code.encode(); assert mutant != corrected[path]; (M / path).write_bytes(mutant)
            assert all((M / p).read_bytes() == b for p, b in regressions.items())
            receipt = test('package03-eh-settings-mutant-' + name, failures)
            result['mutants'].append({'name': name, 'source': path, 'source_sha256': sha(mutant),
                'status': 'KILLED_BY_NATIVE_CONTRACT_ASSERTION', 'native_receipt': receipt})
            (M / path).write_bytes(corrected[path])
        result['checks'].append(test('package03-eh-settings-isolated-rollback', 0))
        result['status'] = 'EXECUTED_ALL_11_KILLED_CORRECTED_ROLLBACK_GREEN'
    finally:
        for path, data in originals.items(): (M / path).write_bytes(data)
        for path in FIXTURES: (M / path).unlink(missing_ok=True)
        result['original_inputs_restored'] = all((M / p).read_bytes() == b for p, b in originals.items())
        result['new_fixtures_removed'] = all(not (M / p).exists() for p in FIXTURES)
        (E / 'PACKAGE03_EVENTHUB_SETTINGS_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
    assert result['original_inputs_restored'] and result['new_fixtures_removed']


if __name__ == '__main__':
    main()

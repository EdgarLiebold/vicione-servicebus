#!/usr/bin/env python3
"""Execute four isolated topology mutants; exclude the independently red F038 case."""
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
BASE = 'src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureServiceBusTransport/Configuration/Topology/'
SESSION = BASE + 'SessionIdSendTopologyConvention.cs'
TYPED = BASE + 'SessionIdMessageSendTopologyConvention.cs'
RULE = BASE + 'SubscriptionConsumeTopologySpecification.cs'
TEST = 'tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests/ServiceBusFamilyRegressionTests.cs'
PROJECT = 'tests/Transports/ViciOne.ServiceBus.AzureServiceBus.Tests/ViciOne.ServiceBus.AzureServiceBus.Tests.csproj'
METHODS = [('DefaultSessionFormatter_ReachesTheSendFilterAndPreservesExplicitOverridesAsync', 1),
           ('ConsumeSubscriptions_RetainRuleFilterValidation', 2),
           ('BusConstruction_RejectsConflictingConsumeRuleAndFilter', 2)]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def operation(phase, command, expected, cases=None, method=None):
    actual = subprocess.run([sys.executable, str(D / 'run_operation.py'), phase, str(M), '--', *command]).returncode
    receipt = next(row for row in map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()) if row['phase'] == phase)
    assert actual == receipt['exit_code'] == expected, (phase, actual, expected)
    log_bytes = Path(receipt['log']).read_bytes()
    assert sha(log_bytes) == receipt['log_sha256']
    log = log_bytes.decode()
    if cases is not None:
        assert f'gesamt: {cases}' in log and 'übersprungen: 0' in log, phase
        assert f'fehlgeschlagen: {cases if expected == 2 else 0}' in log, phase
        assert f'erfolgreich: {0 if expected == 2 else cases}' in log, phase
        if expected == 2:
            assert method in log and 'Assert.' in log, phase
        assert 'NestedQueueDestinations_HaveDistinctAutomaticSubscriptionsAsync' not in log, phase
    return receipt


def test(phase, index, expected):
    method, cases = METHODS[index]
    command = ['dotnet', 'test', '--project', PROJECT, '-c', 'Release', '--no-restore',
               '--filter-class', 'ViciOne.ServiceBus.AzureServiceBus.Tests.ServiceBusFamilyRegressionTests',
               '--filter-method', '*.' + method, '--minimum-expected-tests', str(cases), '--max-parallel-test-modules', '1']
    return operation(phase, command, expected, cases, method)


def main():
    paths = [SESSION, TYPED, RULE]
    original = {path: (F / path).read_bytes() for path in paths}
    corrected = {path: (O / path).read_bytes() for path in paths}
    regression = (O / TEST).read_bytes()
    assert not (F / TEST).exists() and not (M / TEST).exists()
    assert all((M / path).read_bytes() == data for path, data in original.items())
    result = {'status': 'RUNNING', 'checks': [], 'mutants': [], 'test_sha256': sha(regression),
              'corrected_sources': {path: sha(data) for path, data in corrected.items()},
              'proof_limit': 'Focused F036/F037 cases only; F038 remains independently red and is excluded from every mutant kill.'}
    mutations = [
        ('default-formatter-ignored', SESSION, 'owner.DefaultFormatter);', 'owner.DefaultFormatter == null ? null : null);', 0),
        ('typed-override-ignored', TYPED, '_formatter = formatter;', 'if (_formatter == null)\n            _formatter = formatter;', 0),
        ('conflict-validation-removed', RULE, 'if (_rule != null && _filter != null)\n            yield return this.Failure("Rule/Filter", "only a rule or a filter may be specified");', 'yield break;', 1),
        ('single-rule-overrejected', RULE, '_rule != null && _filter != null', '_rule != null || _filter != null', 1),
    ]
    try:
        (M / TEST).write_bytes(regression)
        result['checks'].append(operation('package09-isolated-restore', ['dotnet', 'restore', PROJECT, '--locked-mode', '--disable-parallel'], 0))
        for index in range(3):
            result['checks'].append(test('package09-isolated-original-' + str(index), index, 2))
        for path, data in corrected.items():
            (M / path).write_bytes(data)
        for index in range(3):
            result['checks'].append(test('package09-isolated-corrected-' + str(index), index, 0))
        for name, path, before, after, index in mutations:
            source = corrected[path].decode()
            assert source.count(before) == 1, name
            mutant = source.replace(before, after).encode()
            (M / path).write_bytes(mutant)
            assert (M / TEST).read_bytes() == regression
            receipt = test('package09-mutant-' + name, index, 2)
            result['mutants'].append({'name': name, 'source': path, 'status': 'KILLED_BY_CONTRACT_ASSERTION',
                                      'source_sha256': sha(mutant), 'test_sha256': sha(regression),
                                      'replacement': {'before': before, 'after': after}, 'native_receipt': receipt})
            (M / path).write_bytes(corrected[path])
        for index in range(3):
            result['checks'].append(test('package09-isolated-rollback-' + str(index), index, 0))
        result['status'] = 'EXECUTED_ALL_4_KILLED_CORRECTED_ROLLBACK_GREEN'
    finally:
        for path, data in original.items():
            (M / path).write_bytes(data)
        (M / TEST).unlink(missing_ok=True)
        result['all_owned_sources_restored'] = all((M / path).read_bytes() == data for path, data in original.items())
        result['new_test_removed'] = not (M / TEST).exists()
        (E / 'PACKAGE09_AZURE_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
    assert result['all_owned_sources_restored'] and result['new_test_removed']


if __name__ == '__main__':
    main()

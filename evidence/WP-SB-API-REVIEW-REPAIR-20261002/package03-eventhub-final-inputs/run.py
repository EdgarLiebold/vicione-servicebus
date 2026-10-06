#!/usr/bin/env python3
"""Run causal EventHub mutants in the owned isolated original checkout.

F016 generation tests use their separately native-verified eleven-mutant proof.
"""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

W = Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0')
D = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR'
O = W / 'repositories/vicione-servicebus'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
M = R / 'mutations/source-main'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
BASE = 'src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegration/'
CLOSE = BASE + 'EventHubProcessorContext.cs'
CONFIG = BASE + 'Configuration/EventHubReceiveEndpointConfigurator.cs'
BUILDER = BASE + 'Configuration/EventHubReceiveEndpointBuilder.cs'
PRODUCER = BASE + 'ConsumeContextEventHubProducerProvider.cs'
OWNER = 'tests/Transports/ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests/'
PROJECT = OWNER + 'ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.csproj'
NEW_TESTS = [OWNER + 'EventHubIntegration/' + name for name in (
    'EventHubClosingRegressionTests.cs', 'EventHubReceivePolicyRegressionTests.cs',
    'EventHubDeferredCallerCancellationRegressionTests.cs', 'EventHubBuiltContextCapture.cs')]
PROJECTION = OWNER + 'Requirements/EventHubLocalIntegrationRequirements.json'
METHODS = {
    'closing': ['Closing_AlwaysAwaitsInternalCallbackAndPreservesBothOutcomesAsync'],
    'validation': ['InvalidReceivePolicy_FailsOrdinaryBusBuildWithActionableProviderDiagnosticAsync',
                   'InheritedTransportGuards_StillRejectTheirInvalidLimitsAsync',
                   'ValidTimerBoundary_PreservesWholeMillisecondSemanticsAsync',
                   'ValidConfiguration_BuildsWithOrWithoutOptionalEventHubSelectionAsync'],
    'producer-fast': ['PreCanceledCaller_CompletesBeforeSharedResolutionAndPreservesLiveWaiterAsync',
                      'PositiveRoutes_ForwardExactlyOnceAndAwaitDeliveryAsync'],
}
COUNTS = {'closing': 10, 'validation': 19, 'producer-fast': 24}
RUN_PREFIX = 'package03-eh-r3-'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def operation(phase, command, expected=0, cases=None):
    phase = phase.replace('package03-eh-', RUN_PREFIX, 1)
    receipts = list(map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()))
    existing = [row for row in receipts if row['phase'] == phase]
    if existing:
        assert '--resume-owned-unchanged-inputs' in sys.argv and len(existing) == 1
        receipt = existing[0]
        assert receipt['command'] == command and receipt['cwd'] == str(M)
    else:
        output = subprocess.run([sys.executable, str(D / 'run_operation.py'), phase, str(M), '--', *command],
                                stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        receipt = next(row for row in map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines())
                       if row['phase'] == phase)
        assert output.returncode == expected, (phase, output.stdout)
    assert receipt['exit_code'] == expected, (phase, receipt)
    raw = Path(receipt['log']).read_bytes()
    assert sha(raw) == receipt['log_sha256']
    log = raw.decode()
    if cases is not None:
        assert f'gesamt: {cases}' in log and 'übersprungen: 0' in log, (phase, log)
        assert 'TimeoutException' not in log and 'error CS' not in log, (phase, log)
        if expected:
            precise_timer_rejection = phase.endswith('validation-fraction-overrejected') and all(value in log for value in (
                'ticks: 42949672949999', 'ticks: 42949672940001',
                'CheckpointInterval is outside its valid range', 'fehlgeschlagen: 2', 'erfolgreich: 17'))
            assert 'Assert.' in log or 'SDK closing completed before' in log or precise_timer_rejection, (phase, log)
    print(json.dumps({'phase': phase, 'native_exit': expected, 'cases': cases}), flush=True)
    return receipt


def test(phase, family, expected):
    args = ['env', 'VICIONE_TESTS__Profile=LocalIntegration', 'dotnet', 'test', '--project', PROJECT,
            '-c', 'Release', '--no-restore', '--filter-method',
            *['*.' + method for method in METHODS[family]], '--minimum-expected-tests', str(COUNTS[family])]
    return operation(phase, args, expected, COUNTS[family])


def replace_once(text, before, after):
    assert text.count(before) == 1, before
    return text.replace(before, after)


def main():
    sources = [CLOSE, CONFIG, BUILDER, PRODUCER]
    originals = {path: (F / path).read_bytes() for path in sources + [PROJECTION]}
    corrected = {path: (O / path).read_bytes() for path in sources}
    regressions = {path: (O / path).read_bytes() for path in NEW_TESTS}
    assert all((M / path).read_bytes() == data for path, data in originals.items())
    assert all(not (M / path).exists() for path in NEW_TESTS)
    previous = E / 'PACKAGE03_EVENTHUB_MUTATIONS_FINAL_INPUTS.json'
    resume = '--resume-owned-unchanged-inputs' in sys.argv
    if previous.exists() and not resume:
        preserved = E / 'PACKAGE03_EVENTHUB_MUTATIONS_ATTEMPT1.json'
        assert not preserved.exists()
        preserved.write_bytes(previous.read_bytes())
    result = {'status': 'RUNNING', 'findings': ['F013', 'F014', 'F017'], 'checks': [], 'mutants': [],
              'excluded': 'F016 generation cases are separately verified; this run selects the 19 F013/F014/F017 mutants against final input bytes.'}
    if resume:
        previous_result = json.loads(previous.read_text())
        assert previous_result['status'] == 'RUNNING' and len(previous_result['mutants']) == 12
        preserved = E / 'PACKAGE03_EVENTHUB_MUTATIONS_ATTEMPT2.json'
        assert not preserved.exists()
        preserved.write_bytes(previous.read_bytes())
        result['resume_evidence'] = str(preserved)
        result['resume_limit'] = 'Root-owned r2 input bytes remained unchanged across the receipt-classifier interruption. Existing exact commands/log hashes are verified; no native receipt overwritten.'
    result['regression_hashes'] = {path: sha(data) for path, data in regressions.items()}
    mutants = []
    close = corrected[CLOSE].decode()
    mutants.append(('closing-original', CLOSE, originals[CLOSE].decode(), 'closing'))
    mutants.append(('closing-no-internal-await', CLOSE, replace_once(close,
        'await context.OnPartitionClosingAsync(args).ConfigureAwait(false);',
        '_ = context.OnPartitionClosingAsync(args);'), 'closing'))
    mutants.append(('closing-primary-discarded', CLOSE, replace_once(close,
        'applicationFault?.Throw();', '// Captured application cause intentionally discarded.'), 'closing'))
    mutants.append(('closing-reversed-dual-causes', CLOSE, replace_once(close,
        'new AggregateException(applicationFault.SourceException, cleanupFault)',
        'new AggregateException(cleanupFault, applicationFault.SourceException)'), 'closing'))
    mutants.append(('closing-double-internal', CLOSE, replace_once(close,
        'await context.OnPartitionClosingAsync(args).ConfigureAwait(false);',
        'await context.OnPartitionClosingAsync(args).ConfigureAwait(false);\n                await context.OnPartitionClosingAsync(args).ConfigureAwait(false);'), 'closing'))
    mutants.append(('closing-single-cause-wrapped', CLOSE, replace_once(close,
        'applicationFault?.Throw();', 'if (applicationFault != null) throw new AggregateException(applicationFault.SourceException);'), 'closing'))
    configuration = corrected[CONFIG].decode()
    for property_name in ['CheckpointMessageCount', 'CheckpointMessageLimit', 'CheckpointInterval',
                          'ConcurrentDeliveryLimit', 'PrefetchCount']:
        expression = r'        if \(' + property_name + r'[^\n]*\)\n(?:[^\n]*\n){4}'
        altered, count = re.subn(expression, '', configuration)
        assert count == 1, property_name
        mutants.append(('validation-omit-' + property_name.lower(), CONFIG, altered, 'validation'))
    mutants.append(('validation-base-lost', CONFIG, replace_once(configuration,
        '        foreach (var result in base.Validate())\n            yield return result;', ''), 'validation'))
    mutants.append(('validation-fraction-overrejected', CONFIG, replace_once(configuration,
        '(long)CheckpointInterval.TotalMilliseconds > uint.MaxValue - 1L',
        'CheckpointInterval.TotalMilliseconds > uint.MaxValue - 1L'), 'validation'))
    producer = corrected[PRODUCER].decode()
    awaited = 'await _producerTask.WaitAsync(cancellationToken).ConfigureAwait(false)'
    positions = [match.start() for match in re.finditer(re.escape(awaited), producer)]
    assert len(positions) == 4
    for number, start in enumerate(positions):
        altered = producer[:start] + 'await _producerTask.ConfigureAwait(false)' + producer[start + len(awaited):]
        mutants.append((f'producer-caller-wait-lost-{number}', PRODUCER, altered, 'producer-fast'))
    altered, count = re.subn(r'await producer\.ProduceAsync\((message|messages|values), sendPipeAdapter, cancellationToken\);',
                            r'_ = producer.ProduceAsync(\1, sendPipeAdapter, cancellationToken);', producer)
    assert count == 4
    mutants.append(('producer-delivery-not-awaited', PRODUCER, altered, 'producer-fast'))
    altered, count = re.subn(r'producer\.ProduceAsync\((message|messages|values), sendPipeAdapter, cancellationToken\)',
                            r'producer.ProduceAsync(\1, sendPipeAdapter, CancellationToken.None)', producer)
    assert count == 4
    mutants.append(('producer-downstream-token-lost', PRODUCER, altered, 'producer-fast'))
    try:
        for path, data in regressions.items(): (M / path).write_bytes(data)
        (M / PROJECTION).write_bytes((O / PROJECTION).read_bytes())
        result['checks'].append(operation('package03-eh-isolated-restore', ['dotnet', 'restore', PROJECT, '--locked-mode', '--disable-parallel']))
        for path, data in corrected.items(): (M / path).write_bytes(data)
        for family in METHODS:
            result['checks'].append(test('package03-eh-isolated-corrected-' + family, family, 0))
        for name, path, code, family in mutants:
            mutant = code.encode(); (M / path).write_bytes(mutant)
            if resume:
                prior = [row for row in previous_result['mutants'] if row['name'] == name]
                if prior: assert prior[0]['source_sha256'] == sha(mutant)
            assert all((M / p).read_bytes() == b for p, b in regressions.items())
            receipt = test('package03-eh-mutant-' + name, family, 2)
            result['mutants'].append({'name': name, 'source': path, 'source_sha256': sha(mutant),
                                      'source_before_sha256': sha(corrected[path]), 'family': family,
                                      'status': 'KILLED_BY_NATIVE_CONTRACT_ASSERTION', 'native_receipt': receipt})
            (M / path).write_bytes(corrected[path])
        for family in METHODS:
            result['checks'].append(test('package03-eh-isolated-rollback-' + family, family, 0))
        result['status'] = f'EXECUTED_ALL_{len(mutants)}_KILLED_CORRECTED_ROLLBACK_GREEN'
    finally:
        for path, data in originals.items(): (M / path).write_bytes(data)
        for path in NEW_TESTS: (M / path).unlink(missing_ok=True)
        result['original_inputs_restored'] = all((M / p).read_bytes() == b for p, b in originals.items())
        result['new_fixtures_removed'] = all(not (M / p).exists() for p in NEW_TESTS)
        (E / 'PACKAGE03_EVENTHUB_MUTATIONS_FINAL_INPUTS.json').write_text(json.dumps(result, indent=2) + '\n')
    assert result['original_inputs_restored'] and result['new_fixtures_removed']


if __name__ == '__main__':
    main()

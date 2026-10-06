"""Actual EF saga ownership predicate mutants against compiled package-only consumers."""
import csv, hashlib, json, shutil, subprocess, sys
from pathlib import Path
sys.dont_write_bytecode = True
W = Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0')
O = W / 'repositories/vicione-servicebus'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
M = R / 'mutations/source-main'
REL = 'src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/EntityFrameworkCoreIntegration/Saga/EntityFrameworkSagaRepositoryContextFactory.cs'
PROJECT = 'src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/ViciOne.ServiceBus.EntityFrameworkCore.Sagas.csproj'
DLL = 'ViciOne.ServiceBus.EntityFrameworkCore.Sagas.dll'
SOURCE = M / REL
COMPILED = M / 'artifacts/sdk/bin/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/release' / DLL
CONSUMERS = {x: R / 'repair-consumers/package04' / (x + '-corrected') for x in ['ownership', 'identity']}
APPS = {'ownership': 'F018.PublicConsumer.dll', 'identity': 'F018.IdentityGateConsumer.dll'}
RUN = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR/run_operation.py'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
original = SOURCE.read_bytes()
assert original == (R / 'frozen-repository' / REL).read_bytes()
corrected = (O / REL).read_text()
send = 'if (context.TryGetPayload(out IDbTransactionContext? transactionContext)\n                && dbContext.Database.CurrentTransaction is { } currentTransaction\n                && currentTransaction.TransactionId == transactionContext.TransactionId)'
query = 'var hasOuterTransaction = context.TryGetPayload(out IDbTransactionContext? transactionContext)\n                && dbContext.Database.CurrentTransaction is { } currentTransaction\n                && currentTransaction.TransactionId == transactionContext.TransactionId;'
assert corrected.count(send) == corrected.count(query) == 1
oracle_hashes = {str(p): sha(p) for c in CONSUMERS.values() for p in [*filter(lambda p: p.is_file(), c.iterdir()), c / 'bin/Release/net10.0' / APPS[c.name.split('-')[0]]]}
runtime = {str(c / 'bin/Release/net10.0' / DLL): (c / 'bin/Release/net10.0' / DLL).read_bytes() for c in CONSUMERS.values()}
result = dict(status='RUNNING', oracle_hashes=oracle_hashes, mutants=[], controls=[], rollback=[])
def native(label, cwd, args, expected=0):
    phase = 'package04-f018-mut-' + label
    p = subprocess.run([sys.executable, str(RUN), phase, str(cwd), '--', *args], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    rows = list(map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()))
    receipt = next(x for x in reversed(rows) if x['phase'] == phase)
    print(phase + ' native=' + str(receipt['exit_code']), flush=True)
    if p.returncode != expected: raise RuntimeError(p.stdout[-7000:])
    assert sha(receipt['log']) == receipt['log_sha256']
    return receipt

def build(label):
    receipt = native(label + '-build', M, ['dotnet', 'build', PROJECT, '-c', 'Release', '--no-restore', '-warnaserror'])
    directory = R / 'repair-evidence/package04-f018-binaries' / label
    directory.mkdir(parents=True, exist_ok=False)
    snapshot = directory / DLL
    shutil.copyfile(COMPILED, snapshot)
    for c in CONSUMERS.values(): shutil.copyfile(COMPILED, c / 'bin/Release/net10.0' / DLL)
    return dict(receipt=receipt, snapshot=str(snapshot), sha256=sha(snapshot), source_sha256=sha(SOURCE))

def case(label, consumer, name='all', expected=0, cause=None):
    c = CONSUMERS[consumer]
    receipt = native(label + '-' + consumer + '-' + name, c, ['dotnet', 'bin/Release/net10.0/' + APPS[consumer], name, '00:00:20', '00:01:00'], expected)
    observed = json.loads(Path(receipt['log']).read_text())
    if expected:
        assert observed['count'] == observed['failures'] == 1
        failed = observed['cases'][0]
        assert failed['case'] == name and not failed['assertions_passed'] and not failed['bounded_guard_failure']
        assert failed['exception_type'] == cause
    else:
        assert observed['failures'] == 0 and all(x['assertions_passed'] for x in observed['cases'])
    return dict(receipt=receipt, actual=observed)

mutations = [
    ('send-marker-only', send, 'if (context.TryGetPayload(out IDbTransactionContext? _))', 'ownership', 'send-separate-success', 'Xunit.Sdk.NotNullException', 'ownership', 'query-separate-success'),
    ('query-marker-only', query, 'var hasOuterTransaction = context.TryGetPayload(out IDbTransactionContext? _);', 'ownership', 'query-separate-success', 'Xunit.Sdk.NotNullException', 'ownership', 'send-separate-success'),
    ('send-id-comparison-lost', send, send.replace('\n                && currentTransaction.TransactionId == transactionContext.TransactionId', ''), 'identity', 'send-foreign', 'Xunit.Sdk.IsTypeException', 'identity', 'send-matching'),
    ('query-id-comparison-lost', query, query.replace('\n                && currentTransaction.TransactionId == transactionContext.TransactionId', ''), 'identity', 'query-foreign', 'Xunit.Sdk.IsTypeException', 'identity', 'query-matching'),
    ('send-valid-owner-rejected', send, send.replace('TransactionId == transactionContext.TransactionId', 'TransactionId != transactionContext.TransactionId'), 'identity', 'send-matching', 'Xunit.Sdk.NullException', 'identity', 'query-matching'),
    ('query-valid-owner-rejected', query, query.replace('TransactionId == transactionContext.TransactionId', 'TransactionId != transactionContext.TransactionId'), 'identity', 'query-matching', 'Xunit.Sdk.NullException', 'identity', 'send-matching'),
]
try:
    result['locked_restore'] = native('locked-restore', M, ['dotnet', 'restore', PROJECT, '--locked-mode'])
    SOURCE.write_text(corrected)
    result['corrected_binary'] = build('corrected')
    for consumer in CONSUMERS: result['controls'].append(case('corrected', consumer))
    for name, before, after, consumer, negative, cause, positive_consumer, positive in mutations:
        SOURCE.write_text(corrected.replace(before, after))
        binary = build(name)
        rejection = case(name, consumer, negative, 1, cause)
        control = case(name, positive_consumer, positive)
        result['mutants'].append(dict(name=name, binary=binary, rejection=rejection, positive_control=control, status='KILLED_BY_CAUSAL_ASSERTION'))
    SOURCE.write_text(corrected)
    result['rollback_binary'] = build('rollback')
    for consumer in CONSUMERS: result['rollback'].append(case('rollback', consumer))
    result['status'] = 'ALL_SIX_MUTANTS_KILLED_AND_SIXTEEN_ROLLBACK_CASES_GREEN'
finally:
    SOURCE.write_bytes(original)
    for path, content in runtime.items(): Path(path).write_bytes(content)
    rows = list(csv.DictReader((R / 'FILE_COVERAGE.csv').open()))
    result['original_files'] = len(rows)
    result['original_mismatches'] = [x['path'] for x in rows if sha(M / x['path']) != x['sha256']]
    result['oracle_hashes_preserved'] = all(sha(p) == h for p, h in oracle_hashes.items())
    result['runtime_restored'] = all(Path(p).read_bytes() == content for p, content in runtime.items())
    (E / 'PACKAGE04_F018_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
    assert len(rows) == 6209 and not result['original_mismatches'] and result['oracle_hashes_preserved'] and result['runtime_restored']

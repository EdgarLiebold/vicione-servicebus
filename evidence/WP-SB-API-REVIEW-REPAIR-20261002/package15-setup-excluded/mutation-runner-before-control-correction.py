"""Compile isolated SignalR variants serially, with exact public oracle and constant Core/Abstractions."""
import ast
import csv
import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path

W = Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0')
O = W / 'repositories/vicione-servicebus'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
M = R / 'mutations/source-main'
F = R / 'frozen-repository'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
RUN = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR/run_operation.py'
C = R / 'repair-consumers/package15-f033-signalr/corrected'
OUT = C / 'bin/Release/net10.0'
APP = 'SignalRLifetime.PublicConsumer.dll'
DLL = 'ViciOne.ServiceBus.SignalR.dll'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
prior_ast = ast.parse((W / 'SERVICEBUS_API_REVIEW_AND_REPAIR/package14_f031_mutations.py').read_text())
PATHS = ast.literal_eval(next(n.value for n in prior_ast.body if isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'PATHS' for t in n.targets)))
PATHS.update(provider='src/Transports/ViciOne.ServiceBus.SignalR/Runtime/DependencyInjectionBackplaneScopeProvider.cs',
    manager='src/Transports/ViciOne.ServiceBus.SignalR/Runtime/ServiceBusHubLifetimeManager.cs',
    backplane_lifetime='src/Transports/ViciOne.ServiceBus.SignalR/Runtime/BackplaneOperationLifetime.cs')
original = {key: (F / rel).read_bytes() if (F / rel).exists() else None for key, rel in PATHS.items()}
for key, rel in PATHS.items():
    assert ((M / rel).read_bytes() if (M / rel).exists() else None) == original[key], rel
corrected = {key: (O / rel).read_text() for key, rel in PATHS.items()}
runtime = (OUT / DLL).read_bytes()
oracle = {str(p): sha(p) for p in C.iterdir() if p.is_file()}
oracle[str(OUT / APP)] = sha(OUT / APP)
constants = {name: sha(OUT / name) for name in ['ViciOne.ServiceBus.dll', 'ViciOne.ServiceBus.Abstractions.dll']}
result = dict(status='RUNNING', oracle_hashes=oracle, constant_runtime_dependencies=constants, controls=[], mutants=[])


def native(label, cwd, command, expected=0):
    phase = 'package15-f033-signalr-mut-r3-' + label
    completed = subprocess.run([sys.executable, str(RUN), phase, str(cwd), '--', *command], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    receipt = next(json.loads(line) for line in reversed((E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()) if json.loads(line)['phase'] == phase)
    assert sha(receipt['log']) == receipt['log_sha256']
    print(phase, receipt['exit_code'], flush=True)
    if completed.returncode != expected or receipt['exit_code'] != expected:
        raise RuntimeError(completed.stdout[-7000:])
    return receipt


def build(label, sources):
    for key, rel in PATHS.items():
        (M / rel).write_text(sources[key])
    receipt = native(label + '-build', M, ['dotnet', 'build', 'src/Transports/ViciOne.ServiceBus.SignalR/ViciOne.ServiceBus.SignalR.csproj', '-c', 'Release', '--no-restore', '-warnaserror'])
    folder = R / 'repair-evidence/package15-f033-signalr-binaries-r3' / label
    folder.mkdir(parents=True, exist_ok=False)
    source_hashes = {}
    for key, rel in PATHS.items():
        snapshot = folder / (key + '.cs.txt'); snapshot.write_bytes((M / rel).read_bytes())
        source_hashes[rel] = dict(path=str(snapshot), sha256=sha(snapshot))
    binaries = []
    for name in [DLL]:
        compiled = M / 'artifacts/sdk/bin/ViciOne.ServiceBus.SignalR/release' / name
        snapshot = folder / name; shutil.copyfile(compiled, snapshot)
        binaries.append(dict(path=str(snapshot), sha256=sha(snapshot)))
    shutil.copyfile(folder / DLL, OUT / DLL)
    return dict(receipt=receipt, binaries=binaries, source_snapshots=source_hashes)


def case(label, name='all', expected=0, assertion=None):
    receipt = native(label + '-' + name, C, ['dotnet', 'bin/Release/net10.0/' + APP, name, '00:00:12', '00:00:45'], expected)
    actual = json.loads(Path(receipt['log']).read_text())
    if expected:
        assert actual['count'] == actual['failures'] == 1
        failed = actual['cases'][0]
        assert failed['case'] == name and not failed['assertions_passed'] and not failed['bounded_guard_failure']
        assert failed['exception_type'] == assertion, failed
        assert 'Cases.cs:line' in failed['StackTrace'], failed
    else:
        assert actual['failures'] == 0 and all(x['assertions_passed'] for x in actual['cases'])
        assert actual['count'] == (41 if name == 'all' else 1)
    loaded = {Path(x['path']).name: x['sha256'] for x in actual['runtime']['loadedProductAssemblies']}
    assert loaded[DLL] == current_binary['binaries'][0]['sha256']
    for dll, digest in constants.items():
        assert loaded[dll] == digest and sha(OUT / dll) == digest
    return dict(receipt=receipt, actual=actual)


def method(text, signature):
    start = text.index(signature)
    begin = text.index('{', start)
    depth = 0
    for index in range(begin, len(text)):
        if text[index] == '{': depth += 1
        elif text[index] == '}': depth -= 1
        if depth == 0: return start, index + 1
    raise ValueError(signature)


def mutation(name):
    sources = dict(corrected)
    if name == 'resolution-primary-discarded':
        sources['provider'] = original['provider'].decode()
        return sources, 'publishendpointresolution-dualfault', 'publishendpointresolution-primaryonly', 'Xunit.Sdk.IsTypeException'
    routes = {'publish': ('    async Task PublishAsync<TMessage>(', 'publish'),
        'connections': ('    public override async Task SendConnectionsAsync(', 'connections'),
        'groups': ('    public override async Task SendGroupsAsync(', 'groups'),
        'users': ('    public override async Task SendUsersAsync(', 'users'),
        'remotegroup': ('    async Task ChangeGroupMembershipAsync(', 'remotegroup')}
    if name.endswith('-await-using-restored'):
        route = name.removesuffix('-await-using-restored'); signature, mode = routes[route]
        start, end = method(sources['manager'], signature)
        old_start, old_end = method(original['manager'].decode(), signature)
        sources['manager'] = sources['manager'][:start] + original['manager'].decode()[old_start:old_end] + sources['manager'][end:]
        return sources, mode + '-dualfault', mode + '-primaryonly', 'Xunit.Sdk.IsTypeException'
    helper = sources['backplane_lifetime']
    if name == 'request-fault-skips-scope':
        before = '            (failures ??= []).Add(exception);'
        assert helper.count(before) == 2
        sources['backplane_lifetime'] = helper.replace(before, '            throw;', 1).replace('catch (Exception exception)', 'catch (Exception)', 1)
        return sources, 'advancedhandle-responseandhandlefault', 'remotegroup-success', 'Xunit.Sdk.TrueException'
    if name == 'aggregate-order-reversed':
        before = '            throw new AggregateException("Backplane operation and release encountered multiple failures.", failures);'
        assert helper.count(before) == 1
        sources['backplane_lifetime'] = helper.replace(before, '        {\n            failures.Reverse();\n' + before + '\n        }')
        return sources, 'advancedhandle-controlledthreefaults', 'publish-primaryonly', 'Xunit.Sdk.SameException'
    if name == 'single-fault-wrapped':
        before = '            ExceptionDispatchInfo.Capture(failures[0]).Throw();'
        assert helper.count(before) == 1
        sources['backplane_lifetime'] = helper.replace(before, '            throw new AggregateException(failures);')
        return sources, 'publish-cleanuponly', 'publish-success', 'Xunit.Sdk.SameException'
    if name == 'request-async-preferred':
        before = '            request?.Dispose();'
        assert helper.count(before) == 1
        sources['backplane_lifetime'] = helper.replace(before, '            if (request is IAsyncDisposable asyncRequest)\n                await asyncRequest.DisposeAsync().ConfigureAwait(false);\n            else\n                request?.Dispose();')
        return sources, 'dualinterfacehandle-stillsynchronous', 'standardremoveacknowledged-control', 'Xunit.Sdk.EqualException'
    raise ValueError(name)


try:
    current_binary = build('corrected', corrected); result['corrected_binary'] = current_binary
    result['controls'].append(case('corrected'))
    names = ['resolution-primary-discarded', 'publish-await-using-restored', 'connections-await-using-restored', 'groups-await-using-restored', 'users-await-using-restored', 'remotegroup-await-using-restored', 'request-fault-skips-scope', 'aggregate-order-reversed', 'single-fault-wrapped', 'request-async-preferred']
    for name in names:
        sources, negative, positive, assertion = mutation(name)
        current_binary = build(name, sources)
        rejection = case(name, negative, 1, assertion)
        control = case(name, positive)
        result['mutants'].append(dict(name=name, binary=current_binary, rejection=rejection, positive_control=control, status='KILLED_BY_SELECTED_ASSERTION_WITH_GREEN_CONTROL'))
    current_binary = build('rollback', corrected); result['rollback_binary'] = current_binary
    result['rollback'] = case('rollback')
    result['status'] = 'TEN_COMPILED_MUTANTS_KILLED_WITH_CONTROLS_AND_FORTYONE_BASELINE_ROLLBACK_GREEN'
finally:
    for key, rel in PATHS.items():
        if original[key] is None: (M / rel).unlink(missing_ok=True)
        else: (M / rel).write_bytes(original[key])
    (OUT / DLL).write_bytes(runtime)
    result['oracle_hashes_preserved'] = all(sha(p) == expected for p, expected in oracle.items())
    result['runtime_restored'] = (OUT / DLL).read_bytes() == runtime and all(sha(OUT / dll) == digest for dll, digest in constants.items())
    rows = list(csv.DictReader((R / 'FILE_COVERAGE.csv').open()))
    result['original_files_checked'] = len(rows)
    result['original_mismatches'] = [row['path'] for row in rows if sha(M / row['path']) != row['sha256']]
    result['new_identity_and_helper_removed'] = not (M / PATHS['identity']).exists() and not (M / PATHS['backplane_lifetime']).exists()
    (E / 'PACKAGE15_F033_SIGNALR_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')
    assert result['oracle_hashes_preserved'] and result['runtime_restored'] and not result['original_mismatches']
    assert result['original_files_checked'] == 6209 and result['new_identity_and_helper_removed']

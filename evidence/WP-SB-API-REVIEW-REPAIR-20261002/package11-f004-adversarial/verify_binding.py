"""Readonly independent Package11 binding; writes this dossier only, never imports a runner."""
import ast
import base64
import csv
import difflib
import hashlib
import importlib.util
import json
import sys
import zipfile
from pathlib import Path

sys.dont_write_bytecode = True
O = Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
D = E / 'package11-f004-adversarial'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
M = R / 'mutations/source-main'
C = R / 'repair-consumers/package11-f004'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
freeze = json.loads((E / 'PACKAGE11_F004_FREEZE.json').read_text())
assert sha(E / 'PACKAGE11_F004_FREEZE.json') == 'b0096b5364b2a84f949bc51fce12bb4881502e2fa73911d6c347f881bc739d23'
for row in freeze['owned_inputs']:
    for label in ['path', 'snapshot']:
        assert sha(row[label]) == row['sha256'] and Path(row[label]).stat().st_size == row['bytes']
for row in [freeze['root_binding'], freeze['mutations'], *freeze['preauthor_plans']]:
    assert sha(row['path']) == row['sha256']
root = json.loads(Path(freeze['root_binding']['path']).read_text())
mut = json.loads(Path(freeze['mutations']['path']).read_text())
runner = Path(freeze['owned_inputs'][38]['snapshot']).read_text()
tree = ast.parse(runner)
paths = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'PATHS' for t in n.targets))
corrected = {k: Path(next(x['snapshot'] for x in freeze['owned_inputs'] if x['path'] == str(O / rel))).read_text() for k, rel in paths.items()}
def expected(name):
    src = dict(corrected)
    if name in ['corrected', 'rollback']: return src
    if name == 'shared-transport-graph-restored':
        src['inmemory_factory'] = (F / paths['inmemory_factory']).read_text()
        return src
    changes = {
        'process-meter-selected': [('journal_telemetry', 'meterFactory!.Create(new MeterOptions(ServiceBusTelemetry.MeterName) { Version = version })', 'new Meter(ServiceBusTelemetry.MeterName, version)')],
        'factory-failure-global-fallback': [('journal_telemetry', '            try { processMeter?.Dispose(); } catch { }', '            try { processMeter?.Dispose(); } catch { }\n            if (!processOwned)\n            {\n                _operations = Process._operations;\n                _duration = Process._duration;\n                _activitySource = Process._activitySource;\n            }')],
        'borrowed-factory-meter-disposed': [('journal_telemetry', '            _processMeter = processMeter;', '            _processMeter = meter;')],
        'owned-source-disposal-omitted': [('journal_telemetry', '        try { _activitySource?.Dispose(); } catch { }', '')],
        'registration-caches-first-telemetry': [('journal_registration', '    public Type BusType => typeof(TBus);', '    MessageJournalTelemetry? _cachedTelemetry;\n\n    public Type BusType => typeof(TBus);'), ('journal_registration', '            provider.GetRequiredService<MessageJournalTelemetry>());', '            _cachedTelemetry ??= provider.GetRequiredService<MessageJournalTelemetry>());')],
        'di-writer-forgets-owner': [('journal_registration', '        var writer = new MessageJournalWriter(store, policy, options,\n            provider.GetRequiredService<MessageJournalTelemetry>());', '        var writer = new MessageJournalWriter(store, policy, options);')],
    }
    for key, before, after in changes[name]:
        assert src[key].count(before) == 1
        src[key] = src[key].replace(before, after)
    return src
helper = E / 'package04-f018-canonical-fixture-adversarial/verify_pdb.py'
assert sha(helper) == '26527e1c6caadb47808dd705098a4da6690d37bbe46770b5ab4950c5e2d1bc78'
spec = importlib.util.spec_from_file_location('readonly_pdb', helper)
pdb = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pdb)
variants = [('corrected', mut['corrected_binary'])] + [(x['name'], x['binary']) for x in mut['mutants']] + [('rollback', mut['rollback_binary'])]
effects = []
binary_checks = []
for name, frame in variants:
    exp = expected(name)
    for key, rel in paths.items():
        row = frame['source_snapshots'][rel]
        assert sha(row['path']) == row['sha256']
        assert Path(row['path']).read_bytes() == exp[key].encode()
        claimed = next(x for x in root['source_effect_checks'] if x['variant'] == name and x['source'] == rel)
        assert claimed['sha256'] == row['sha256']
        effects.append(dict(variant=name, source=rel, snapshot=row, independently_reconstructed=True))
    for binary in frame['binaries']:
        assert sha(binary['path']) == binary['sha256']
        checks = pdb.document_checksums(binary['path'])
        binary_checks.append(dict(variant=name, **checks))
    diff = []
    for key, rel in paths.items():
        if exp[key] != corrected[key]:
            diff += list(difflib.unified_diff(corrected[key].splitlines(True), exp[key].splitlines(True), fromfile=rel, tofile=name + '/' + rel))
    (D / ('SOURCE_EFFECT_' + name + '.diff')).write_text(''.join(diff))
assert len(effects) == 126
receipts = [json.loads(x) for x in (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines() if x.strip()]
bound = []
for row in root['bound_receipts']:
    assert row in receipts
    assert sha(row['log']) == row['log_sha256']
    bound.append(row)
assert len(bound) == 40
chains = []
for row in root['selected_package_chains']:
    package = Path(row['package']); b = package.read_bytes()
    assert hashlib.sha256(b).hexdigest() == row['package_sha256']
    content = base64.b64encode(hashlib.sha512(b).digest()).decode()
    assert content == row['contentHash']
    name = package.name.removesuffix('.1.0.0.nupkg')
    cp = C / row['consumer']
    lock = json.loads((cp / 'packages.lock.json').read_text())['dependencies']['net10.0']
    assert lock[name]['contentHash'] == content
    assets = json.loads((cp / 'obj/project.assets.json').read_text())
    cache = Path(next(iter(assets['packageFolders']))) / name.lower() / '1.0.0'
    assert sha(cache / package.name.lower()) == row['package_sha256']
    assert (cache / (package.name.lower() + '.sha512')).read_text().strip() == content
    with zipfile.ZipFile(package) as z:
        dll = z.read('lib/net10.0/' + name + '.dll')
        assert hashlib.sha256(dll).hexdigest() == row['runtime_dll_sha256']
        assert (cache / ('lib/net10.0/' + name + '.dll')).read_bytes() == dll
        assert (cp / ('bin/Release/net10.0/' + name + '.dll')).read_bytes() == dll
    phase = 'package11-f004-' + row['consumer'] + '-sixteen'
    receipt = next(x for x in bound if x['phase'] == phase)
    actual = json.loads(Path(receipt['log']).read_text())
    loaded = next(x for x in actual['runtime']['loadedProductAssemblies'] if x['name'] == name)
    assert loaded['sha256'] == row['runtime_dll_sha256']
    assert Path(loaded['path']) == cp / ('bin/Release/net10.0/' + name + '.dll')
    chains.append(dict(**row, cache=str(cache), actual_receipt=phase, independently_verified=True))
    binary_checks.append(dict(variant='package-' + row['consumer'] + '-' + name, **pdb.document_checksums(cp / ('bin/Release/net10.0/' + name + '.dll'))))
ledger = list(csv.DictReader((R / 'FILE_COVERAGE.csv').open()))
bad = [x['path'] for x in ledger if sha(M / x['path']) != x['sha256']]
assert len(ledger) == 6209 and not bad and not (M / paths['identity']).exists()
for path, digest in mut['oracle_hashes'].items(): assert sha(path) == digest
for name in ['previous', 'corrected', 'corrected-r2']:
    for source in ['JournalApplication.cs', 'JournalCases.cs', 'JournalEvidence.cs', 'OwnedLifetime.cs', 'Program.cs', 'JournalOwnership.PublicConsumer.csproj']:
        assert (C / name / source).read_bytes() == (C / 'corrected-r2' / source).read_bytes()
for name, data in [('FREEZE_HASH_CHECK.json', freeze), ('SOURCE_EFFECT_CHECK.json', effects), ('RECEIPT_BINDING.json', bound), ('PACKAGE_BINDING.json', chains), ('PDB_DOCUMENTS.json', binary_checks), ('RESTORE_CHECK.json', dict(originals=6209, mismatches=bad, new_identity_removed=True, oracle_unchanged=True, actual_selected_runtime_restored=True))]:
    (D / name).write_text(json.dumps(data, indent=2) + '\n')
print(json.dumps(dict(freeze_inputs=40, independently_reconstructed_sourceframes=126, receipts=40, package_chains=6, binary_pdb_documents=len(binary_checks), original_M_restored=6209)))

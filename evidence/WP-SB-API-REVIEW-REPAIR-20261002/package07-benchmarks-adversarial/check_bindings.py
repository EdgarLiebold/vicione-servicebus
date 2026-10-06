"""Independent read-only hashes and receipt projections; no product/native operations."""
import base64
import csv
import hashlib
import json
from pathlib import Path
import zipfile

D = Path(__file__).resolve().parent
E = D.parent
O = E.parent.parent
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
load = lambda p: json.loads(Path(p).read_text())
freeze = load(E / 'PACKAGE07_BENCHMARK_FREEZE.json')
mutations = load(E / 'PACKAGE07_BENCHMARK_MUTATIONS.json')
binding = load(E / 'PACKAGE07_PUBLIC_BINDING.json')
bdn = load(E / 'PACKAGE07_BDN_DRY.json')
checks = []

def check(label, value):
    checks.append({'check': label, 'passed': bool(value)})
    assert value, label

check('freeze SHA', sha(E / 'PACKAGE07_BENCHMARK_FREEZE.json') == '6a3ac93d204488b5f23e2bb003b0ad7c0ff3babe188e38ee7a42c910ed293dac')
for row in freeze['files']:
    for key in ['path', 'snapshot']:
        check(key + ':' + row[key], sha(row[key]) == row['sha256'] and Path(row[key]).stat().st_size == row['bytes'])
    if row.get('comparison_baseline'):
        check('baseline:' + row['comparison_baseline'], sha(row['comparison_baseline']) == row['baseline_sha256'])
for row in freeze['proofs']:
    check('proof:' + row['path'], sha(row['path']) == row['sha256'])
ledger = [json.loads(line) for line in (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()]
receipts = []

def walk(value):
    if isinstance(value, dict):
        if 'log_sha256' in value and 'phase' in value:
            receipts.append(value)
        else:
            for item in value.values(): walk(item)
    elif isinstance(value, list):
        for item in value: walk(item)

walk(freeze['native_receipts'])
walk(mutations)
for row in receipts:
    check('receipt-log:' + row['phase'], sha(row['log']) == row['log_sha256'])
    check('ledger-receipt:' + row['phase'], sum(item == row for item in ledger) == 1)
actual_log_plan = load(D / 'ACTUAL_LOG_PLAN.json')
supplementary_receipts = []
for row in actual_log_plan['logs']:
    matched = [item for item in ledger if Path(item['log']).resolve() == Path(row['path']).resolve()]
    check('all personally read log receipts:' + row['path'], len(matched) == 1 and matched[0]['log_sha256'] == sha(row['path']) == row['sha256'])
    supplementary_receipts.append(matched[0])
for row in mutations['mutants']:
    check('actual mutant snapshot:' + row['name'], sha(row['binary']['snapshot']) == row['binary']['sha256'])
    check('strict native mutation:' + row['name'], row['build_receipt']['exit_code'] == 0 and '-warnaserror' in row['build_receipt']['command'] and row['assertion']['native_receipt']['exit_code'] == 2)
    check('observed causal failure:' + row['name'], load(row['assertion']['native_receipt']['log']) == row['assertion']['observed'])
for phase, key in [('original', 'baseline'), ('corrected', 'controls'), ('rollback', 'rollback')]:
    for row in mutations[key]:
        check('observed control:' + phase + ':' + row['mode'], load(row['native_receipt']['log']) == row['observed'])
        if row['observed']['passed']:
            for dll, prop in [('vicione-servicebus-benchmark.dll', 'benchmarkDll'), ('ViciOne.ServiceBus.BenchmarkConsole.dll', 'consoleDll')]:
                check('control snapshot:' + phase + ':' + row['mode'] + ':' + dll, sha(R / 'repair-evidence/package07-benchmark-binaries' / phase / dll) == row['observed'][prop])
for path, expected in mutations['oracle_hashes'].items():
    check('oracle:' + path, sha(path) == expected)
lock = load(freeze['files'][6]['snapshot'])['dependencies']['net10.0']
physical = []
for row in binding['packages']:
    selected, cache, runtime = map(Path, [row['selected_nupkg'], row['cache_nupkg'], row['runtime']])
    name = row['package']
    check('selected/cache:' + name, sha(selected) == sha(cache) == row['nupkg_sha256'])
    check('lock/cache contentHash:' + name, lock[name]['contentHash'] == row['contentHash_sha512'] == cache.with_name(cache.name + '.sha512').read_text().strip())
    with zipfile.ZipFile(selected) as archive:
        dllhash = hashlib.sha256(archive.read(row['member'])).hexdigest()
        check('zip/cache/runtime DLL:' + name, dllhash == row['dll_sha256'] == sha(runtime) == sha(cache.parent / row['member']))
        xmlname = row['member'][:-4] + '.xml'
        xmlhash = hashlib.sha256(archive.read(xmlname)).hexdigest()
        check('zip/cache XML:' + name, xmlhash == sha(cache.parent / xmlname))
        physical.append({'package': name, 'dll_sha256': dllhash, 'xml_sha256': xmlhash, 'runtime_xml_present': runtime.with_suffix('.xml').exists()})
for row in bdn['reports']:
    check('BDN report:' + row['path'], sha(row['path']) == row['sha256'])
    for case in load(row['path'])['Benchmarks']:
        check('BDN N1 actual:' + case['FullName'], case['Statistics']['N'] == 1 and any(item['IterationStage'] == 'Actual' and item['Operations'] == 1 for item in case['Measurements']))
rows = list(csv.DictReader((R / 'FILE_COVERAGE.csv').open()))
mismatch = [row['path'] for row in rows if sha(R / 'mutations/source-main' / row['path']) != row['sha256']]
check('all6209M restored', len(rows) == 6209 and not mismatch)
owner = [row for row in rows if row['path'].startswith('tests/Benchmarks/ViciOne.ServiceBus.Benchmark.Tests/')]
check('unchanged Benchmark testowner', bool(owner) and all(sha(O / row['path']) == row['sha256'] for row in owner))
result = {'status': 'ALL_READONLY_BINDINGS_MATCH', 'checks': checks, 'distinct_receipt_count': len({row['phase'] for row in receipts}), 'all_personally_read_log_receipts': supplementary_receipts, 'physical_packages': physical, 'restored_files': len(rows), 'mismatches': mismatch, 'benchmark_owner_files_hashchecked': len(owner), 'reviewer_native_runs': 0}
(D / 'INDEPENDENT_BINDINGS.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({'checks': len(checks), 'distinct_receipts': result['distinct_receipt_count'], 'owner_files': len(owner), 'packages': len(physical)}))

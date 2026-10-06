"""Read-only static verification; writes only this independent review directory."""
import pathlib, json, hashlib, base64, zipfile, struct, zlib, uuid, csv

E = pathlib.Path(__file__).resolve().parent.parent
A = pathlib.Path(__file__).resolve().parent
R = pathlib.Path('/private/tmp/vicione-servicebus-api-review-20261001')
sha = lambda b: hashlib.sha256(b).hexdigest()
def write(name, obj):
    p = A / name
    assert not p.exists(), 'Immutable research artifact already exists: ' + str(p)
    p.write_text(json.dumps(obj, indent=2, ensure_ascii=False) + '\n')
    print(name, sha(p.read_bytes()))

freeze_p = E / 'PACKAGE04_F018_ORIGINAL_PROVENANCE_FREEZE.json'
f = json.loads(freeze_p.read_bytes())
assert sha(freeze_p.read_bytes()) == 'd21f250036f840f4efe5909e8388be3d514b5f5c0205aa75f4a306f57ec3d143'
bind_p = E / 'PACKAGE04_F018_ORIGINAL_PROVENANCE_BINDING.json'
binding = json.loads(bind_p.read_bytes())
assert sha(bind_p.read_bytes()) == '7a767f70066b5dc73ba524cb73aa8c34fcc90fcc9d18d6f757f95e3eda7efbf5'
old = json.loads((A / 'READ_CLOSURE.json').read_bytes())
read_hashes = {r['sha256']: r for r in old['files']}
files = []
for r in f['owned_inputs']:
    p = pathlib.Path(r['snapshot']); b = p.read_bytes()
    assert sha(b) == r['sha256']
    # Factory at M is restored original; current supplement snapshots are immutable.
    assert b == pathlib.Path(r['path']).read_bytes()
    previous = read_hashes.get(r['sha256'])
    if previous:
        mode = 'READ_FULL_OWN_PREVIOUS_PERSONAL_READ_EXACT_SHA_REUSE'
        assert pathlib.Path(previous.get('snapshot', previous['path'])).read_bytes() == b
        note = 'Exact own personally FULL-read text reused; no foreign manifest substitution.'
    else:
        assert p.name.endswith(('packages.lock.json', 'NuGet.Config'))
        mode = 'READ_FULL'
        note = 'Complete new lock/config personally read; identical second consumer copy fully byte checked.'
    files.append(dict(r, bytes=len(b), read_status=mode, semantic_notes=note))
assert len(files) == 11

receipts = []
for r in f['receipts']:
    assert r in binding['receipts']
    p = pathlib.Path(r['log']); b = p.read_bytes()
    assert sha(b) == r['log_sha256']
    files.append(dict(path=str(p), sha256=sha(b), bytes=len(b), read_status='READ_FULL',
                      kind='ACTUAL_NATIVE_RECEIPT_LOG', semantic_notes='Entire actual log personally read, including all causal stack frames; no operations executed by reviewer.'))
    receipts.append(r)
for p in (freeze_p, bind_p):
    b = p.read_bytes()
    files.append(dict(path=str(p), sha256=sha(b), bytes=len(b), read_status='READ_FULL',
                      kind='SUPPLEMENT_PROOF', semantic_notes='All JSON fields personally FULL read in complete untruncated sections.'))

bindings = []
graphs = []
old_graph = json.loads((A / 'PACKAGE_BINDING_INDEPENDENT.json').read_bytes())['graphs'][0]
for c in binding['consumers']:
    root = pathlib.Path(c['consumer'])
    lock_p = root / 'packages.lock.json'; lock = json.loads(lock_p.read_bytes())['dependencies']['net10.0']
    assert sha(lock_p.read_bytes()) == c['lock_sha256']
    for r in c['packages']:
        p = pathlib.Path(r['nupkg']); b = p.read_bytes()
        cache = pathlib.Path(r['cache']); raw512 = base64.b64encode(hashlib.sha512(b).digest()).decode()
        assert sha(b) == r['nupkg_sha256'] and raw512 == r['contentHash'] == lock[r['package']]['contentHash']
        assert cache.read_bytes() == b
        with zipfile.ZipFile(p) as z: dll = z.read(r['member'])
        cache_dll = cache.parent / r['member']
        assert dll == cache_dll.read_bytes() == pathlib.Path(r['runtime']).read_bytes()
        assert sha(dll) == r['dll_sha256']
        bindings.append(dict(r, consumer=str(root), status='SELECTED_ARCHIVE_SHA512_LOCK_OWNCACHE_RUNTIME_EXACT'))
    # Reuse previous complete graph paths/member mapping, but independently verify every new archive/lock/runtime.
    pkgrows = []
    for row in old_graph['packages']:
        name, version = row['package'].split('/')
        pc = root / 'restored-packages' / name.lower() / version
        arc = pc / (name.lower() + '.' + version + '.nupkg')
        b = arc.read_bytes(); raw512 = base64.b64encode(hashlib.sha512(b).digest()).decode()
        metadata = json.loads((pc / '.nupkg.metadata').read_bytes())
        assert metadata['contentHash'] == lock[name]['contentHash']
        assert (pc / (name.lower()+'.'+version+'.nupkg.sha512')).read_text().strip() == raw512
        if name != 'ViciOne.ServiceBus.EntityFrameworkCore.Sagas': assert sha(b) == row['archive_sha256']
        pkgrows.append(dict(package=row['package'], archive=str(arc), sha256=sha(b), lock_hash=lock[name]['contentHash'], raw_sha512=raw512))
    runrows = []
    for row in old_graph['runtime']:
        name, version = row['package'].split('/')
        pc = root / 'restored-packages' / name.lower() / version
        arc = pc / (name.lower()+'.'+version+'.nupkg')
        with zipfile.ZipFile(arc) as z: member = z.read(row['member'])
        runtime = root / 'bin/Release/net10.0' / pathlib.Path(row['runtime']).name
        assert member == (pc / row['member']).read_bytes() == runtime.read_bytes()
        runrows.append(dict(package=row['package'], member=row['member'], runtime=str(runtime), sha256=sha(member)))
    assert len(pkgrows) == 40 and len(runrows) == 37
    actual_dlls = {str(p) for p in (root / 'bin/Release/net10.0').glob('*.dll') if not p.name.startswith('F018.')}
    assert actual_dlls == {r['runtime'] for r in runrows}
    runlog = next(r for r in receipts if r['phase'].endswith(('ownership-run' if 'ownership-' in str(root) else 'identity-run')))
    actual = json.loads(pathlib.Path(runlog['log']).read_bytes())
    assert actual == c['actual']
    assert actual['failures'] == sum(not x['assertions_passed'] for x in actual['cases'])
    assert not any(x.get('bounded_guard_failure') for x in actual['cases'])
    graphs.append(dict(consumer=str(root), packages=pkgrows, runtime=runrows, actual=actual,
                       status='ALL40_PACKAGE_HASH_METADATA_AND37_MANAGED_RUNTIME_MEMBERS_VERIFIED'))
assert len(bindings) == 16

# Parse the embedded Portable PDB directly from exactly the archive DLL bound above.
target = binding['consumers'][0]['packages'][3]
assert target['package'] == 'ViciOne.ServiceBus.EntityFrameworkCore.Sagas'
with zipfile.ZipFile(target['nupkg']) as z: dll = z.read(target['member'])
pe = struct.unpack_from('<I', dll, 60)[0]
sections_n = struct.unpack_from('<H', dll, pe+6)[0]
optional_size = struct.unpack_from('<H', dll, pe+20)[0]
opt = pe+24
magic = struct.unpack_from('<H', dll, opt)[0]
data_dirs = opt + (112 if magic == 0x20b else 96)
debug_rva, debug_size = struct.unpack_from('<II', dll, data_dirs+6*8)
sections = []
for i in range(sections_n):
    q = opt+optional_size+i*40
    vsize, vaddr, rsize, rptr = struct.unpack_from('<IIII', dll, q+8)
    sections.append((vaddr, max(vsize, rsize), rptr))
def rva_offset(rva):
    for va, size, ptr in sections:
        if va <= rva < va+size: return ptr+rva-va
    raise AssertionError('Unmapped RVA')
pdb = None
for q in range(rva_offset(debug_rva), rva_offset(debug_rva)+debug_size, 28):
    typ, n, addr, ptr = struct.unpack_from('<IIII', dll, q+12)
    if typ == 17:
        packed = dll[ptr:ptr+n]; assert packed[:4] == b'MPDB'
        pdb = zlib.decompress(packed[8:], -15)
        assert len(pdb) == struct.unpack_from('<I', packed, 4)[0]
assert pdb and pdb[:4] == b'BSJB'
vlen = struct.unpack_from('<I', pdb, 12)[0]
q = (16+vlen+3) & ~3
flags, stream_n = struct.unpack_from('<HH', pdb, q); q += 4
streams = {}
for _ in range(stream_n):
    off, n = struct.unpack_from('<II', pdb, q); q += 8
    end = pdb.index(0, q); name = pdb[q:end].decode(); q = (end+4) & ~3
    streams[name] = pdb[off:off+n]
table = streams['#~']; valid = struct.unpack_from('<Q', table, 8)[0]; q = 24; rows = {}
for i in range(64):
    if valid >> i & 1: rows[i] = struct.unpack_from('<I', table, q)[0]; q += 4
assert min(rows) == 48 and table[6] == 0 # all blob/guid indexes two bytes
def compressed(b, q):
    first = b[q]
    if first < 0x80: return first, q+1
    if first < 0xc0: return ((first & 0x3f) << 8) | b[q+1], q+2
    return ((first & 0x1f) << 24) | (b[q+1]<<16) | (b[q+2]<<8) | b[q+3], q+4
def blob(index):
    b = streams['#Blob']; n, start = compressed(b, index); return b[start:start+n]
documents = []
for i in range(rows[48]):
    name_idx, alg_idx, hash_idx, language_idx = struct.unpack_from('<HHHH', table, q+i*8)
    name_blob = blob(name_idx); sep = chr(name_blob[0]); nq = 1; segments = []
    while nq < len(name_blob):
        idx, nq = compressed(name_blob, nq); segments.append(blob(idx).decode())
    algorithm = str(uuid.UUID(bytes_le=streams['#GUID'][(alg_idx-1)*16:alg_idx*16]))
    documents.append(dict(path=sep.join(segments), algorithm_guid=algorithm, checksum=blob(hash_idx).hex()))
factory = [r for r in documents if r['path'].endswith('/EntityFrameworkSagaRepositoryContextFactory.cs')]
assert len(factory) == 1
assert factory[0]['algorithm_guid'] == '8829d00f-11b8-4213-878b-770e8597ac16'
assert factory[0]['checksum'] == binding['original_factory_sha256'] == files[0]['sha256']
assert sha(dll) == binding['binary_sha256'] == target['dll_sha256']
pdbproof = dict(status='EMBEDDED_PORTABLE_PDB_DOCUMENT_TABLE48_EXACT_ORIGINAL_FACTORY_SHA256',
                dll_sha256=sha(dll), embedded_pdb_sha256=sha(pdb), embedded_pdb_bytes=len(pdb),
                document_count=len(documents), factory_document=factory[0],
                source_snapshot=f['owned_inputs'][0]['snapshot'], source_sha256=files[0]['sha256'],
                method='PE embedded debug entry17 -> MPDB raw-deflate -> metadata streams -> Document table48 -> Name/HashAlgorithm/Hash columns',
                limits=['Static checksum evidence binds original factory to exact tested archive DLL; no full compiler/PDB method-body or every source-document review claimed.'])

coverage = R / 'FILE_COVERAGE.csv'
source_rows = list(csv.DictReader(coverage.open()))
bad = []
for r in source_rows:
    actual = sha((R/'mutations/source-main'/r['path']).read_bytes())
    if actual != r['sha256']: bad.append(dict(path=r['path'], expected=r['sha256'], actual=actual))
assert len(source_rows) == 6209
rollback = dict(status=('ALL6209_ORIGINAL_TRACKED_BYTES_RESTORED_AFTER_SUPPLEMENT' if not bad else 'CURRENT_LATER_PACKAGE05_MUTATION_OBSERVATION'), coverage=str(coverage),
                coverage_sha256=sha(coverage.read_bytes()), files=len(source_rows), mismatches=bad, read_status='HASH_CHECK_ONLY',
                limits=['Current read occurs after F018 original-provenance native checkpoint; Root reports independent later Package05 mutation process owns five possible differences. No historical-time state inferred from this current-state hash check.'])
write('SOURCE_PDB_BINDING_SUPPLEMENT.json', pdbproof)
write('PACKAGE_BINDING_SUPPLEMENT_INDEPENDENT.json', dict(status='ALL16_SELECTED_BINDINGS_AND_TWO_COMPLETE_RUNTIME_GRAPHS_EXACT', bindings=bindings, graphs=graphs))
write('ROLLBACK_INDEPENDENT_SUPPLEMENT.json', rollback)
write('READ_CLOSURE_SUPPLEMENT.json', dict(status='ALL11_SUPPLEMENT_INPUTS_PERSONALLY_FULL_OR_OWN_FULL_EXACT_REUSE', read_by='azure_test_scope independent internal reviewer',
                                         files=files, original_owned_inputs=11, receipts=receipts,
                                         previous_personal_read_closure=dict(path=str(A/'READ_CLOSURE.json'), sha256=sha((A/'READ_CLOSURE.json').read_bytes())),
                                         foreign_read_reuse=False, native_operations_executed=0, product_writes=0,
                                         limits=['Generated package graph inspected structurally and binaries/hash bound; no full textual graph-reading claim.', 'Root canonical owner reading/test repair and 410/409/1 native boundary remain open.', 'Eleven actual supplement owned inputs, separate from thirteen main freeze inputs.']))

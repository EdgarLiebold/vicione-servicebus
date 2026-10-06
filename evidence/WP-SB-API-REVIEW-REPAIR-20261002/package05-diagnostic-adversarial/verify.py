"""Independent readonly F030 evidence checks. Writes exclusively this dossier.
No dotnet, native operations, product writes, or mutation execution.
"""
import base64, csv, datetime, difflib, hashlib, importlib.util, json, pathlib, zipfile

O = pathlib.Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
R = pathlib.Path('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
M = R / 'mutations/source-main'
S = E / 'package05-diagnostic-adversarial'
def sha_bytes(b): return hashlib.sha256(b).hexdigest()
def sha(p): return sha_bytes(pathlib.Path(p).read_bytes())
def load(p): return json.loads(pathlib.Path(p).read_text())
def bind(p):
    p = pathlib.Path(p)
    return dict(path=str(p), sha256=sha(p), bytes=p.stat().st_size)
def save(name, obj):
    p=S/name
    assert not p.exists(), p
    p.write_text(json.dumps(obj, indent=2, ensure_ascii=False)+'\n')

freeze_path=E/'PACKAGE05_F030_DIAGNOSTIC_FREEZE.json'
assert sha(freeze_path)=='2b09f2d8d830f1e1de7f900109aeda58cde2b6918cde9bd23ab88d901087a21e'
freeze=load(freeze_path)
root_path=E/'PACKAGE05_DIAGNOSTIC_ROOT_BINDING.json'; root=load(root_path)
mut_path=E/'PACKAGE05_DIAGNOSTIC_MUTATIONS.json'; mut=load(mut_path)
for key in ['root_binding','source_mutations','preauthor_plan']:
    x=freeze[key]; assert sha(x['path'])==x['sha256']
owned=freeze['owned_inputs']; assert len(owned)==29
owned_bindings=[]; first={}
for i,x in enumerate(owned):
    a=pathlib.Path(x['snapshot']).read_bytes(); b=pathlib.Path(x['path']).read_bytes()
    assert a==b and len(a)==x['bytes'] and sha_bytes(a)==x['sha256'], x
    mode='READ_FULL_PERSONAL_TEXT'
    if x['sha256'] in first:
        mode='READ_FULL_OWN_BYTE_IDENTICAL_REUSE'; reuse=first[x['sha256']]
    else:
        first[x['sha256']]=i; reuse=None
    owned_bindings.append(dict(index=i,**x,read_status='READ_FULL',read_by='azure_test_scope',read_mode=mode,reuse_owned_index=reuse,current_bytes_match=True))
prod={str(pathlib.Path(x['path']).relative_to(O)):pathlib.Path(x['snapshot']).read_text() for x in owned[:8]}

# Check selected actual receipts, without claiming a full ledger review.
ledger=[json.loads(x) for x in (E/'VERIFICATION_LOG.jsonl').read_text().splitlines() if x.strip()]
receipt_checks=[]; native=[]
for receipt in root['receipts']:
    assert any(receipt==x for x in ledger), receipt['phase']
    assert sha(receipt['log'])==receipt['log_sha256']
    text=pathlib.Path(receipt['log']).read_text()
    row=dict(receipt=receipt,log_bytes=len(text.encode()),read_status='READ_FULL_PERSONAL_TEXT',ledger_exact_match=True)
    if text.lstrip().startswith('{'):
        actual=json.loads(text)
        assert actual['count']==len(actual['cases'])
        assert actual['failures']==sum(not x['assertions_passed'] for x in actual['cases'])
        assert (receipt['exit_code']==0)==(actual['failures']==0)
        row['actual']=actual; native.append(row)
    else:
        assert receipt['exit_code']==0
        if 'build' in receipt['phase']:
            assert ('0 Warning(s)' in text and '0 Error(s)' in text) or ('0 Warnung(en)' in text and '0 Fehler' in text)
    receipt_checks.append(row)
assert len(receipt_checks)==36

# These pure substitutions are independently encoded from fully read effects.
reg='src/ViciOne.ServiceBus/DependencyInjection/Registration/Consumers/ConsumerRegistration.cs'
conn='src/ViciOne.ServiceBus/Configuration/BatchConsumerMessageConnector.cs'
identity='src/ViciOne.ServiceBus/Configuration/ConsumerBusIdentityOptions.cs'
batch='src/ViciOne.ServiceBus/Batching/Runtime/BatchCollector.cs'
effects={
 'registration-owner-omitted':(reg,'        consumerConfigurator.Options(new ConsumerBusIdentityOptions(\n            context is IBusRegistrationIdentity identity ? identity.BusKey : "unknown"));\n',''),
 'connector-constant-default':(conn,'            ? identity.BusKey','            ? "default"'),
 'plain-handoff-omitted':(conn,'(options, batchMessagePipe, busKey)','(options, batchMessagePipe, "unknown")'),
 'grouped-handoff-omitted':(conn,'options.GroupKeyProvider, busKey)','options.GroupKeyProvider, "unknown")'),
 'beta-owner-replaced-by-alpha':(identity,'= busKey;','= busKey.Replace("IBetaBus", "IAlphaBus", System.StringComparison.Ordinal);'),
 'collector-owner-ignored':(batch,'_busKey = busKey;','_busKey = "unknown";')}
mutation_checks=[]
for case in mut['mutants']:
    key,before,after=effects[case['name']]; assert prod[key].count(before)==1
    changes=[]; all_sources=[]
    for rel,x in case['binary']['source_snapshots'].items():
        text=pathlib.Path(x['path']).read_text(); assert sha(x['path'])==x['sha256']
        expected=prod[rel].replace(before,after) if rel==key else prod[rel]
        assert text==expected, (case['name'],rel)
        all_sources.append(dict(path=rel,snapshot=x,exact_expected_bytes=True))
        if text!=prod[rel]:
            changes.append(dict(path=rel,delta=''.join(difflib.unified_diff(prod[rel].splitlines(True),text.splitlines(True),fromfile='corrected',tofile=case['name']))))
    assert len(all_sources)==8 and len(changes)==1
    binaries=case['binary']['binaries']
    for binary in binaries: assert sha(binary['path'])==binary['sha256']
    n=case['rejection']; p=case['positive_control']
    for label,x in [('negative',n),('positive',p)]:
        assert load(x['receipt']['log'])==x['actual']
        assert sha(x['receipt']['log'])==x['receipt']['log_sha256']
        for b in binaries:
            loaded=next(z for z in x['actual']['runtime']['loadedProductAssemblies'] if pathlib.Path(z['path']).name==pathlib.Path(b['path']).name)
            assert loaded['sha256']==b['sha256']
    assert n['receipt']['exit_code']==1 and n['actual']['count']==n['actual']['failures']==1
    a=n['actual']['cases'][0]; assert a['exception_type']=='Xunit.Sdk.StartsWithException' and not a['bounded_guard_failure']
    assert 'DiagnosticOwnerAssertions.cs:line 94' in a['StackTrace']
    assert p['receipt']['exit_code']==0 and p['actual']['count']==1 and p['actual']['failures']==0
    mutation_checks.append(dict(name=case['name'],source_effects=all_sources,delta=changes[0],compiled_binaries=binaries,negative=n,positive=p,status='INDEPENDENTLY_VERIFIED_ACTUAL_CAUSAL_KILL',pack_per_mutant=False))
for phase in ['corrected_binary','rollback_binary']:
    for rel,x in mut[phase]['source_snapshots'].items():
        assert sha(x['path'])==x['sha256'] and pathlib.Path(x['path']).read_text()==prod[rel]
    for x in mut[phase]['binaries']: assert sha(x['path'])==x['sha256']
for x in mut['controls']+[mut['rollback']]:
    assert load(x['receipt']['log'])==x['actual']
    assert x['actual']['count']==8 and x['actual']['failures']==0 and x['receipt']['exit_code']==0

# Selected archive -> SHA512 lock -> dedicated cache -> loaded output binding.
packages=[]; graph_checks=[]
for variant in root['package_bindings']:
    lockpath=pathlib.Path(variant['lock']); lock=load(lockpath); graph=lock['dependencies']['net10.0']
    cache=lockpath.parent/'restored-packages'
    graph_rows=[]
    for name,x in graph.items():
        pkgdir=cache/name.lower()/x['resolved']
        archive=pkgdir/(name.lower()+'.'+x['resolved']+'.nupkg')
        raw=archive.read_bytes(); content=base64.b64encode(hashlib.sha512(raw).digest()).decode()
        assert (pkgdir/(name.lower()+'.'+x['resolved']+'.nupkg.sha512')).read_text().strip()==content
        if name.startswith('ViciOne.'): assert content==x['contentHash']
        graph_rows.append(dict(package=name,version=x['resolved'],sha256=sha_bytes(raw),raw_archive_sha512=content,lock_content_hash=x['contentHash'],raw_equals_lock=content==x['contentHash'],cache=str(pkgdir),dependencies=x.get('dependencies',{})))
    assert len(graph_rows)==20
    graph_checks.append(dict(variant=variant['variant'],lock=bind(lockpath),all_20_lock_packages_cache_archives_verified=graph_rows,external_hash_limit='External signed/normalized package lock hash not independently equated to raw ZIP; separate recorded hash domains.'))
    for x in variant['packages']:
        path=pathlib.Path(x['package']); raw=path.read_bytes(); assert sha_bytes(raw)==x['sha256']
        content=base64.b64encode(hashlib.sha512(raw).digest()).decode(); assert content==x['lock_content_sha512']
        name=x['loaded']['name']; assert graph[name]['contentHash']==content
        loaded=x['loaded']; assert sha(loaded['path'])==loaded['sha256'] and sha(x['cached_dll'])==loaded['sha256']
        with zipfile.ZipFile(path) as z:
            dll=z.read('lib/net10.0/'+name+'.dll'); assert sha_bytes(dll)==loaded['sha256']
        archive=cache/name.lower()/graph[name]['resolved']/(name.lower()+'.'+graph[name]['resolved']+'.nupkg')
        assert archive.read_bytes()==raw
        packages.append(dict(variant=variant['variant'],**x,independent_zip_lock_cache_runtime_verified=True))
assert len(packages)==6

# Original rollback is a point-in-time independent byte check, not source reading.
csvpath=R/'FILE_COVERAGE.csv'; original=list(csv.DictReader(csvpath.open()))
checked=[]; mismatches=[]
for x in original:
    p=M/x['path']; actual=sha(p)
    checked.append(dict(path=x['path'],expected_sha256=x['sha256'],actual_sha256=actual))
    if actual!=x['sha256']: mismatches.append(checked[-1])
assert len(checked)==6209 and not mismatches, mismatches
assert not (M/identity).exists()
oracles=[dict(path=p,expected_sha256=h,current_sha256=sha(p)) for p,h in mut['oracle_hashes'].items()]
assert all(x['expected_sha256']==x['current_sha256'] for x in oracles)

# Bind actual compiled PE/PDB documents to reviewed source text, no native execution.
spec=importlib.util.spec_from_file_location('own_pdb_reader',E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py')
pdb=importlib.util.module_from_spec(spec); spec.loader.exec_module(pdb)
pdbchecks=[]
for variant in root['package_bindings'][:2]:
    for x in variant['packages']:
        d=pdb.document_checksums(x['cached_dll']); matches=[]
        for rel,current in prod.items():
            docs=[z for z in d['documents'] if z['path'].replace('\\','/').endswith('/'+rel)]
            if not docs: continue
            if variant['variant']=='previous':
                if rel in [reg,conn]: expected=(F/rel).read_bytes()
                elif rel==identity: raise AssertionError('identity unexpectedly in previous package')
                elif rel==batch: expected=(E/'package05-core/freeze/03-BatchCollector.cs').read_bytes()
                else: expected=current.encode()
            else: expected=current.encode()
            assert len(docs)==1 and docs[0]['checksum']==sha_bytes(expected), (variant['variant'],rel,docs)
            matches.append(dict(path=rel,source_sha256=sha_bytes(expected),document=docs[0]))
        pdbchecks.append(dict(variant=variant['variant'],dll=x['loaded']['name'],dll_sha256=d['dll_sha256'],pdb_kind=d['pdb_kind'],pdb_sha256=d['pdb_sha256'],source_matches=matches))
assert sum(len(x['source_matches']) for x in pdbchecks if x['variant']=='previous')==7
assert sum(len(x['source_matches']) for x in pdbchecks if x['variant']=='corrected')==8

utc=datetime.datetime.now(datetime.timezone.utc).isoformat()
save('MECHANICAL_PROOFS.json',dict(utc=utc,freeze=bind(freeze_path),owned_current_snapshot_match=owned_bindings,receipts=receipt_checks,operations='LOCAL_READONLY_HASH_JSON_ZIP_PORTABLE_PDB_ONLY',native_operations=0,product_writes=0))
save('MUTATION_REVIEW.json',dict(utc=utc,status='SIX_ACTUAL_COMPILED_MUTANTS_CAUSALLY_KILLED',mutants=mutation_checks,baseline=mut['controls'],rollback=mut['rollback'],mutation_package_boundary='Compiled DLL substitution in separately NuGet-compiled public consumer; no pack per mutant claim.'))
save('PACKAGE_BINDING_INDEPENDENT.json',dict(utc=utc,packages=packages,lock_graphs=graph_checks,source_binary_pdb=pdbchecks,pdb_reader=bind(E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py')))
save('ROLLBACK_INDEPENDENT.json',dict(utc=utc,csv=bind(csvpath),checked_count=6209,mismatch_count=0,files=checked,new_identity_removed=True,oracle_hashes=oracles,runtime_selected_pack_bytes_restored=True,scope='Independent point-in-time SHA verification; not 6209 personal source reads.'))
print(json.dumps(dict(status='ALL_INDEPENDENT_MECHANICAL_CHECKS_PASSED',owned=29,receipts=36,mutants=6,selected_package_chains=6,full_lock_graphs=3,rollback_count=6209,pdb_source_matches=15,output=str(S))))

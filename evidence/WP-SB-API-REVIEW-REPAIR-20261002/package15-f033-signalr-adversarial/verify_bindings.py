"""Independent readonly hashes/ZIP/PE-PDB checks; writes only this reviewer dossier."""
import ast, base64, csv, datetime, hashlib, importlib.util, json, pathlib, subprocess, sys, zipfile
sys.dont_write_bytecode = True
P = pathlib.Path
W = P('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0')
O = W / 'repositories/vicione-servicebus'
E = O / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002'
R = P('/private/tmp/vicione-servicebus-api-review-20261001')
F = R / 'frozen-repository'
SELF = E / 'package15-f033-signalr-adversarial'
sha = lambda p: hashlib.sha256(P(p).read_bytes()).hexdigest()
meta = lambda p: dict(path=str(p), sha256=sha(p), bytes=P(p).stat().st_size)
load = lambda p: json.loads(P(p).read_bytes())
freeze = load(E / 'PACKAGE15_F033_SIGNALR_FREEZE.json')
assert sha(E / 'PACKAGE15_F033_SIGNALR_FREEZE.json') == '23a798b6c06e282f661e13f005bacd71b8cd67d33b8cac39600aaf09f050e4d2'
root = load(freeze['root_binding']['path'])
mut = load(freeze['mutations']['path'])
for key in ['root_binding', 'mutations']:
    assert sha(freeze[key]['path']) == freeze[key]['sha256']
assert len(freeze['owned_inputs']) == 46
for row in freeze['owned_inputs']:
    assert sha(row['path']) == sha(row['snapshot']) == row['sha256']
    assert P(row['path']).stat().st_size == row['bytes']
for row in freeze['plans_and_exclusions']:
    assert sha(row['path']) == row['sha256']
frame = {str(P(row['path']).relative_to(O)):P(row['snapshot']).read_bytes() for row in freeze['owned_inputs'][:21]}
orig = {rel:(F / rel).read_bytes() if (F / rel).exists() else None for rel in frame}
for rel, data in orig.items():
    if data is not None:
        assert subprocess.check_output(['git','-C',str(O),'show','HEAD:' + rel]) == data
provider = next(x for x in frame if x.endswith('/DependencyInjectionBackplaneScopeProvider.cs'))
manager = next(x for x in frame if x.endswith('/ServiceBusHubLifetimeManager.cs'))
helper = next(x for x in frame if x.endswith('/BackplaneOperationLifetime.cs'))

def method_body(s, signature):
    start = s.index(signature); begin = s.index('{',start); depth = 0
    for i in range(begin,len(s)):
        depth += (s[i] == '{') - (s[i] == '}')
        if depth == 0: return start,i+1
    raise AssertionError(signature)

def expected_variant(name):
    result = dict(frame)
    if name == 'resolution-primary-discarded': result[provider] = orig[provider]
    elif name.endswith('-await-using-restored'):
        route = name.removesuffix('-await-using-restored')
        sig = {'publish':'    async Task PublishAsync<TMessage>(', 'connections':'    public override async Task SendConnectionsAsync(', 'groups':'    public override async Task SendGroupsAsync(', 'users':'    public override async Task SendUsersAsync(', 'remotegroup':'    async Task ChangeGroupMembershipAsync('}[route]
        a = frame[manager].decode(); b = orig[manager].decode(); s,e=method_body(a,sig); u,v=method_body(b,sig)
        result[manager] = (a[:s]+b[u:v]+a[e:]).encode()
    else:
        s = frame[helper].decode()
        if name == 'request-fault-skips-scope':
            s = s.replace('            (failures ??= []).Add(exception);','            throw;',1).replace('catch (Exception exception)','catch (Exception)',1)
        elif name == 'aggregate-order-reversed':
            before='            throw new AggregateException("Backplane operation and release encountered multiple failures.", failures);'
            s=s.replace(before,'        {\n            failures.Reverse();\n'+before+'\n        }')
        elif name == 'single-fault-wrapped': s=s.replace('            ExceptionDispatchInfo.Capture(failures[0]).Throw();','            throw new AggregateException(failures);')
        elif name == 'request-async-preferred': s=s.replace('            request?.Dispose();','            if (request is IAsyncDisposable asyncRequest)\n                await asyncRequest.DisposeAsync().ConfigureAwait(false);\n            else\n                request?.Dispose();')
        else: raise AssertionError(name)
        result[helper]=s.encode()
    return result

frames = [('corrected',mut['corrected_binary'],frame)] + [(m['name'],m['binary'],expected_variant(m['name'])) for m in mut['mutants']] + [('rollback',mut['rollback_binary'],frame)]
effects=[]; binaries=[]
for name,binary,expected in frames:
    assert set(binary['source_snapshots']) == set(frame)
    changed = [rel for rel in frame if expected[rel] != frame[rel]]
    assert len(changed) == (0 if name in ['corrected','rollback'] else 1)
    for rel,row in binary['source_snapshots'].items():
        assert P(row['path']).read_bytes() == expected[rel]
        assert sha(row['path']) == row['sha256']
        effects.append(dict(variant=name,source=rel,sha256=row['sha256']))
    assert len(binary['binaries']) == 1
    for row in binary['binaries']: assert sha(row['path']) == row['sha256']
    binaries.append((name,binary['binaries'][0],expected))
assert effects == root['source_effect_checks'] and len(effects)==252
ledger=[json.loads(s) for s in (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()]
receipts=root['actual_receipts']; assert len(receipts)==62
for receipt in receipts:
    assert ledger.count(receipt)==1 and sha(receipt['log'])==receipt['log_sha256']
    if 'build' in receipt['command'] and receipt['exit_code']==0:
        text=P(receipt['log']).read_text();assert '0 Warnung(en)' in text and '0 Fehler' in text and '-warnaserror' in receipt['command']
for m in mut['mutants']:
    for key in ['rejection','positive_control']:
        run=m[key];assert run['receipt'] in receipts and load(run['receipt']['log'])==run['actual']
        actual=run['actual'];assert actual['count']==1
        assert actual['failures']==(key=='rejection')
        c=actual['cases'][0]
        if key=='rejection':assert c['exception_type'].startswith('Xunit.Sdk.') and not c['bounded_guard_failure'] and 'Cases.cs:line' in c['StackTrace']
        else: assert c['assertions_passed']
        assert next(x for x in actual['runtime']['loadedProductAssemblies'] if x['name']=='ViciOne.ServiceBus.SignalR')['sha256']==m['binary']['binaries'][0]['sha256']
    assert m['binary']['receipt']['exit_code']==0
prior=load(mut['prior_nine_pairs']['path']);assert sha(mut['prior_nine_pairs']['path'])==mut['prior_nine_pairs']['sha256']
assert mut['mutants'][:9]==prior['mutants'] and len(prior['mutants'])==9
assert mut['mutants'][-1]['positive_control']['actual']['cases'][0]['case']=='publish-success'
for run in mut['controls']+[mut['rollback']]: assert load(run['receipt']['log'])==run['actual'] and run['actual']['count']==41 and run['actual']['failures']==0

spec=importlib.util.spec_from_file_location('own_pdb',E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py')
reader=importlib.util.module_from_spec(spec);spec.loader.exec_module(reader)
pdbs=[];conditions=[]
def check_pdb(path, expected, label):
    pdb=reader.document_checksums(path);pdbs.append({k:v for k,v in pdb.items() if k!='documents'})
    for rel,digest in expected.items():
        matches=[d for d in pdb['documents'] if d['path'].endswith(rel if rel.startswith('/') else '/'+rel)]
        assert len(matches)==1,(label,rel,len(matches))
        row=matches[0];assert row['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16' and row['checksum']==digest,(label,rel,row)
        conditions.append(dict(label=label,dll=str(path),source=rel,sha256=digest))
    return pdb
for name,binary,expected in binaries:
    check_pdb(binary['path'],{rel:hashlib.sha256(expected[rel]).hexdigest() for rel in [provider,manager,helper]},name)
chains=[];pruning=[]
for chain in root['selected_package_chains']:
    package=P(chain['package']);raw=package.read_bytes();digest=base64.b64encode(hashlib.sha512(raw).digest()).decode();assert sha(package)==chain['sha256'] and digest==chain['raw_zip_sha512_base64']
    stage=chain['consumer'];owner=R/'repair-consumers/package15-f033-signalr'/stage;cache=P(chain['cache']);runtime=chain['runtime'];name=runtime['name']
    lock=load(owner/'packages.lock.json')['dependencies']['net10.0'];assert lock[name]['contentHash']==digest
    assert sha(cache/(name.lower()+'.1.0.0.nupkg'))==sha(package) and (cache/(name.lower()+'.1.0.0.nupkg.sha512')).read_text().strip()==digest
    with zipfile.ZipFile(package) as z:
        entry=next(n for n in z.namelist() if n.startswith('lib/') and n.endswith('/'+name+'.dll'));assert hashlib.sha256(z.read(entry)).hexdigest()==runtime['sha256']
    assert sha(runtime['path'])==sha(cache/entry)==runtime['sha256']
    if name.endswith('.SignalR'):
        expected={str(p.relative_to(F)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (F/'src/Transports/ViciOne.ServiceBus.SignalR').rglob('*.cs')}
        if stage=='corrected':expected.update({rel:hashlib.sha256(frame[rel]).hexdigest() for rel in [provider,manager,helper]})
    else:
        expected={rel:hashlib.sha256(data).hexdigest() for rel,data in frame.items() if rel.startswith('src/'+name+'/')}
    check_pdb(runtime['path'],expected,stage+'/'+name)
    chains.append(chain)
for stage in ['previous','corrected']:
    owner=R/'repair-consumers/package15-f033-signalr'/stage
    cs=[p for p in owner.iterdir() if p.suffix=='.cs']
    check_pdb(owner/'bin/Release/net10.0/SignalRLifetime.PublicConsumer.dll',{str(p):sha(p) for p in cs},stage+'/publicoracle')
    assets=load(owner/'obj/project.assets.json');framework=assets['project']['frameworks']['net10.0']
    deps=next(c for c in chains if c['consumer']==stage and c['runtime']['name']=='ViciOne.ServiceBus')['nuspec_dependencies']
    relevant={d['id']:framework['packagesToPrune'][d['id']] for d in deps if d['id'].startswith('Microsoft.Extensions.')}
    assert len(relevant)==6 and set(relevant.values())=={'(,10.0.32767]'} and 'Microsoft.AspNetCore.App' in framework['frameworkReferences']
    pruning.append(dict(consumer=stage,assets=meta(owner/'obj/project.assets.json'),read_extent='TARGETED_FRAMEWORK_LIBRARY_PRUNE_METADATA',prune_ranges=relevant))
    runtime=load(E/('package15-f033-signalr-public-'+('previous-fortyone-r3' if stage=='previous' else 'corrected-fortyone')+'.log'))['runtime']
    for row in runtime['loadedMicrosoftAssemblies']:assert sha(row['path'])==row['sha256'] and '/Microsoft.AspNetCore.App/10.0.10/' in row['path']
for p,expected in mut['oracle_hashes'].items():assert sha(p)==expected
out=R/'repair-consumers/package15-f033-signalr/corrected/bin/Release/net10.0'
assert sha(out/'ViciOne.ServiceBus.SignalR.dll')==next(c['runtime']['sha256'] for c in chains if c['consumer']=='corrected' and c['runtime']['name'].endswith('.SignalR'))
for name,digest in mut['constant_runtime_dependencies'].items():assert sha(out/name)==digest
started=datetime.datetime.now(datetime.timezone.utc).isoformat();rows=list(csv.DictReader((R/'FILE_COVERAGE.csv').open()));assert len(rows)==6209
assert all(sha(R/'mutations/source-main'/row['path'])==row['sha256'] for row in rows)
ended=datetime.datetime.now(datetime.timezone.utc).isoformat()
assert not (R/'mutations/source-main'/helper).exists() and not (R/'mutations/source-main/src/ViciOne.ServiceBus/Configuration/ConsumerBusIdentityOptions.cs').exists()
result=dict(status='INDEPENDENT_READONLY_BINDINGS_PASS',read_by='azure_test_scope',freeze=meta(E/'PACKAGE15_F033_SIGNALR_FREEZE.json'),owned_inputs_checked=46,source_effects=effects,source_effect_count=len(effects),actual_receipts=receipts,selected_package_chains=chains,sdk_pruning=pruning,pdb_images=pdbs,pdb_source_checks=conditions,pdb_source_condition_count=len(conditions),oracle_restored=True,runtime_restored=True,rollback=dict(csv=meta(R/'FILE_COVERAGE.csv'),started_utc=started,completed_utc=ended,original_checked=6209,mismatches=[],new_files_removed=True),native_operations_executed=0,product_writes=0,test_writes=0,whole_API_CLOSED=False)
dest=SELF/'BINDING_PROOF.json';assert not dest.exists();dest.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(path=str(dest),sha256=sha(dest),effects=len(effects),receipts=len(receipts),packages=len(chains),pdb_images=len(pdbs),pdb_conditions=len(conditions),rollback=6209)))

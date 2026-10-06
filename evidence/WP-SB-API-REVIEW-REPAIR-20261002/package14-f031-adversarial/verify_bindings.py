"""Readonly independent F031 source, package, receipt and PDB proof; dossier writes only."""
from pathlib import Path
import base64,csv,hashlib,importlib.util,json,subprocess,sys,zipfile
from datetime import datetime,timezone
sys.dont_write_bytecode=True
R=Path('/private/tmp/vicione-servicebus-api-review-20261001')
O=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';S=E/'package14-f031-adversarial';F=R/'frozen-repository';M=R/'mutations/source-main'
sha=lambda b:hashlib.sha256(b).hexdigest()
load=lambda p:json.loads(Path(p).read_text())
def save(name,value):
 with (S/name).open('x') as f:f.write(json.dumps(value,indent=2)+'\n')
def desc(p):
 p=Path(p);b=p.read_bytes();return dict(path=str(p),sha256=sha(b),bytes=len(b))
freeze=load(E/'PACKAGE14_F031_FREEZE.json');mut=load(E/'PACKAGE14_F031_MUTATIONS.json');root=load(E/'PACKAGE14_F031_ROOT_BINDING.json')
assert sha((E/'PACKAGE14_F031_FREEZE.json').read_bytes())=='dd8260a0f701f3a9c47cf8efe4232e13b12a54c3bbd24cd30d411e85f23fdb2b'
for key in ['mutations','root_binding']:
 assert sha(Path(freeze[key]['path']).read_bytes())==freeze[key]['sha256']
pre=R/'repair-research/package14-f031-preflight';pm=pre/'MUTATION_PREFLIGHT_SUPPLEMENT.json';pc=pre/'READ_CLOSURE.json';p13=E/'package13-f033-core-adversarial/READ_CLOSURE.json'
known=[]
for p,keys in [(pm,['source_frame','public_console_inputs','fresh_inputs']),(pc,['source_inputs','additional_SPI_sources','consumer_inputs','metadata_inputs','excluded_setup_sources_and_logs']),(p13,['owned_inputs','shared_public_route_sources'])]:
 for key in keys:
  for row in load(p).get(key,[]):known.append((row,p))
owned=[]
for row in freeze['owned_inputs']:
 b=Path(row['path']).read_bytes();assert b==Path(row['snapshot']).read_bytes();assert sha(b)==row['sha256'] and len(b)==row['bytes']
 matches=[p for x,p in known if x['sha256']==row['sha256'] and x.get('read_status')=='READ_FULL']
 assert matches,row['path'];owned.append(dict(row,read_by='azure_test_scope',read_status='READ_FULL',read_mode='OWN_PRIOR_PERSONAL_FULL_EXACT_SHA_REUSE_FRESH_CURRENT_AND_SNAPSHOT_CHECK',own_prior_manifest=str(matches[0]),own_prior_manifest_sha256=sha(matches[0].read_bytes())))
assert len(owned)==34
frame={str(Path(x['path']).relative_to(O)):x for x in owned[:18]};corrected={k:Path(v['snapshot']).read_bytes() for k,v in frame.items()}
context='src/ViciOne.ServiceBus/Configuration/DependencyInjection/BusRegistrationContext.cs';combined='src/ViciOne.ServiceBus/Configuration/DependencyInjection/CombinedEndpointDefinition.cs'
original=(F/context).read_bytes();assert original==subprocess.run(['git','-C',str(O),'show','HEAD:'+context],check=True,capture_output=True).stdout
assert sha(original)=='4f7c63c487d9e8916075f53f7d7aad97b261731e93020805e60c3e679a075825'
expected={'corrected':dict(corrected),'rollback':dict(corrected)}
transforms={'category-order-reversed':(context,b'            .OrderBy(x => x.Kind.Order)',b'            .OrderByDescending(x => x.Kind.Order)'), 'shared-qos-ownership-weakened':(combined,b'        return owners > 1',b'        return owners >= 1'),'mixed-topology-check-weakened':(combined,b'        else if (_definitions.All(x => x.ConfigureConsumeTopology == false))',b'        else if (_definitions.Any(x => x.ConfigureConsumeTopology == false))')}
for m in mut['mutants']:
 name=m['name'];d=dict(corrected)
 if name=='first-category-restored':d[context]=original
 else:
  rel,a,b=transforms[name];assert d[rel].count(a)==1;d[rel]=d[rel].replace(a,b)
 assert sum(d[k]!=corrected[k] for k in d)==1;expected[name]=d
variants={'corrected':mut['corrected_binary'],**{m['name']:m['binary'] for m in mut['mutants']},'rollback':mut['rollback_binary']};effects=[]
for name,v in variants.items():
 assert len(v['source_snapshots'])==18
 for rel,row in v['source_snapshots'].items():
  b=Path(row['path']).read_bytes();assert b==expected[name][rel];assert sha(b)==row['sha256'];mode='OWN_PRIOR_PERSONAL_FULL_EXACT_SHA_REUSE'
  if b!=corrected[rel] and name!='first-category-restored':mode='PERSONAL_FRESH_FULL_CHANGED_MUTANT_NUMBERED_TEXT'
  effects.append(dict(variant=name,source=rel,sha256=sha(b),bytes=len(b),snapshot=row['path'],read_status='READ_FULL',read_by='azure_test_scope',read_mode=mode))
 for row in v['binaries']:assert sha(Path(row['path']).read_bytes())==row['sha256']
assert [{k:x[k] for k in ['variant','source','sha256']} for x in effects]==root['source_effect_checks']
ledger=[json.loads(line) for line in (E/'VERIFICATION_LOG.jsonl').read_text().splitlines()];receipts=root['actual_receipts'];logs=[];observations=[]
for r in receipts:
 assert any(x==r for x in ledger),r['phase'];b=Path(r['log']).read_bytes();assert sha(b)==r['log_sha256'];logs.append(dict(r,bytes=len(b),read_status='READ_FULL',read_by='azure_test_scope',read_mode='OWN_PRIOR_FULL_EXACT_SHA_REUSE' if r['phase'] in ['package14-f031-public-previous-build','package14-f031-public-previous-build-r2','package14-f031-public-previous-build-r3'] else 'PERSONAL_FRESH_FULL_ACTUAL_LOG_TEXT'))
 if b.lstrip().startswith(b'{'):
  a=json.loads(b);assert a['count']==len(a['cases']);assert a['failures']==sum(not x['assertions_passed'] for x in a['cases']);assert not any(x.get('bounded_guard_failure',False) for x in a['cases']);assert r['exit_code']==(1 if a['failures'] else 0);observations.append(dict(phase=r['phase'],count=a['count'],failures=a['failures'],cases=a['cases'],runtime=a['runtime']))
 elif 'build' in r['phase'] and r['exit_code']==0:assert '0 Warnung(en)' in b.decode() and '0 Fehler' in b.decode()
assert len(receipts)==29
for m in mut['mutants']:
 for x in m['rejections']+m['positive_controls']:
  assert x['receipt'] in receipts and x['actual']==load(x['receipt']['log'])
  for b in m['binary']['binaries']:
   actual=next(y for y in x['actual']['runtime']['loadedProductAssemblies'] if Path(y['path']).name==Path(b['path']).name);assert actual['sha256']==b['sha256']
 for x in m['rejections']:
  a=x['actual'];assert a['count']==a['failures']==1;fail=a['cases'][0];assert not fail['bounded_guard_failure'];assert 'EndpointCases.cs:line 82' in fail['StackTrace'] if m['name'] in ['first-category-restored','category-order-reversed'] else 'EndpointCases.cs:line 50' in fail['StackTrace'];assert fail['exception_type']==('Xunit.Sdk.EqualException' if m['name'] in ['first-category-restored','category-order-reversed'] else 'Xunit.Sdk.NotNullException')
 for x in m['positive_controls']:assert x['actual']['count']==1 and x['actual']['failures']==0
for x in mut['controls']+[mut['rollback']]:assert x['actual']==load(x['receipt']['log']) and x['actual']['count']==14 and x['actual']['failures']==0
for p,h in mut['oracle_hashes'].items():assert sha(Path(p).read_bytes())==h
dep=mut['unchanged_capability_dependency'];assert sha(Path(dep['path']).read_bytes())==dep['sha256']
for obs in observations:
 d=next(x for x in obs['runtime']['loadedProductAssemblies'] if x['name']=='ViciOne.ServiceBus.Sagas');assert d['sha256']==dep['sha256']
packages=[]
for c in root['selected_package_chains']:
 p=Path(c['package']);b=p.read_bytes();assert sha(b)==c['sha256'];h=base64.b64encode(hashlib.sha512(b).digest()).decode();assert h==c['raw_zip_sha512_base64']==c['lock_contentHash'];name=c['runtime']['name'];consumer=R/'repair-consumers/package14-f031'/c['consumer'];assert load(consumer/'packages.lock.json')['dependencies']['net10.0'][name]['contentHash']==h
 cache=Path(c['cache']);assert (cache/(name.lower()+'.1.0.0.nupkg')).read_bytes()==b;assert (cache/(name.lower()+'.1.0.0.nupkg.sha512')).read_text().strip()==h
 with zipfile.ZipFile(p) as z:dll=z.read('lib/net10.0/'+name+'.dll')
 assert dll==(cache/'lib/net10.0'/(name+'.dll')).read_bytes()==Path(c['runtime']['path']).read_bytes();assert sha(dll)==c['runtime']['sha256'];packages.append(dict(c,independently_verified=True))
for consumer in ['previous','corrected']:
 observation=next(x for x in observations if x['phase']=='package14-f031-public-'+consumer+'-fourteen')
 for c in packages:
  if c['consumer']==consumer:assert c['runtime'] in observation['runtime']['loadedProductAssemblies']
save('INDEPENDENT_SOURCE_EFFECTS_PACKAGES_AND_RECEIPTS.json',dict(status='VERIFIED',source_effect_count=len(effects),source_effects=effects,selected_package_chains=packages,actual_receipt_count=len(receipts),selected_exact_ledger_receipts=receipts,actual_native_logs=logs,observations=observations,accepted_native_observation_count=sum(x['count'] for x in observations),accepted_negative_count=sum(x['failures'] for x in observations),original_context=dict(sha256=sha(original),bytes=len(original),frozen_equals_git_HEAD=True),oracle_preserved=True,runtime_restored_to_selected_packages=True,unchanged_sagas_dependency=dep))
helper=E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py';spec=importlib.util.spec_from_file_location('own_pdb_reader',helper);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);images={};docs=[]
def bind(image,rel,h,group):
 image=Path(image);k=str(image)
 if k not in images:images[k]=module.document_checksums(image)
 m=images[k];matches=[x for x in m['documents'] if x['path'].replace('\\','/').endswith('/'+rel)];assert len(matches)==1,(image,rel,matches);doc=matches[0];assert doc['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16';assert doc['checksum']==h,(image,rel,doc['checksum'],h);docs.append(dict(group=group,dll=str(image),dll_sha256=m['dll_sha256'],pdb_sha256=m['pdb_sha256'],source=rel,sha256=h,document=doc['path']))
def assembly(rel):
 return 'ViciOne.ServiceBus.Sagas' if rel.startswith('src/ViciOne.ServiceBus.Sagas/') else 'ViciOne.ServiceBus.Abstractions' if rel.startswith('src/ViciOne.ServiceBus.Abstractions/') else 'ViciOne.ServiceBus'
routes={}
for x in load(p13)['shared_public_route_sources']:routes[x['path']]=(x['sha256'],p13)
for key in ['source_inputs','additional_SPI_sources']:
 for x in load(pc)[key]:
  rel=str(Path(x['path']).relative_to(O)) if x['path'].startswith(str(O)) else x['path'];routes[rel]=(x['sha256'],pc)
route_read=[]
for rel,(h,manifest) in routes.items():
 p=O/rel;assert sha(p.read_bytes())==h,(rel,h);route_read.append(dict(path=rel,sha256=h,bytes=p.stat().st_size,read_status='READ_FULL',read_by='azure_test_scope',read_mode='OWN_PRIOR_PERSONAL_FULL_EXACT_SHA_REUSE_FRESH_CURRENT_CHECK',own_prior_manifest=str(manifest),own_prior_manifest_sha256=sha(manifest.read_bytes())))
for consumer in ['previous','corrected']:
 out=R/'repair-consumers/package14-f031'/consumer/'bin/Release/net10.0'
 for rel,x in frame.items():bind(out/(assembly(rel)+'.dll'),rel,sha(original) if rel==context and consumer=='previous' else x['sha256'],'selected-frame-'+consumer)
 for rel,(h,manifest) in routes.items():bind(out/(assembly(rel)+'.dll'),rel,sha(original) if rel==context and consumer=='previous' else h,'public-route-'+consumer)
 for name in ['Components.cs','EndpointCases.cs','OwnedLifetime.cs','Program.cs']:
  p=out.parents[2]/name;bind(out/'SharedEndpoint.PublicConsumer.dll',name,sha(p.read_bytes()),'consumer-'+consumer)
for name,v in variants.items():
 for rel,row in v['source_snapshots'].items():
  image=next(x['path'] for x in v['binaries'] if Path(x['path']).name==assembly(rel)+'.dll');bind(image,rel,row['sha256'],'variant-'+name)
save('INDEPENDENT_PDB_SOURCE_BINDING.json',dict(status='ALL_BOUNDED_SHA256_DOCUMENT_CHECKSUMS_MATCH',helper=desc(helper),pe_image_count=len(images),source_checksum_condition_count=len(docs),public_route_count=len(routes),public_route_inputs=route_read,images=[{k:v for k,v in x.items() if k!='documents'} for x in images.values()],document_checks=docs,limits='Counts are source-binding conditions with overlap, not unique entire product files. Static PE/embedded or CodeView matched sidecar PDB only; no whole compiled assembly source closure.'))
start=datetime.now(timezone.utc).isoformat();coverage=R/'FILE_COVERAGE.csv';cb=coverage.read_bytes();rows=list(csv.DictReader(cb.decode().splitlines()));mismatches=[]
for row in rows:
 if sha((M/row['path']).read_bytes())!=row['sha256']:mismatches.append(row['path'])
assert len(rows)==6209 and not mismatches;identity='src/ViciOne.ServiceBus/Configuration/ConsumerBusIdentityOptions.cs';assert not (M/identity).exists()
save('INDEPENDENT_6209_ROLLBACK_CHECK.json',dict(status='ALL_6209_ORIGINAL_HASHES_MATCH',started_utc=start,completed_utc=datetime.now(timezone.utc).isoformat(),coverage_sha256=sha(cb),original_files_checked=len(rows),mismatches=mismatches,new_identity_source_removed=True,limits='Completed readonly byte checkpoint; no native build run and no correctness inference from source hash restoration alone.'))
save('READ_CLOSURE.json',dict(status='READ_FULL_BOUNDED_F031_REVIEW',read_by='azure_test_scope',utc=datetime.now(timezone.utc).isoformat(),freeze=desc(E/'PACKAGE14_F031_FREEZE.json'),owned_inputs=owned,owned_count=len(owned),public_route_inputs=route_read,public_route_count=len(route_read),actual_native_logs=logs,actual_log_count=len(logs),selected_exact_ledger_receipts=receipts,ledger_limit='Only selected29 receipts personally FULL; not whole appendonly verification ledger.',mutant_source_snapshot_reads=effects,mutation_json_read_mode='Complete structured-field read; 108 snapshot source bindings own FULL exact reuse/three fresh FULL changed texts, exact receipts reuse from FULL Rootbinding, actual payloads byte-equal personally FULL actual logs; remaining metadata freshly FULL structured read. No raw full-line dump claim.',own_preflight=desc(pc),own_mutation_preflight=desc(pm),root_read_transfer=False,limits=['No retained QoS value projection, two buses, persistence/reload, parallel State.Probe, active cancellation, Core766 wholeowner, wholeAPI/ARM/cloud grade or symbol CLOSED.']))
print('SOURCE_EFFECTS',len(effects),'CHAINS',len(packages),'RECEIPTS',len(receipts),'PE_IMAGES',len(images),'PDB_CONDITIONS',len(docs),'PUBLIC_ROUTE_INPUTS',len(routes),'M_ORIGINALS',len(rows))

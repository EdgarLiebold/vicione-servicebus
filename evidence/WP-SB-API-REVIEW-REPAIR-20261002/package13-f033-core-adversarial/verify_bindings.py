"""Independent readonly byte/ZIP/PE/PDB verification; outputs only this review dossier."""
from pathlib import Path
import ast,base64,csv,hashlib,importlib.util,json,subprocess,sys
sys.dont_write_bytecode=True
R=Path('/private/tmp/vicione-servicebus-api-review-20261001');O=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus');E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';S=E/'package13-f033-core-adversarial';F=R/'frozen-repository'
sha=lambda b:hashlib.sha256(b).hexdigest()
load=lambda p:json.loads(Path(p).read_text())
freeze=load(E/'PACKAGE13_F033_CORE_FREEZE.json');mut=load(E/'PACKAGE13_F033_CORE_MUTATIONS.json');root=load(E/'PACKAGE13_F033_CORE_ROOT_BINDING.json')
frame={str(Path(x['path']).relative_to(O)):x for x in freeze['owned_inputs'][:16]};corrected={k:Path(v['snapshot']).read_text() for k,v in frame.items()}
factory='src/ViciOne.ServiceBus/DependencyInjection/ScopeConsumerFactory.cs';life='src/ViciOne.ServiceBus/Consumers/OwnedConsumerLifetime.cs'
original=(F/factory).read_bytes();git=subprocess.run(['git','-C',str(O),'show','HEAD:'+factory],check=True,capture_output=True).stdout;assert git==original
expected={};expected['corrected']=dict(corrected);expected['rollback']=dict(corrected)
for variant in mut['mutants']:
 name=variant['name'];d=dict(corrected)
 if name=='original-scope-factory':d[factory]=original.decode()
 elif name=='primary-capture-dropped':d[factory]=d[factory].replace('            operationFailure = exception;','            operationFailure = null;\n            _ = exception;')
 elif name=='dual-failure-order-reversed':d[life]=d[life].replace('                operationFailure,\n                releaseFailure);','                releaseFailure,\n                operationFailure);')
 elif name=='cleanup-failure-swallowed':d[life]=d[life].replace('            releaseFailure = exception;','            _ = exception;')
 elif name=='container-consumer-disposed-twice':
  d[factory]=d[factory].replace('        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);','        if (operationFailure is not null && scope.Context.Consumer is IDisposable disposable)\n        {\n            try { disposable.Dispose(); } catch { }\n        }\n        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);')
 elif name=='mixed-cancellation-collapsed':
  d[factory]=d[factory].replace('        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);','        try\n        {\n            await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);\n        }\n        catch (AggregateException) when (operationFailure is OperationCanceledException)\n        {\n            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(operationFailure).Throw();\n        }')
 else:raise ValueError(name)
 assert len([k for k in d if d[k]!=corrected[k]])==1
 expected[name]=d
variants={'corrected':mut['corrected_binary'],**{m['name']:m['binary'] for m in mut['mutants']},'rollback':mut['rollback_binary']};effects=[]
for name,v in variants.items():
 assert len(v['source_snapshots'])==16
 for rel,row in v['source_snapshots'].items():
  b=Path(row['path']).read_bytes();assert b==expected[name][rel].encode();assert sha(b)==row['sha256'];effects.append({'variant':name,'source':rel,'sha256':sha(b),'snapshot':row['path'],'bytes':len(b),'read_status':'READ_FULL','read_by':'azure_test_scope','read_mode':'PERSONAL_FULL_CHANGED_MUTANT_TEXT' if b!=corrected[rel].encode() else 'OWN_PERSONAL_PRIOR_FULL_EXACT_SHA_REUSE'})
 for row in v['binaries']:assert sha(Path(row['path']).read_bytes())==row['sha256']
assert [{k:x[k] for k in ['variant','source','sha256']} for x in effects]==root['source_effect_checks']
for m in mut['mutants']:
 for x in m['rejections']+m['positive_controls']:
  assert x['actual']==load(x['receipt']['log'])
  for b in m['binary']['binaries']:
   actual=next(y for y in x['actual']['runtime']['loadedProductAssemblies'] if Path(y['path']).name==Path(b['path']).name);assert actual['sha256']==b['sha256']
for p,h in mut['oracle_hashes'].items():assert sha(Path(p).read_bytes())==h
checks=[];import zipfile
for c in root['selected_package_chains']:
 p=Path(c['package']);b=p.read_bytes();assert sha(b)==c['sha256'];h=base64.b64encode(hashlib.sha512(b).digest()).decode();assert h==c['raw_zip_sha512_base64']==c['lock_contentHash'];name=c['runtime']['name'];consumer=R/'repair-consumers/package13-f033'/c['consumer'];lock=load(consumer/'packages.lock.json');assert lock['dependencies']['net10.0'][name]['contentHash']==h
 cache=Path(c['cache']);assert (cache/(name.lower()+'.1.0.0.nupkg')).read_bytes()==b;assert (cache/(name.lower()+'.1.0.0.nupkg.sha512')).read_text().strip()==h
 with zipfile.ZipFile(p) as z:dll=z.read('lib/net10.0/'+name+'.dll')
 assert dll==(cache/'lib/net10.0'/(name+'.dll')).read_bytes()==Path(c['runtime']['path']).read_bytes();assert sha(dll)==c['runtime']['sha256'];checks.append(dict(c,verified=True))
(S/'INDEPENDENT_SOURCE_EFFECTS_AND_PACKAGES.json').open('x').write(json.dumps({'status':'VERIFIED','source_effect_count':len(effects),'source_effects':effects,'selected_package_chains':checks,'oracle_preserved':True,'runtime_restored_to_selected_packages':True,'original_scope_factory':{'sha256':sha(original),'bytes':len(original),'frozen_equals_git_HEAD':True}},indent=2)+'\n')
helper=E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py';spec=importlib.util.spec_from_file_location('own_pdb_reader',helper);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
images={};docs=[]
def bind(image,rel,h,group):
 image=Path(image);k=str(image)
 if k not in images:images[k]=module.document_checksums(image)
 m=images[k];matches=[x for x in m['documents'] if x['path'].replace('\\','/').endswith('/'+rel)];assert len(matches)==1,(image,rel,matches);doc=matches[0];assert doc['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16';assert doc['checksum']==h,(image,rel,doc['checksum'],h);docs.append({'group':group,'dll':str(image),'dll_sha256':m['dll_sha256'],'pdb_sha256':m['pdb_sha256'],'source':rel,'sha256':h,'document':doc['path']})
for consumer in ['previous','corrected']:
 for rel,x in frame.items():
  name='ViciOne.ServiceBus.Abstractions' if rel.startswith('src/ViciOne.ServiceBus.Abstractions/') else 'ViciOne.ServiceBus';h=sha(original) if rel==factory and consumer=='previous' else x['sha256'];bind(R/'repair-consumers/package13-f033'/consumer/'bin/Release/net10.0'/(name+'.dll'),rel,h,'selected-frame-'+consumer)
 for name in ['ConsumerCases.cs','OwnedLifetime.cs','Probe.cs','Program.cs']:
  p=R/'repair-consumers/package13-f033'/consumer/name;bind(p.parent/'bin/Release/net10.0/ScopeFault.PublicConsumer.dll',name,sha(p.read_bytes()),'consumer-'+consumer)
for name,v in variants.items():
 for rel,row in v['source_snapshots'].items():
  dll='ViciOne.ServiceBus.Abstractions.dll' if rel.startswith('src/ViciOne.ServiceBus.Abstractions/') else 'ViciOne.ServiceBus.dll';image=next(x['path'] for x in v['binaries'] if Path(x['path']).name==dll);bind(image,rel,row['sha256'],'variant-'+name)
# Public normal registration/scopes/observer routes are own previously FULL; changed later F031 BusRegistrationContext binds frozen F/Git, never current Core wholesale.
pf=R/'repair-research/lifetime-final-findings/F033_CURRENT_SOURCE_PREFLIGHT.json';pg=E/'package12-graph-adversarial/READ_CLOSURE.json';pm=R/'repair-research/lifetime-final-findings/F033_MUTATION_PREFLIGHT.json';pl=R/'repair-research/lifetime-final-findings/READ_CLOSURE_FINAL.json';routes={}
for x in load(pf)['files']:
 if x['path'].startswith('src/ViciOne.ServiceBus/') or x['path'].startswith('src/ViciOne.ServiceBus.Abstractions/'):routes[x['path']]=(x['sha256'],pf)
for x in load(pg)['shared_public_route_source_inputs']:routes[str(Path(x['path']).relative_to(O))]=(x['sha256'],pg)
x=load(pm)['limits_contract'];routes[str(Path(x['path']).relative_to(O))]=(x['sha256'],pm)
for x in load(pl)['files']:
 if x['path'] in ['src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionConsumerRegistrationExtensions.cs','src/ViciOne.ServiceBus/DependencyInjection/Registration/Consumers/ConsumerRegistrationConfigurator.cs']:routes[x['path']]=(x['sha256'],pl)
route_read=[]
for rel,(h,manifest) in routes.items():
 current=O/rel
 if sha(current.read_bytes())==h:source=current;matches_current=True
 else:
  source=F/rel;assert sha(source.read_bytes())==h,(rel,h);assert subprocess.run(['git','-C',str(O),'show','HEAD:'+rel],check=True,capture_output=True).stdout==source.read_bytes();matches_current=False
 route_read.append({'path':rel,'sha256':h,'bytes':source.stat().st_size,'read_status':'READ_FULL','read_by':'azure_test_scope','read_mode':'OWN_PERSONAL_PRIOR_FULL_EXACT_SHA_REUSE','own_prior_manifest':str(manifest),'own_prior_manifest_sha256':sha(manifest.read_bytes()),'source_read_binding':str(source),'matches_current':matches_current})
 for consumer in ['previous','corrected']:
  name='ViciOne.ServiceBus.Abstractions' if rel.startswith('src/ViciOne.ServiceBus.Abstractions/') else 'ViciOne.ServiceBus';want=sha(original) if rel==factory and consumer=='previous' else h;bind(R/'repair-consumers/package13-f033'/consumer/'bin/Release/net10.0'/(name+'.dll'),rel,want,'public-route-'+consumer)
result={'status':'ALL_EXACT_SHA256_DOCUMENT_CHECKSUMS_MATCH','helper':{'path':str(helper),'sha256':sha(helper.read_bytes()),'read_status':'READ_FULL'},'pe_image_count':len(images),'source_checksum_conditions':len(docs),'route_input_count':len(routes),'route_inputs':route_read,'images':[{k:v for k,v in x.items() if k!='documents'} for x in images.values()],'document_checks':docs,'limits':'Static PE/embedded-or-CodeView-matched-sidecar PDB checksum binding; bounded source paths only, not whole Core or every SDK implementation.'}
(S/'INDEPENDENT_PDB_SOURCE_BINDING.json').open('x').write(json.dumps(result,indent=2)+'\n');print('EFFECTS',len(effects),'PACKAGE_CHAINS',len(checks),'PE_IMAGES',len(images),'PDB_CONDITIONS',len(docs),'PUBLIC_ROUTE_INPUTS',len(routes))

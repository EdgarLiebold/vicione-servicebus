"""Independent pure read/hash/AST/ZIP/PE evidence checker. No .NET/native tests, no product writes."""
import ast,base64,csv,difflib,hashlib,importlib.util,json,re,subprocess,sys,zipfile
from pathlib import Path
sys.dont_write_bytecode=True
W=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0');O=W/'repositories/vicione-servicebus';R=Path('/private/tmp/vicione-servicebus-api-review-20261001');F=R/'frozen-repository';M=R/'mutations/source-main';E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';A=E/'package18-f035-adversarial';D=W/'SERVICEBUS_API_REVIEW_AND_REPAIR';C=R/'repair-consumers/package18-f035';P=R/'repair-research/package18-f035-public-preflight'
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):return digest(Path(p).read_bytes())
def read(p):return json.loads(Path(p).read_text())
def save(n,v):
 p=A/n;assert not p.exists(),p;p.write_text(json.dumps(v,indent=2,ensure_ascii=False)+'\n')
freeze=E/'PACKAGE18_F035_FREEZE.json';assert sha(freeze)=='024d8c1da8184ebb7236e08270ec7fc3267589c13aa59247972077734aa74fda';z=read(freeze)
previous_closure=E/'package17-f029-adversarial/READ_CLOSURE.json';assert sha(previous_closure)=='25c6a45b3e3ebfe365c648f3c79875fc24ad52b007894328786e29124041b71f';pr=read(previous_closure)
whole_path=P/'whole-owner-001/READ_CLOSURE.json';assert sha(whole_path)=='30a56eeca72db15be679e4b166742a96d8e3b810babb024eed6879ebe9eecc43';whole=read(whole_path)
source_path=P/'source-original-qualification-001/READ_CLOSURE.json';assert sha(source_path)=='6ed2a403d196b391307f1ae9f45df1d4c807fb6dd95010a1aa5db4788f20ae7a';source=read(source_path)
runner_path=P/'runner-r2-001/READ_CLOSURE.json';assert sha(runner_path)=='8e80ac41b7cfbabca11f2c8210dfa5a87406b402244b34a2d3398ea0ad2c27e6';runner=read(runner_path)
reuse={q['path']:(q,str(previous_closure))for q in pr['owned_inputs']}
reuse.update({q['path']:(q,str(whole_path))for q in whole['owner_inputs']})
reuse.update({q['path']:(q,str(runner_path))for q in runner['personally_FULL_whole_runners']})
sq=source['new_source_personal_FULL'];reuse[sq['path']]=(sq,str(source_path))
reuse[str(D/'package18_f035_bind.py')]=({'sha256':'80aebd6ebe87ef2877c521fa41cf781f9c6c6a833e9100647fb10cce72a2aaed'},str(P/'binder-001/REVIEW.json'))
reuse[str(C/'corrected/packages.lock.json')]=({'sha256':'07881084828c017cc5b61cb33263452337e76c5227731a10c2fbb2b2106d978f'},str(P/'corrected-lock-001/MANIFEST.json'))
owned=[]
for q in z['owned_inputs']:
 assert sha(q['path'])==q['sha256']==sha(q['snapshot']);assert Path(q['path']).stat().st_size==q['bytes'];old,cl=reuse[q['path']];assert old['sha256']==q['sha256'];
 if 'snapshot'in old:assert sha(old['snapshot'])==q['sha256']
 owned.append({**q,'personal_read':'PERSONAL_FULL_EXACT_OWN_READ_REUSE','own_closure':cl,'own_closure_sha256':sha(cl),'own_prior_row':old})
assert len(owned)==48
save('FREEZE_HASH_CHECK.json',{'freeze':str(freeze),'sha256':sha(freeze),'inputs':owned})
for q in z['plans_and_exclusions']+[z['root_binding'],z['mutations']]:assert sha(q['path'])==q['sha256']
save('PLANS_EXCLUSIONS_CHECK.json',z['plans_and_exclusions'])
bind=read(z['root_binding']['path']);mut=read(z['mutations']['path']);frame=mut['frame'];corrected={k:(O/rel).read_text()for k,rel in frame.items()};assert len(frame)==27
ast0=ast.parse((D/'package18_f035_mutations.py').read_text());pure=ast.Module(body=[n for n in ast0.body if isinstance(n,ast.FunctionDef)and n.name in ['replace_once','mutation']],type_ignores=[]);ns={'corrected':corrected};exec(compile(pure,'own-pure-mutators','exec'),ns)
variants=ast.literal_eval(next(n.value for n in ast0.body if isinstance(n,ast.Assign)and any(isinstance(t,ast.Name)and t.id=='variants'for t in n.targets)))
effects=[];native=[]
for st in mut['controls']+mut['mutants']:
 texts=ns['mutation'](st['name'])if st in mut['mutants']else corrected
 assert sha(st['binary']['path'])==st['binary']['sha256'];assert st['build']['exit_code']==0
 for q in st['source_snapshots']:
  expected=texts[q['key']].encode();actual=Path(q['path']).read_bytes();assert actual==expected;assert digest(actual)==q['sha256']==q['current_source_sha256'];changed=actual!=corrected[q['key']].encode();assert changed==(st in mut['mutants']and q['key']=='definition_registrar');effects.append({'stage':st['name'],**q,'changed':changed})
 if st in mut['mutants']:
  name,neg,pos,typ=next(v for v in variants if v[0]==st['name']);nr=st['negative'];rr=st['positive'];nt=Path(nr['log']).read_text();pt=Path(rr['log']).read_text();headers=[s for s in nt.splitlines()if s.startswith('FAIL ')];assert len(headers)==1 and headers[0].startswith('FAIL '+neg+' '+typ+':');assert 'TimeoutException'not in nt and 'AggregateException'not in headers[0];assert nr['exit_code']==1 and rr['exit_code']==0;assert re.findall(r'^PASS (.+)$',pt,re.M)==[pos]and not re.search('^FAIL ',pt,re.M)
  for txt in [nt,pt]:assert st['binary']['sha256']in txt
  native.append({'name':name,'negative':neg,'positive':pos,'direct_failure':headers[0],'core_sha256':st['binary']['sha256'],'negative_log_sha256':sha(nr['log']),'positive_log_sha256':sha(rr['log']),'negative_complete':nt,'positive_complete':pt})
  diff=''.join(difflib.unified_diff(corrected['definition_registrar'].splitlines(keepends=True),texts['definition_registrar'].splitlines(keepends=True),fromfile='frozen-corrected-Registrar',tofile=name));(A/('SOURCE_EFFECT_'+name+'.diff')).write_text(diff)
 else:
  t=Path(st['all_cases']['log']).read_text();assert len(re.findall('^PASS ',t,re.M))==46 and not re.search('^FAIL ',t,re.M)and 'TOTAL 46 PASS 0 FAIL'in t;assert st['binary']['sha256']in t
assert len(effects)==378 and len(native)==12
save('SOURCE_EFFECT_CHECK.json',effects);save('NATIVE_COUNTERPROOF.json',native)
receipts=bind['actual_receipts'];allactual=[json.loads(s)for s in(E/'VERIFICATION_LOG.jsonl').read_text().splitlines()];dictionary=[];indices={};maps=[]
for q in receipts:
 assert q in allactual;raw=Path(q['log']).read_bytes();assert digest(raw)==q['log_sha256'];text=raw.decode();ids=[]
 for s in text.splitlines(keepends=True):
  if s not in indices:indices[s]=len(dictionary);dictionary.append(s)
  ids.append(indices[s])
 assert ''.join(dictionary[i]for i in ids).encode()==raw
 maps.append({'phase':q['phase'],'path':q['log'],'sha256':q['log_sha256'],'bytes':len(raw),'line_ids':ids})
 if '-build'in q['phase']and q['exit_code']==0:assert '0 Warnung(en)'in text and '0 Fehler'in text and '-warnaserror'in q['command']
assert len(receipts)==51
excluded=[q for q in receipts if q['exit_code']not in[0]and 'build'in q['phase']];assert len(excluded)==1 and excluded[0]['phase']=='package18-f035-public-previous-build'
for consumer,expected_red,expected_pass in [('previous',27,19),('corrected',0,46)]:
 q=next(q for q in receipts if q['phase']=='package18-f035-public-'+consumer+'-fortysix');t=Path(q['log']).read_text();assert len(re.findall('^FAIL ',t,re.M))==expected_red;assert len(re.findall('^PASS ',t,re.M))==expected_pass;assert 'TimeoutException'not in t
save('ACTUAL_RECEIPTS.json',receipts);save('LOG_DICTIONARY.json',dictionary);save('LOSSLESS_LOG_READ_MAP.json',maps)
packages=[]
for q in bind['selected_package_chains']:
 archive=Path(q['package']);raw=archive.read_bytes();assert digest(raw)==q['package_sha256'];lock=read(C/q['consumer']/'packages.lock.json')['dependencies']['net10.0'];a=q['loaded'];name=a['name'];entry=lock[name];hash512=base64.b64encode(hashlib.sha512(raw).digest()).decode();assert hash512==entry['contentHash']==q['locked_sha512'];cache=Path(q['cache']);root=cache.parents[2];zipcache=root/(name.lower()+'.1.0.0.nupkg');assert zipcache.read_bytes()==raw;assert(root/(name.lower()+'.1.0.0.nupkg.sha512')).read_text().strip()==hash512
 with zipfile.ZipFile(archive)as zz:
  assert '.signature.p7s'not in zz.namelist();payload=zz.read('lib/net10.0/'+name+'.dll');assert payload==cache.read_bytes()==Path(a['path']).read_bytes();assert digest(payload)==a['sha256']
 packages.append({**q,'physical_zip_sha512':hash512,'cache_nupkg':str(zipcache),'selected_lock_cache_actual_payload_equal':True})
assert len(packages)==4;save('PACKAGE_BINDING.json',packages)
ms=[]
for q in bind['actual_Microsoft_runtime']:
 cache=Path(q['cache']);root=cache.parents[2];name=q['name'];version=q['package_version'];lock=read(C/q['consumer']/'packages.lock.json')['dependencies']['net10.0'];entry=lock[name];assert version==entry['resolved']=='10.0.12';archive=root/(name.lower()+'.'+version+'.nupkg');raw=archive.read_bytes();physical=base64.b64encode(hashlib.sha512(raw).digest()).decode();side=root/(name.lower()+'.'+version+'.nupkg.sha512');assert side.read_text().strip()==physical;metadata=read(root/'.nupkg.metadata');assert metadata['contentHash']==entry['contentHash'];assert sha(cache)==sha(q['path'])==q['sha256']
 with zipfile.ZipFile(archive)as zz:
  assert '.signature.p7s'in zz.namelist();assert zz.read('lib/net10.0/'+name+'.dll')==cache.read_bytes()
 ms.append({**q,'archive':str(archive),'archive_sha256':digest(raw),'physical_zip_sha512':physical,'lock_signature_independent_contentHash':entry['contentHash'],'metadata_contentHash':metadata['contentHash'],'physical_zip_sha512_matches_cache_sidecar':True,'signature_entry_present_no_signature_crypto_validation':True,'payload_equal_to_actual_loaded':True})
assert len(ms)==20;save('MICROSOFT_RUNTIME_BINDING.json',ms)
for p,h in {**mut['oracle_hashes'],**mut['constant_dependencies']}.items():assert sha(p)==h
rows=list(csv.DictReader((R/'FILE_COVERAGE.csv').open()));assert len(rows)==6209
for q in rows:assert sha(F/q['path'])==sha(M/q['path'])==q['sha256']
new=[rel for rel in frame.values()if not(F/rel).exists()];assert new==['src/ViciOne.ServiceBus/Configuration/ConsumerBusIdentityOptions.cs'];assert not(M/new[0]).exists()
maincore=C/'corrected/bin/Release/net10.0/ViciOne.ServiceBus.dll';assert sha(maincore)=='29a0a2fb6462eb68d4fe781682db0750f689d0280f106b4c5097fe8bd34a3c90'
save('RESTORE_CHECK.json',{'originals6209_F_M_current_ledger_equal':True,'current_ledger_sha256':sha(R/'FILE_COVERAGE.csv'),'exact_new_frame_paths':new,'exact_new_frame_paths_removed':True,'all_public_oracle_and_constant_files_sha_unchanged':True,'restored_runtime_core':str(maincore),'restored_runtime_core_sha256':sha(maincore)})
# Readonly original Git byte binding for every original member of the bounded frame.
head=read(R/'FROZEN_COPY_RECEIPT.json')['head'];git=[]
for rel in frame.values():
 if not(F/rel).exists():continue
 b=subprocess.run(['git','show',head+':'+rel],cwd=O,check=True,capture_output=True).stdout;assert b==(F/rel).read_bytes();git.append({'head':head,'relative':rel,'sha256':digest(b)})
assert len(git)==26;save('ORIGINAL_GIT_BINDING.json',git)
# Own FULL audited parser only. No parent binder execution, no .NET or process loading.
parser=E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py';assert sha(parser)=='26527e1c6caadb47808dd705098a4da6690d37bbe46770b5ab4950c5e2d1bc78';sp=importlib.util.spec_from_file_location('own_pdb_parser',parser);pm=importlib.util.module_from_spec(sp);sp.loader.exec_module(pm)
pdbs=[];checks=[]
def bindpdb(label,path,texts=None,consumer=None):
 p=pm.document_checksums(path);p['stage']=label;pdbs.append(p);match=0;generated=0;members=[]
 for d in p['documents']:
  doc=d['path'];b=None;basis=None
  if '/src/'in doc:
   rel='src/'+doc.split('/src/',1)[1];key=next((k for k,v in frame.items()if v==rel),None)
   if key is not None:
    b=texts[key].encode()if texts else(F/rel).read_bytes();basis='bounded_frame_stage_source'
   elif(F/rel).exists():b=(F/rel).read_bytes();basis='original_frozen_nonframe_document_hash_binding_not_FULL_semantic_read'
   elif '/obj/'in rel or '/artifacts/'in rel:generated+=1;continue
   else:raise AssertionError((label,doc,'missing original'))
  elif consumer is not None and Path(doc).name in ['Cases.cs','RuntimeOwner.cs','OwnedLifetime.cs','Resources.cs','Probe.cs','Program.cs']:
   b=(C/consumer/Path(doc).name).read_bytes();basis='current_whole_publicowner'
  else:generated+=1;continue
  assert d['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16';assert digest(b)==d['checksum'],(label,doc,digest(b),d['checksum']);match+=1;members.append({'path':doc,'sha256':d['checksum'],'basis':basis})
 checks.append({'stage':label,'dll':str(path),'dll_sha256':p['dll_sha256'],'pdb_kind':p['pdb_kind'],'pdb_sha256':p['pdb_sha256'],'matching_source_docs':match,'generated_or_other_docs_not_semantically_claimed':generated,'members':members})
for st in mut['controls']+mut['mutants']:bindpdb('M-'+st['name'],st['binary']['path'],ns['mutation'](st['name'])if st in mut['mutants']else corrected)
originaltexts=dict(corrected);originaltexts['definition_registrar']=(F/frame['definition_registrar']).read_text()
for consumer in ['previous','corrected']:
 t=originaltexts if consumer=='previous'else corrected
 for name in ['ViciOne.ServiceBus','ViciOne.ServiceBus.Abstractions']:bindpdb(consumer+'-'+name,C/consumer/('bin/Release/net10.0/'+name+'.dll'),t)
 bindpdb(consumer+'-public-app',C/consumer/'bin/Release/net10.0/DefinitionLifetime.PublicConsumer.dll',consumer=consumer)
assert len(pdbs)==20
save('PDB_DOCUMENTS.json',pdbs);save('PDB_SOURCE_BINDING.json',checks)
# Preserve only byte-equal whole-file prior personal reads; peer/root claims remain navigation only.
save('READ_CLOSURE.json',{'role':'internal independent readonly adversarial evidence/code review','freeze':str(freeze),'freeze_sha256':sha(freeze),'owned_inputs':owned,'owned_full_total':48,'no_foreign_FULL_transfer':True,'additional_public_SPI_own_closure':str(whole_path),'additional_public_SPI_own_closure_sha256':sha(whole_path),'own_original_and_sdk_qualification_closure':str(source_path),'own_original_and_sdk_qualification_closure_sha256':sha(source_path),'raw_evidence':{'actual_receipts_personally_FULL':51,'all_raw_logs_personally_FULL_lossless':51,'unique_complete_line_dictionary':len(dictionary),'ordered_lossless_sha_byte_reconstruction':'LOSSLESS_LOG_READ_MAP.json','method':'Every complete unique line and complete ordered map personally read; duplicates exactly byte identical.'},'source_effects':378,'PE_PDB_images':20,'original_git_bounded_paths':26,'native_operations_by_reviewer':0,'canonicalCore766_FULL':False,'whole_API_CLOSED':False})
print(json.dumps({'owned':len(owned),'effects':len(effects),'receipts':len(receipts),'unique_log_lines':len(dictionary),'packages':len(packages),'MS':len(ms),'PDBimages':len(pdbs),'source_docs':sum(q['matching_source_docs']for q in checks),'generated_unclaimed':sum(q['generated_or_other_docs_not_semantically_claimed']for q in checks),'originals_restored':len(rows)},indent=2))

"""Supplemental pure static linkage checker. No product writes or runtime execution."""
import json,re,hashlib,difflib
from pathlib import Path
W=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0');O=W/'repositories/vicione-servicebus';E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';A=E/'package18-f035-adversarial';R=Path('/private/tmp/vicione-servicebus-api-review-20261001');F=R/'frozen-repository'
def read(p):return json.loads(Path(p).read_text())
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
b=read(E/'PACKAGE18_F035_ROOT_BINDING.json');m=read(E/'PACKAGE18_F035_MUTATIONS.json');receipts=read(A/'ACTUAL_RECEIPTS.json');runtime=[]
baseMS={q['consumer']:{z['name']:z for z in b['actual_Microsoft_runtime']if z['consumer']==q['consumer']}for q in b['actual_Microsoft_runtime']}
for q in receipts:
 text=Path(q['log']).read_text();loaded=[{'name':a,'version':v,'path':p,'sha256':h}for a,v,p,h in re.findall(r'^LOADED (\S+) (\S+) (.*?) ([0-9a-f]{64})$',text,re.M)]
 if not loaded:continue
 mode=q['command'][-2];consumer='previous'if'-previous-'in q['phase']else'corrected';chunks=[loaded[i:i+12]for i in range(0,len(loaded),12)];assert all(len(c)==12 and len({z['name']for z in c})==12 for c in chunks);assert len(chunks)==(46 if mode=='all'else 1)
 for chunk in chunks:
  byname={z['name']:z for z in chunk}
  for n,z in baseMS[consumer].items():assert byname[n]=={k:z[k]for k in ['name','version','path','sha256']}
  abs1=next(z for z in b['selected_package_chains']if z['consumer']==consumer and z['loaded']['name']=='ViciOne.ServiceBus.Abstractions')['loaded'];assert byname[abs1['name']]==abs1
  if q['phase'].startswith('package18-f035-mut-'):
   st=next(st for st in m['controls']+m['mutants']if q in [st.get('negative'),st.get('positive'),st.get('all_cases')]);expected=st['binary']['sha256']
  else:expected=next(z for z in b['selected_package_chains']if z['consumer']==consumer and z['loaded']['name']=='ViciOne.ServiceBus')['loaded']['sha256']
  assert byname['ViciOne.ServiceBus']['sha256']==expected
  assert byname['ViciOne.ServiceBus']['path']==str(R/f'repair-consumers/package18-f035/{consumer}/bin/Release/net10.0/ViciOne.ServiceBus.dll')
 runtime.append({'phase':q['phase'],'log_sha256':sha(q['log']),'actual_complete_loaded_blocks':len(chunks),'core_sha256':expected,'all_ten_actual_MS_and_Abs_exact_locked_cache_chain':True,'chunks':chunks})
assert len(runtime)==28 and sum(z['actual_complete_loaded_blocks']for z in runtime)==208
pdbs=read(A/'PDB_DOCUMENTS.json')
def docs(p):return{z['path'].split('/src/',1)[1]:z['checksum']for z in p['documents']if'/src/'in z['path']}
base=docs(next(z for z in pdbs if z['stage']=='M-corrected'));diffs=[]
for p in pdbs:
 if p['stage'].endswith('public-app')or p['stage'].endswith('Abstractions'):continue
 dm=docs(p);assert dm.keys()==base.keys();changed=[k for k in dm if dm[k]!=base[k]]
 expected=[]if p['stage']in ['M-corrected','M-rollback','corrected-ViciOne.ServiceBus']else['ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionContainerRegistrar.cs'];assert changed==expected,(p['stage'],changed)
 diffs.append({'stage':p['stage'],'dll_sha256':p['dll_sha256'],'changed_product_document_checksums_vs_corrected':changed})
registrar='src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionContainerRegistrar.cs';original=(F/registrar).read_text();current=(O/registrar).read_text();(A/'REGISTRAR_PRODUCT.diff').write_text(''.join(difflib.unified_diff(original.splitlines(keepends=True),current.splitlines(keepends=True),fromfile='Frozen-Git-original',tofile='Package18-corrected')))
for n,v in [('ACTUAL_RUNTIME_LOG_BINDING.json',runtime),('PDB_SINGLE_SOURCE_EFFECTS.json',diffs)]:
 p=A/n;assert not p.exists();p.write_text(json.dumps(v,indent=2)+'\n')
print(json.dumps({'runtime_case_operations':len(runtime),'actual_loaded_blocks':sum(z['actual_complete_loaded_blocks']for z in runtime),'PDB_single_source_effect_compares':len(diffs),'current_registrar_sha256':sha(O/registrar)},indent=2))

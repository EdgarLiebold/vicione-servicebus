"""Independent readonly file/source/PE-PDB/package/receipt audit. Dossier writes only; no native or Root runner execution."""
from pathlib import Path
import hashlib,json,csv,base64,zipfile,difflib,re,importlib.util,sys,subprocess
sys.dont_write_bytecode=True
O=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';B=E/'package17-f029-adversarial';R=Path('/private/tmp/vicione-servicebus-api-review-20261001');F=R/'frozen-repository';C=R/'repair-consumers/package17-f029'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def save(n,j): (B/n).write_text(json.dumps(j,indent=2)+'\n')
fpath=E/'PACKAGE17_F029_FREEZE.json';assert H(fpath)=='56c06d9e5de08732798bf5eca6db7cae442dfc96545ad8e1c37176fb97c0edf1';fr=json.loads(fpath.read_text());rt=json.loads(Path(fr['root_binding']['path']).read_text());mu=json.loads(Path(fr['mutations']['path']).read_text())
for x in fr['owned_inputs']:
 for k in ['path','snapshot']:assert H(x[k])==x['sha256'] and Path(x[k]).stat().st_size==x['bytes']
for x in [fr['root_binding'],fr['mutations'],*fr['plans_and_exclusions']]:assert H(x['path'])==x['sha256']
save('FREEZE_HASH_CHECK.json',fr)
paths=mu['frame'];base={k:Path(next(x['snapshot'] for x in fr['owned_inputs'] if x['path']==str(O/v))).read_text() for k,v in paths.items()}
changes={
'RestoreUtcChronologyGuard':('batch_snapshot','        for (var index = 0;','        if (lastMessageReceived < firstMessageReceived)\n            throw new ArgumentOutOfRangeException(nameof(lastMessageReceived), lastMessageReceived, "The last-message timestamp must not precede the first-message timestamp.");\n        for (var index = 0;'),
'ClampRawLastUtc':('batch_snapshot','LastMessageReceived = lastMessageReceived;','LastMessageReceived = lastMessageReceived < firstMessageReceived ? firstMessageReceived : lastMessageReceived;'),
'OverwriteRawFirstUtc':('batch_snapshot','FirstMessageReceived = firstMessageReceived;','FirstMessageReceived = lastMessageReceived;')}
stages=mu['controls']+mu['mutants'];effects=[];expected={}
for st in stages:
 texts=dict(base)
 if st['name'] in changes:
  k,a,b=changes[st['name']];assert texts[k].count(a)==1;texts[k]=texts[k].replace(a,b,1)
 expected[st['name']]=texts
 assert H(st['binary']['path'])==st['binary']['sha256']
 ds=[]
 for x in st['source_snapshots']:
  p=Path(x['path']);assert p.read_bytes()==texts[x['key']].encode();assert H(p)==x['sha256']==x['current_source_sha256'];effects.append(dict(stage=st['name'],key=x['key'],relative=x['relative'],snapshot=str(p),sha256=H(p),changed=texts[x['key']]!=base[x['key']]))
  if texts[x['key']]!=base[x['key']]:ds.extend(difflib.unified_diff(base[x['key']].splitlines(True),texts[x['key']].splitlines(True),fromfile=x['relative'],tofile=st['name']))
 if ds:(B/('SOURCE_EFFECT_'+st['name']+'.diff')).write_text(''.join(ds))
assert len(effects)==130 and sum(x['changed'] for x in effects)==3
assert effects==rt['source_effect_checks'] or all(next(y for y in rt['source_effect_checks'] if y['stage']==x['stage'] and y['key']==x['key'])['sha256']==x['sha256'] for x in effects)
save('SOURCE_EFFECT_CHECK.json',effects)
# Exact actual receipt membership and line-lossless dictionary; all distinct text personally read in two bounded sections.
allrec=[json.loads(x) for x in (E/'VERIFICATION_LOG.jsonl').read_text().splitlines() if x.strip()];dic=[];idx={};encoded=[];reads=[]
for x in rt['actual_receipts']:
 assert x in allrec and H(x['log'])==x['log_sha256'];data=Path(x['log']).read_text();lines=data.splitlines(keepends=True);ids=[]
 for l in lines:
  if l not in idx:idx[l]=len(dic);dic.append(l)
  ids.append(idx[l])
 assert ''.join(dic[i] for i in ids)==data
 encoded.append(dict(phase=x['phase'],line_ids=ids,log_sha256=x['log_sha256']))
 reads.append(dict(**x,pass_modes=re.findall(r'^PASS (.+)$',data,re.M),failure_headers=re.findall(r'^FAIL (.+)$',data,re.M),guard_or_cleanup_fail=('TimeoutException' in data or 'CASE AND OWNED CLEANUP FAILED' in data)))
 if 'build' in x['command'] and x['exit_code']==0:assert '0 Warnung(en)' in data and '0 Fehler' in data
assert len(reads)==32
save('ACTUAL_RECEIPTS.json',reads);save('LOG_DICTIONARY.json',dic);save('LOSSLESS_LOG_READ_MAP.json',encoded)
get=lambda ph:next(x for x in reads if x['phase']==ph)
prev=get('package17-f029-public-previous-twentysix-r2');corr=get('package17-f029-public-corrected-twentysix-r4')
assert prev['exit_code']==1 and len(prev['failure_headers'])==7 and len(prev['pass_modes'])==19 and not prev['guard_or_cleanup_fail']
assert corr['exit_code']==0 and len(corr['pass_modes'])==26 and not corr['failure_headers']
assert all('Xunit.Sdk.NullException' in x for x in prev['failure_headers'])
counter=[]
for st in stages:
 if st['name'] in ['corrected','rollback']:
  q=get(st['all_cases']['phase']);assert q['exit_code']==0 and len(q['pass_modes'])==26 and not q['failure_headers'];proof=[q]
 else:
  neg=get(st['negative']['phase']);pos=get(st['positive']['phase']);assert neg['exit_code']==1 and len(neg['failure_headers'])==1 and not neg['guard_or_cleanup_fail'];assert pos['exit_code']==0 and len(pos['pass_modes'])==1 and not pos['failure_headers'];proof=[neg,pos]
 for q in proof:
  loads=re.findall(r'^LOADED ViciOne.ServiceBus 1\.0\.0\.0 (\S+) ([0-9a-f]{64})$',Path(q['log']).read_text(),re.M);assert loads and all(d==st['binary']['sha256'] for p,d in loads)
 counter.append(dict(stage=st['name'],binary=st['binary'],proof_phases=[q['phase'] for q in proof],failure_headers=[a for q in proof for a in q['failure_headers']]))
save('NATIVE_COUNTERPROOF.json',counter)
chains=[];microsoft=[]
for ch in rt['selected_package_chains']:
 mode=ch['consumer'];cp=C/mode;pkg=Path(ch['package']);assert H(pkg)==ch['package_sha256'];sha512=base64.b64encode(hashlib.sha512(pkg.read_bytes()).digest()).decode();name=ch['loaded']['name'];lock=json.loads((cp/'packages.lock.json').read_text())['dependencies']['net10.0'];assert sha512==lock[name]['contentHash']==ch['locked_sha512'];folder=cp/'restored-packages'/name.lower()/'1.0.0';assert H(folder/pkg.name.lower())==H(pkg);assert (folder/(pkg.name.lower()+'.sha512')).read_text().strip()==sha512
 with zipfile.ZipFile(pkg) as z:raw=z.read('lib/net10.0/'+name+'.dll')
 assert hashlib.sha256(raw).hexdigest()==H(ch['cache'])==H(ch['loaded']['path'])==ch['loaded']['sha256'];chains.append(dict(**ch,independently_verified=True))
for x in rt['actual_Microsoft_runtime']:
 assert H(x['cache'])==H(x['path'])==x['sha256'];lock=json.loads((C/x['consumer']/'packages.lock.json').read_text())['dependencies']['net10.0'];assert lock[x['name']]['resolved']=='10.0.12';microsoft.append(x)
assert len(chains)==4 and len(microsoft)==20
save('PACKAGE_BINDING.json',chains);save('MICROSOFT_RUNTIME_BINDING.json',microsoft)
# This inspected helper has no native or mutation side effects. Only reads PE/debug/portablePDB byte structures.
helper=E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py';assert H(helper)=='26527e1c6caadb47808dd705098a4da6690d37bbe46770b5ab4950c5e2d1bc78';sp=importlib.util.spec_from_file_location('pdb_ro',helper);pdb=importlib.util.module_from_spec(sp);sp.loader.exec_module(pdb)
checks=[];generated=[];docs=[];frame_rel={v:k for k,v in paths.items()}
def bind(label,p,kind,texts=None):
 d=pdb.document_checksums(p);docs.append(dict(label=label,**d))
 for a in d['documents']:
  assert a['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16'
  if kind=='app':
   actual=Path(a['path']);assert actual.exists() and H(actual)==a['checksum'];checks.append(dict(label=label,source=str(actual),source_sha256=H(actual),**a));continue
  if '/src/' not in a['path']:generated.append(dict(label=label,**a));continue
  rel='src/'+a['path'].split('/src/',1)[1]
  if kind=='mutant' and rel in frame_rel:
   raw=texts[frame_rel[rel]].encode();source=next(x['snapshot'] for x in effects if x['stage']==label and x['relative']==rel)
  else:
   p0=F/rel
   if p0.exists() and H(p0)==a['checksum']:raw=p0.read_bytes();source=str(p0)
   else:
    x=next(x for x in fr['owned_inputs'][:26] if x['path']==str(O/rel));raw=Path(x['snapshot']).read_bytes();source=x['snapshot']
  assert hashlib.sha256(raw).hexdigest()==a['checksum'];checks.append(dict(label=label,relative=rel,source=source,source_sha256=hashlib.sha256(raw).hexdigest(),**a))
for st in stages:bind(st['name'],st['binary']['path'],'mutant',expected[st['name']])
for ch in chains:bind('package-'+ch['consumer']+'-'+ch['loaded']['name'],ch['loaded']['path'],'package')
for mode in ['previous','corrected']:bind('app-'+mode,C/mode/'bin/Release/net10.0/BatchClock.PublicConsumer.dll','app')
save('PDB_DOCUMENTS.json',docs);save('PDB_SOURCE_BINDING.json',dict(matched=len(checks),mismatches=[],checks=checks,generated_unclaimed=generated))
ledger=list(csv.DictReader((R/'FILE_COVERAGE.csv').open()));assert len(ledger)==6209;bad=[]
for x in ledger:
 assert H(F/x['path'])==x['sha256'];
 if H(R/'mutations/source-main'/x['path'])!=x['sha256']:bad.append(x['path'])
assert not bad;new=[v for v in paths.values() if not (F/v).exists()];assert new==[paths["identity"]] and all(not (R/'mutations/source-main'/v).exists() for v in new)
for p,d in {**mu['oracle_hashes'],**mu['constant_dependencies']}.items():assert H(p)==d
save('RESTORE_CHECK.json',dict(originals=6209,mismatches=bad,removed_new_framefiles=new,oracle_runtime_constants_restored=True,ledger_sha256=H(R/'FILE_COVERAGE.csv')))
# Git is read-only; independently bind the two originals to exact recorded Frozen head.
head=json.loads((R/'FROZEN_COPY_RECEIPT.json').read_text())['head'];gitchecks=[]
for rel in [paths['batch_snapshot'],paths['batch_contract']]:
 q=subprocess.run(['git','show',head+':'+rel],cwd=O,stdout=subprocess.PIPE,stderr=subprocess.PIPE,check=True);assert q.stdout==(F/rel).read_bytes();gitchecks.append(dict(head=head,relative=rel,sha256=hashlib.sha256(q.stdout).hexdigest()))
save('ORIGINAL_GIT_BINDING.json',gitchecks)
print(json.dumps(dict(freeze_owned=47,source_effects=130,single_effects=3,receipts=32,package_chains=4,microsoft=20,product_and_app_pdb_images=len(docs),matched_document_sources=len(checks),generated_unclaimed=len(generated),original_restoration=6209)))

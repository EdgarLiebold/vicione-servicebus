"""Static independent log-to-binary/consumer source verifier. No native operations."""
import hashlib, importlib.util, json, sys
from pathlib import Path
sys.dont_write_bytecode=True
O=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';D=E/'package11-f004-adversarial'
R=Path('/private/tmp/vicione-servicebus-api-review-20261001');C=R/'repair-consumers/package11-f004'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
f=json.loads((E/'PACKAGE11_F004_FREEZE.json').read_text());m=json.loads(Path(f['mutations']['path']).read_text());bound=json.loads((D/'RECEIPT_BINDING.json').read_text())
expect={
 'process-meter-selected':('SameException','JournalEvidence.cs:line 63'),
 'factory-failure-global-fallback':('EmptyException','JournalCases.cs:line 81'),
 'borrowed-factory-meter-disposed':('EqualException','JournalCases.cs:line 143'),
 'owned-source-disposal-omitted':('NullException','JournalCases.cs:line 131'),
 'registration-caches-first-telemetry':('SameException','JournalEvidence.cs:line 63'),
 'di-writer-forgets-owner':('SameException','JournalEvidence.cs:line 63'),
 'shared-transport-graph-restored':('ConfigurationException','BaseHostConfiguration.cs:line 149')}
rows=[]
def actual(receipt,frame,expected_count,expected_failures):
 assert receipt in bound and sha(receipt['log'])==receipt['log_sha256']
 a=json.loads(Path(receipt['log']).read_text());assert a['count']==expected_count and len(a['cases'])==expected_count and a['failures']==expected_failures
 assert sum(not c['assertions_passed'] for c in a['cases'])==expected_failures
 assert all(not c.get('bounded_guard_failure',False) for c in a['cases'])
 for b in frame['binaries']:
  name=Path(b['path']).stem;loaded=next(x for x in a['runtime']['loadedProductAssemblies'] if x['name']==name)
  assert loaded['sha256']==b['sha256']==sha(b['path'])
  assert loaded['path']==str(C/'corrected-r2/bin/Release/net10.0'/Path(b['path']).name)
 return a
for x in m['mutants']:
 n=x['name'];a=actual(x['rejection']['receipt'],x['binary'],1,1);assert a==x['rejection']['actual']
 case=a['cases'][0];typ,line=expect[n];assert case['exception_type'].endswith(typ) and line in case['StackTrace']
 assert x['rejection']['receipt']['exit_code']==1
 if n=='shared-transport-graph-restored':assert 'Payload admission is already configured' in case['Message']
 p=actual(x['positive_control']['receipt'],x['binary'],1,0);assert p==x['positive_control']['actual'] and x['positive_control']['receipt']['exit_code']==0
 build=x['binary']['receipt'];assert build['exit_code']==0 and build in bound and sha(build['log'])==build['log_sha256'];text=Path(build['log']).read_text();assert ('0 Warning(s)' in text or '0 Warnung(en)' in text) and ('0 Error(s)' in text or '0 Fehler' in text)
 rows.append({'variant':n,'scope':'SEPARATE_GRAPH_EXCLUDED_FROM_F004' if n.startswith('shared-') else 'F004','rejection':x['rejection']['receipt'],'positive':x['positive_control']['receipt'],'expected_exception':typ,'expected_stack_location':line,'exact_native_assertion_not_guard':True,'both_loaded_dlls_match_same_compiled_variant':True})
actual(m['controls'][0]['receipt'],m['corrected_binary'],16,0);actual(m['rollback']['receipt'],m['rollback_binary'],16,0)
phases={x['phase']:x for x in bound};original=json.loads(Path(phases['package11-f004-previous-sixteen']['log']).read_text())
assert original['count']==16 and original['failures']==12
scope_red=[x for x in original['cases'] if not x['assertions_passed'] and x['exception_type'].endswith('SameException')]
graph_red=[x for x in original['cases'] if not x['assertions_passed'] and x['exception_type'].endswith('ConfigurationException')]
controls=[x['case'] for x in original['cases'] if x['assertions_passed']]
assert len(scope_red)==11 and all('Actual:   null' in x['Message'] and 'JournalEvidence.cs:line 63' in x['StackTrace'] for x in scope_red)
assert len(graph_red)==1 and graph_red[0]['case']=='two-roots-same-service-collection' and 'Payload admission is already configured' in graph_red[0]['Message']
assert set(controls)=={'nondi-all','nondi-outgoing','nondi-consume','default-off'}
assert all(not x.get('bounded_guard_failure',False) for x in original['cases'])
first=json.loads(Path(phases['package11-f004-corrected-sixteen']['log']).read_text());assert first['count']==16 and first['failures']==1 and next(x for x in first['cases'] if not x['assertions_passed'])['case']=='two-roots-same-service-collection'
final=json.loads(Path(phases['package11-f004-corrected-r2-sixteen']['log']).read_text());assert final['count']==16 and final['failures']==0
(D/'COUNTERPROOF_BINDING.json').write_text(json.dumps({'F004_valid_compiled_mutants':6,'separate_graph_reversion':1,'paired_controls':7,'baseline':{'count':16,'causal_F004_scope_null':11,'positive_cases':controls,'excluded_graph_construction':graph_red[0]['case']},'first_F004_corrected':{'count':16,'passed':15,'excluded_graph_construction':1},'combined_final_corrected':{'count':16,'passed':16},'mutation_corrected_and_rollback':{'each_count':16,'each_passed':16},'mutants':rows},indent=2)+'\n')
helper=E/'package04-f018-canonical-fixture-adversarial/verify_pdb.py';assert sha(helper)=='26527e1c6caadb47808dd705098a4da6690d37bbe46770b5ab4950c5e2d1bc78'
spec=importlib.util.spec_from_file_location('readonly_pdb',helper);pdb=importlib.util.module_from_spec(spec);spec.loader.exec_module(pdb)
cs={x['path'] for x in f['owned_inputs'] if x['path'].endswith('.cs')};pdbrows=[]
for mode in ['previous','corrected','corrected-r2']:
 cp=C/mode;b=pdb.document_checksums(cp/'bin/Release/net10.0/JournalOwnership.PublicConsumer.dll');source_rows=[];generated=[]
 for doc in b['documents']:
  p=Path(doc['path'])
  if p.name in ['JournalApplication.cs','JournalCases.cs','JournalEvidence.cs','OwnedLifetime.cs','Program.cs']:
   assert str(p) in cs and doc['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16' and sha(p)==doc['checksum'];source_rows.append(doc)
  else:generated.append(doc)
 assert len(source_rows)==5
 pdbrows.append({**b,'personal_FULL_sources_sha256_bound':source_rows,'generated_documents_not_semantic_claim':generated})
(D/'CONSUMER_PDB_BINDING.json').write_text(json.dumps(pdbrows,indent=2)+'\n')
print(json.dumps({'mutants_F004':6,'graph':1,'same_binary_controls':7,'consumer_PDB':3,'consumer_FULL_source_documents':15}))

"""Reads document checksums only; source baselines follow each actual compile route."""
import ast
import hashlib
import json
from pathlib import Path
O=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';D=E/'package11-f004-adversarial'
R=Path('/private/tmp/vicione-servicebus-api-review-20261001');F=R/'frozen-repository'
freeze=json.loads((E/'PACKAGE11_F004_FREEZE.json').read_text())
effects=json.loads((D/'SOURCE_EFFECT_CHECK.json').read_text())
documents=json.loads((D/'PDB_DOCUMENTS.json').read_text())
f004={x['path'].split(str(O)+'/')[1] for x in freeze['owned_inputs'][9:13]}
rows=[];generated=[];mismatches=[]
for b in documents:
 for doc in b['documents']:
  if '/src/' not in doc['path']:
   generated.append({'variant':b['variant'],**doc});continue
  rel='src/'+doc['path'].split('/src/',1)[1]
  variant=b['variant']
  if variant.startswith('package-'):
   stage='previous' if variant.startswith('package-previous-') else 'corrected-r2' if variant.startswith('package-corrected-r2-') else 'corrected'
   frozen=next((x for x in freeze['owned_inputs'][:14] if x['path']==str(O/rel)),None)
   original=F/rel
   if original.exists() and hashlib.sha256(original.read_bytes()).hexdigest()==doc['checksum']:
    baseline=original
   elif frozen and frozen['sha256']==doc['checksum']:
    baseline=Path(frozen['snapshot'])
   else:
    support=json.loads((D/'SELECTED_SUPPORT_SOURCES.json').read_text())[rel]
    assert support['sha256']==doc['checksum']
    baseline=Path(support['snapshot'])
   data=baseline.read_bytes();source=str(baseline)
  else:
   effect=next((x for x in effects if x['variant']==variant and x['source']==rel),None)
   baseline=Path(effect['snapshot']['path']) if effect else F/rel
   data=baseline.read_bytes();source=str(baseline)
  assert doc['algorithm_guid']=='8829d00f-11b8-4213-878b-770e8597ac16'
  digest=hashlib.sha256(data).hexdigest()
  row={'variant':variant,'source':source,'relative':rel,'expected_source_sha256':digest,**doc}
  rows.append(row)
  if digest!=doc['checksum']:mismatches.append(row)
(D/'PDB_SOURCE_BINDING.json').write_text(json.dumps({'unrelated_F033_current_O_delta_excluded':True,'matched_source_documents':len(rows)-len(mismatches),'mismatches':mismatches,'generated_documents_unclaimed':generated,'checks':rows},indent=2)+'\n')
print(json.dumps({'checked':len(rows),'mismatches':len(mismatches),'generated_unclaimed':len(generated),'first_mismatches':mismatches[:10]}))
assert not mismatches

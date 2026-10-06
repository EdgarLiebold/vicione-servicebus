from pathlib import Path
import json,hashlib,datetime,xml.etree.ElementTree as ET
W=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0');O=W/'repositories/vicione-servicebus';E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';R=Path('/private/tmp/vicione-servicebus-api-review-20261001');P=R/'repair-research/package20-f002-source-preflight';OUT=E/'package20-f002-adversarial/001'
sha=lambda b:hashlib.sha256(b).hexdigest()
def record(path):
 p=Path(path);b=p.read_bytes();return dict(path=str(p),sha256=sha(b),bytes=len(b))
def dump(name,obj):(OUT/name).write_text(json.dumps(obj,indent=2)+'\n')
freeze_path=E/'PACKAGE20_F002_FINAL_NATIVE_FREEZE_R1.json';assert sha(freeze_path.read_bytes())=='08578984be9546f919c2200dd966ca5f3b4ed267a796a5bebf3095c0530672cc';freeze=json.loads(freeze_path.read_text())
inputs={str(Path(x['path']).relative_to(O)):x for x in freeze['inputs']};assert set(inputs)=={'.github/workflows/native-tests.yml','docs/build.md'}
for row in inputs.values():assert record(row['path'])=={k:row[k] for k in ['path','sha256','bytes']}
reuse=json.loads((P/'001/READ_CLOSURE.json').read_text());reused=[]
for row in reuse['owned_personal_FULL']:
 assert sha(Path(row['snapshot']).read_bytes())==row['sha256'] and sha(Path(row['path']).read_bytes())==row['sha256'];reused.append(row)
plans=[]
for frame in freeze['frames']:
 assert record(frame['path'])==frame;plans.append(frame)
all_receipts=[json.loads(l) for l in (E/'VERIFICATION_LOG.jsonl').read_text().splitlines()];receipts=freeze['native_receipts'];assert len(receipts)==15 and len({r['phase'] for r in receipts})==15
project='tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj';rule='*.EveryExecutableTestProject_UsesPortableSymbolsRequiredByMtpDiscovery';other='*.EveryExecutableTestProject_IsClassifiedAsTestProject'
for receipt in receipts:
 phase=receipt['phase'];assert [x for x in all_receipts if x['phase']==phase]==[receipt];assert record(receipt['log'])['sha256']==receipt['log_sha256'];gen=phase.split('package20-f002-',1)[1].split('-',1)[0];assert receipt['cwd']==str(R/'ci-workers/package20-f002'/gen)
 suffix=phase.removeprefix('package20-f002-'+gen+'-')
 if suffix in ['unit-restore','engineering-restore','engineering-restore-only']:
  cmd=['dotnet','restore','ViciOne.ServiceBus.Tests.Unit.slnx' if suffix=='unit-restore' else 'ViciOne.ServiceBus.Engineering.slnx','--locked-mode']
 elif suffix=='unit-build':cmd=['dotnet','build','ViciOne.ServiceBus.Tests.Unit.slnx','-c','Release','--no-restore','--warnaserror' if gen=='countercase' else '-warnaserror']
 elif suffix=='unit-unfiltered':cmd=['dotnet','test','--solution','ViciOne.ServiceBus.Tests.Unit.slnx','-c','Release','--no-build','--no-restore','--results-directory','artifacts/test-results/unit','--minimum-expected-tests','4900','--max-parallel-test-modules','1']
 else:cmd=['dotnet','test','--project',project,'-c','Release','--no-build','--no-restore','--filter-method',other if suffix=='unrelated-positive' else rule,'--minimum-expected-tests','1']
 assert cmd==receipt['command'];expected_exit=2 if suffix in ['omitted-prerequisite-negative'] or gen=='previous' and suffix=='unit-unfiltered' else 0;assert receipt['exit_code']==expected_exit
 text=Path(receipt['log']).read_text()
 if suffix=='unit-build':assert '0 Warnung(en)' in text and '0 Fehler' in text
 if suffix=='unit-unfiltered':
  assert 'gesamt: 12324' in text and 'übersprungen: 0' in text
  assert ('fehlgeschlagen: 1' in text and 'erfolgreich: 12323' in text) if gen=='previous' else ('fehlgeschlagen: 0' in text and 'erfolgreich: 12324' in text)
 if expected_exit==2:assert '  Expected: "true"' in text and '  Actual:   ""' in text and 'EvaluatedBuildGraphTests.cs:67' in text and 'TimeoutException' not in text
 if suffix not in ['unit-build','unit-restore','engineering-restore','engineering-restore-only','unit-unfiltered']:
  assert 'gesamt: 1' in text and 'übersprungen: 0' in text
  assert ('fehlgeschlagen: 1' in text) if expected_exit==2 else ('erfolgreich: 1' in text and 'fehlgeschlagen: 0' in text)
ordered=sorted(receipts,key=lambda x:x['started_utc']);assert all(a['completed_utc']<b['started_utc'] for a,b in zip(ordered,ordered[1:]));dump('INDEPENDENT_15_EXACT_RECEIPT_BINDINGS.json',receipts)
im=json.loads((R/'INPUT_MANIFEST.json').read_text());originals={x['path']:x for x in im['files']};assert len(originals)==6209
workers=[]
for generation in ['previous','corrected','countercase']:
 worker=R/'ci-workers/package20-f002'/generation;rows=[];changed=[]
 for path,row in originals.items():
  b=(worker/path).read_bytes();f=(R/'frozen-repository'/path).read_bytes();assert sha(f)==row['sha256'] and len(f)==row['bytes']
  if generation=='corrected' and path in inputs:
   assert b==Path(inputs[path]['path']).read_bytes();changed.append(path)
  else:assert b==f
  rows.append({'relative':path,'sha256':sha(b),'bytes':len(b),'original_equal':b==f})
 assert sorted(changed)==(sorted(inputs) if generation=='corrected' else []);workers.append({'generation':generation,'current_source_checks':rows,'exact_changed_files':changed})
dump('INDEPENDENT_18627_CURRENT_SOURCE_BINDINGS.json',workers)
pre_path=E/'PACKAGE20_F002_COUNTERCASE_STRICT_PRE_ENGINEERING_FRAME_R1.json';post_path=E/'PACKAGE20_F002_COUNTERCASE_STRICT_POST_ENGINEERING_FRAME_R1.json';pre=json.loads(pre_path.read_text());post=json.loads(post_path.read_text());worker=Path(pre['worker']);assert worker==R/'ci-workers/package20-f002/countercase'
restore=next(x for x in receipts if x['phase']=='package20-f002-countercase-engineering-restore-only');assert post['restore_receipt']==restore and post['pre_frame_sha256']==sha(pre_path.read_bytes());assert pre['capture_completed_utc']==post['pre_capture_completed_utc']<restore['started_utc']<restore['completed_utc']<post['utc'];assert post['utc']<next(x for x in receipts if x['phase']=='package20-f002-countercase-same-rule-positive')['started_utc']
checks={}
for group in ['original_sources','built_files','pre_restore_assets']:
 rows=pre[group];assert len(rows)==len({x['relative'] for x in rows});validated=[]
 for row in rows:
  b=(worker/row['relative']).read_bytes();assert sha(b)==row['sha256'] and len(b)==row['bytes'];validated.append(row)
 checks[group]=validated
assert len(checks['original_sources'])==6209 and len(checks['built_files'])==4648 and len(checks['pre_restore_assets'])==61
assert {x['relative'] for x in checks['original_sources']}==set(originals)
for row in checks['original_sources']:assert row['sha256']==originals[row['relative']]['sha256'] and row['bytes']==originals[row['relative']]['bytes']
actual_bins={str(x.relative_to(worker)) for x in (worker/'artifacts/sdk/bin').rglob('*') if x.is_file()};assert actual_bins=={x['relative'] for x in checks['built_files']}
assets={str(x.relative_to(worker)):record(x) for x in (worker/'artifacts/sdk/obj').rglob('project.assets.json')};added=set(assets)-{x['relative'] for x in pre['pre_restore_assets']};assert len(assets)==78 and len(added)==17 and added==set(post['added_assets']) and post['existing_assets_hash_changes']==[]
checks['strict_timing']={'capture_started_utc':pre['capture_started_utc'],'capture_completed_utc':pre['capture_completed_utc'],'restore_started_utc':restore['started_utc'],'restore_completed_utc':restore['completed_utc'],'post_capture_utc':post['utc'],'time_gap_seconds':(datetime.datetime.fromisoformat(restore['started_utc'])-datetime.datetime.fromisoformat(pre['capture_completed_utc'])).total_seconds()};checks['assets_current_count']=78;checks['actual_added_asset_paths']=sorted(added);dump('INDEPENDENT_COUNTERCASE_10918_SOURCE_BIN_ASSET_CHECKS.json',checks)
dump('EXACT_OWN_PERSONAL_SOURCE_REUSE.json',{'own_complete_prior_closure':str(P/'001/READ_CLOSURE.json'),'current15source_snapshots_bytes_unchanged':reused,'limits':'Exact own prior FULL reuse only;6209/4648 frame checks are byte/hash checks, no semantic source-owner FULL claim.'})
print(json.dumps({'receipts':len(receipts),'sources':len(originals)*3,'strictFrameSources':6209,'strictFrameBuilt':4648,'assetsBefore':61,'assetsAfter':78,'strictFrameGapSeconds':checks['strict_timing']['time_gap_seconds']}))

from pathlib import Path
import json,hashlib,sys
out=Path(__file__).parent;m=json.loads((out/'READ_PROGRESS.json').read_text());n=int(sys.argv[1]);lookup={x['path']:x for x in m['files']}
for part in m['batches'][n-1]:
 d=(Path(m['root'])/part['path']).read_bytes();row=lookup[part['path']]
 assert hashlib.sha256(d).hexdigest()==row['sha256'], 'INPUT_CHANGED '+part['path']
 print('FILE',part['path'],'SHA256',row['sha256'],'BYTES',len(d))
 ls=d.decode().splitlines()
 for i in range(part['lo'],part['hi']+1): print(f'{i:4}: {ls[i-1]}')
 print('END_RANGE',part['lo'],part['hi'],'TOTAL_LINES',len(ls))

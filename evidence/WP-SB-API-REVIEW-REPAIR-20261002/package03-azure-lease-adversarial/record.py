from pathlib import Path
import json,sys
out=Path(__file__).parent;m=json.loads((out/'READ_PROGRESS.json').read_text());n=int(sys.argv[1]);assert n not in m['completed'];m['completed'].append(n)
for f in m['files']:
 relevant=[i for i,b in enumerate(m['batches'],1) if any(p['path']==f['path'] for p in b)]
 if relevant and all(i in m['completed'] for i in relevant):f['read_status']='READ_FULL_PERSONALLY_OBSERVED'
(out/'READ_PROGRESS.json').write_text(json.dumps(m,indent=2)+'\n');print({'completed_batches':len(m['completed']),'full_files':sum(x['read_status']=='READ_FULL_PERSONALLY_OBSERVED' for x in m['files']),'next_batch':next((i for i in range(1,len(m['batches'])+1) if i not in m['completed']),None)})

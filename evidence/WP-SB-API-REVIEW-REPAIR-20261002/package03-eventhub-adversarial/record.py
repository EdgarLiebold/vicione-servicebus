import json,hashlib,sys,os
from pathlib import Path
p=Path(__file__).parent;f=p/'READ_CLOSURE.json';d=json.loads(f.read_text());i=int(sys.argv[1]);x=d['files'][i];assert hashlib.sha256(Path(x['path']).read_bytes()).hexdigest()==x['sha256'];note=sys.stdin.read().strip();assert len(note)>35;x.update(status='READ_FULL',semantic_notes=note);t=p/'READ_CLOSURE.json.tmp';t.write_text(json.dumps(d,indent=2)+'\n');os.replace(t,f);print(i,Path(x['path']).name,'RECORDED_FULL')

import json,hashlib,sys
from pathlib import Path
p=Path(__file__).parent
r=json.loads((p/'READ_CLOSURE.json').read_text())['files']
i=int(sys.argv[1]);x=r[i];b=Path(x['path']).read_bytes();assert hashlib.sha256(b).hexdigest()==x['sha256'];lines=b.decode('utf-8-sig').splitlines();lo=int(sys.argv[2]) if len(sys.argv)>2 else 1;hi=int(sys.argv[3]) if len(sys.argv)>3 else len(lines)
print('FILE',i,x['path'],'SHA256',x['sha256'],'BYTES',len(b),'TOTAL_LINES',len(lines))
for n in range(lo,min(hi,len(lines))+1):print(f'{n:4}: {lines[n-1]}')
print('END_FILE',i,'SHOWN',lo,min(hi,len(lines)))

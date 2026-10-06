import pathlib,json,hashlib,sys
p=pathlib.Path(__file__).parent;j=json.loads((p/'NATIVE_RECEIPT_INDEX.json').read_text());table=[];lookup={};rows=[]
for index in map(int,sys.argv[1:]):
 x=j['rows'][index];f=pathlib.Path(x['log']);b=f.read_bytes();assert hashlib.sha256(b).hexdigest()==x['log_sha256'];ids=[]
 for line in b.decode().splitlines(keepends=True):
  if line not in lookup: lookup[line]=len(table);table.append(line)
  ids.append(lookup[line])
 rows.append((index,x,ids))
print('LOSSLESS_LINES_COMPLETE_NO_ELISION')
for n,l in enumerate(table): print('L'+str(n)+': '+json.dumps(l,ensure_ascii=False))
for index,x,ids in rows: print('FILE_COMPLETE',index,json.dumps(x,ensure_ascii=False),'ORDER',json.dumps(ids))
print('END_BATCH')

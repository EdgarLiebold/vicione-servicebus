from pathlib import Path
import hashlib,json,struct
R=Path('/private/tmp/vicione-servicebus-api-review-20261001')
OUT=Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002/package20-f002-adversarial/001')
ORIGINAL_PATHS={x['path'] for x in json.loads((R/'INPUT_MANIFEST.json').read_text())['files']}
sha=lambda b:hashlib.sha256(b).hexdigest()
def u16(b,o):return struct.unpack_from('<H',b,o)[0]
def u32(b,o):return struct.unpack_from('<I',b,o)[0]
def streams(b):
    assert b[:4]==b'BSJB'
    pos=(16+u32(b,12)+3)&~3
    count=u16(b,pos+2);pos+=4;result={}
    for _ in range(count):
        off,size=struct.unpack_from('<II',b,pos);pos+=8;end=b.index(0,pos)
        name=b[pos:end].decode();pos=(end+4)&~3
        assert name not in result and off+size<=len(b)
        result[name]=b[off:off+size]
    return result

def compressed(b,o):
    x=b[o]
    if x<128:return x,o+1
    if x<192:return ((x&63)<<8)|b[o+1],o+2
    assert x<224
    return ((x&31)<<24)|(b[o+1]<<16)|(b[o+2]<<8)|b[o+3],o+4

def blob(heap,o):
    n,start=compressed(heap,o);assert start+n<=len(heap);return heap[start:start+n]

def documents(pdb):
    s=streams(pdb);t=s['#~'];valid=struct.unpack_from('<Q',t,8)[0];pos=24;counts={}
    for bit in range(64):
        if valid>>bit&1:counts[bit]=u32(t,pos);pos+=4
    assert not any(k<48 for k in counts)
    bw=4 if t[6]&4 else 2;gw=4 if t[6]&2 else 2;rows=[]
    for _ in range(counts[48]):
        values=[]
        for width in [bw,gw,bw,gw]:values.append(int.from_bytes(t[pos:pos+width],'little'));pos+=width
        name_index,algorithm_index,hash_index,language_index=values
        encoded=blob(s['#Blob'],name_index);separator=chr(encoded[0]);index=1;parts=[]
        while index<len(encoded):handle,index=compressed(encoded,index);parts.append(blob(s['#Blob'],handle).decode())
        path=separator.join(parts)
        algorithm=s['#GUID'][(algorithm_index-1)*16:algorithm_index*16]
        digest=blob(s['#Blob'],hash_index).hex()
        assert algorithm.hex()=='0fd02988b8111342878b770e8597ac16' # SHA256 ECMA portable PDB GUID
        rows.append({'path':path,'sha256':digest})
    return s,rows

def codeview(image):
    pe=u32(image,60);assert image[pe:pe+4]==b'PE\0\0';count=u16(image,pe+6);optional=pe+24;magic=u16(image,optional);dd=optional+(112 if magic==0x20b else 96);rva,size=struct.unpack_from('<II',image,dd+6*8)
    sections=optional+u16(image,pe+20)
    def offset(rva):
        for i in range(count):
            p=sections+40*i;vsize,vaddr,rsize,raw=struct.unpack_from('<IIII',image,p+8)
            if vaddr<=rva<vaddr+max(vsize,rsize):return raw+rva-vaddr
        raise AssertionError(rva)
    start=offset(rva);found=[]
    for pos in range(start,start+size,28):
        stamp=u32(image,pos+4);kind=u32(image,pos+12);length=u32(image,pos+16);raw=u32(image,pos+24)
        if kind==2:
            data=image[raw:raw+length];assert data[:4]==b'RSDS'
            found.append({'guid_hex':data[4:20].hex(),'stamp':stamp,'age':u32(data,20),'path':data[24:].split(b'\0',1)[0].decode()})
    assert len(found)==1
    return found[0]

result=[]
for gen in ['previous','corrected','countercase']:
    worker=R/'ci-workers/package20-f002'/gen
    output=worker/'artifacts/sdk/bin/ViciOne.ServiceBus.Architecture.Tests/release'
    dll=output/'ViciOne.ServiceBus.Architecture.Tests.dll';pdb=output/'ViciOne.ServiceBus.Architecture.Tests.pdb'
    image=dll.read_bytes();raw=pdb.read_bytes();cv=codeview(image);metadata,docs=documents(raw)
    assert cv['age']==1 and metadata['#Pdb'][:16].hex()==cv['guid_hex'] and u32(metadata['#Pdb'],16)==cv['stamp']
    checks=[]
    for row in docs:
        source=Path(row['path'])
        source_equal=source.is_file() and sha(source.read_bytes())==row['sha256']
        if not source_equal:print('CURRENT_DOC_MISMATCH',gen,row['path'],row['sha256'],sha(source.read_bytes()) if source.is_file() else 'MISSING')
        try:relative=str(source.relative_to(worker))
        except ValueError:relative=None
        original=False
        if relative in ORIGINAL_PATHS:
            original=source.read_bytes()==(R/'frozen-repository'/relative).read_bytes()
            assert original and source_equal
        checks.append(dict(row,relative=relative,current_PDB_source_equal=source_equal,frozen_original_equal=original))
    targets=['tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Build/EvaluatedBuildGraphTests.cs','tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Build/MsBuildEvaluation.cs','tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/RepositoryLayout.cs']
    for target in targets:assert any(row['relative']==target and row['frozen_original_equal'] and row['current_PDB_source_equal'] for row in checks)
    deps_path=output/'ViciOne.ServiceBus.Architecture.Tests.deps.json';deps=json.loads(deps_path.read_text());assert len(deps['targets'])==1;runtime_checks=[]
    for key,record in next(iter(deps['targets'].values())).items():
        for asset in record.get('runtime',{}):
            path=output/Path(asset).name
            if path.is_file():runtime_checks.append({'dependency':key,'asset':asset,'actual_path':str(path),'sha256':sha(path.read_bytes())})
    runtime_path=output/'ViciOne.ServiceBus.Architecture.Tests.runtimeconfig.json'
    result.append({'generation':gen,'dll':str(dll),'dll_sha256':sha(image),'pdb':str(pdb),'pdb_sha256':sha(raw),'codeview':cv,'all_document_source_SHA_checks_with_explicit_generated_absences':checks,'deps_path':str(deps_path),'deps_sha256':sha(deps_path.read_bytes()),'runtimeconfig':json.loads(runtime_path.read_text()),'runtimeconfig_sha256':sha(runtime_path.read_bytes()),'physical_dependency_assets':runtime_checks,'scope':'PE/sidecar portablePDB content-ID and document SHA/source bytes, deps physical outputs; not proof of exact live loaded SDK/runtimehost.'})
(OUT/'INDEPENDENT_ARCHITECTURE_PE_PDB_DEPS_BINDING.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps([{'generation':x['generation'],'documents':len(x['all_document_source_SHA_checks_with_explicit_generated_absences']),'originalDocs':sum(y['frozen_original_equal'] for y in x['all_document_source_SHA_checks_with_explicit_generated_absences']),'physicalDeps':len(x['physical_dependency_assets']),'dllSHA':x['dll_sha256']} for x in result]))

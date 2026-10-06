"""Static PE/Portable-PDB Document checksum reader. No .NET/native operations."""
import pathlib, struct, zlib, uuid, hashlib, json, sys

def document_checksums(path):
    path=pathlib.Path(path); dll=path.read_bytes()
    pe=struct.unpack_from('<I',dll,60)[0]; count=struct.unpack_from('<H',dll,pe+6)[0]
    size=struct.unpack_from('<H',dll,pe+20)[0];opt=pe+24
    dirs=opt+(112 if struct.unpack_from('<H',dll,opt)[0]==0x20b else 96)
    rva,sz=struct.unpack_from('<II',dll,dirs+48);sects=[]
    for i in range(count):
        vs,va,rs,rp=struct.unpack_from('<IIII',dll,opt+size+i*40+8);sects.append((va,max(vs,rs),rp))
    def offset(v):
        return next(rp+v-va for va,n,rp in sects if va<=v<va+n)
    pdb=None;codeview=None;debug_stamp=None;pdb_kind='EMBEDDED_MPDB'
    for q in range(offset(rva),offset(rva)+sz,28):
        typ,n,addr,ptr=struct.unpack_from('<IIII',dll,q+12)
        if typ==17:
            x=dll[ptr:ptr+n];assert x[:4]==b'MPDB';pdb=zlib.decompress(x[8:],-15)
            assert len(pdb)==struct.unpack_from('<I',x,4)[0]
        if typ==2:
            codeview=dll[ptr:ptr+n];debug_stamp=struct.unpack_from('<I',dll,q+4)[0]
    if pdb is None:
        pdb=path.with_suffix('.pdb').read_bytes();pdb_kind='SIDECAR_PORTABLE_PDB_CODEVIEW_ID_MATCHED'
    assert pdb and pdb[:4]==b'BSJB'
    q=(16+struct.unpack_from('<I',pdb,12)[0]+3)&~3;ns=struct.unpack_from('<H',pdb,q+2)[0];q+=4;streams={}
    for _ in range(ns):
        a,n=struct.unpack_from('<II',pdb,q);q+=8;e=pdb.index(0,q);name=pdb[q:e].decode();q=(e+4)&~3;streams[name]=pdb[a:a+n]
    if pdb_kind.startswith('SIDECAR'):
        assert codeview[:4]==b'RSDS' and codeview[4:20]==streams['#Pdb'][:16]
        assert debug_stamp==struct.unpack_from('<I',streams['#Pdb'],16)[0]
    t=streams['#~'];valid=struct.unpack_from('<Q',t,8)[0];q=24;rows={}
    for i in range(64):
        if valid>>i&1:rows[i]=struct.unpack_from('<I',t,q)[0];q+=4
    assert min(rows)==48
    def compressed(b,q):
        c=b[q]
        if c<128:return c,q+1
        if c<192:return ((c&63)<<8)|b[q+1],q+2
        return ((c&31)<<24)|(b[q+1]<<16)|(b[q+2]<<8)|b[q+3],q+4
    def blob(i):
        b=streams['#Blob'];n,s=compressed(b,i);return b[s:s+n]
    fmt='<'+(('I' if t[6]&4 else 'H')+('I' if t[6]&2 else 'H'))*2;step=struct.calcsize(fmt);docs=[]
    for i in range(rows[48]):
        ni,ai,hi,li=struct.unpack_from(fmt,t,q+i*step);name=blob(ni);sep=chr(name[0]);j=1;parts=[]
        while j<len(name):
            k,j=compressed(name,j);parts.append(blob(k).decode())
        docs.append(dict(path=sep.join(parts),algorithm_guid=str(uuid.UUID(bytes_le=streams['#GUID'][(ai-1)*16:ai*16])),checksum=blob(hi).hex()))
    return dict(dll=str(path),dll_sha256=hashlib.sha256(dll).hexdigest(),pdb_kind=pdb_kind,pdb_sha256=hashlib.sha256(pdb).hexdigest(),documents=docs)

if __name__=='__main__':
    print(json.dumps(document_checksums(sys.argv[1]),indent=2))

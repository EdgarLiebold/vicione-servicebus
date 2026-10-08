"""Capture stable retained evidence; never changes any captured input."""
from pathlib import Path, PurePosixPath
import hashlib, json, os, stat, tarfile

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
REVIEW = Path('/private/tmp/vicione-servicebus-local-completion-reviews-20261007')
ARCHIVE = Path('/private/tmp/vicione-servicebus-remote-checkpoint-20261008.tar.gz')
OLD = REPO / 'evidence/WP-SB-API-REVIEW-REPAIR-20261002/remote-backup-20261006'
def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for part in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(part)
    return h.hexdigest()
def safe(name):
    p = PurePosixPath(name)
    assert name and not p.is_absolute() and '\\' not in name
    assert all(x not in ('', '.', '..') for x in name.split('/'))
    assert not any(ord(c) < 32 for c in name)
    return name
assert not ARCHIVE.exists()
old_manifest = json.loads((OLD / 'ARCHIVE_INPUTS.json').read_text())
old_by = {row['path']: row for row in old_manifest['files']}
old_equal, old_new = [], []
sources = []
for path in sorted((REPO / 'evidence/SERVICEBUS_API_REVIEW_AND_REPAIR').rglob('*')):
    assert not path.is_symlink(), str(path)
    if not path.is_file():
        continue
    rel = str(path.relative_to(REPO / 'evidence'))
    h, size = sha(path), path.stat().st_size
    prior = old_by.get(rel)
    if prior and prior['sha256'] == h and prior['bytes'] == size:
        old_equal.append(dict(path=str(path.relative_to(REPO)), sha256=h, bytes=size,
                              prior_archive_member=rel))
    else:
        old_new.append(str(path.relative_to(REPO)))
        sources.append((path, 'repositories/vicione-servicebus/' + str(path.relative_to(REPO))))
for root, prefix in ((REPO / 'evidence/WP-SB-LOCAL-COMPLETION-20261007',
                      'repositories/vicione-servicebus/evidence/WP-SB-LOCAL-COMPLETION-20261007'),
                     (REVIEW, 'independent-review')):
    for path in sorted(root.rglob('*')):
        assert not path.is_symlink(), str(path)
        if path.is_file():
            sources.append((path, prefix + '/' + str(path.relative_to(root))))
rows = []
for path, name in sources:
    assert stat.S_ISREG(path.stat().st_mode)
    rows.append(dict(path=safe(name), source=str(path), bytes=path.stat().st_size,
                     sha256=sha(path), mode=stat.S_IMODE(path.stat().st_mode)))
assert len({r['path'] for r in rows}) == len(rows)
assert not (HERE / 'ARCHIVE_INPUTS.json').exists()
(HERE / 'ARCHIVE_INPUTS.json').write_text(json.dumps(dict(files=rows, count=len(rows),
    bytes=sum(r['bytes'] for r in rows)), indent=2) + '\n')
(HERE / 'PRIOR_ARCHIVE_REFERENCES.json').write_text(json.dumps(dict(
    prior_commit='6b451cff07f46c6bf3eee325ff81ead1089e8c89',
    prior_manifest_path=str((OLD / 'MANIFEST.json').relative_to(REPO)),
    prior_manifest_sha256=sha(OLD / 'MANIFEST.json'),
    prior_inputs_sha256=sha(OLD / 'ARCHIVE_INPUTS.json'),
    byte_equal_files=old_equal, newly_captured_paths=old_new,
    limits='Previously archived exact regular-file bytes reused; no previous file metadata equality claimed.'
), indent=2) + '\n')
unique = {}
with tarfile.open(ARCHIVE, 'w:gz', compresslevel=6, format=tarfile.PAX_FORMAT) as tar:
    for row in rows:
        path = Path(row['source'])
        assert sha(path) == row['sha256'] and path.stat().st_size == row['bytes']
        assert stat.S_IMODE(path.stat().st_mode) == row['mode']
        key = (row['sha256'], row['mode'])
        info = tar.gettarinfo(str(path), arcname=row['path'])
        info.uid = info.gid = 0
        info.uname = info.gname = ''
        if key in unique:
            info.type = tarfile.LNKTYPE
            info.linkname = unique[key]
            info.size = 0
            tar.addfile(info)
        else:
            with path.open('rb') as stream:
                tar.addfile(info, stream)
            unique[key] = row['path']
for row in rows:
    path = Path(row['source'])
    assert sha(path) == row['sha256'] and path.stat().st_size == row['bytes']
    assert stat.S_IMODE(path.stat().st_mode) == row['mode']
parts = []
with ARCHIVE.open('rb') as stream:
    index = 1
    while chunk := stream.read(40 * 1024 * 1024):
        part = HERE / ('review-evidence.tar.gz.part-' + str(index).zfill(3))
        with part.open('xb') as out:
            out.write(chunk)
        parts.append(dict(file=part.name, bytes=len(chunk), sha256=sha(part)))
        index += 1
manifest = dict(kind='UNRELEASED_SERVICEBUS_REMOTE_CHECKPOINT_EVIDENCE_ARCHIVE',
    baseline_commit='6b451cff07f46c6bf3eee325ff81ead1089e8c89', repository='EdgarLiebold/vicione-servicebus',
    requested_target='main', archive_bytes=ARCHIVE.stat().st_size, archive_sha256=sha(ARCHIVE),
    member_count=len(rows), source_bytes=sum(r['bytes'] for r in rows),
    unique_regular_files=len(unique), duplicate_hardlinks=len(rows)-len(unique), parts=parts,
    inputs_sha256=sha(HERE / 'ARCHIVE_INPUTS.json'),
    prior_references_sha256=sha(HERE / 'PRIOR_ARCHIVE_REFERENCES.json'),
    limits='Regular retained evidence and independent reports only. Temporary private SDK checkouts outside retained evidence are excluded. Current product/test/docs are normal Git files. Incomplete candidates and non-green attempts are retained without acceptance; no release, new SDK/test run or aggregate closure.')
(HERE / 'MANIFEST.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(json.dumps({k:v for k,v in manifest.items() if k not in ('parts','limits')}), flush=True)

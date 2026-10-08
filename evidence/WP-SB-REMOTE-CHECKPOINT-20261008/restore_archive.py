"""Restore ordered checkpoint parts into a new directory and verify every byte."""
from pathlib import Path, PurePosixPath
import argparse, hashlib, json, os, stat, tarfile, tempfile

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()
def safe(name):
    p = PurePosixPath(name)
    assert name and not p.is_absolute() and '\\' not in name
    assert all(c not in ('', '.', '..') for c in name.split('/'))
    assert not any(ord(c) < 32 for c in name)
    return name
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('destination', type=Path)
args = parser.parse_args()
here = Path(__file__).resolve().parent
manifest = json.loads((here / 'MANIFEST.json').read_text())
assert sha(here / 'ARCHIVE_INPUTS.json') == manifest['inputs_sha256']
assert sha(here / 'PRIOR_ARCHIVE_REFERENCES.json') == manifest['prior_references_sha256']
rows = json.loads((here / 'ARCHIVE_INPUTS.json').read_text())['files']
by = {safe(r['path']): r for r in rows}
assert len(by) == len(rows) == manifest['member_count']
destination = args.destination.resolve()
assert not destination.exists()
destination.mkdir(parents=True)
seen = set()
with tempfile.TemporaryDirectory(prefix='servicebus-checkpoint-restore-') as temporary:
    archive = Path(temporary) / 'evidence.tar.gz'
    with archive.open('xb') as out:
        for row in manifest['parts']:
            part = here / safe(row['file'])
            assert part.stat().st_size == row['bytes'] and sha(part) == row['sha256']
            with part.open('rb') as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                    out.write(chunk)
    assert archive.stat().st_size == manifest['archive_bytes']
    assert sha(archive) == manifest['archive_sha256']
    with tarfile.open(archive, 'r:gz') as tar:
        for member in tar:
            name = safe(member.name)
            assert name in by and name not in seen
            row = by[name]
            assert member.mode == row['mode']
            target = destination / name
            target.parent.mkdir(parents=True, exist_ok=True)
            if member.islnk():
                link = safe(member.linkname)
                assert link in seen and by[link]['sha256'] == row['sha256']
                assert by[link]['mode'] == row['mode']
                os.link(destination / link, target)
            else:
                assert member.isfile() and member.size == row['bytes']
                with tar.extractfile(member) as src, target.open('xb') as out:
                    for chunk in iter(lambda: src.read(1024 * 1024), b''):
                        out.write(chunk)
                target.chmod(row['mode'])
            assert target.stat().st_size == row['bytes'] and sha(target) == row['sha256']
            assert stat.S_IMODE(target.stat().st_mode) == row['mode']
            seen.add(name)
assert seen == set(by)
receipt = dict(kind='FULL_NEW_CHECKPOINT_RESTORE_BYTE_MODE_PROOF',
    member_count=len(seen), archive_sha256=manifest['archive_sha256'],
    input_sha256=manifest['inputs_sha256'], all_bytes_sizes_modes_exact=True,
    destination=str(destination), prior_archive_is_separate_dependency=True,
    limits='Restores new evidence archive only. Previously archived identical files are retrieved separately from the retained 6 October archive; normal product/test/docs are in Git.')
(destination / 'RESTORE_RECEIPT.json').write_text(json.dumps(receipt, indent=2)+'\n')
print(json.dumps(receipt), flush=True)

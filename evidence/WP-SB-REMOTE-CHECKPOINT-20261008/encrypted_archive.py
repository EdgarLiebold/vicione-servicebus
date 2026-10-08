"""Publish encrypted evidence only; restore with the separately held local key."""
from pathlib import Path
import argparse, hashlib, json, os, shutil, stat, subprocess, tempfile

HERE = Path(__file__).resolve().parent
def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('mode', choices=['encrypt', 'restore'])
parser.add_argument('--key', type=Path, required=True)
parser.add_argument('--pwsh', default='pwsh')
parser.add_argument('--destination', type=Path)
args = parser.parse_args()
key = args.key.resolve()
assert not key.is_relative_to(HERE.parent.parent)
def crypt(mode, source, target, aad, check=True):
    result = subprocess.run([args.pwsh, '-NoLogo', '-NoProfile', '-NonInteractive', '-File',
        str(HERE / 'archive_crypto.ps1'), '-Mode', mode, '-InputFile', str(source),
        '-OutputFile', str(target), '-KeyFile', str(key), '-Aad', aad],
        capture_output=True, timeout=120)
    if check and result.returncode:
        raise RuntimeError('Archive cryptography failed: ' + result.stderr.decode(errors='replace'))
    return result.returncode
manifest = json.loads((HERE / 'MANIFEST.json').read_text())
if args.mode == 'encrypt':
    assert not key.exists()
    key.parent.mkdir(parents=True, exist_ok=True)
    descriptor = os.open(key, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(descriptor, 'wb') as stream:
        stream.write(os.urandom(32))
    assert stat.S_IMODE(key.stat().st_mode) == 0o600 and key.stat().st_size == 32
    assert not (HERE / 'ENCRYPTED_MANIFEST.json').exists()
    parts, nonces = [], set()
    with tempfile.TemporaryDirectory(prefix='servicebus-encryption-qualification-') as temporary:
        work = Path(temporary)
        for index, row in enumerate(manifest['parts'], 1):
            source = HERE / row['file']
            assert sha(source) == row['sha256'] and source.stat().st_size == row['bytes']
            target = HERE / ('review-evidence.aesgcm.part-' + str(index).zfill(3))
            aad = 'archive=' + manifest['archive_sha256'] + ';part=' + str(index) + ';plain=' + row['sha256']
            crypt('Encrypt', source, target, aad)
            with target.open('rb') as stream:
                magic = b'VICIONE-SERVICEBUS-AES256GCM-V1\n'
                assert stream.read(len(magic)) == magic
                nonce = stream.read(12)
            assert nonce not in nonces
            nonces.add(nonce)
            restored = work / ('restored-' + str(index))
            crypt('Decrypt', target, restored, aad)
            assert sha(restored) == row['sha256'] and restored.stat().st_size == row['bytes']
            if index == 1:
                corrupt = work / 'corrupted'
                data = bytearray(target.read_bytes())
                data[-1] ^= 1
                corrupt.write_bytes(data)
                rejected = work / 'rejected'
                assert crypt('Decrypt', corrupt, rejected, aad, check=False) != 0
                assert not rejected.exists()
                assert crypt('Decrypt', target, rejected, aad + ';wrong', check=False) != 0
                assert not rejected.exists()
            parts.append(dict(file=target.name, bytes=target.stat().st_size, sha256=sha(target),
                              plaintext_file=row['file'], plaintext_sha256=row['sha256'], aad=aad))
    receipt = dict(kind='AES256_GCM_ENCRYPTED_REVIEW_ARCHIVE_R1', parts=parts,
        plaintext_manifest_sha256=sha(HERE / 'MANIFEST.json'), all_parts_decrypted_hash_exact=True,
        all_nonces_unique=True, ciphertext_tamper_rejected=True, incorrect_aad_rejected=True,
        key_file_not_in_repository=True, key_mode='0600',
        limits='Raw archive protected; key is held separately locally and must be backed up separately. Normal source/test/docs are public Git files. No platform hardware/cloud/Suite test qualification.')
    (HERE / 'ENCRYPTED_MANIFEST.json').write_text(json.dumps(receipt, indent=2)+'\n')
    print(json.dumps(dict(parts=len(parts), manifest_sha256=sha(HERE / 'ENCRYPTED_MANIFEST.json'),
                         key_path=str(key), key_contents_printed=False)), flush=True)
else:
    assert args.destination is not None and not args.destination.exists()
    assert key.is_file() and key.stat().st_size == 32
    encrypted = json.loads((HERE / 'ENCRYPTED_MANIFEST.json').read_text())
    assert sha(HERE / 'MANIFEST.json') == encrypted['plaintext_manifest_sha256']
    assert len(encrypted['parts']) == len(manifest['parts'])
    with tempfile.TemporaryDirectory(prefix='servicebus-encrypted-restore-') as temporary:
        work = Path(temporary)
        for name in ('MANIFEST.json','ARCHIVE_INPUTS.json','PRIOR_ARCHIVE_REFERENCES.json','restore_archive.py'):
            shutil.copyfile(HERE / name, work / name)
        for item, original in zip(encrypted['parts'], manifest['parts']):
            assert Path(item['file']).name == item['file'] and Path(original['file']).name == original['file']
            source = HERE / item['file']
            assert sha(source) == item['sha256'] and source.stat().st_size == item['bytes']
            assert item['plaintext_file'] == original['file'] and item['plaintext_sha256'] == original['sha256']
            target = work / original['file']
            crypt('Decrypt', source, target, item['aad'])
            assert sha(target) == original['sha256'] and target.stat().st_size == original['bytes']
        import sys
        subprocess.run([sys.executable, str(work / 'restore_archive.py'), str(args.destination)],
                       check=True, timeout=600)

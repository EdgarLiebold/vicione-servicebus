#!/usr/bin/env python3
"""Revert one packed XML member at a time in an owned consumer cache; binaries stay fixed."""
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile

W = Path(__file__).resolve().parent.parent
D = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR'
R = Path('/private/tmp/vicione-servicebus-api-review-20261001')
E = W / 'repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002'
C = R / 'repair-consumers/package08/docs-corrected'
OLD_FEED = R / 'repair-packages/package02-identity'
FEED = R / 'repair-packages/package08-docs'
MODES = ['empty-address', 'pre-auth', 'ef-single', 'ef-separate', 'ef-same']
MEMBERS = [
    ('empty-address', 'ViciOne.ServiceBus.Abstractions', 'P:ViciOne.ServiceBus.Advanced.Serialization.IMessageData.Address', False),
    ('pre-auth', 'ViciOne.ServiceBus', 'M:ViciOne.ServiceBus.Serialization.IEncryptionKeyProvider.TryGetKey(System.String,ViciOne.ServiceBus.Serialization.EncryptionKey@)', False),
    ('ef-same', 'ViciOne.ServiceBus.EntityFrameworkCore', 'M:ViciOne.ServiceBus.EntityFrameworkCore.EntityFrameworkOutboxConfigurationExtensions.ConfigureEntityFrameworkTransactionalStore``2(', True),
]


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def run(phase, mode, expected):
    command = ['dotnet', 'run', '--project', 'DocumentationContracts.csproj', '-c', 'Release', '--no-build', '--no-restore', '--', mode]
    process = subprocess.run([sys.executable, str(D / 'run_operation.py'), phase, str(C), '--', *command], stdout=subprocess.PIPE, text=True)
    receipt = next(row for row in map(json.loads, (E / 'VERIFICATION_LOG.jsonl').read_text().splitlines()) if row['phase'] == phase)
    assert process.returncode == receipt['exit_code'] == expected, (phase, process.returncode, expected)
    raw = Path(receipt['log']).read_bytes()
    assert sha(raw) == receipt['log_sha256']
    observation = json.loads(raw.decode().strip())
    assert observation['mode'] == mode and observation['contract'] == ('PASS' if expected == 0 else 'FAIL')
    if expected:
        assert observation['assertion'].startswith('Packed '), observation
    print(json.dumps({'phase': phase, 'exit_code': expected, 'observation': observation}), flush=True)
    return receipt


def select(tree, id, prefix):
    matches = [member for member in tree.findall('./members/member') if
               (member.attrib['name'].startswith(id) if prefix else member.attrib['name'] == id)]
    assert len(matches) == 1, (id, len(matches))
    return matches[0]


def main():
    original_program = R / 'repair-consumers/package08/docs-original/Program.cs'
    assert original_program.read_bytes() == (C / 'Program.cs').read_bytes()
    dlls = {str(path): sha(path.read_bytes()) for path in (C / 'bin/Release/net10.0').glob('*.dll')}
    result = {'status': 'RUNNING', 'consumer_program_sha256': sha((C / 'Program.cs').read_bytes()), 'checks': [], 'mutants': [],
              'proof_limit': 'Documentation-only member counterreversions in isolated restored package XML. No production source or provider behavior is mutated; packaged/runtime DLLs must remain unchanged.'}
    try:
        for mode in MODES:
            result['checks'].append(run('package08-docs-corrected-' + mode, mode, 0))
        for mode, package, id, prefix in MEMBERS:
            cache = C / 'restored-packages' / package.lower() / '1.0.0/lib/net10.0' / (package + '.xml')
            corrected = cache.read_bytes()
            with zipfile.ZipFile(FEED / (package + '.1.0.0.nupkg')) as archive:
                assert archive.read('lib/net10.0/' + package + '.xml') == corrected
            with zipfile.ZipFile(OLD_FEED / (package + '.1.0.0.nupkg')) as archive:
                old = archive.read('lib/net10.0/' + package + '.xml')
            tree = ET.fromstring(corrected)
            member = select(tree, id, prefix)
            replacement = copy.deepcopy(select(ET.fromstring(old), id, prefix))
            parent = tree.find('members')
            index = list(parent).index(member)
            parent.remove(member)
            parent.insert(index, replacement)
            before_members = {value.attrib['name']: ET.tostring(value) for value in ET.fromstring(corrected).findall('./members/member')}
            after_members = {value.attrib['name']: ET.tostring(value) for value in tree.findall('./members/member')}
            assert set(before_members) == set(after_members)
            assert [key for key in before_members if before_members[key] != after_members[key]] == [member.attrib['name']]
            mutant = ET.tostring(tree, encoding='utf-8', xml_declaration=True)
            try:
                cache.write_bytes(mutant)
                receipt = run('package08-docs-mutant-' + mode, mode, 2)
                result['mutants'].append({'mode': mode, 'member': member.attrib['name'], 'status': 'KILLED_BY_PUBLIC_RUNTIME_PACKED_XML_CONTRACT_ASSERTION',
                                          'corrected_xml_sha256': sha(corrected), 'mutant_xml_sha256': sha(mutant),
                                          'only_one_logical_member_changed': True, 'native_receipt': receipt})
            finally:
                cache.write_bytes(corrected)
            result['checks'].append(run('package08-docs-rollback-' + mode, mode, 0))
        result['status'] = 'EXECUTED_ALL_3_LOGICAL_XML_MEMBER_MUTANTS_KILLED_ROLLBACK_GREEN'
    finally:
        assert dlls == {str(path): sha(path.read_bytes()) for path in (C / 'bin/Release/net10.0').glob('*.dll')}
        result['all_runtime_dll_hashes_unchanged'] = True
        result['runtime_dlls'] = dlls
        (E / 'PACKAGE08_DOCS_MUTATIONS.json').write_text(json.dumps(result, indent=2) + '\n')


if __name__ == '__main__':
    main()

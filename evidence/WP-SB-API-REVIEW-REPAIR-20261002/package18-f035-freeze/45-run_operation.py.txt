"""Run one owned verification operation and preserve its actual command, exit and log hash."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

sys.dont_write_bytecode = True
W = Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0')
E = W / 'repositories/vicione-servicebus/evidence/WP-SB-API-REVIEW-REPAIR-20261002'
R = W / 'SERVICEBUS_API_REVIEW_AND_REPAIR'
phase, cwd, *command = sys.argv[1:]
if command and command[0] == '--':
    command = command[1:]
assert command and Path(cwd).is_dir()
assert phase.replace('-', '').replace('_', '').isalnum()
log = E / (phase + '.log')
assert not log.exists(), 'Use a new phase name; verification logs are append-only'
started = datetime.datetime.now(datetime.timezone.utc).isoformat()
env = os.environ.copy()
env.update(DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1',
           DOTNET_CLI_HOME='/private/tmp/vicione-servicebus-api-review-20261001/runtime/repair-dotnet-home')
print('RUNNING ' + phase + ': ' + repr(command), flush=True)
with log.open('w') as stream:
    result = subprocess.run(command, cwd=cwd, env=env, stdout=stream, stderr=subprocess.STDOUT)
row = dict(phase=phase, command=command, cwd=cwd, exit_code=result.returncode,
           started_utc=started, completed_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
           log=str(log), log_sha256=hashlib.sha256(log.read_bytes()).hexdigest())
with (E / 'VERIFICATION_LOG.jsonl').open('a') as stream:
    stream.write(json.dumps(row) + '\n')
print(json.dumps(row), flush=True)
print(log.read_text()[-10000:], flush=True)
sys.exit(result.returncode)

"""Freeze personally completed readonly review; no product/native operations."""
import datetime, difflib, hashlib, json, pathlib
O=pathlib.Path('/Users/edgar.liebold/Downloads/ViciOne Suite 2.0/repositories/vicione-servicebus')
E=O/'evidence/WP-SB-API-REVIEW-REPAIR-20261002';R=pathlib.Path('/private/tmp/vicione-servicebus-api-review-20261001')
S=E/'package05-diagnostic-adversarial';F=R/'frozen-repository'
def load(p):return json.loads(pathlib.Path(p).read_text())
def bind(p):
 p=pathlib.Path(p);b=p.read_bytes();return dict(path=str(p),sha256=hashlib.sha256(b).hexdigest(),bytes=len(b))
def save(name,x):
 p=S/name;assert not p.exists(),p;p.write_text(json.dumps(x,indent=2,ensure_ascii=False)+'\n')
proof=load(S/'MECHANICAL_PROOFS.json');freeze=load(E/'PACKAGE05_F030_DIAGNOSTIC_FREEZE.json')
utc=datetime.datetime.now(datetime.timezone.utc).isoformat()
own_core=R/'repair-research/core-family-review/READ_MANIFEST_CHECKPOINT207.json';cm=load(own_core)
own_core_sha=bind(own_core)['sha256'];assert own_core_sha=='47c1538fe6f41d2df013c3ac75d272ff39b1af3a224aeeffe288ec851a72c572'
reused=[
 'src/ViciOne.ServiceBus/Advanced/Registration/IConsumerKindEndpointContext.cs',
 'src/ViciOne.ServiceBus/Advanced/Registration/IConsumerKindContext.cs',
 'src/ViciOne.ServiceBus/Configuration/ConsumerConfigurator.cs',
 'src/ViciOne.ServiceBus/Configuration/IConsumerSpecification.cs',
 'src/ViciOne.ServiceBus/InMemoryTransport/Configuration/InMemoryBusFactoryConfigurator.cs']
fresh=[
 'src/ViciOne.ServiceBus/Configuration/DependencyInjection/IBusRegistrationIdentity.cs',
 'src/ViciOne.ServiceBus.Abstractions/Configuration/ConfigurationMessages.cs',
 'src/ViciOne.ServiceBus/DependencyInjection/Registration/Consumers/ConsumerKind.cs',
 'src/ViciOne.ServiceBus/Configuration/DependencyInjection/BusRegistrationContext.cs',
 'src/ViciOne.ServiceBus/Configuration/ConsumerSpecification.cs',
 'src/ViciOne.ServiceBus/Logging/Internal/BusLogContext.cs',
 'src/ViciOne.ServiceBus.Abstractions/Configuration/OptionsSet.cs',
 'src/ViciOne.ServiceBus.Abstractions/Configuration/IOptions.cs',
 'src/ViciOne.ServiceBus.Abstractions/Configuration/Consumers/IConsumerConfigurator.cs',
 'src/ViciOne.ServiceBus/Configuration/DependencyInjection/DependencyInjectionRegistrationExtensions.cs',
 'src/ViciOne.ServiceBus/Configuration/MessageLimitsConfigurationExtensions.cs',
 'src/ViciOne.ServiceBus/Configuration/InMemoryTransport/InMemoryConfigurationExtensions.cs',
 'src/ViciOne.ServiceBus/Configuration/DependencyInjection/ServiceCollectionBusConfigurator.cs']
# Full personally read metadata and logs are distinguished from roots' source-reading assertions.
shared=[]
for rel in reused:
 row=next(x for x in cm['files'] if x['path']==rel);b=bind(O/rel);assert b['sha256']==row['sha256']
 shared.append(dict(**b,read_status='READ_FULL',read_by='azure_test_scope',read_mode='OWN_PRIOR_FULL_EXACT_SHA_REUSE',own_manifest=bind(own_core)))
for rel in fresh:
 shared.append(dict(**bind(O/rel),read_status='READ_FULL',read_by='azure_test_scope',read_mode='PERSONAL_FULL_CURRENT_TEXT'))
metadata=[E/'PACKAGE05_F030_DIAGNOSTIC_FREEZE.json',E/'PACKAGE05_DIAGNOSTIC_ROOT_BINDING.json',E/'PACKAGE05_DIAGNOSTIC_MUTATIONS.json',E/'PACKAGE05_F030_DIAGNOSTIC_OWNER_PLAN.json',E/'PACKAGE05_DIAGNOSTIC_SETUP_EXCLUSION.json',E/'PACKAGE05_CORE_FREEZE.json',E/'package05-diagnostic-initial-consumer.cs.txt']
metadata_reads=[]
for p in metadata:
 mode='PERSONAL_FULL_STRUCTURED_PARTITIONS_ALL_KEYS' if p.name in ['PACKAGE05_DIAGNOSTIC_ROOT_BINDING.json','PACKAGE05_DIAGNOSTIC_MUTATIONS.json'] else 'PERSONAL_FULL_TEXT'
 metadata_reads.append(dict(**bind(p),read_status='READ_FULL',read_mode=mode,read_by='azure_test_scope'))

# Original comparisons: exact shared full current text plus ALL original replacement text
# personally read in complete deltas, rather than merely claiming raw original re-reading.
oldfreeze=load(E/'PACKAGE05_CORE_FREEZE.json');deltas=[]
for i in range(8):
 current=pathlib.Path(freeze['owned_inputs'][i]['snapshot']);rel=str(pathlib.Path(freeze['owned_inputs'][i]['path']).relative_to(O))
 if i<5:
  candidates=[x for x in oldfreeze['owned_inputs'] if pathlib.Path(x['path']).name==pathlib.Path(rel).name]
  assert len(candidates)==1;prior=pathlib.Path(candidates[0]['snapshot'])
 elif i<7:prior=F/rel
 else:
  assert not (F/rel).exists();deltas.append(dict(path=rel,status='NEW_INTERNAL_FILE_ABSENT_FROZEN_ORIGINAL',current=bind(current)));continue
 a=current.read_bytes();b=prior.read_bytes();aa=a.splitlines(keepends=True);bb=b.splitlines(keepends=True)
 opcodes=difflib.SequenceMatcher(None,aa,bb,autojunk=False).get_opcodes()
 rebuilt=b''.join(aa[a0:a1][0:0]+b''.join(bb[b0:b1]) if tag!='equal' else b''.join(aa[a0:a1]) for tag,a0,a1,b0,b1 in []) if False else b''.join(b''.join(aa[a0:a1]) if tag=='equal' else b''.join(bb[b0:b1]) for tag,a0,a1,b0,b1 in opcodes)
 assert rebuilt==b
 delta=''.join(difflib.unified_diff(b.decode().splitlines(True),a.decode().splitlines(True),fromfile=str(prior),tofile=str(current)))
 deltas.append(dict(path=rel,previous=bind(prior),current=bind(current),unchanged=a==b,read_status='READ_FULL',read_mode='EXACT_SHARED_PERSONALLY_FULL_CURRENT_TEXT_AND_COMPLETE_ORIGINAL_REPLACEMENTS_FULL',reverse_reconstruction_sha256=hashlib.sha256(rebuilt).hexdigest(),complete_delta=delta))
save('DELTA_PROOF.json',dict(utc=utc,files=deltas,original_frozen_head='5afd0d077788f594b268a5ac441e788d54d57a70',previous_package05_collector_sha256='df09223331e2acd9be04e5e241510bd39cc2572618b3d3b0d16f0548e4f9c0d0',unchanged_prior_product_inputs=[0,1,2,4],public_signature_changes=False))

skillsroot=R/'skills-source/skills-2124a6e3518b2120cda1c076d62ed79491b00693/plugins/dotnet-test/skills'
skills=[]
for rel in ['test-gap-analysis/SKILL.md','test-anti-patterns/SKILL.md','test-analysis-extensions/SKILL.md','test-analysis-extensions/extensions/dotnet.md']:
 p=skillsroot/rel
 if not p.exists() and rel.endswith('extensions/dotnet.md'):p=skillsroot/'extensions/dotnet.md'
 if p.exists():skills.append(dict(**bind(p),read_mode='OWN_PRIOR_PINNED_FULL_READING_REUSE',read_status='READ_FULL',pin='2124a6e3518b2120cda1c076d62ed79491b00693'))
closure=dict(utc=utc,status='READ_FULL_BOUNDED_F030_DIAGNOSTIC_SCOPE',read_by='azure_test_scope',role='Independent internal readonly adversarial reviewer; no authorship of product/oracle',freeze=proof['freeze'],owned_inputs=proof['owned_current_snapshot_match'],shared_inputs=shared,metadata_inputs=metadata_reads,logs=[dict(receipt=x['receipt'],log_bytes=x['log_bytes'],read_status=x['read_status']) for x in proof['receipts']],skills=skills,baseline_reading=bind(S/'DELTA_PROOF.json'),mutation_snapshots_reading='Eight source files per mutant: own personally full frozen current text reused for seven exact-equal files and complete changed text read for one pure substitution; source SHA verification and full deltas in MUTATION_REVIEW.json.',root_closure_transfer=False,additional_mechanical_checks=[bind(S/x) for x in ['PACKAGE_BINDING_INDEPENDENT.json','ROLLBACK_INDEPENDENT.json','MUTATION_REVIEW.json']],limits=['No builds, restores, tests, native operations or product writes by reviewer.','Ledger reviewed through all 36 selected exact receipt objects, not whole ledger.','6209 rollback hashes are mechanical reads, not personal semantic readings.','No whole Core 766 test-owner reading or grading, no Whole API closure, no A+ transfer.'])
save('READ_CLOSURE.json',closure)

contracts=[
 dict(source='ConsumerRegistration.cs',conclusion='Per-consumer configurator options capture canonical IBusRegistrationIdentity.BusKey before definition/actions and transfer through normal ConsumerKind endpoint context. ConsumerConfigurator owns a fresh ConsumerSpecification/OptionsSet per configuration; tag is not global, endpoint-shared, or a retained provider.',evidence='Current registration/consumer specification/owner context and default+typed service collection factory fully read; same consumer type on two typed buses exercised.'),
 dict(source='BatchConsumerMessageConnector.cs',conclusion='Reads options BusKey once, passes it through plain constructor and grouped reflection Activator fourth argument. Optional ctor argument keeps internal callers compatible; absent tag gives honest unknown rather than fabricating default or deriving host identity.',evidence='Both grouped/plain eight-case execution and distinct handoff mutants; manual/custom/factory-without-normal-tag unknown fallback is statically reviewed only.'),
 dict(source='ConsumerBusIdentityOptions.cs',conclusion='Internal sealed string-only tag; no public signature change or provider lifetime retention. Canonical default is default; typed key is assembly name plus full contract name, independent of equal queue names or differing host addresses.',evidence='Public normal DI default/typed configuration plus beta-to-alpha substitution causal kill; no claim that distinct hosts themselves define bus key.'),
 dict(source='BatchCollector.cs',conclusion='Stores owner string; existing System TimeProvider whole-millisecond timer guard/admission predicate is unchanged. ConfigurationMessages adds feature/bus/problem/fix to the actual endpoint, Batch.TimeLimit, 4294967294 and remediation. Result key/failure aggregation preserved. Both collector constructors use the same owner handoff.',evidence='Complete prior package05 collector delta and source-PDB checksums; exact starts-with, results, endpoint and bound/fix assertions plus unchanged 42 regressions.')]
native_evidence=dict(previous_final=dict(total=8,causal_red=4,positive=4,phase='package05-diagnostic-previous-eight-r2'),corrected=dict(total=8,pass_count=8,phase='package05-diagnostic-corrected-eight'),regression=dict(total=42,pass_count=42,phase='package05-diagnostic-regression-fortytwo'),mutations=dict(strict_compiled=6,causal_negative=6,valid_positive=6,baseline=8,rollback=8,negative_exception='Xunit.Sdk.StartsWithException at DiagnosticOwnerAssertions.cs:94'),excluded_initial=dict(total=8,failures=6,typed_setup_failures=4,other_default_causal_red=2,reason='Typed configurator helper erased static TBus and selected default Limits registration; corrected final r2 keeps typed Limits/UsingInMemory in typed lambdas. Initial fixture/archive retained; no setup failure counted as mutation kill.'))
limits=[
 'F030 startup validation remains PO_PENDING; this review accepts only bounded runtime owner diagnostic propagation. The provider-dependent oversized System timer guard runs at batch admission, not newly at startup.',
 'Eight new and forty-two regression cases are actual public NuGet console cases, not an unfiltered canonical Core owner suite or Core 766 FULL closure/grade.',
 'Two typed buses exercise the same consumer type and same queue string with distinct configured hosts, native valid/invalid and grouped/plain routes. Multi-consumer same-endpoint registrations, manual constructor/configurator, custom registration SPI and factory-only routes have static reasoning, no new native controls.',
 'Diagnostics assert the observed actual endpoint address; they do not independently assert an expected host-authority routing contract.',
 'OwnedLifetime preserves original body stack, aggregates body first and attempts independent cleanup stages. Buses are tracked before Start and stopped in reverse; ordinary completed cases verify terminal/delivery counts. No injected Stop/Disconnect/Dispose failure, second-route construction failure or active-load teardown race is exercised. A loop of handle.Dispose actions can skip later handles if one throws; provider cleanup still runs. This is an untested harness fault boundary, not a demonstrated product defect.',
 'Cancellation/Timeout from body or cleanup explicitly fails guard checks, not a causal diagnostic pass. Cleanup uses bounded waits with CancellationToken.None for stopping owned buses; no claim about all external SDK cooperative cancellation.',
 'Actual runtime is .NET 10.0.10 X64 with NuGet Microsoft.Extensions 10.0.12 and xunit.v3.assert 4.0.0. In-memory transport only; no broker/cloud/ARM or custom TimeProvider active-load closure.',
 'Six compiled mutants replace runtime DLLs in a NuGet-only compiled consumer; no per-mutant nupkg pack claim. Corrected fresh package/runtime binaries and mutation-frame path-dependent binaries are separate hash frames.',
 'All twenty lock nodes have separately recorded cache raw ZIP/sidecar SHA512. Only the six selected unsigned ViciOne archive chains were independently equated to lock SHA512; external raw archives differ from lock contentHash and their normalization is outside this review.',
 'Original rollback 6209 hashes independently checked at this timestamp and new identity source removed; root reading closure and other findings do not transfer.']
review=dict(utc=utc,status='ACCEPTED_SCOPED_F030_RUNTIME_BUS_DIAGNOSTIC_SUPPLEMENT',freeze=proof['freeze'],blocking_findings=[],contracts=contracts,oracle=dict(public_surface='No project references, internals access, reflection into product or canonical fixture infrastructure; SDK packages and public DI/transport APIs only.',eight_matrix='default or two typed buses x invalid/valid x grouped/plain; two typed routes independently checked',checks='Exact single ConfigurationException and Batch.TimeLimit failure, canonical prefix, observed endpoint, max bound/remediation and no other owner. Invalid has zero batch delivery; valid has exact owner, item IDs/order and batches. Completed routes check terminal counts after reverse stop.',cleanup='OwnedLifetime body-first ExceptionDispatchInfo/AggregateException and per-stage cleanup; no native injected fault-path proof.'),native_evidence=native_evidence,proofs=[bind(S/x) for x in ['READ_CLOSURE.json','MECHANICAL_PROOFS.json','DELTA_PROOF.json','PACKAGE_BINDING_INDEPENDENT.json','MUTATION_REVIEW.json','ROLLBACK_INDEPENDENT.json']],limits=limits,whole_api_status='PARTIAL',symbols_closed=0,native_operations_by_reviewer=0,product_writes_by_reviewer=0)
save('REVIEW.json',review)
text="""Scoped acceptance of F030 runtime bus diagnostic supplement

Freeze 2b09f2d8d830f1e1de7f900109aeda58cde2b6918cde9bd23ab88d901087a21e is independently unchanged. All 29 owned inputs, complete current code, complete original/pre-supplement deltas, relevant shared DI/options/consumer contracts and all 36 actual native receipt logs were personally reviewed. Root's source closure was not adopted. No blocking defect was found in the four diagnostic changes for this scope.

Canonical ownership travels from normal default/typed bus registration through each consumer's own configurator options and both plain/grouped batch collector constructors. It stores only a string. Equal queue names and different host addresses cannot substitute for the canonical bus key. Missing owner metadata honestly falls back to unknown; those manual/custom routes have static evidence only. Both constructors are exercised, including the grouped Activator path. The existing System timer guard, result key, admission timing and four unchanged prior product inputs remain intact; no public API signature changed.

The final previous-package run has eight actual cases: four exact causal diagnostic failures and four positives. Corrected fresh NuGet cases pass 8/8 and fresh regressions pass 42/42. All six strictly compiled, one-effect mutants fail specifically at Xunit.Sdk.StartsWithException on the owner prefix, with a valid positive each; baseline and rollback pass 8/8. Existing NuGet consumer runtime DLLs were substituted for mutants; no pack-per-mutant claim is made. The initial erased typed-configurator helper produced four setup failures, is explicitly excluded and remains archived. Its final correction preserves typed Limits/UsingInMemory lambdas and existing assertion bodies.

Six selected archive SHA256/raw SHA512 -> lock -> dedicated cache -> actual runtime DLL chains independently match. Portable PDB document checksums independently bind all seven previous and eight corrected product inputs to the selected package DLLs. All twenty lock graph nodes have separately recorded cache/archive hashes; third-party raw ZIP SHA512 differs from lock contentHash, so no unverified equality or normalization claim is made for those nodes. All 6209 original mutation-tree hashes independently match the ledger now; the new identity file is absent, seven oracle hashes are unchanged and the two consumer DLLs are restored to selected package bytes.

The assertions cover exact results/diagnostic prefix, observed endpoint, upper bound and fix, invalid zero delivery, valid owner/IDs/order and completed terminal counts. OwnedLifetime preserves a failing body's stack and attempts independent cleanup stages; tracked buses stop in reverse and provider cleanup remains attempted. No cleanup fault injection or active-load shutdown race was run. A handle-disposal loop can skip subsequent handles if one Dispose throws; provider cleanup remains independent. Second-route construction failures and manual/custom registration cases also remain outside actual controls. There is no demonstrated product failure in these untested harness boundaries.

F030 startup closure remains PO_PENDING. Eight diagnostic plus forty-two regression console cases do not close the canonical Core test owner, Core 766, the whole API, or an A+ grade. Multi-consumer same-endpoint, manual/custom/factory metadata fallback, broker/cloud/ARM and active-load/custom-provider lifetime obligations remain open. Actual runtime is .NET 10.0.10 X64. The reviewer ran no native build/test/restore and made no product changes. API review remains PARTIAL with zero CLOSED symbols.
"""
p=S/'REVIEW.txt';assert not p.exists();p.write_text(text)
manifest=dict(utc=utc,status='IMMUTABLE_SCOPED_REVIEW_FINAL',files=[bind(p) for p in sorted(S.iterdir()) if p.is_file() and p.name!='MANIFEST.json'])
save('MANIFEST.json',manifest)
print(json.dumps([bind(S/x) for x in ['REVIEW.txt','REVIEW.json','READ_CLOSURE.json','MANIFEST.json']],indent=2))

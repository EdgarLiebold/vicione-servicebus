# Iteration 119 — context contract packet

## Human research and scope

The lead personally read the complete current `BasePipeContext`, `ScopePipeContext`,
`ListPayloadCache`, `IPayloadCache`, and the existing scope/cache tests. Comments are
authored manually after understanding the code; no source or comment generator is used.
Project ownership remains unchanged: Abstractions owns these context contracts, Core is
a sibling project, and provider families remain grouped separately.

The supplied parse-only Roslyn pairing analyzer ran against a temporary symlink view
containing only Abstractions source and tests. Protected `review` and `TestResults` were
not scanned. It found 710 source files, 101 test files, 217 paired and 493 unpaired files.
Base and Scope are already paired; pairing is not proof of branch or behavioral coverage.
Base's six referring test files include the existing scope tests, while Scope has one.
The new Base test class is a focused depth check, not a claim that Base had no tests.

## Findings and implementation plan

- Both cache-taking Base constructors must reject a null required cache consistently.
- Base and Scope must reject a null runtime payload type with its exact parameter name.
- Required get/add/update delegates must be validated before self, local or parent fast
  paths; invalid calls must invoke no delegate and change no retained payload.
- Preserve all six Base construction paths, optional null/empty payload arrays, initial
  array ownership, cancellation identity, compatible-self precedence and cached identity.
- Preserve Scope's self/local/parent precedence, nearest-parent fallback, local writes,
  fresh parent cancellation reads, and rejection of malformed initial payload arrays.

Write the behavioral tests and requirement tuples first. Run a strict Abstractions
build and the focused tests against unchanged product code for causal red evidence.
Then implement only the required validations and matching manually written XML comments.
Run focused and complete Abstractions/Core tests. Exercise individually compilable
validation/isolation mutations, restore the exact source bytes, and rerun the final tests.
Review the bounded source and tests with the explicitly authorized internal red team.
Commit, tag and push the coherent checkpoint without force; the overall A+ goal remains
active, including the separately identified retry fault/getter scenarios and provider work.

## Requirement-to-oracle map

| Requirement | Concrete oracle |
|---|---|
| Required inputs | Exact exception type/parameter name before callbacks; retained identity unchanged |
| Optional payloads | Null/empty arrays remain valid; missing payloads are created and retained |
| Context identity | Compatible requests return the same context, without invoking factories |
| Scope isolation | Local adds/updates cannot mutate the parent; siblings observe their own values |
| Cancellation | Base preserves the supplied token; Scope reads the parent's current token |
| Snapshot ownership | Changing a caller-owned initial array cannot replace retained payloads |

Execution results will be recorded only after the corresponding processes terminate.

## Executed corrective sequence

- Strict unchanged-product test build: zero warnings/errors. Base30 cases:22pass8fail;
  Scope22:15pass7fail. The fifteen failures are behavioral, not compilation errors.
- Manual validation/comment correction: strict build zero warnings/errors, then whole
  Abstractions746/746 passes with the existing explicit source coverage profile.
- First frozen internal gpt-5.6-sol review identifies a genuine missing Scope absent-type
  AddOrUpdate isolation oracle, not a product defect. Add success and thrown/null-add
  atomic recovery tests manually, with their exact two requirement tuples.
- Expanded55/55 passes: Base9methods30cases, Scope10methods25cases. This adds17 test
  methods and53 cases to the existing host; no previous test is deleted or skipped.
- Repeated frozen internal review confirms the absent-branch gap is closed and finds
  no further concrete bounded finding. Both reviews release before executable edits.
- Four separately compiled mutations kill exactly1/2/3/3cases: omitted required cache,
  bypassed Base self-factory validation, bypassed Scope self/local/parent validation,
  and absent-type writes redirected to the parent. Every source restoration passes
  exact SHA256 comparison before the next mutation.
- Final byte-restored Abstractions and Core builds pass with zero warnings/errors.

Runner diagnosis: the initial combined filter placed a wildcard inside a class name;
the runner rejected it with exit5 before executing tests. The documented multi-value
`--filter-class` option with the two exact names passes. This CLI rejection is not
counted as causal red or a mutation kill. Standard targeted whitespace formatting
exits0 with the known workspace-load warning and makes no changes.

Final complete-host/coverage/format proof results and raw artifact hashes belong in
`CONTEXT_CONTRACT_PACKET.md`; they are recorded only after process termination.

## Full-host discoveries and telemetry proof plan

The first restored Core run finds a repository-removal synchronization gap in the
correlated scheduling test. The existing consumed observation precedes the repository
delete, so the test now awaits the actual removal milestone before its retained null
assertion. A compiled no-delete mutation fails that exact milestone. No saga runtime
behavior changes; manually corrected saga comments describe actual behavior.

The next restored Core run passes scheduling but finds one request-monitoring timeout
and two later sampling failures in the same serialized telemetry collection. An
immutable internal Sol review confirms the timed-out outer wait does not cancel the
underlying fake-clock operation, leaving its global sampling listener active. Its
first-receive/any-timer-change milestone is not a complete request-trace idle barrier.

The lead personally reads the complete tracker, telemetry extension, rolling timer,
active harness extension and observable test clock. Before runtime changes, four
controlled-activity tests check unrelated-trace isolation, a late child's invalidation
of an old idle deadline, preservation of the absolute maximum duration and idle
beginning only after action completion. Then correct only causally proven behavior,
replace the fixture signal with required receive count plus actual idle due-time
arming, and cancel/drain every bounded fake-clock operation during cleanup. Retain
exact tick-boundary, message/response identity and sampling assertions. Run causal
red, focused and complete green proofs, then individually compiled tracker mutations
and exact-byte restorations before the final checkpoint.

## Completed bounded proof

The four new tracker cases all fail causally against unchanged runtime source.
The corrected29-case telemetry packet passes, followed by full Core3,827/3,827.
Three additional queued-callback/disposal/constructor-failure cases deepen the proof.
The next internal review finds a real child created in a root-start listener callback;
its new eighth tracker case fails7pass1fail before assigning an unstarted root first.
After that correction all30 telemetry cases pass; repeated frozen internal Sol review
finds no further concrete bounded finding and explicitly RELEASES.

Four independently compiled tracker counterchanges are killed by one case each,
with exact source restoration between runs. Combined with four foundation and one
actual saga-deletion counterchanges, all nine selected mutations are effective.
Final exact-restored strict builds have zero warnings/errors. Full Core3,828/3,828
and Abstractions749/749 pass, zero skips; both scoped Product/Unit format verifications
exit0 unchanged with the known workspace-load warning. Requirement projections pass;
catalogues contain2,835/555 unique tuples. No test is deleted or skipped.

The lead personally reads17 complete runtime source files and manually writes the
affected functional comments. There are25 new test methods/61 cases. All25 final
source/raw-report hash bindings pass byte verification. Native host coverage and
overlapping-graph caveats are recorded in CONTEXT_CONTRACT_PACKET.md, not misreported
as provider or entire-product coverage. The original A+ goal remains active through
this coherent checkpoint; real provider, retry-gap, complete-source and wider gates
remain open. Checkpoint publication requires normal atomic push and remote verification.

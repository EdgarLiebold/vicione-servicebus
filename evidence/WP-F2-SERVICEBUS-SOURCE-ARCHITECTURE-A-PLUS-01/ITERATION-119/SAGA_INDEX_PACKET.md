# In-memory saga query and index integrity checkpoint

## Authority and acceptance boundary

This connected packet continues the original whole-product A+ API/code-architecture
goal and iteration 119. It does not close either. The secured starting commit is
`a582426c8aa17c3a582022ec87e4ba8f8bf3f1ed` on `feature/servicebus-a-plus-api`,
tagged `servicebus-a-plus-iteration-119-saga-ownership-checkpoint-2026-09-15` and
previously verified at origin without force-pushing.

Authority remains PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01 and its
hash-bound Development Slice:
`5a9cd605540cd826a756e24ca3f813cb3ce9a1eaf1fc4f881b6cecfbee9c7199`.
No Suite store, second source of truth, provider dependency, compatibility alias
or externally independent acceptance role is introduced. Protected `review/` and
`TestResults/` are neither read nor changed or staged.

## Complete personal source reading and manual changes

The Lead personally reads all nine complete source files and their comments:

| Source owner in ViciOne.ServiceBus.Sagas | Accepted responsibility |
|---|---|
| Saga/InMemoryRepository/IndexedSagaDictionary.cs | Reference membership, staged keys, unique registered IDs, materialized queries and bounded dictionary lease |
| Saga/InMemoryRepository/IndexedSagaProperty.cs | Exact property getter, nullable captured keys, reference buckets and mutable-key correctness |
| Saga/InMemoryRepository/IIndexedSagaProperty.cs | Public property-index contract and uniform wrapper argument names |
| Saga/InMemoryRepository/IStagedSagaIndex.cs | Internal capture/removal collaboration; not a public capability |
| Saga/InMemoryRepository/SagaIndexRegistration.cs | Own successful addition and once-only rollback eligibility |
| Saga/SagaQuery.cs | Required expression and lazily cached compiled predicate |
| Sagas/ISagaQuery.cs | Current referenced-state query contract |
| Saga/InMemoryRepository/InMemorySagaRepositoryContext.cs | Readonly query input priority and captured registered IDs |
| Saga/InMemoryRepository/InMemorySagaRepositoryContextFactory.cs | Message-query input priority and captured registered IDs |

All changed source comments are manually authored from the understood current
functionality. No source/test/comment/namespace/folder generator or rewriting
script is used. Read-only inventories and ordinary compiler/test/report output
do not substitute for personal reading. All four new test files are also read in
full by the Lead; their nested protocol/hostile-state fixtures are not extra public
APIs or actual provider acceptance. Type filenames match their primary types and
stay within the Sagas project's ownership boundary. The existing source-navigation
decision keeps independent SDK projects beside the Core project under `src`;
Persistence, Scheduling and Transports remain integration-project families.

## Corrected contracts and feature preservation

- Queries evaluate the actual predicate against a materialized membership snapshot,
  including current mutable secondary/non-indexed state. Secondary registration
  buckets cannot incorrectly exclude a live matching state.
- Membership and buckets use exact wrapper reference identity. Public state-value
  equality remains available; equal wrappers and changed state hashes do not collapse
  or strand repository membership.
- Removal uses captured keys, does not repeat state getters, and never removes or
  invalidates an unrelated equal reference or a same-ID replacement.
- Every required getter succeeds before publication. Duplicate IDs, invalidated
  wrappers and same-wrapper reentrant admission fail without replacing retained
  state. Failed getter/admission paths leave the same wrapper reusable.
- Canonical correlation reads the ISaga contract, including explicit implementations.
  Indexed metadata covers inherited property overrides, implemented/inherited and
  default interface members, exact hidden members, and distinct closed-generic
  getter identities. Duplicate metadata for one implemented getter is captured once.
- Unsupported static/indexer/write-only/ref-return/ref-like/pointer/function-pointer
  attributed metadata fails at construction with the exact property diagnosis.
  Direct public PropertyInfo construction validates owner, getter and matching key
  type. Nullable reference and nullable value keys are genuinely supported.
- Queries/transformations run outside owner locks and materialize their results.
  An actually removed snapshot member may subsequently change its ID. A retained
  reference, including direct wrapper invalidation without dictionary removal,
  remains subject to its registered-ID invariant.
- Both repository query entries publish captured original registered IDs, not IDs
  reread from states retired by a callback. Required input validation precedes
  pre-cancellation/acquisition. The dictionary lease has capacity exactly one.
- Admission diagnosis names the pre-publication phase separately from retained
  registration diagnosis. Four public wrapper-parameter names are manually aligned
  to `instance` in source and the packed contract; no compatibility aliases are added.

The scope is intentionally in-memory query/index correctness. Existing independent
package capabilities, referenced saga state, nullable keys, explicit-interface
states, multiple members per secondary bucket, distinct-key Count semantics and
public value equality are preserved rather than removed to simplify tests.

## Requirement and assertion evidence

Requirement `REQ-VSB-SAGA-INDEX-INTEGRITY` has 58 exact manually authored catalogue
tuples in CoreRequirements.json: 34 Dictionary methods/52 cases, 18 Property methods/
32 cases, two SagaQuery methods/cases and four repository-query methods/11 cases.
Total is 97 declared cases and 2,922 unique whole-Core catalogue tuples.

All 58 methods are personally assertion-reviewed. There are 313 direct assertion
calls (5.3966 per method), plus one physical assertion in the owned lease-check
helper. The fixtures' SendAsync/Probe are not misclassified as assertion-free tests.
Assertions cover exact references and values, Boolean/null/collection results,
negative matching, exception type/argument/phase/fault identity, getter counts,
membership side effects, retirement/replacement and once-only lease behavior.
No assertion-free/trivial-only tests, skipped cases, sleeps, timing-performance
claims or abandoned async assertions are introduced. A five-second cancellation
watchdog bounds deadlock/lease oracles, not a benchmark. The synchronous callback
cross-thread lock oracle drains its owned task after the owner lock has unwound.
Awaitable test names end in Async; synchronous test names do not. A protocol method
returning Task.CompletedTask is still an awaitable API, not a required suspension.

Manual Research → Plan → Implement follows the testing skill without its generator
or generated-test-agent machinery because the explicit PO prohibition takes priority.
The manual test-gap, assertion-quality and anti-pattern passes find and address the
keyed-filter gap below; their evidence is not inferred from raw coverage percentages.

## Causal correction and internal counterreview

Initial strict compilation succeeds with zero warnings/errors; the original 50-case
red replay exits 2 with 43 failures and seven passes. Connected stronger boundary
tests later produce an observed strict-built 86-case replay, exit 2: 20 failures,
66 passes, zero skips. These expose retired-reference invariants, live-ID projection,
input priority, inherited/interface metadata and unsupported construction shapes.
Their corrections are replayed with the previous ownership/concurrency/capability
tests: first 190/190, then final 200/200, observed native exit 0, zero skips.

The small admission diagnosis has its own strict-built two-case causal replay,
exit 2: one passing empty-repository oracle and one exact phase-string failure.
After correction the expanded 200-case host exits 0 on the accepted source bytes.

Internal Sol performs frozen read-only counterreviews. The first finds the real
retired-reference/live-ID issues; strengthened tests causally reproduce them and
the source correction closes them. The next review suggests selective keyed
filtering, direct value materialization and getter-identity/default-interface
boundaries. The final delta review finds no further concrete source/comment/API/
file-role deviation; all 15 of its own entry/exit SHA256 bindings are identical and
it explicitly RELEASES. It executes no tests or mutations and is not an external
Red Team or whole-product A+ acceptance.

## Actual individually compiled mutations

Each candidate is manually injected alone, the owning Core test project is rebuilt
strictly and observed to exit 0, the exact covering native host is observed to exit 2,
and the source is manually restored and SHA-verified before the next candidate.
All builds have zero warnings/errors and all mutation cases have zero skips.

| Candidate | One changed behavior | Direct functional failure | Failed cases |
|---|---|---|---:|
| M00 | Bypass keyed Property filter | Selective query contains both bucket members | 1/1 |
| M01 | Registration-map value equality | Equal wrappers collapse; changed hash strands membership | 2/2 |
| M02 | Secondary-bucket value equality | Keyed membership contains one instead of two wrappers | 1/1 |
| M03 | Recompute key during removal | Old bucket remains retained after key change | 1/1 |
| M04 | Treat every custom key hash as stable | Equivalent changed key no longer finds retained wrapper | 1/1 |
| M05 | Apply entries during key capture | Later getter fault leaves an initially empty dictionary nonempty | 1/1 |
| M06 | Enforce retained-ID invariant on retired reference | Legitimate Where/Select retirement raises a spurious invariant failure | 2/2 |
| M07 | Project live rather than captured Query IDs | Both query entries publish the changed retired ID | 4/4 |
| M08 | Defer Property transformation values | Result changes from original 5 to later 13 | 1/1 |
| M09 | Remove dictionary maximum lease capacity | Unowned/repeated Release fails to throw | 2/2 |
| M10 | Fold closed generic getter identity | Required getter/capture or exact getter fault disappears | 2/2 |
| M11 | Ignore Boolean predicate result | Both positive/negative queries contain nonmatching states | 2/2 |

These are 12/12 selected empirically killed candidates and twenty failing native
cases, not a whole-product mutation score. The first actual M00 experiment on the
earlier 30 direct Property cases exits 0: a genuine survivor. Its manual selective/
false-filter oracle is written and positively replayed, then the same candidate
is reinjected and directly killed. This survivor history is retained, not relabeled
as an original kill. No compilation failure or arrange-only technical exception
is credited as functional mutation evidence.

Accepted Dictionary SHA:
`035823eaa435ceca70c5eddd543dd2e18ff9dc2350f2e382e4a25aa5fde31b90`.
Accepted Property SHA:
`e3e1ee47e51382b1286cdc6e20728fd4b5d7aea01ae35e01b0d7c379b632712b`.

## Final restored-snapshot gates

Final strict Core build exits 0 with zero warnings/errors (9.69 seconds).
The complete native Core host exits 0: 4,007/4,007, zero failures/skips (24.847
seconds), including all 97 new cases and the compiled 2,922-tuple requirement
projection. Exact CTRF subset verification also finds 97/97 new cases passed.
Source-only loaded-Core coverage is 49,603/61,163 lines (81.0997%) and
17,058/23,268 branches (73.3110%). Direct class figures are Dictionary 89.6825%
line/87.5% branch, Property 92.5926% line/90% branch, and Registration 61.5385%
line/0% branch. Closure classes remain included; unexecuted rollback branches are
not hidden. Product and Unit scoped whitespace hosts both exit 0 with the known
generic workspace-load warning. Neither formatter writes files.

The fresh package script is observed to terminate with exit 1, specifically at
the final API-baseline comparison. Before that point its set-e workflow successfully
packs/checks all31packages, strictly builds/executes18journeys and three isolated
provider-testing consumers, strictly builds the API consumer and reflects all30
runtime assemblies. Every reported consumer build has zero warnings/errors.
This is not recorded as a successful complete package/API gate.

The Lead personally reads the complete388-line diff, repairing truncated output.
The additional internal Sol declaration/facade mapping review verifies57 genuinely
omitted public-type blocks are already committed internal implementations and55
relevant source owners match starting HEAD exactly. Public configuration/factories/
policy/context/observer/facade behavior remains available. The remaining paired
TYPE row is a previously sealed nested SplitFilterPipeSpecification, not a newly
sealed outer PipeConfigurator. Counts are264removed rows/oneadded row, net263.
All new saga wrapper-parameter names already match the freshly reflected snapshot.

The current FormatType collector truncates a generic nested definition at its first
backtick, losing the nested type name and conflating its generic arguments with
the enclosing type. This is a concrete assurance/tool defect, not a source change
or proof of feature loss. Blindly accepting the reflected baseline would retain
that ambiguity. The API-contract reconciliation stays open until a coherent
formatter correction, direct naming oracles and reviewed final contract are complete.
The immutable failed-gate log and18561-line snapshot are hash-bound; neither an
automatic contract update nor a fictitious successful comparison/replay is made.

The unchanged source-only coverage profile includes all loaded src assemblies,
does not exclude automatic properties or attributes, and excludes test assemblies.
Its SHA is `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.
Only a loaded graph is measured, not whole-product/provider/cloud coverage. Earlier
graph samples and the unchanged Abstractions checkpoint are not summed.

## Remaining connected work

Actual allocation/Apply-failure/dictionary-Rollback fault injection is not covered
by a later-getter admission test. Explicit saga-acquisition cancellation and exact
token normalization, factory ownership unwind, generic query/Undo, cross-provider
dispatch cleanup, timer/retry/provider paths and complete whole-source personal
reading/global gates remain open. The no-await readonly QueryAsync behavior is not
silently replaced with different synchronous-fault/cancellation semantics. No new
CS1998-suppression claim is made without evidence. Full A+, 100% API correctness,
whole-product coverage and independent/provider acceptance are not asserted.

The newly identified generic-nested API-inventory defect and committed119 visibility
baseline drift are explicit next connected assurance work before whole-iteration
acceptance. The current Git capture is an intermediate source/test backup, not a
claim that the package/API gate or whole iteration has passed. The remaining open
packet paths above are preserved in the connected worklist.

Checkpoint tag identity is
`servicebus-a-plus-iteration-119-saga-index-checkpoint-2026-09-15`.
Normal commit/tag/push follow all owned execution hosts terminating. Artifact hashes
bind actual inputs and observed raw reports. The original goal continues; no
release/final-A+ label or successful package/API-gate claim is attached to this backup.

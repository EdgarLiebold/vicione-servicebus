# Iteration 119 — governed traversal and generic API contracts

This is a connected, bounded checkpoint of the original whole-product A+ goal,
not whole-iteration acceptance, external red-team acceptance, or a claim that
the complete source/API/coverage obligations are finished.

## Authority, ownership and complete personal reading

The starting commit is `1e86575a712c5fe590f1b808eb592943d102f7ea`, already
committed, tagged, pushed and independently remote-verified. The intervening
navigation-answer turn makes no implementation change. This continuation
revalidates unfinished owned work rather than restarting or changing the goal.

Agreement, glossary, decisions, current README/order/findings and the development
slice retain the previously complete personal-read hash bindings. The Licensing
exception is not ServiceBus authority. Protected product `review` and
`TestResults` trees are excluded from implementation and validation discovery;
their contents are not read, changed, staged or used as inputs. One navigation
`rg --files` command was incorrectly scoped to the product root, despite filename
filters. It could walk protected names; no protected match or content was returned.
This is a procedural deviation, not a claim that filtered root discovery is safe.
Subsequent discovery uses explicit governed owners; the previous advisor's
separately documented protected-name traversal is not relabeled acceptance.

Before test design/editing, the complete Architecture owner binds 42 tracked
files at tree `f2c09ba1c51b57d51e9bd404eccfd0911bbb2086`: 39 C# files / 9,013
lines, project, lock and embedded requirement catalogue. This reuses verified
complete personal reading plus fully read manual deltas, not a fresh whole-src
claim. All three new test files are personally reread in full. Current C# ownership
is 42 files / 9,300 lines including the three new files; the checkpoint has 45
Architecture files, not merely the still-tracked starting 42.

The Lead personally reads ten complete production files, 888 starting lines:

- `Middleware/Configuration/PipeConfigurator.cs`, `PipeBuilder.cs`,
  `ChildSpecificationPipeBuilder.cs`, `SpecificationPipeBuilder.cs` and
  `Filters/SplitFilterPipeSpecification.cs` in Abstractions: all five partials.
- Abstractions' `ISpecificationPipeBuilder.cs`, complete send specification and
  complete publish specification implementations.
- Core's complete consume specification and message-data send topology.

These are a bounded read inventory, including previously read files, not ten
additional unique files to add blindly to a whole-product total. Six files have
manually rewritten functional comments only; signatures and executable source
statements are unchanged. All five partial files retain their namespace/type
ownership. Public nested builders are not silently removed as legacy code.

The complete 391-line inventory tool is personally read. Code, tests, comments
and this report are manually authored through patches. No code/comment
generator, automatic baseline copy or test-platform change is introduced.
The inventory host explicitly references the ASP.NET Core shared framework;
no additional NuGet dependency or product-library runtime dependency is added.

## Governed traversal correction

The original root-recursive build/project walks can enter protected/unowned
trees before an artifact filter runs. Three real consumers now use scoped
truth: restore graphs, language-version declarations and legacy test-platform
disposition. Discovery starts only at `src`, `tests`, `samples`, `benchmarks`
and `tools`; root build policies use nonrecursive discovery. Reparse points are
excluded during traversal, and inaccessible or missing owned trees fail closed.
Remaining direct root scans in Architecture use `TopDirectoryOnly`; other
recursive scans start at explicit source/test/sample/tool or artifact owners.
An actual non-following alias check finds no aliases in the five real code trees.

Independent isolated fixtures assert all 19 expected build-file paths, all five
governed families, ordinal ordering, recursion, supported extensions and exclusion
of outside poison inputs. Their `review`/result names are temporary owned data,
not product protected-tree access. All fixture cleanup targets its own temp root.

| Exact new test method | Oracle |
|---|---|
| `BuildFileTraversal_PreservesEveryOwnedTreeAndExcludesUngovernedInputs` | Exact independent 19-file array plus three outside-prefix exclusions |
| `BuildFileTraversal_DoesNotFollowDirectoryAliasesToReviewInputs` | Directory alias excluded from the exact array and prefix membership |
| `BuildFileTraversal_FailsWhenAnOwnedTreeIsMissing` | Exact `DirectoryNotFoundException` for each of five missing canonical trees |

Original broad walk, evaluated only on isolated fixture roots: strict owner
build 0, native exit 2, three failures / three cases. Corrected direct baseline:
three / three passed; strengthened missing-tree baseline: seven / seven passed.
No real protected-root broad walk is executed to obtain red evidence.

## Generic API inventory correction

The previous inventory preserves generic names but omits constraints/variance.
Type-owned `GENERIC` rows now capture complete runtime parameter flags, sorted
base/interface/dependent/constructed-type constraints, parameter nullable flags
and the C# unmanaged marker. Nested types do not repeat inherited parameters.
Method contracts are appended to the actual method declaration, in parameter
declaration order. Ordinary members receive no false generic suffix.

Nullable parameter annotations use explicit attributes first, then the nearest
lexical method/type context; unannotated value constraints use zero. The encoding
and containing-context lookup follow the primary
[Roslyn nullable metadata description](https://github.com/dotnet/roslyn/blob/main/docs/features/nullable-metadata.md).
The `AllowByRefLike` flag is verified against the installed .NET 10 reference
documentation, not discarded by a legacy special-constraint mask. Unmanaged
recording reads attribute metadata without invoking attribute constructors.

| Exact new test method | Oracle |
|---|---|
| `TypeGenericContracts_PreserveRequiredConstraintsAndAnnotations` | Eight independent exact records: unconstrained, class, class?, struct, new(), unmanaged, notnull, allows ref struct |
| `TypeGenericContracts_PreserveBothVarianceDirections` | Exact contravariant/covariant pair with distinct nullable annotations |
| `TypeGenericContracts_PreserveAllExplicitConstraintsInOrdinalOrder` | Complete base/interface constraint field and its exact ordinal order |
| `TypeGenericContracts_PreserveDependentParameterConstraints` | Two records, exact anchor record and dependent-parameter constraint field |
| `TypeGenericContracts_PreserveConstructedNestedConstraintIdentities` | Exact constructed interface and closed nested constraint identity |
| `TypeGenericContracts_DoNotDuplicateInheritedParameters` | Exact own child contract; no duplicate contracts for nongeneric or closed children |
| `MethodGenericContracts_BindConstraintsToTheirOwnDeclarations` | Exact five-row method array including names, parameter order, suffixes and constraints |
| `MethodGenericContracts_DoNotMisclassifyOrdinaryMembers` | No type-level generic rows and exact unchanged ordinary declaration |

Original inventory: strict owner build 0, native exit 2, 14 failures / 15 cases.
Corrected combined run: native exit 0, 47 / 47 passed, zero failures/skips:
15 generic cases, 24 previous direct identity cases, seven traversal cases and
the compiled requirement projection. That run binds 153 catalogue tuples;
the subsequent two runtime regression bindings bring the catalogue to 155.

## Inventory host shared-framework regression

The actual fresh package gate terminates with native exit 134 while reflecting
the SignalR package: Microsoft.AspNetCore.SignalR.Core cannot be loaded. The
ordinary Console SDK no longer implicitly supplies the ASP.NET Core framework.
SignalR remains a retained package; its API is not skipped or weakened.

The first two regression tests fail because of arrangement defects, not causal
framework assertions. The existing MSBuild helper initially omits the requested
FrameworkReference item type, and default Debug TargetPath does not correspond
to the executing Release test assembly. Those attempts are retained and not
credited as kills. The helper now requests the real framework items, and both
tests explicitly use the executing assembly's configuration.

With correct arrangements and the unchanged Console project, strict build exits
0, zero warnings/errors (8.72 seconds); native execution exits 2, two failures /
two cases (35.910 seconds). The evaluated collection contains only
Microsoft.NETCore.App, and the actual Release runtime configuration likewise
lacks Microsoft.AspNetCore.App. Both failures assert the actual missing contract.

| Exact new test method | Oracle |
|---|---|
| `InventoryHost_EvaluatesTheSignalRSharedFramework` | Real evaluated FrameworkReference identities contain Microsoft.AspNetCore.App |
| `InventoryHost_MaterializesTheSignalRSharedFrameworkInRuntimeConfig` | Actual current-configuration runtime JSON contains exactly the .NET and ASP.NET Core shared frameworks |

The project receives one explicit versionless FrameworkReference for
Microsoft.AspNetCore.App. It remains a nonpackable Console SDK tool. No Web SDK,
absolute shared-framework path, NuGet substitute, warning suppression or version
pinning is introduced. Final corrected-host results are recorded below.

Locked owner restore exits 0 with both tracked lock graphs unchanged. Strict
corrected-host build exits 0, zero warnings/errors (56.44 seconds). The bounded
native run exits 0: 48 / 48 passed, zero failures/skips (40.644 seconds), covering
all four formatter/traversal/runtime test classes. An incorrect projection-class
filter matches no additional test; this run is not credited with the projection.
The subsequent unfiltered owner run must prove all 155 catalogue bindings.
Two updated scoped whitespace checks exit 0 without writes or log output.

## Assertion and empirical mutation review

The three new test files contain thirteen test methods / 24 cases and 26 physical
assertion calls (seven traversal, seventeen generic, two runtime). No assertion-free,
trivial-only, self-referential or unawaited assertion is present. Exact array
equality is also structural/collection proof; negative membership and empty
results are meaningful exclusions. The five exception cases need no artificial
extra assertions. Pure inventory functions require no mock collaborator seam.

The two runtime tests are real build-artifact consistency checks, not mocked
MSBuild XML or a hard-coded success flag. Their exact configuration prevents
stale Debug output from serving as evidence. Across the thirteen methods, five
assert negative behavior (including the exception case), one asserts an exact
exception, and six perform structural array comparisons. Equality, string,
collection, negative, exception and structural categories are meaningful here;
26 physical assertions / thirteen methods is not a coverage percentage.
The constructed-constraint tests focus on identity, not every nullable byte in
constraint arguments. That wider annotation obligation stays explicitly open;
it is not mislabeled an empirically observed survivor.

Every selected candidate is individually patched, strictly compiled before
native execution, observed terminal, then immediately byte-restored. All owner
builds exit 0 with zero warnings/errors. No compiler/arrangement failure counts
as a kill; no static candidate is described as an observed survivor.

| Candidate | Single behavioral change | Failed / executed cases | Native exit |
|---|---|---|---|
| scope-M00 | Follow reparse aliases | 1 / 3 | 2 |
| scope-M01 | Omit tools from owned families | 2 / 3 | 2 |
| scope-M02 | Recurse from the real-root argument | 2 / 3 | 2 |
| scope-M03 | Silently skip missing owned trees | 5 / 7 | 2 |
| generic-M00 | Discard AllowByRefLike | 1 / 15 | 2 |
| generic-M01 | Reverse ordinal constraint order | 1 / 15 | 2 |
| generic-M02 | Repeat inherited type parameters | 1 / 15 | 2 |
| generic-M03 | Omit method contracts through an impossible zero-arity gate | 1 / 15 | 2 |
| generic-M04 | Ignore unmanaged metadata | 1 / 15 | 2 |
| generic-M05 | Ignore lexical nullable contexts | 2 / 15 | 2 |

Ten selected kills and seventeen failed cases are bounded evidence, not an
exhaustive mutation score for the repository. Restored SHA-256 values are
`326e412c06597e85b1c47f7557a6dcfc88a3546d736725504b3b2f6cbd726603`
for RepositoryLayout and
`860384083a31212ecde0fa0fb98572651d935320963edc60d9fffebc398da447`
for the inventory tool. Both are revalidated before final owner execution.

The prescribed read-only Roslyn pairing analyzer is executed once at the safe
tool root. Its JSON reports one source, zero in-scope tests, no suggested path.
Architecture tests are outside that scope; the result is not absence of real
tests or a line/branch coverage statement. No wider unsafe pairing scan runs.

## Final validation and remaining whole-goal work

The first complete strict owner build exits 0 with zero warnings/errors (5.91 seconds).
Complete native Architecture execution exits 0: 356 / 356 passed, zero failed
or skipped, 2m 51.072s. This includes the previously unsafe three real-root
consumers, all new cases, the embedded projection and the existing bidirectional
async-contract convention check. Native handle and final CTRF agree.
Three scoped whitespace checks terminate exit 0 without writes; logs are empty.
Tracked diff-check exits 0. No new warning suppression or preprocessor directive
is introduced. The earlier lost-observation build is not assigned a fabricated
native exit: its completed log and absent own process permit a new definitive run.
This 356-case result predates the shared-framework regression and is not the
final acceptance evidence for the subsequent runtime binding correction.

After the corrected fresh package execution, final strict owner build exits 0,
zero warnings/errors (46.44 seconds). The unfiltered native Architecture owner
then exits 0: 358 / 358 passed, zero failures/skips (2m 48.352s). The final CTRF
independently records 358 tests / 358 passed and agrees with the terminal handle.
All 155 catalogue tuples, both shared-framework regressions, the three real-root
traversal consumers and bidirectional asynchronous naming are included. No
executable source, test or project change occurs after this final green run.

The corrected actual fresh gate exits 1 at the final baseline comparison, not
at runtime reflection. The unchanged script reaches that comparison only after
31 fresh packages, eighteen developer journeys, three isolated provider-testing
consumers, the locked thirty-package API consumer and successful inventory of
all thirty runtime assemblies. All reported strict consumer/tool builds have
zero warnings/errors. The generated contract has 20,045 lines; its SHA-256 is
`96436e3b855dee3c678da4430a01c0947b14126227faff5a37be040455790676`.
The temporary package feed is removed by the script's owned cleanup, not retained
or reused as if it were a subsequent fresh run. The gate is not reported green.

Read-only output diagnostics count 1,484 type-generic records and 2,639 generic
method suffixes. The old text has 3,287 type declarations; fresh output has 3,230.
Twenty-five repeated old identity keys across nested-generic families have no
equivalent identity collisions in the fresh text. A first dictionary diagnostic
overwrites old colliding keys and therefore cannot prove complete block pairing
or feature-preserving baseline disposition. Further reconciliation must retain
all colliding old blocks and manually review actual signature/visibility changes.

The corrected collision-preserving diagnostic retains all 3,287 old blocks and
all 3,230 fresh blocks. Of 3,190 shared singleton identities, 1,587 are identical,
1,602 differ only by the newly emitted generic records/suffixes, and one needs
separate identity review: DynamicFilter<TInput,TKey> references its nested
KeyOutputFilter<TOutput>. Six old collision families retain all 25 extra old
blocks. These are structural diagnostic counts, not a manual review of every
generic constraint, an acceptance decision or a reason to auto-copy a baseline.

Package-baseline reconciliation is still open. The committed old baseline is not
copied from generated output or silently accepted. Full Architecture checks do
not replace the actual fresh package/journey/consumer/baseline gate. Wider
nullable constraint-argument annotations, member annotations/custom modifiers
and full C# API metadata require further assurance; these generic tests do not
prove every API or every new parameter across all packages.

Production builder required-input guards and whether consume/base/parent
validation should aggregate are connected contract follow-up work. The original
saga factory cancellation/unwind, staging allocation/rollback, query/Undo,
cross-provider primary-failure preservation, timer/retry/provider acceptance,
all-source personal reading, all-comment/type/file/folder/legacy/dummy/directive
reviews, global code/branch coverage and whole-product A+ gates remain active.
No fresh whole-product coverage, 100% correctness or cloud acceptance is claimed.

## Checkpoint and next connected work

The owned checkpoint consists of sixteen modified tracked files and five
manually authored new files. The new files are three Architecture test files
and this packet/hash manifest. Exact paths are staged; unrelated workspace work,
protected product inputs, local build artifacts and private raw outputs are not.
The normal commit and annotated tag use the governed-traversal/generic-contract
checkpoint identity. Branch/tag are pushed atomically without force and checked
against origin independently before the user handoff claims remote security.

Next: finish collision-preserving actual API reconciliation, manually inspect
the nested DynamicFilter/StateMachine/connector families and retired visibility
blocks against retained functionality, strengthen remaining metadata obligations,
then disposition the baseline deliberately. Complete the connected pipeline
builder input/validation review with its full test-owner read and causal tests.
Only 230 lines of DynamicFilter.cs were opened in this checkpoint's diagnostic;
that partial read is not counted as another complete source read or comment audit.

# Iteration 119 — packed API type identity

## Authority and scope

This bounded packet continues the active whole-product A+ goal and
`PO-2026-09-08-SERVICEBUS-SOURCE-ARCHITECTURE-A-PLUS-01`. The secured starting
commit is `fd11887df54fbf5731a50e4626ef4224b5f43c6e`; its annotated remote tag is
`servicebus-a-plus-iteration-119-saga-index-checkpoint-2026-09-15`.
No protected review input, provider capability or package feature is changed by
this tooling packet. The one production-file change clarifies comments only.
No complete A+ or external acceptance is claimed.

## Personal reading and independent oracles

Before modifying tests, the Lead personally read all 41 tracked Architecture
project files at starting tree `312e8d888682e4264f9c36e006c8a94457d4aada`:
38 C# sources / 8,900 lines, the project file, 287-line embedded requirement
catalogue and 617-line locked graph. Truncated outputs were reread to EOF in
smaller ranges, not accepted as complete reads. The complete 318-line existing
formatter and 226-line package script, effective root/test build contracts,
central dependencies, native configurations/workflow and Unit/Engineering
solution memberships were also personally read. This bounded inventory is not
the entire source-code read required by the original goal.

The original formatter trims metadata at the first generic backtick. A generic
parent's child name is lost; inherited and own arguments are collapsed into one
segment. CLR vectors and non-vector rank-one arrays also collapse to `[]`.
Tests use explicitly written expected strings and actual reflected fixture types,
not the formatter's algorithm copied into a test oracle.

The existing manually authored CLI moves to a dependency-free, nonpackable regular
SDK console project. An internal friend seam binds direct Architecture tests to
the real executable code. Both solution closures include this tool; shipping
product projects remain separate. The package script still compares the complete
actual inventory byte-for-byte with the committed contract. Its automatic
baseline-update switch is not used for acceptance or manual source edits.

## Procedural counterreview qualification

The internal Sol design reviewer did not complete the mandatory reading and
therefore supplied only advisory reasoning from the design description. It is
not a completed file-backed review, a frozen acceptance or external red-team
approval. Its initial broad navigation accidentally enumerated protected
`review/**` README path names. It reports no protected contents read, no writes,
no builds/tests. This procedural scope error is retained explicitly; those paths
are not task input. Subsequent work uses exact authorized paths only.

The advisory identified useful partitions: closed leaf arguments rather than
open declaring-type arguments, child-name/arity collisions, and a zero-own-arity
middle segment. The Lead manually added the corresponding cases.

## Execution status

Private raw output owner: `/private/tmp/vsb-iteration119-api-identity.mFjlT2`.
The scoped normal restore exited 0. Reviewed lock delta adds only the tool's
project identity; the existing tool lock remains an empty net10.0 graph (NuGet
changes its final-newline representation). No NuGet dependency/version changes.

The red-stage formatter SHA-256 is
`1a816e9e6a12265b8240f99b425ead85f2613139f744c28356966e7bb9ab9eb5`.
Its rendering was intentionally still faulty after project migration. The first
build exited 1 because the new test file lacked its explicit `using Xunit`;
52 unresolved attribute errors, zero warnings. This compile failure is not causal
test evidence. After that import correction, the same faulty formatter's strict
owner build exited 0, zero warnings/errors (6.58 seconds).

The first native invocation exited 5 for an unsupported `--report-ctrf` option;
it executed no tests and is not red evidence. The verified xUnit 4/MTP option is
`--report-xunit-ctrf`. With that option, the real original-formatter test host
exited 2: 24 cases, 14 direct functional assertion failures, 10 passing cases,
zero skips. This is the causal red proof. Keep this distinction in subsequent
reports rather than treating any nonzero command exit as a behavioral failure.

The manually corrected formatter strict owner build exited 0, zero warnings/errors
(6.88 seconds). The corrected formatter SHA-256 before mutations is
`b2e42d62b8dbe025696acc3fa8836d6c24bb06f81806ee99f93ad1586c322036`;
the seven manually authored test methods / 24 cases have source SHA-256
`9b169f88ad57ceec31c5955d6d6e2cfb8f1ae8a9f71f9ea0b7f34abf66da496f`.
The shell script syntax check exited 0.

The first corrected native bounded run exited 0: 31/31, zero failures/skips
(1 minute 29.403 seconds). In addition to all 24 direct formatter cases, it executes
all four DeveloperJourneyArchitectureTests, the compiled requirement projection
and both exact Unit solution closure checks. The final embedded catalogue has
142 tuples, including seven new method bindings; the compiled projection passes.

| Candidate | Single causal change | Strict owner build | Native direct cases | Result |
|---|---|---|---|---|
| M00 | Reset the generic argument cursor after each segment | exit 0, zero warnings/errors, 6.50 s | exit 2; 8 failed, 16 passed, 0 skipped | Killed by exact argument-placement strings |
| M01 | Return only the first metadata segment | exit 0, zero warnings/errors, 6.13 s | exit 2; 16 failed, 8 passed, 0 skipped | Killed by exact declaring/child names |
| M02 | Recognize non-vector arrays at impossible rank zero | exit 0, zero warnings/errors, 5.47 s | exit 2; 1 failed, 23 passed, 0 skipped | Killed by `System.Int32[*]` versus `System.Int32[]` |

Each candidate is applied manually, compiled in the actual Architecture owner,
run to an observed terminal native exit, and manually restored. SHA-256 after
each restoration exactly matches `b2e42d62...c322036`. No compile, discovery,
arrangement or unsupported-option failure is counted as a mutation kill. These
three selected candidates are not an exhaustive repository mutation score.

The existing 59-line `PipeConfigurator.cs` API anchor was personally read in
full. All its comments are manually clarified to describe registration order,
required specification admission, validation enumeration and empty-pipeline
behavior. Its reviewed diff contains comments only; no signature/runtime changes.
File/folder/namespace placement of the wider partial type remains wider owned work.

Final restored strict owner build exits 0, zero warnings/errors (9.13 seconds).
Final native bounded run exits 0: 31/31, zero failures/skips (1 minute 25.924 s).
Tool, three edited Architecture sources and the production comment-only anchor
each pass scoped whitespace verification (three commands, all exit 0, empty logs,
no writes). `git diff --check` exits 0. No owned build/test/formatter process is
left running before checkpoint creation. Raw and canonical identities are bound
in [the artifact manifest](PUBLIC_API_TYPE_IDENTITY_HASHES.md).

The testing skills enforce manual red-first behavior partitions, direct assertions,
native MTP option verification and compilation before credited mutation results.
The build review keeps the new owner minimal, dependency-free and nonpackable;
it adds no redundant framework override, side-effecting MSBuild target or warning
suppression. No source/comment generator is used.

## Remaining obligations

Root-recursive build-policy enumeration in RepositoryGraphTests and
VerificationCapabilityDispositionTests can enter protected review inputs. Do
not execute those methods or claim a full Architecture run before governed-root
enumeration is corrected and proved. Actual newly packed API differences require
personal review and manual baseline disposition; the previous final package
comparison failed and is not relabeled as passed. Wider saga/factory cancellation,
ownership unwind, staged rollback, timer/retry/provider and whole-product full-read,
coverage, API, comments and layout obligations remain active.

The full formatter read also establishes a wider contract-inventory limitation:
generic variance/constraints are not emitted. The public `PipeConfigurator<TContext>`
declares `where TContext : class, PipeContext`, whereas existing TYPE rows carry
no constraint clause. This bounded type-identity repair does not certify complete
API metadata, nullable contracts, custom modifiers or unsupported CLR type forms.
Review those axes before calling the package inventory a complete A+ API contract.

# R0-PY findings

Cohort `R0-PY`, baseline commit `ae73c6da748e3bc3257dffa4971ee8680e086207`.
Each finding names how it was established: **measured** (a command was run and its output is
quoted), **read** (derived from the source, which is quoted), or **inferred** (a conclusion from
read code that was not executed here, and is marked as such).

---

## F-01 — The required "Identity tool self-tests" step is red at this baseline, and the cause is a rule that counts untracked files

**Measured.**

```
$ python3 -m unittest discover -s tools/identity -p 'test_*.py'
Ran 99 tests in 4.637s
FAILED (failures=2)
```

Both failures are in `tools/identity/test_change_list.py`:
`test_accepts_the_generated_document` and `test_reports_the_number_of_rows_it_classified`, each
asserting `0 == change_list.main([...])` and getting `1`.

Cause, measured directly, with a second data point as a control:

```
$ git ls-files --others --exclude-standard | wc -l   -> 9
$ python3 tools/identity/change_list.py --repository .
FAIL change-list line 15 differs
  expected: | Added | 1240 |
  actual:   | Added | 1231 |

... one more untracked file later ...

$ git ls-files --others --exclude-standard | wc -l   -> 10
$ python3 tools/identity/change_list.py --repository .
FAIL change-list line 15 differs
  expected: | Added | 1241 |
  actual:   | Added | 1231 |
```

The expected count moves one-for-one with the untracked file count, and the tracked document
says 1231, which is correct for the committed tree. `change_list.classify()` builds its
"present" set from `identity_gate.commit_candidate_files()`, which is
`git ls-files --cached --others --exclude-standard`, so **every non-ignored file in the working
tree counts as Added** — including this cohort's own R0 evidence directory.

Two consequences, pointing in opposite directions:

* On a fresh CI checkout nothing is untracked, so the step is green there. The redness measured
  here is caused by R0 itself and is not a pre-existing defect of the committed tree.
* The Apache-2.0 section 4(b) change evidence therefore **cannot be checked while any work is in
  progress**, which is exactly when somebody would want to check it. A successor that keeps this
  promise should classify tracked files only and report untracked ones separately.

**QUESTION for the Lead:** does the successor change-evidence gate classify the committed tree
or the working tree?

---

## F-02 — Four identity self-test cases require a Git object a depth-1 CI checkout does not fetch

**Read, not executed.** `.github/workflows/build.yml` checks out with
`- uses: actions/checkout@3d3c42e5...` and no `with:` block, so `fetch-depth` is the default `1`.

`identity_gate.baseline_archive()` runs
`git archive --format=tar 1de4bf6eb45c406da3cd6f26bdab6ed6d5aeefbc` and raises `RuntimeError` on
a non-zero exit. That commit is an ancestor of `HEAD` (measured here in a full clone:
`git merge-base --is-ancestor` returns 0) and is therefore **absent from a depth-1 fetch**.

Four discovered cases reach it:

| Case | Path to `baseline_archive` |
|---|---|
| `test_change_list.Reading_the_canonical_change_list.test_accepts_the_generated_document` | `change_list.classify` |
| `test_change_list.Reading_the_canonical_change_list.test_reports_the_number_of_rows_it_classified` | `change_list.classify` |
| `test_identity_gate...test_rejects_unchanged_commentable_format_exception` | `validate_format_exceptions -> baseline_paths` |
| `test_identity_gate...test_rejects_commentable_readme_format_exception` | `validate_format_exceptions -> baseline_paths` |

I did not run CI, so I do not claim the job is red today — this is an inference from the checkout
default plus the code. It matters for the successor either way: **any assurance bound to the
imported upstream baseline needs the baseline objects, and the successor's CI checkout has to say
so explicitly.** If the provenance promises are retained at all (F-08), the workflow needs
`fetch-depth: 0` or an explicit fetch of that one commit.

---

## F-03 — Three lock-file cases are green whenever the restore fails for any reason at all

**Read.** `tools/ci/tests/test_locked_restore.py::LockedRestoreTests` has six cases that run a
real `dotnet restore`. Three assert only that the exit code is not zero:

```python
def test_a_plain_restore_is_locked_without_asking_for_it(self):
    self.sabotage_lock_file()
    result = restore(self.root, PROBE_PROJECT)
    self.assertNotEqual(0, result.returncode, "...locked mode is not the default")
```

— and likewise `test_a_changed_lock_file_fails_the_locked_restore` and
`test_a_changed_package_graph_fails_the_locked_restore`.

A non-zero exit is produced by the sabotage the case intends **and** by a missing SDK, an
unreachable feed, a wrong working directory or a malformed `NuGet.config`. None of the three
asserts anything about *why* the restore failed. This is the "the probe must be red for its own
reason" failure mode: the probe is red, but the message never names the assurance under test.

Aggravating context, **read**: the `policy` job that runs these cases has **no `setup-dotnet`
step** — its only steps are `checkout`, `policy_validator.py`, and the two `unittest discover`
commands. `global.json` pins `10.0.302` with `"rollForward": "disable"`. If the runner image does
not carry that exact SDK, all six cases meet a failing `dotnet`, three pass for the wrong reason
and three fail for a reason unrelated to lock files.

**Consequence for the disposition:** the *promise* (a deleted or edited lock file cannot reopen
the package graph) is real and is carried as an `MSBUILD_RULE` row
(`MsbuildPolicy.check_restore_lock_files`). The *proof* must be rewritten so it asserts on the
NuGet diagnostic (the NU1004/NU1403 class of messages), not merely on a non-zero exit.

---

## F-04 — Two behavioural broker assurances have no named successor and will disappear silently

The highest-risk items in the cohort: assurances **about the product** that are proved from
outside the test process, for which Lead plan section 12 names no owner.

### F-04a — `assert_one_refusal_per_vhost` (`tools/ci/fixtures/broker_logs.py`)

Counts, in RabbitMQ's own freshly collected log, how many `resource_locked` channel exceptions
each `test-exclusive-*` virtual host saw, and fails the run unless the answer is exactly one per
vhost. Its own reasoning: *"Reply code 405 is permanent: asking a second time cannot change the
answer, so a second refusal in the same virtual host is a retry loop that should not exist, and
no refusal at all is a spec whose precondition never came about. Both were previously invisible
to the suite — the specs counted endpoint faults and watched a quiet window, which says nothing
about how often the broker was actually asked."*

It also refuses a vacuous match: if no matching vhost appears in the log, that is red, not green.

This is a behavioural claim about the RabbitMQ transport — that the client asks exactly once —
and no in-process xUnit assertion can make it, because the evidence is in the broker's log. Only
the `rabbitmq` category declares it (`oneRefusalPerVhost: "test-exclusive-*"`).

### F-04b — `broker_logs.capture_logs` and the log digest binding

The broker log is collected before `down -v` (after which it is gone for good), written under the
run root, hashed, carried into the fixture record and rehashed by the receipt reader. Nothing in
MTP or xUnit collects a container log.

**QUESTION for the Lead:** which owner in wave C3 takes these two? Without an explicit assignment
they are dropped by omission, which section 3.1 forbids ("auch ihre sinnvolle Schutzwirkung darf
nicht still verschwinden").

---

## F-05 — The process-tree takedown and the survivor census have no successor and no named gap

**Read.** `tools/ci/verification/process_tree.py` carries three controls with named, measured
counterexamples:

* the child runs in a session of its own and the **group** is signalled, because "a parent that
  exits is not a group that exited: waiting for the child alone returned a clean takedown in a
  fifth of a second with a member still running";
* **"three such trees survived more than thirteen hours on this machine because nothing ever
  asked them to stop"**;
* the survivor census recognises the SDK's shared compiler by its exact file in its exact
  directory, because the substring rule it replaced was defeated by
  `python hold-port.py --label VBCSCompiler`, which "was running and the census reported nothing
  at all".

I disposed all three as `REMOVED_WITH_RATIONALE` on the ground that MTP *is* the process once no
external tool starts it. That rationale holds only if the release-effective profiles are started
as MTP executables. If they are started with `dotnet test`, an MSBuild node tree still exists, a
hang is still possible, and nothing will report a survivor.

**QUESTION for the Lead:** is the release-effective run `dotnet test <profile>.slnx` or the MTP
executable directly? If the former, this cohort's process-ownership promise needs an owner.

---

## F-06 — Removing the receipt removes a real anti-fabrication control; the idea should be carried into the Ebene-1/2 binding

**Read.** `tools/ci/verification/receipt.py` exists because "every derived number was read back
exactly as the caller wrote it, so a document claiming three expected identities, one executed
identity and nothing missing was accepted against the right commit, the right tree and the right
model hash". It answers with three disciplines:

1. **Every derived field is recomputed** from the record's primary facts and compared.
2. **Every field is classified exactly once** into `CHECKED_AGAINST_THE_MODEL`, `DERIVED_FIELDS`,
   `PRIMARY_FACTS` or `DECIDED_HERE`, and
   `test_the_classification_covers_every_field_exactly_once` asserts the classification covers
   `CATEGORY_FIELDS` exactly — so adding a field to the record is a decision about what checks it.
3. **The shape is closed in both directions**: a missing field cannot be checked at all, and an
   unknown one is either another schema being read as this one or a claim nobody checks.

The receipt itself is correctly removed — Lead plan section 2 item 6 forbids the second verdict it
audits. But discipline 2 is exactly what section 12.1 Ebene 1/2 needs: the frozen obligation set
and the per-assembly projections are documents with fields, and "a field nobody classified is a
field nobody checks" applies to them verbatim.

**Recommendation:** carry `CHECKED_AGAINST_THE_MODEL / DERIVED / PRIMARY / DECIDED_HERE` and its
self-assertion into the projection schema. A design inheritance, not a code copy.

---

## F-07 — The public-API-surface promise is real, is not currently gated, and has a native replacement

**Read plus measured.** `tools/ci/collect_api_assemblies.py`, `tools/ci/compare_api_surface.py`
and `tools/ci/api_surface.cs` together prove that the public messaging API did not move. They
carry five named counterexamples, four about provenance rather than about surfaces:

* a glob over one target framework silently dropped two assemblies, one of them shipped;
* forcing `TargetFramework` as a global property overrode the declaration under test;
* a real `ViciOne.ServiceBus.dll` copied over the name `ViciOne.ServiceBus.Abstractions.dll` was
  measured as Abstractions, with the wrong 13292 members and exit code 0 — hence keying on
  `AssemblyDefinition`;
* a counterfeit assembly in a clean checkout's `bin` passed a green commit check with an empty
  status — hence the tool creating its own detached worktree;
* a `DROP` mutation removing one overload of `IPublishEndpoint.Publish` left the surface
  unchanged — hence numbering repeated member lines.

**Measured:** no required job invokes any of the three. The promise is currently unenforced.

**Recommendation, and a QUESTION.** The native replacement is
`Microsoft.CodeAnalysis.PublicApiAnalyzers` (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`,
RS0016/RS0017) wired centrally with `TreatWarningsAsErrors` — per-project, fail-closed, no second
process, and no provenance problem at all because the compiler reads the compilation rather than a
copied DLL. That is `MSBUILD_RULE` and needs no allowlist entry.

If the Lead prefers to keep the Python tool, the allowlist entry is prepared in the ledger:
path `tools/ci/collect_api_assemblies.py` + `tools/ci/compare_api_surface.py` +
`tools/ci/api_surface.cs`; owner Team 1 integrator; purpose public-API-surface provenance;
forbidden test effect **it must never discover, count, classify or judge a C# test and must never
produce a PASS a gate reads**; native positive probe *"add a public member, the analyzer raises
RS0016"*; discriminating negative probe *"rename a private member, nothing is raised"*. My
recommendation is the analyzer, because a retained Python tool needs an xUnit owner it cannot have
without reimplementing itself.

---

## F-08 — The identity/provenance scanner is the only genuinely retention-worthy Python, and it cannot keep its own proof

**Read.** `tools/identity/identity_rules.py` plus the scanning half of
`tools/identity/identity_gate.py` are the strongest work in the cohort:

* 19 detector families over 41 mapping rules, bound by a SHA-256 over a canonical projection;
* **41 isolated-effect mutants** (each rule alone must change its probe; the same rule removed
  must not) and **41 removal mutants** against the digest — so a rule that is present but inert
  and a rule that is quietly removed are two different, both-detected failures;
* four scanner channels: path, UTF-8 text, ASCII binary, UTF-16 binary at both byte offsets and
  both endiannesses;
* the twenty header roots are asserted against an **independently written list** inside the test,
  with the reason stated: *"A test that iterates the product list shrinks with it: delete a root
  and the loop simply runs one case fewer, still green."*
* the legal provenance paths are **masked, not skipped**, and an authorised context occurring zero
  or more than one time is itself a finding — so `README.md` cannot become a hiding place;
* a harmless-word guard (`amount`, `format`, `empty`, `vsbuild`, `observable`) keeps the detector
  usable.

This is a legal obligation (Apache-2.0 attribution and the fork's identity separation), not a test
obligation, so it survives the ban on second test judges.

**The problem.** Lead plan section 3.1 requires a retained Python tool's correctness to be proven
"ausschliesslich durch einen source-eigenen xUnit-/MTP-Testowner oder eine native MSBuild-Regel" —
and its 57 Python self-tests are forbidden by 12.2 item 12. A retained `identity_rules.py` would
therefore be an allowlisted tool with **no proof at all**.

**Recommendation:** port the detector to C# in
`tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests`. The byte scanner is small; the
registry is a data table; the 41+41 mutants become `[Theory]` cases over the same table. That
converts the disposition from `RETAINED_ENGINEERING_TOOL` to `XUNIT_ARCHITECTURE_TEST` and removes
the allowlist entry entirely. The ledger currently records the conservative reading
(`RETAINED_ENGINEERING_TOOL`) with this recommendation in its notes.

**QUESTION for the Lead:** port to C#, or allowlist with an explicit acceptance that the tool has
no executable proof?

---

## F-09 — A rule that ran over nothing and passed for every repository — the pattern, not just the instance

**Read.** `ModelPolicy.check_executed_floor` carries its own confession:

> *"This rule read a top level 'categories' object, which the verification model has not had since
> it replaced the two files before it. The loop therefore ran over nothing at all and the check
> passed for every repository, including one with no floor anywhere."*

The same shape appears twice more in this cohort:

* `Every_rule_this_validator_can_report.policy_sources()` — *"The rules moved out of the entry
  point into `policies/`, and this case went on reading the entry point: it found no rule at all
  and would have passed on an empty set."* It now asserts
  `self.assertTrue(reported, "no rule was found at all, so this case proves nothing")` first.
* `Every_rule_this_validator_has_runs.test_every_rule_of_every_policy_module_is_one_this_validator_composes`
  — a whole policy class can be left out of the composition and every remaining rule still has its
  case.

The countermeasure the cohort settled on is worth naming as a rule for the successor: **a check
that iterates must assert that it iterated over something.** In the new architecture the
equivalent is an architecture test that finds zero types, or a projection that resolves to zero
obligations. Both must be red, not green. Directly relevant to Lead plan 12.2 item 8 (the
zero-lock) and item 17 (the partition duty).

---

## F-10 — Two production gates are named `test_*.py` and are imported as test modules

**Read plus measured.** `tools/identity/test_result_gate.py` and
`tools/identity/test_failure_partition_gate.py` are production gates. The pattern
`unittest discover -p 'test_*.py'` matches their filenames, so the required policy job imports
both as test modules. They declare no `TestCase`, so they contribute zero cases — measured:
`tools/identity` yields 99 discovered cases, which equals its 100 class-level definitions minus
the one nested class, with no contribution from these two files.

It is harmless today and it is a trap. `ModelPolicy.check_every_tool_test_module_runs` globs
`tools/**/test_*.py` and demands that a required job start each match; it therefore treats two
production gates as proofs that must run, and would be satisfied by a job that "runs" them. The
collision also explains the doubled self-test names `test_test_result_gate.py` and
`test_test_failure_partition_gate.py`.

Relevant to 12.2 item 13: after removal, no file under `tools/**` may be named in a way that makes
a discovery mechanism import it. Worth a naming rule in the successor.

---

## F-11 — Four hand-written project lists will not inherit a new project

**Read.** Four rules operate over lists a new project does not join automatically:

| Location | Hand-written list |
|---|---|
| `policies/__init__.py::REQUIRED_CATEGORIES` | eleven job names |
| `policies/fixtures.py::check_no_hardcoded_broker_endpoint_in_tests` | four test project directories |
| `policies/fixtures.py::check_transport_operations_take_a_lease` | two source files with their exempt operations |
| `policies/__init__.py::ROSLYN_COMPONENT_PROJECTS` / `ANALYZER_PACKAGE_PROJECT` | three project paths |

The last is deliberate and correct — the exception is granted *by path*, and the cohort proves both
directions (nobody else may have it; those three must keep being what it was granted for). The
first three are not: a fifth test project that hardcodes `activemq://localhost:61616` is invisible
to the rule, and a new required category is invisible to `check_required_profile`.

In the successor, "the four projects" must become "every test project of the LocalIntegration
profile", derived from the profile solution. Otherwise the rule is inherited in a weaker form than
it has today.

---

## F-12 — `map_text` carries a special case for a file wave F2 deletes

**Read.** `identity_rules.map_text()` injects, into any text containing `*.log*`:

```
!src/ViciOne.ServiceBus.TestFramework/ViciOne.ServiceBus.TestFramework.log4net.xml
.testagent/
```

so a renamed log4net configuration stays stageable under the general log ignore rule. Lead plan
section 2 item 3 and wave F2 remove `src/ViciOne.ServiceBus.TestFramework/**` entirely. After that
removal the rule writes a `.gitignore` exception for a file that does not exist, and
`derive_refactor_conformance()` — which compares the target byte-for-byte against `map_text` of the
baseline — will demand that the line be present in `.gitignore`.

If `identity_rules.py` is retained (F-08), this special case must be disposed together with the
TestFramework removal, or the identity conformance derivation contradicts wave F2.

---

## F-13 — Four places name an option that does not exist

**Measured.**

```
$ python3 tools/ci/verify.py --selection core --record-expected
verify.py: error: unrecognized arguments: --record-expected
$ ls tools/ci/verification_model.py
ls: tools/ci/verification_model.py: No such file or directory
```

Stale references to `verify.py --record-expected`:

1. `docs/build.md:144` — `python3 tools/ci/verify.py --selection core --record-expected   # regenerate an expected set`
2. `build/verification/VERIFICATION_MODEL.json`, `notes.expectedIdentities` — *"generated from a
   clean run of that category by tools/ci/verify.py --record-expected and never edited by hand"*
3. the three-line header of **ten of the eleven** anchor files under
   `build/verification/expected/` (all but `diagnostics.txt`)
4. this cohort's own task brief

Plus one stale path: `docs/build.md:130` names `tools/ci/verification_model.py`; the module is
`tools/ci/verification/model.py`.

Two consequences beyond tidiness:

* The header split is why the eleven files carry 3 or 4 documenting header lines. **Re-recording
  any of the ten would change its bytes and therefore its SHA-256**, breaking the Lead-bound digest
  in section 9 — so the ten anchors are effectively frozen against regeneration by the current
  tool. Whoever regenerates one must expect a new hash and a new Lead binding.
* `docs/build.md` is not among the eight root markdown files section 2 item 9 governs, but it
  carries two instructions that cannot work. It should be disposed with the rest of the docs.

---

## F-14 — One tracked `.py` outside `tools/`

**Measured.** `git ls-files '*.py' | grep -v '^tools/'` returns exactly one path:
`evidence/WP-F2-SERVICEBUS-CI-BASELINE-03/records/0077/api-surface/collect_assemblies.py`.

It is a frozen evidence copy of an earlier generation of `collect_api_assemblies.py`. Lead plan
12.2 item 12 forbids "kein Python-Testmodul, keine Python-Testframeworkabhaengigkeit und kein
`unittest`-/`pytest`-/`nose`- oder gleichartiger Testaufruf im finalen Baum oder in CI", and
section 3.1 says "Nicht allowlistete Python-Dateien sind im Endzustand verboten".

Read: it is not a test module, it makes no test-framework call, and it is immutable evidence of a
closed work package. It should be **explicitly excluded** by the successor rule (evidence records
are frozen), not deleted — deleting it would rewrite a bound record. But the rule has to say so,
or a literal reading of "no unallowlisted Python file" deletes it.

**QUESTION for the Lead:** does the "no unallowlisted Python file" rule apply under `evidence/**`?

---

## F-15 — Nothing maps the 3114 anchor identities onto the new obligation IDs

**Read.** Lead plan section 9 binds the eleven anchors by path, SHA-256 and identity count and
requires R0 to reconcile "pro Kategorie und insgesamt". Section (d) of `RECONCILIATION.md` does
that: all eleven agree, and the total is 3114.

What does not exist is the other half. The anchors carry NUnit-derived identity strings
(`Namespace.Fixture("arg").Case(True)`). The successor carries `obligationId` plus `variantKey` on
xUnit cases. **No artefact named in the plan maps one onto the other**, and 12.2 item 9 requires
every ledger obligation to be connected to an executed test/variant ID.

Concretely: `build/verification/expected/core.txt` holds 1873 identities; the Core cohort will
produce N obligations. Without a mapping, "anchor identities not covered by a ledger row" — which
the cohort reading rules section 6 requires to be listed and explained — cannot be computed for any
cohort, including this one.

**QUESTION for the Lead:** is a per-cohort `ANCHOR_TO_OBLIGATION.tsv` (old identity ->
obligationId, or -> an explicit removal rationale) part of the R0 checkpoint? Without it 12.2 item
9 has no input.

---

## F-16 — The disposition vocabulary of this cohort is not the one in the cohort reading rules

**Read.** `COHORT_READING_RULES.md` section 5 names four terminal dispositions —
`REPLACED_EXECUTING`, `REMOVED_WITH_PRODUCT_CAPABILITY`, `BENCHMARK_ONLY`, `DIAGNOSTIC_ONLY` — and
requires `PROPOSED_<disposition>` in R0. Lead plan section 3.1 names a different five for the
Tooling-Assurance-Register: `NATIVE_MTP`, `MSBUILD_RULE`, `XUNIT_ARCHITECTURE_TEST`,
`RETAINED_ENGINEERING_TOOL`, `REMOVED_WITH_RATIONALE`.

`LEDGER_DRAFT.jsonl` uses the register vocabulary, because this cohort *is* the register and its
rows are tool assurances rather than C# test obligations. The mapping is stated here so the
integrator does not have to guess:

| Register disposition | Section 5 equivalent |
|---|---|
| `NATIVE_MTP`, `MSBUILD_RULE`, `XUNIT_ARCHITECTURE_TEST` | `PROPOSED_REPLACED_EXECUTING` |
| `RETAINED_ENGINEERING_TOOL` | `DIAGNOSTIC_ONLY`, plus an allowlist entry |
| `REMOVED_WITH_RATIONALE` | `REMOVED_WITH_PRODUCT_CAPABILITY` where the capability really goes; otherwise it needs its own name |

The third row is the honest gap: several `REMOVED_WITH_RATIONALE` rows remove an assurance because
the *mechanism it protected* is gone (TRX shape defects, the receipt, the process-group census)
rather than because a product capability was removed. Section 5 has no word for that.

**QUESTION for the Lead:** confirm the register vocabulary for this cohort, and confirm that "the
artefact this assurance was about no longer exists" is an admissible terminal rationale. No row
was silently dropped; every one carries its reason.

---

## Summary of open QUESTIONs

| # | Question |
|---|---|
| Q1 (F-01) | Does the successor change-evidence gate classify the committed tree or the working tree? |
| Q2 (F-02) | Does CI need the imported baseline objects (`fetch-depth: 0`) for any retained provenance promise? |
| Q3 (F-04) | Which C3 owner takes `assert_one_refusal_per_vhost` and the broker-log capture? |
| Q4 (F-05) | Is the release-effective run `dotnet test <profile>.slnx` or the MTP executable? Does process ownership need an owner? |
| Q5 (F-07) | PublicApiAnalyzers as an MSBuild rule, or allowlist the three API-surface tools? |
| Q6 (F-08) | Port the identity detector to C#, or allowlist it with no executable proof? |
| Q7 (F-14) | Does "no unallowlisted Python file" apply under `evidence/**`? |
| Q8 (F-15) | Is a per-cohort anchor-identity to obligationId mapping part of the R0 checkpoint? |
| Q9 (F-16) | Confirm the register disposition vocabulary and the "mechanism no longer exists" rationale. |

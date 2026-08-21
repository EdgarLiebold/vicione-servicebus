# R0-PY reconciliation

Cohort `R0-PY` — the Python test platform under `tools/ci/**` and `tools/identity/**`.
Work package `WP-F2-SERVICEBUS-TEST-RECONSTRUCTION-11`, baseline commit
`ae73c6da748e3bc3257dffa4971ee8680e086207`, tree `e5897e7632be4f491e01d51221ee59081d4d2aa0`.
Every number below names its derivation, its scope and the commit it was taken at.

---

## 0. Scope and completeness (TLP-017)

| Fact | Value | Derivation |
|---|---:|---|
| Tracked paths under `tools/ci/` | 34 | `git ls-files tools/ci \| wc -l` @ ae73c6d |
| of them `.py` | 33 | the one non-Python path is `tools/ci/api_surface.cs` |
| Tracked paths under `tools/identity/` | 22 | `git ls-files tools/identity \| wc -l` @ ae73c6d |
| of them `.py` | 22 | — |
| **Python files in scope** | **55** | 33 + 22 |
| **Tracked paths in scope** | **56** | 34 + 22 |
| Lines of Python read | 18 953 | `wc -l` over the 55 `.py` files |
| Additional files read | 3 | `tools/ci/api_surface.cs`, `.github/workflows/build.yml`, `build/verification/VERIFICATION_MODEL.json` |

`READ_MANIFEST.tsv` holds all 56 scope paths with their SHA-256, sorted under `LC_ALL=C`.
Every one of the 56 was read completely; none was sampled. Every one of the 56 appears as the
`sourceFile` of at least one row of `LEDGER_DRAFT.jsonl` — verified programmatically, the
uncovered set is empty.

---

## (a) The Lead expects 55 Python files — **CONFIRMED**

```
git ls-files tools/ci      → 34 paths, 33 of them *.py  (tools/ci/api_surface.cs is the 34th)
git ls-files tools/identity → 22 paths, 22 of them *.py
                              33 + 22 = 55
```

No deviation. `tools/ci/api_surface.cs` is a C# file-based app invoked as a subprocess by
`tools/ci/collect_api_assemblies.py`; it is in the cohort's reading scope, it is not a Python
file, and it is therefore not subject to the "no Python file that is not allowlisted" rule of
Lead plan § 3.1. It is disposed on its own merits in the ledger.

Repository-wide there is exactly **one further tracked `.py` file outside `tools/`**:
`evidence/WP-F2-SERVICEBUS-CI-BASELINE-03/records/0077/api-surface/collect_assemblies.py`
(`git ls-files '*.py' | grep -v '^tools/'`). It is a frozen evidence copy of an earlier
generation of `collect_api_assemblies.py`. It is out of this cohort's scope but it is a Python
file in the final tree, so § 12.2 item 12 has to dispose of it explicitly — see `FINDINGS.md`
F-14.

---

## (b) 509 versus 508 — **RESOLVED, both numbers are right about different things**

### Counting rule, stated before the count

Three different questions are asked of the same 55 files, and they have three different answers:

1. **Raw textual hits** — lines matching the regular expression `^\s*def test` anywhere in a
   file, whatever encloses them. This is the Lead's / the reporting agent's `grep`.
2. **`unittest` test method definitions** — `FunctionDef`/`AsyncFunctionDef` nodes whose name
   starts with `test` and whose immediate parent is a `ClassDef`. This is what "a `unittest`
   method definition" means.
3. **Cases `unittest` actually discovers** — what
   `unittest.TestLoader().discover(start_dir, pattern='test_*.py')` yields. This is what a run
   executes.

All three were measured with `ast` over `git ls-files tools/ci tools/identity | grep '\.py$'`
at commit `ae73c6d`, not by reading a report.

### Result

| Question | Count | Delta |
|---|---:|---|
| 1. `grep -hE '^\s*def test'` over all 55 files | **509** | — |
| 2. Class-level `def test*` (AST) | **508** | −1 |
| 3. Cases `unittest discover` yields | **507** | −1 |

### The 509th hit — the extra definition, named

```
tools/identity/identity_gate.py:209
def test_sabotage_findings(before: str, after: str, path: str) -> list[Finding]:
```

This is a **module-level production helper of the identity gate**, not a `unittest` test
method. It compares the occurrence count of `[Ignore`, `[Explicit`, `Assert.Pass(`, `.Skip =`
and `Skip =` between a mapped baseline file and its target, and it is called from
`derive_refactor_conformance()` for every path under `tests/`. It starts with `test` only
because the thing it is about is a test.

AST measurement, verbatim:

```
files: 55
class-level def test* : 508
module-level def test*: 1 [('tools/identity/identity_gate.py', 'test_sabotage_findings', 209)]
nested def test*      : 0 []
```

**The Lead's 508 is exactly right for `unittest` method definitions.** The 509 in the raw
`grep` is right for its own question and is one production function.

### The 508th definition that never runs — a second, smaller discrepancy worth recording

508 definitions are not 508 executed cases. Measured discovery:

```
tools/ci      discovered test cases: 408
tools/identity discovered test cases:  99
                                      507
```

408 equals the class-level count of `tools/ci` exactly. `tools/identity` has 100 class-level
definitions and yields 99. The one that is never discovered is:

```
tools/identity/test_change_list.py
  class Deliberately_failing(Refusing_a_document_that_no_longer_matches):
      def test_fails_after_damaging_its_own_fixture(inner) -> None:
```

The class is declared **inside the body of another test method**
(`Leaving_the_checkout_alone.test_a_failing_isolated_case_leaves_the_tracked_document_untouched`),
so it is not a module attribute and no loader can reach it. It is loaded deliberately by name,
inside the enclosing case, to prove that a deliberately failing isolated case still leaves the
tracked `CHANGELIST.md` and the porcelain status untouched. It is intentional, and it means the
correct figures are **508 definitions / 507 discovered cases**.

### Executed assertions are higher again, and are not a fourth count

Nine of the 507 cases use `subTest`, so a run reports more sub-results than cases — for example
`test_each_of_41_original_mapping_rules_has_non_equivalent_isolated_effect_mutant` carries 41.
That is a run statistic, not a definition count, and no number in this section is derived from
it.

### Per-file distribution (AST, class-level)

| File | Definitions |
|---|---:|
| `tools/ci/tests/test_policy_validator.py` | 202 |
| `tools/ci/tests/test_verify.py` | 71 |
| `tools/ci/tests/test_run_test_category.py` | 64 |
| `tools/ci/tests/test_run_broker_category.py` | 42 |
| `tools/identity/test_identity_gate.py` | 34 |
| `tools/identity/test_identity_rules.py` | 23 |
| `tools/ci/tests/test_record_expected.py` | 18 |
| `tools/identity/test_artifact_gate.py` | 13 |
| `tools/identity/test_change_list.py` | 11 |
| `tools/ci/tests/test_locked_restore.py` | 8 |
| `tools/identity/test_freeze_manifest.py` | 6 |
| `tools/identity/test_test_result_gate.py` | 6 |
| `tools/ci/tests/test_module_names.py` | 3 |
| `tools/identity/test_analyzer_baseline_gate.py` | 2 |
| `tools/identity/test_not_executed_source_gate.py` | 2 |
| `tools/identity/test_proof_contract_gate.py` | 2 |
| `tools/identity/test_test_failure_partition_gate.py` | 1 |
| **Total** | **508** |

Neither 509 nor 508 was adopted without this derivation.

---

## (c) Every `unittest` / `pytest` / `nose` invocation and every Python test module

### C.1 Test-framework invocations in CI — exactly two, both in the required `policy` job

`.github/workflows/build.yml`:

| Line | Command |
|---:|---|
| 54 | `python3 -m unittest discover -s tools/ci -p 'test_*.py'` |
| 56 | `python3 -m unittest discover -s tools/identity -p 'test_*.py'` |

There is no `pytest`, no `nose`, no `tox` and no `py.test` invocation anywhere in the tracked
tree (`git grep -E 'pytest|\bnose\b|py\.test|tox'`). The only other occurrence of the string
`pytest` is `.gitignore:34` — `.pytest_cache/`, an ignore rule for a tool this repository does
not use. `.github/workflows/build.yml` is the only workflow file with jobs;
`.github/FUNDING.yml`, `.github/ISSUE_TEMPLATE/*.yml` are not workflows.

Both lines must be deleted (Lead plan § 12.2 item 12).

### C.2 Every Python test module — 19 files match `test_*.py`, but only 17 are test modules

`git ls-files | grep -E '(^|/)(test_[^/]*\.py|[^/]*_test\.py|conftest\.py)$'` returns 19 paths.
Seventeen are real test modules; **two are production gates whose filename happens to begin with
`test_`** and which `unittest discover -p 'test_*.py'` therefore imports as test modules:

| Path | What it is |
|---|---|
| `tools/ci/tests/test_locked_restore.py` | test module |
| `tools/ci/tests/test_module_names.py` | test module |
| `tools/ci/tests/test_policy_validator.py` | test module |
| `tools/ci/tests/test_record_expected.py` | test module |
| `tools/ci/tests/test_run_broker_category.py` | test module |
| `tools/ci/tests/test_run_test_category.py` | test module |
| `tools/ci/tests/test_verify.py` | test module |
| `tools/identity/test_analyzer_baseline_gate.py` | test module |
| `tools/identity/test_artifact_gate.py` | test module |
| `tools/identity/test_change_list.py` | test module |
| `tools/identity/test_freeze_manifest.py` | test module |
| `tools/identity/test_identity_gate.py` | test module |
| `tools/identity/test_identity_rules.py` | test module |
| `tools/identity/test_not_executed_source_gate.py` | test module |
| `tools/identity/test_proof_contract_gate.py` | test module |
| `tools/identity/test_test_failure_partition_gate.py` | test module |
| `tools/identity/test_test_result_gate.py` | test module |
| **`tools/identity/test_failure_partition_gate.py`** | **production gate**, not a test module |
| **`tools/identity/test_result_gate.py`** | **production gate**, not a test module |

The last two are two of the three tools the Lead named for certain removal. See F-10 for why
the naming collision matters beyond aesthetics.

Package markers that exist only so discovery reaches a directory:
`tools/ci/tests/__init__.py`, `tools/ci/fixtures/__init__.py`, `tools/ci/verification/__init__.py`.
`tools/identity/` has no `__init__.py`; discovery reaches it directly.

### C.3 Every other Python invocation in the tracked non-evidence tree

| Location | Command | Status |
|---|---|---|
| `.github/workflows/build.yml:49` | `python3 tools/ci/policy_validator.py` | live, required |
| `.github/workflows/build.yml` ×9 | `python3 tools/ci/verify.py --selection <name>` | live, required |
| `README.md:43-44` | `python3 tools/ci/verify.py --selection all \| activemq` | doc, must be rewritten (Lead plan § 2 item 9) |
| `CONTRIBUTING.md:61` | `python3 tools/ci/verify.py --selection sql-transport` | doc, must be rewritten |
| `CHANGELIST.md:5` | `python3 tools/identity/change_list.py --write` | doc, generated header |
| `build/test-infrastructure/compose.yaml:13` | `python3 tools/ci/run_broker_category.py …` | comment |
| `docs/build.md:130` | `python3 tools/ci/verification_model.py` | **stale — that path does not exist** (F-13) |
| `docs/build.md:131,142,143,192` | policy validator, verify ×2, validate_receipt | doc |
| `docs/build.md:144` | `python3 tools/ci/verify.py --selection core --record-expected` | **stale — option is rejected** (F-13) |
| `tests/…/RabbitMqTestSetUpFixture.cs:47` | prose in an assertion message naming the broker runner | product test source, C4 owner |

### C.4 Which of the 55 files CI actually reaches today

Reachable from a required job: `policy_validator.py`, `policies/*` (5), `verify.py`,
`verification/*` (7), `run_test_category.py`, `run_broker_category.py`, `fixtures/*` (4), and
the 7 modules under `tools/ci/tests/` — 26 files. Plus the 10 identity test modules and the
production modules they import — but only as imports of the discovery step, **not as gates**.

Never invoked by any required job, in either direction: `collect_api_assemblies.py`,
`compare_api_surface.py`, `api_surface.cs`, `validate_receipt.py`, `record_expected.py`,
`vulnerability_inventory.py`, and **every gate under `tools/identity/`**
(`identity_gate.py`, `artifact_gate.py`, `analyzer_baseline_gate.py`,
`not_executed_source_gate.py`, `test_failure_partition_gate.py`, `test_result_gate.py`,
`proof_contract_gate.py`, `freeze_manifest.py`, `evidence_summary_gate.py`,
`apply_identity_refactor.py`, `change_list.py`). Their only appearance in CI is that their
self-tests run.

---

## (d) How the eleven identity anchors are produced

### D.1 The generating command is **`tools/ci/record_expected.py`, and only that**

`tools/ci/verify.py` has **no** `--record-expected` option. Measured at `ae73c6d`:

```
$ python3 tools/ci/verify.py --selection core --record-expected
usage: verify.py [-h] --selection SELECTION [--evidence-dir EVIDENCE_DIR]
verify.py: error: unrecognized arguments: --record-expected
```

`verify.build_parser()`'s own docstring states why: *"There is no option here that writes an
expected set. This command reads them; the command that writes them is
`tools/ci/record_expected.py`, and they are two commands because they were one: a run in which
a test had disappeared recorded the smaller set as the new expectation and printed PASS."*
`tools/ci/tests/test_verify.py::Verifying_writes_nothing_it_is_measured_against` holds that
boundary with two cases, one of which asserts the parser rejects the option.

The task brief, `docs/build.md:144`, `VERIFICATION_MODEL.json`'s `notes.expectedIdentities` and
ten of the eleven anchor headers all still name the removed option. See F-13.

### D.2 The chain, end to end

```
python3 tools/ci/record_expected.py --category <c> [--approval <file>]
 ├─ refuse if `git status --porcelain` is non-empty
 ├─ previous = read build/verification/expected/<c>.txt   (or [] if the model records none)
 ├─ verify.verify_category(declared_run(model, c), evidence_dir)
 │    ├─ run_scope.mint_run_root(artifacts/run-output)  → vicione-<12 hex>, plus a token file
 │    ├─ verification_model.child_command(run, …)
 │    │     no brokers → tools/ci/run_test_category.py --category <c> --project <csproj> …
 │    │     brokers    → tools/ci/run_broker_category.py --broker … → the same category runner
 │    ├─ run_test_category runs exactly:
 │    │     dotnet test <project> -c Release --logger "trx;LogFileName=<run root>/<c>.trx"
 │    │     with TZ=UTC, nothing else forwarded
 │    └─ trx.parse_result_file(<run root>/<c>.trx) → executed / failed / skipped / omitted
 ├─ refuse if blocking_findings(result) is non-empty
 │    (a failed case, an unapproved skip, a timeout, a surviving process, a defective result
 │     file — a set difference is NOT blocking, it is the thing recording resolves)
 ├─ removed = previous − executed  → requires --approval bound by removal_digest(removed)
 ├─ write_expected(c, sorted(executed))  → build/verification/expected/<c>.txt
 └─ bind_to_model(model, c, path, len(executed))
      → run["expectedIdentities"] and run["minimumExecutedCases"] are written together
```

### D.3 The identity string format — the source of truth

The identity is derived in `tools/ci/verification/trx.py`:

```python
def identity_of(method):                       # method = <TestMethod> of a <UnitTest>
    return f"{type_name(method.attrib.get('className',''))}.{method.attrib.get('name','')}"
```

so, in words:

> **`<full TRX className, including any parameterised-fixture argument list>` `.` `<full TRX case name as the adapter reported it>`**

with `type_name()` stripping a trailing `", Assembly"` qualification **only** when the tail
cannot be part of an argument list — it refuses to strip when the tail contains `(` or `)`, or
when the head has unbalanced parentheses, so `Suite.Fixture(1, 2), Suite.Tests` yields
`Suite.Fixture(1, 2)` and not `Suite.Fixture(1`. There is a named counterexample: shortening the
identity to the last class segment "made a fixture in one namespace authorise a fixture of the
same name in another, and collapsed parameterised cases into a single permission".

Observed forms in the anchors:

```
ViciOne.ServiceBus.Tests.A_fault_event.Should_include_the_faulted_message_types
ViciOne.ServiceBus.ActiveMqTransport.Tests.A_serialization_exception("activemq").Should_have_the_exception
ViciOne.ServiceBus.RabbitMqTransport.Tests.HostConfigurator_Specs.Should_set_client_certificate_as_authentication_identity_when_configured(False)
ViciOne.ServiceBus.RabbitMqTransport.UnitTests.Owning_a_transport_subject("channel").Should_dispose_exactly_once_however_often_it_is_asked
```

Both parameterisation axes therefore appear **inside** the single identity string: a
parameterised fixture puts its arguments after the class name, a parameterised case puts them
after the case name. Neither is a separate column.

### D.4 The filtering

Only two filters exist, and both are in `parse_result_file`:

1. Identities are read from `<Results>/<UnitTestResult>`, **never** from `<TestDefinitions>`.
   A case that was defined and not started is not evidence of anything.
2. A result whose `outcome` is exactly `NotExecuted` goes to `skipped`, everything else goes to
   `executed` (and additionally to `failed` when the outcome is not `Passed`). Only `executed`
   is written into the anchor.

Everything else is refused rather than filtered: a result naming a `testId` the file defines no
case for, a `testId` defined twice, one case reported twice under one id, a missing counter
summary, non-numeric counters, and a summary whose counters disagree with the results beneath
it. The `notExecuted` counter is recorded and never believed — measured on a real `rabbitmq`
run, the writer reported `notExecuted="0"` for a file holding twenty `NotExecuted` results.

On write: `sorted(executed)` — plain lexicographic sort of the identity strings, one per line,
LF, after a comment header. `check_expected_identity_manifests` then enforces sorted, unique,
tracked, not a symlink, at the canonical path, and of a length equal to the recorded floor.

### D.5 Anchor reconciliation against Lead plan § 9 — **eleven of eleven agree**

Measured: `grep -cvE '^\s*(#|$)'` for the identity count, `shasum -a 256` for the digest.

| Category | Path | Identities | Lead | SHA-256 | Lead | Header lines |
|---|---|---:|---:|---|---|---:|
| Abstractions | `build/verification/expected/abstractions.txt` | 74 | 74 | `3745e3c7…f64e` | ✔ | 3 |
| ActiveMQ | `…/activemq.txt` | 178 | 178 | `d5bdacf4…7f83` | ✔ | 3 |
| Analyzer | `…/analyzer.txt` | 115 | 115 | `117f1e65…610c` | ✔ | 3 |
| Benchmarks | `…/benchmarks.txt` | 75 | 75 | `63fff26a…0119` | ✔ | 3 |
| Core | `…/core.txt` | 1873 | 1873 | `bc2910d3…865f` | ✔ | 3 |
| Diagnostics | `…/diagnostics.txt` | 44 | 44 | `d0237bff…93e6` | ✔ | **4** |
| EF Core | `…/entity-framework-core.txt` | 160 | 160 | `9880e549…5fdd0`* | ✔ | 3 |
| Quartz | `…/quartz.txt` | 89 | 89 | `839e125f…d87f` | ✔ | 3 |
| RabbitMQ | `…/rabbitmq.txt` | 370 | 370 | `8dbdd5d9…b459d8e`* | ✔ | 3 |
| SignalR | `…/signalr.txt` | 26 | 26 | `66494695…2bbf` | ✔ | 3 |
| SQL transport | `…/sql-transport.txt` | 110 | 110 | `76c7d0da…2fa76`* | ✔ | 3 |
| **Total** | eleven files | **3114** | **3114** | each bound individually | ✔ | 34 |

\* abbreviated for the table only; every full digest was compared character by character against
Lead plan § 9 and every one matches.

**No deviation in path, digest or identity count.** The 3/4 header-line split the Lead plan
mentions is confirmed and explained: `diagnostics.txt` is the only anchor recorded after
`--record-expected` moved out of `verify.py`, so it carries the current four-line
`record_expected.HEADER`; the other ten carry the older three-line header. This is a byte-level
fact with a consequence for the successor — see F-13.

### D.6 What the successor must be able to do without these tools

To reconcile 3114 identities without `verify.py` and `record_expected.py`, the successor needs
five things, all of which Lead plan § 12.1 already provides in a stronger form:

1. **A stable per-case identity that survives parameterisation.** `TestObligation` +
   `variantKey` replaces the TRX-derived string. The old string is *not* a usable key going
   forward: it encodes NUnit fixture-argument syntax that xUnit will not reproduce.
2. **An expected set that is a committed file, sorted and unique.** Ebene 1, hash- and
   commit-bound.
3. **A per-assembly projection so a partial run cannot look complete.** Ebene 2 plus the
   partition duty of § 12.2 item 17 — this is what the eleven per-category files did, one file
   per category being the ancestor of one projection per assembly.
4. **Executed, not merely declared.** The receipt mechanism replaces "read from `<Results>`,
   never from `<TestDefinitions>`".
5. **Separation between measuring and rewriting.** `record_expected.py`'s digest-bound approval
   is the ancestor of "stilles Löschen oder Umdeuten einer Ledgerzeile ist Bindungsdrift und
   rot".

**The one thing that does not carry over is the mapping from the 3114 old strings to the new
obligation IDs.** Nothing in the plan produces it, and no ledger row can be reconciled against
an anchor identity without it. This is raised as a QUESTION in F-15.

---

## Deviations reported, not adopted

| # | Deviation | Reported as |
|---|---|---|
| 1 | 509 raw hits vs. 508 Lead expectation | resolved above — one module-level production helper |
| 2 | 508 definitions vs. 507 discovered cases | resolved above — one class declared inside a test method |
| 3 | `verify.py --record-expected` named in the brief, in `docs/build.md`, in `VERIFICATION_MODEL.json` and in ten anchor headers, but rejected by the parser | F-13 |
| 4 | Nineteen files match `test_*.py`; two of them are production gates | F-10 |
| 5 | One tracked `.py` outside `tools/`, under `evidence/` | F-14 |
| 6 | `tools/identity/*` gates are not invoked by any required job | § C.4 above, and F-02 |
| 7 | `python3 -m unittest discover -s tools/identity` is **red** at this baseline | F-01 |

No deviation was written back into any file. `git status --porcelain` before and after every
measurement shows only this cohort's own untracked evidence directory.

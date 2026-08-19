# Design note — one canonical verification entry point, exact identities, one receipt

Written for directive 0096, which replaces section 1 of 0095 and the runner-design parts of its
sections 2 and 6. It is a note before implementation, not a description of code that exists.

Nothing here proposes a rewrite of a product test. The existing category and broker runners keep their
logic and become private components behind one entry point.

## 1. The measured question first: is there an exact identity?

0096 requires this measurement before the identity source is chosen. It was taken on this commit,
against the real test platform of this repository (NUnit 4.6.1, NUnit3TestAdapter 6.1.0, VSTest 18.6.0).

**Discovery listing is not usable.** `dotnet test --list-tests` prints display names only:

```
    nothing arrived and nothing stood still
    Should_call_the_exact_set_exact
```

No namespace, no fixture, no arguments. Two fixtures with the same case name are indistinguishable
there, so a listing cannot be the expected set.

**The result file is usable, and it is exact.** The TRX carries `TestMethod@className` and
`TestMethod@name`. Measured on the ActiveMQ category, 154 cases:

| Shape | Where the arguments land | Example |
|---|---|---|
| parameterised fixture | in `className` | `…Tests.A_serialization_exception("activemq")` |
| parameterised case | in `name` | `Should_connect_locally("amqp")` |
| plain case | neither | `Should_have_the_correlation_id` |

154 identities, **154 distinct**, no collision. `className + "." + name` is therefore an exact,
stable, human-readable identity, and it is what the category runner already reads.

**One measured ambiguity, and it is ours, not the platform's.** When a `[TestCase]` declares
`TestName`, NUnit replaces the case name with that text and the **method name disappears from the
identity**:

```
className      : …Tests.Bringing_the_consumer_to_a_standstill_before_the_snapshot
TestMethod@name: 'nothing arrived and nothing stood still'
```

The method `Should_name_the_verdict_of_a_run` is nowhere in it. `NUnit.DisplayName=FullName` does not
change this - an explicit `TestName` wins - which was measured rather than assumed. Two methods of one
fixture may therefore declare the same `TestName` and collide.

This is not a platform limitation, so 0096's stop-and-ask condition is not met. It is a repository
choice, and it is closed on our side: the expected set is generated from a run of the same commit, so
a collision appears as a **duplicate expected identity** and the model itself is red before any run is
judged by it. In addition, `TestName` is removed where this work package introduced it, so the method
name stays in the identity.

## 2. Verification-model shape

The existing `build/verification/VERIFICATION_MODEL.json` stays the single truth and gains a selection
layer. Sketch, not final field names:

```jsonc
{
  "selections": {
    "all":       { "members": ["local", "fixture"], "note": "the complete required scope available here" },
    "local":     { "members": ["analyzer", "core", "abstractions", "signalr", "quartz",
                               "benchmarks", "diagnostics"] },
    "fixture":   { "members": ["rabbitmq", "activemq", "sql-transport", "entity-framework-core"] },
    "activemq":  { "members": ["activemq"] }
  },
  "capabilities": [
    { "id": "core",
      "runs": [
        { "category": "core",
          "project": "tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj",
          "fixtureMode": "NONE",              // or PINNED_COMPOSE with the brokers it needs
          "budgetSeconds": 1200,              // the finite budget the runner owns
          "minimumExecutedCases": 1872,       // kept, as a fast regression floor only
          "expectedIdentities": "build/verification/expected/core.txt",
          "notExecuted": [ /* unchanged, the approved skip list */ ] } ] }
  ],
  "excluded": [
    { "capability": "message-data-amazon-s3", "reason": "REAL_EPHEMERAL_CLOUD, no infrastructure here" }
  ]
}
```

- **A selection resolves transitively** to a set of categories, and the resolution is a pure function of
  the model. `all` is defined *in* the model, so a narrower selection can never claim it.
- **`fixtureMode`** moves the broker knowledge out of the workflow: the entry point decides from the
  model whether a category needs the pinned Compose fixture, which brokers, and whether an outage
  control directory is published.
- **`expectedIdentities`** is a generated, committed, sorted file per category. It is written by the
  entry point in a record mode and is evidence, not prose. It is the exact expected set.
- **`budgetSeconds`** is the single central contract for the finite budget 0095 §2 demands.

## 3. Selection resolution and the entry point

```
python3 tools/ci/verify.py --selection all
python3 tools/ci/verify.py --selection activemq
python3 tools/ci/verify.py --selection core --record-expected     # regenerates the expected set
```

For the resolved scope the entry point owns, in this order:

1. a unique run root it mints and claims itself - an ambient `VICIONE_SERVICEBUS_RUN_ROOT` can never
   select it; the internal handoff to a component carries a validated ownership token;
2. the fixture lifecycle for every category whose `fixtureMode` needs one, through the existing broker
   runner logic;
3. the child, started in its own session with the modelled budget, so the whole owned tree can be taken
   down; on expiry or interrupt: SIGTERM to the group, bounded wait, SIGKILL for survivors, then
   bounded cleanup and a nonzero terminal result;
4. parsing of the native result file and the **exact set comparison**;
5. cleanup of everything it owns, and only that;
6. the receipt, written last.

## 4. Terminal result

PASS only when every one of these holds, for every category of the selection:

```
executed == expected                    (set equality, not a count)
unexpected == {}   missing == {}   duplicate == {}   failed == {}
unapproved not-executed == {}           (approved ones come from the model's notExecuted list)
every child and fixture operation succeeded, and nothing owned survived the run
```

`minimumExecutedCases` stays as a fast floor and is explicitly secondary.

## 5. Receipt schema

One immutable JSON file per run, written by the entry point after all cleanup, under the run root and
copied to the caller's evidence parent:

```jsonc
{
  "schemaVersion": 1,
  "kind": "SERVICEBUS_VERIFICATION_RECEIPT",
  "runId": "vicione-<12 hex>",
  "commit": "<sha1>", "tree": "<sha1>",
  "verificationModelSha256": "<sha256 of the model file as it was read>",
  "selection": "all",
  "resolvedCategories": ["abstractions", "activemq", "..."],
  "startedUtc": "...", "finishedUtc": "...",
  "categories": [
    { "category": "core",
      "expected": 1872, "executed": 1872,
      "passed": [...], "failed": [], "skipped": [], "notExecuted": [],
      "missing": [], "unexpected": [], "duplicate": [],
      "childExitCode": 0, "timedOut": false,
      "rawResultSha256": "<sha256 of the TRX>", "logSha256": "<sha256 of the broker log>" } ],
  "fixtureFindings": [], "cleanupFindings": [], "survivingOwnedProcesses": [],
  "terminal": "PASS"
}
```

Identity lists are written in full, sorted, so the receipt is comparable rather than summarised. A
small validator (`tools/ci/validate_receipt.py`) reads a receipt and fails unless it matches the
current commit, the current model hash, the requested selection and the exact-set rules above. A
stale, partial, hand-written, differently selected or incomplete receipt is not accepted.

The receipt is an engineering completeness proof. It is explicitly **not** a claim that a contributor
with repository and branch administration rights can be made harmless by it.

## 6. Workflow boundary and what stays in the policy check

Each required job becomes one step:

```yaml
      - name: Verify
        run: python3 tools/ci/verify.py --selection activemq
```

No categories, projects, filters or broker names in YAML. The policy check therefore shrinks to an
exact command shape rather than a shell parser:

- the job/selection mapping equals the model;
- the run step is **exactly** the canonical invocation - the interpreter, the runner path, `--selection`
  and its value, and nothing else;
- `echo`, a wrapper, chaining, a pipe, redirection, command substitution or `|| true` in that step is
  red, because the step is compared against the one allowed shape instead of being parsed;
- the receipt is validated and uploaded;
- removing a required selection, replacing the command with `echo`, adding `|| true`, reusing a stale
  receipt or presenting a narrow receipt as `all` is red.

The reader written for 0093 keeps its job of splitting a step into effective commands, because that is
what makes "exactly one command, of exactly this shape" decidable. What it no longer has to do is
understand an arbitrary shell program.

## 7. What this replaces, and what it does not

Replaced: the per-job YAML orchestration, the token-presence checks on category and project, and the
count-first completeness argument.

Kept: `run_test_category.py` and `run_broker_category.py` logic as private components, the not-executed
inventory, the executed floor as a secondary signal, the anchor binding, and every product test.

## 8. Open question for the Lead

None that blocks. The exact-identity condition of 0096 is met and measured, so no weaker alternative is
being proposed. The one caveat - a custom `TestName` drops the method name from the identity - is
reported above with the repository-side rule that closes it.

# Request, response, mediator, and multi-bus validation

## Product and API boundary

This cohort preserves useful ViciOne.ServiceBus features, not the historical MassTransit API.
Backward source, binary, overload, naming, and call-shape compatibility is not required. The old
tests are migration evidence and a defect-history lower bound; the complete connected product path
defines the capability model, and the resulting public surface is designed as an A+ greenfield API.

## Closure

- all 40 inherited ledger identities from the six bounded fixtures are present exactly once in
  `INHERITED_BEHAVIOR_DISPOSITION.json`;
- all 40 identities map to 24 existing permanent source-owner test methods;
- the complete request-client, response, mediator, outbox, filter, TTL, dependency-injection, and
  multi-bus paths were read before closure;
- source-derived behavior extends the inherited lower bound instead of reproducing its fixture
  structure;
- all six inherited fixtures are deleted and no empty test directory remains;
- eleven isolated product mutations failed for their intended behavior and were fully restored;
- no skipped, filtered, receipt-based, sentinel-based, Python-policy, wall-clock, or inherited
  TestFramework path is used as evidence.

## Final execution

| Gate | Result |
|---|---|
| Unit/Architecture Release build | exit 0; 0 warnings; 0 errors |
| Native Core executable | 614 passed; 0 failed; 0 skipped |
| Native Abstractions executable | 150 passed; 0 failed; 0 skipped |
| Unfiltered UnitArchitecture MTP profile | 1155 passed; 0 failed; 0 skipped; exit 0 |
| LocalIntegration Release build | exit 0; 0 warnings; 0 errors |
| Unfiltered LocalIntegration MTP profile | 3 passed; 0 failed; 0 skipped; exit 0 |
| Present changed-C# Roslyn formatting check | 152 files; exit 0; no formatting diagnostics; deleted inherited files are not formatter inputs |
| Requirement and disposition JSON | all files parse; exact 40/40 identity-set comparison passes |
| Static repository checks | `git diff --check` passes; no empty test directory; no mutant remains |

The accepted UnitArchitecture command is the repository's documented native MTP form:

```bash
dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/unit --minimum-expected-tests 1155 \
  --max-parallel-test-modules 1
```

The accepted local-integration command is:

```bash
VICIONE_TESTS__Profile=LocalIntegration \
dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx \
  -c Release --no-build --no-restore \
  --results-directory artifacts/test-results/local-integration --minimum-expected-tests 3 \
  --max-parallel-test-modules 1
```

Only these unfiltered successful runs are acceptance evidence. A diagnostic invocation that
executed zero tests is rejected evidence and is not included in any pass count.

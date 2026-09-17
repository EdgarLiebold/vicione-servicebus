# Iteration 195 status

Status: admitted locally for the iteration checkpoint; remote publication remains queued behind the
exact destination/payload confirmation gate while local work continues.

- Scope: 16 previously unadmitted public saga-role, repository/query and registration-runtime
  sources; 467 baseline and 484 final physical lines.
- Personal-read coverage: cumulative 624/4,118 current source files (15.153%).
- Product corrections: exact `IObserves` saga constraint, immediate query null validation,
  immediate registration-context validation, typed callback validation before selector lookup and
  exact nested registration context validation.
- New evidence: 18 unique requirement variants, 18 test methods and 18 expanded cases.
- Focused classes: 5/5, 7/7 and 6/6 passed; complete Sagas regression: 132/132 passed.
- Complete Core: 5,353/5,353 passed, 0 failed, 0 skipped.
- EF unit: 249/249 passed; Core/EF/EF-local requirement projections: 1/1 each.
- Release builds: Core test, EF unit and EF-local projects passed with 0 warnings and 0 errors.
- Mutation proof: 6/6 compiled isolated single-cause mutants killed and restored.
- Admitted coverage: 128/128 executable lines and 77/82 branches; maximum method CRAP 14.
- Final Cobertura SHA-256:
  `a1b47e7e431c3329f7249e846a055befdddc947099a5e41e92893c915d2d4187`.
- Core requirements SHA-256:
  `53199803543cf48de24068b25be179d52edbee0c8126949c687eed552dc9e22d`.
- Strict Core CTRF SHA-256:
  `a369baf4907a818f30ef5f16ce0b251c769685136ee8150f2b6b77c0f00b3e02`.
- Sorted display-name SHA-256 across 5,353 tests:
  `9054075132bfb47718ebfb7e43125f13460fae143ed5e4db0ac6a46ef46a46c4`.
- Source manifest / chain:
  `d41dda82f4bf11c288bbe614d1e15f606ebc2cc8b9ad7ab5afada977d6958ab1` /
  `fb371742bc4dd307154ae39be39ff8c5f2d3b62fd4da7228be8e230b634ba6cb`.
- Test manifest / chain:
  `8dd941259cfffa644779d49dac21f8b8aef1ad84b78e3d3dc07d074937d7f83a` /
  `d559638a369d08e64a6740a40d8aaec3c72644fae08491940540332830f1bcd1`.

The five residual compiler-mapped branches have explicit dispositions and no executable line or
behavioral gap. No unresolved correctness, lifecycle, cancellation, compatibility, coverage or
architecture finding remains in this admitted packet. Local work continues immediately into
iteration 196.

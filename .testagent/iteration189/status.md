# Iteration 189 status

Status: admitted locally; commit and annotated tag pending.

- Scope: 22 current mediator source files, 2,161 physical lines.
- Personal-read coverage: 510/4,118 current source files (12.385%).
- Product corrections: scoped-mediator lazy-factory/disposal serialization, terminal disposal,
  cancellation preflight/token forwarding, explicit null-task failures, and single-flight ordered
  mediator cleanup.
- New evidence: 38 unique requirement variants, 38 test methods and 56 expanded cases.
- Focused classes: 11/11, 34/34 and 11/11 passed.
- Complete Core: 5,118/5,118 passed, 0 failed, 0 skipped.
- EF unit: 249/249 passed; Core/EF/EF-local requirement projections: 1/1 each.
- Release builds: final Core test project and EF unit/local projects passed with 0 warnings and
  0 errors; the complete Unit solution also passed at the source-correction checkpoint.
- Mutation proof: 6/6 compiled single-cause mutants killed and restored.
- Admitted coverage: 767/767 executable lines; 275/278 branches; maximum method CRAP 28.
- Remaining branch dispositions: two compiler-generated async rethrow edges and one
  construction-impossible host-configuration cast-null edge; no uncovered source line remains.
- Final Cobertura SHA-256:
  `abeab924a5c22f08d4a36abda44522d7717f43c762b37d4bccbb9255352fd62a`.
- Core requirements SHA-256:
  `b1bec6e600d20c8f77aedf589370527ce07b0eecfbbb145a8ca87c5e147b4901`.
- Sorted display-name SHA-256 across 5,118 tests:
  `e7d93539481974a3e507db4e10c179e1102df9c4254aaa5a17cef5df8649ce63`.
- Source manifest / chain:
  `88dafb00495346e2608ecdee938ca4df6c2092ac34619b9110f416469243da7a` /
  `e366f3194d56be5e09d876ba0ae27b334516740a52761fc2b1081f57330ae239`.
- Test manifest / chain:
  `b7be26cb88e0a393cc36a504c58d49eab7450c4cf91d1e0c2f83beff3990ed07` /
  `4713ae90b6e7b41071d8a395ba4f03a7e646bc4b616f8a4069570531245e1761`.

Remote publication remains queued until the security gate receives exact confirmation for the
destination and payload. Work continues locally without pausing the active goal.

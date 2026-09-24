# Cumulative product coverage profile at 08d41e4b6

## Scope and validation

- Product/test HEAD: `08d41e4b60864a5dc3582c1775d82815b0638678`.
  The only changed `src` file since the preceding `e51c446f2` profile is
  `src/ViciOne.ServiceBus/Configuration/SerializationConfiguration.cs`.
  The commit also adds two source-owned serialization validation tests, their
  requirement mappings, and a changelog entry. An adversarial Red Team found
  two weak test assertions; both were strengthened, and its second review
  returned PASS.
- The Release Unit/Architecture build passed with zero warnings and errors;
  `artifacts/coverage-a-plus-20260924-08d41e4b6/unit-architecture-build.log`
  has SHA-256 `10ec4ece7ace7d4eebc0e653d2962b879b3d1d78861d2e616503df042b321c79`.
  The serial full gate passed **10,353/10,353** with zero failures and skipped
  tests; `unit-architecture-gate.log` has SHA-256
  `54235f2d5232e1918578007643a0e60309236542f9709dd57ada094b87d5f6f2`.
- The fresh Core run under Microsoft CodeCoverage passed **6,388/6,388** with
  zero failures and skips. `core-coverage.log` has SHA-256
  `4e57f667facedb75159640c868193af2a8070bae8f2799a16ea364dbbf9d677a`;
  its Cobertura report has SHA-256
  `4e046122d08c6013086c350ce9437df87a27665209f227d7eb6a842c4cb5d28b`.
- `artifacts/coverage-a-plus-20260924-08d41e4b6/raw/` contains 41 parseable
  Cobertura reports for 32 product assemblies. Forty inherited reports are
  individually hash-identical to the preceding profile; the Core report is
  new at this HEAD. These are cumulative observations, not 41 executions at
  this HEAD. All 12 broker fixture records are inherited; no new broker run
  occurred in this iteration.

## Product-wide cumulative result

| Measure | `08d41e4b6` | Previous `e51c446f2` |
| --- | ---: | ---: |
| Line coverage | 84,051 / 93,486 = 89.9076% | 84,039 / 93,480 = 89.9005% |
| Conservative branch observation | 30,143 / 36,664 = 82.2142% | 30,132 / 36,660 = 82.1931% |
| Methods with CRAP > 30, exact arithmetic | 32 / 25,991 | 33 / 25,989 |

The former `SerializationConfiguration.Validate` iterator had CRAP 39.78
with 18/21 covered lines. The same public validation now delegates to
`ValidateSerializers` and `ValidateDeserializers`; the three iterator bodies
have respectively CRAP **4**, **18**, and **18**, with **5/5**, **11/11**, and
**11/11** covered lines. The tests check exact failure order, independent
serializer/deserializer registration, deferred evaluation after mutation,
ambiguous multi-format selections, and the real default serializer and
deserializer selected by the created collection. The refactor preserves
validation order and deferred execution.

## Merge method and limits

- `SerializationConfiguration.cs` changed, so exactly 34 inherited reports
  containing that source file are excluded for it. Only the new Core report
  contributes that file's classes, lines, branches, and methods. The older
  JobSaga overlay still excludes exactly eleven stale reports; its previous
  fresh Core observation and the new compatible Core observation are retained.
  All other product source files are unchanged since `e51c446f2`.
- `analysis-41/summary.json`, `methods.json`, `provenance.json`, and
  `overlay-policy.json` record the exact merge, source exclusions, report and
  binary hashes, fixture inheritance, and CRAP decisions. Their SHA-256 values
  are respectively `720ae76aafe2b084e3fdb42131967a6ecac70627cd9bd237bc2009184f38aad4`,
  `22181df7d6c2ee2bd840daf68448df0fa79718ef440c53560dd2e6b96b213710`,
  `8d4b057922b905f5dd1f581fb2a7c76ca2166ba00a1127593ca23865b4adc4ac`,
  and `166126d949b877c82335e7b65b46f8b0f92664ec39cf2a54da12c1febea1a66b`.
- Cobertura lacks stable branch identities. The conservative merge takes the
  largest observed covered count at each source location. The capped sum can
  overstate repeated observations and is not used for the quality comparison.
  CRAP is `complexity + complexity² × (1 − method line coverage)³`.
- The Microsoft `code-testing-agent`, `run-tests`, and `coverage-analysis`
  skills guided test design, execution, and risk measurement. The
  `test-gap-analysis` and `assertion-quality` checks confirmed closure of two
  potential surviving mutations identified by the Red Team: reversed failure order and conflated
  serializer/deserializer registrations. The final tests assert exact ordered
  failures, the remaining failure after one-sided registration, deferred
  evaluation, and the selected runtime serializers.

Global A+ remains open: line and branch coverage are below A+, and 32 methods
remain above CRAP 30.

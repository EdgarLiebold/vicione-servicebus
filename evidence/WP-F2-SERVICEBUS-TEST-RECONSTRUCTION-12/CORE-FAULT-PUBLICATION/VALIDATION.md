# Core fault and publication closure validation

The final technical subject is `07be2a54e93450291092249e0930361de0cc1d2e`
(tree `3dae7390383d7baabe013399016153c626e11e64`) over base
`e31037f60fcada907093a0b8024d761ad1700141`. The technical range contains the
36-obligation replacement and one assertion-only correction discovered by the mutation review.

## Result

- 36/36 obligations in `0144..0167`, `0306..0310` and `0318..0324` are
  `REPLACED_EXECUTING` in the UnitArchitecture profile.
- Six replaced legacy files and 945 legacy lines are gone atomically.
- The complete technical range is 939 additions and 949 deletions: net **-10 lines**.
- The final focused carrier run is 13/13, with 0 failed and 0 skipped.
- The final UnitArchitecture run is 2,484/2,484 across 21 CTRF files, with 0 failed,
  skipped, pending or other results. The executable floor is 2,479.
- Locked restore and Release build completed with 0 warnings and 0 errors.
- Scoped formatting verification passed without modifying a file.
- CI self-tests are 257/257, identity self-tests are 148/148, the Verification Model
  passes and the generated CHANGELIST closes at 10,067 entries.
- Seven buildable, single-cause mutations were killed and byte-restored. One candidate
  outside the actual consumer Fault path was empirically recognized as equivalent for
  this cohort and excluded rather than reported as a kill.
- The Evidence root contains 51 files in total; `SHA256SUMS` binds all 50 nonmanifest
  files.

## A+ oracle review

The final assertions combine positive terminal barriers with exact raw-delivery counts,
provider-owned published snapshots, correlation/routing metadata, exact exception and
message-type sets, and per-overload RequestId propagation. During pseudo-mutation review,
dictionary-only recorders were replaced by raw queues/counters so late duplicate Error,
Fault and Publish deliveries cannot be hidden. The M03 survivor found before the correction
was closed by an assertion on the final provider-published Fault snapshot; its repeated run
then failed exactly at 1,000 rather than 500 publications.

There are no sleeps, delay-based success oracles, skips, assertion-free carriers or
self-derived expected values in the changed tests. Source-mirrored namespaces and the
xUnit 4 / Microsoft Testing Platform 2 profile remain unchanged.

## Commands

All .NET/MSBuild invocations produced binlogs. The final acceptance commands were:

```text
/usr/local/share/dotnet/dotnet restore ViciOne.ServiceBus.Tests.Unit.slnx --locked-mode
/usr/local/share/dotnet/dotnet build ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-restore
/usr/local/share/dotnet/dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx --configuration Release --no-build --no-restore --minimum-expected-tests 2479 --max-parallel-test-modules 1 --report-xunit-ctrf --parallel none
python3 -B -m unittest discover -s tools/ci -p 'test_*.py'
python3 -B -m unittest discover -s tools/identity -p 'test_*.py'
python3 -B tools/ci/verification/model.py
python3 -B tools/identity/change_list.py
```

The first sandboxed CI self-test attempt was rejected only because macOS sandboxing denied
the suite's read-only `ps` process-tree inspection. Repeating the identical command outside
the sandbox produced the bound 257/257 result; no code or test workaround was applied.

# Core Job Service obligations 0201–0227 validation

Technical commit `b2aa104c3bea11d0ff33df16271a97bf31073fc0` closes the contiguous range `OBL-R0-CORE-D-0201..0227` as one coherent Job Service package.

- All 27 obligations are `REPLACED_EXECUTING` in the committed native obligation map.
- Eighteen xUnit methods materialize 28 executable cases: eleven stale-generation rows, one current-generation control, two endpoint-configuration cases, six lifecycle/admission cases and eight in-memory integration cases.
- The five fully replaced inherited files are deleted atomically. The Technical commit is net negative by 105 lines: 1,604 additions and 1,709 deletions, including 1,697 retired legacy test lines.
- The focused Job Service carriers are 28/28. The complete UnitArchitecture profile is independently recorded by 21 CTRFs and aggregates to 2,455/2,455 with zero failed, skipped, pending or other cases; the enforced floor is 2,450.
- The complete Release build and the post-mutation restore build both report zero warnings and zero errors. Scoped format verification is green for every changed C# carrier and the architecture floor owner.
- Verification Model, the generated 9,950-entry CHANGELIST, 257 CI self-tests and 148 identity self-tests are green.
- Six buildable one-cause mutations independently weaken the stale-attempt guard, registration-context outbox, heartbeat drain, admission gate, retry generation and containerless outbox. Every intended carrier turns red and every product file is byte-identical to the Technical commit after restoration.
- `BusActivityIndicatorTests` now waits on its existing positive timer signal after advancing virtual time. This removes a real callback race without adding a test case or changing the floor.
- No empty legacy test directory remains, no external fixture or cloud service was used, and no remote push was performed.

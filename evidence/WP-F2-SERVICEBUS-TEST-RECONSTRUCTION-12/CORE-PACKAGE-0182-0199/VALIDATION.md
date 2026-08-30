# Core obligations 0182–0199 validation

Technical commit `d74650ff20469fe3e244ca63ae5d5215aaeb1830` closes the contiguous obligation range `OBL-R0-CORE-D-0182..0199` as one package.

- 18/18 obligations are `REPLACED_EXECUTING` in the committed native obligation map.
- Five new executable cases carry eight obligations; ten obligations bind to already stronger executing native carriers.
- Four legacy test files were removed atomically. The technical delta is net negative: 453 added and 507 deleted lines.
- The focused positive carriers are 5/5; the compiled requirement projection is 1/1.
- The complete UnitArchitecture solution is 2427/2427 with zero failed and zero skipped tests; the enforced floor is 2422.
- Engineering restore, Engineering Release build, focused build, formatting, verification model, change-list, 257 CI self-tests and 148 identity self-tests are green.
- Five product mutations independently remove failed-outbox discard, the consumer-factory filter boundary, one implemented-interface dispatch edge, consumer-message redelivery wiring, and endpoint redelivery wiring. All five build successfully and are killed by their intended carriers.
- Every mutated source file is byte-identical to the Technical commit after restoration, and the post-restore build is 0 warnings / 0 errors.
- The CI self-tests were rerun outside the restricted process sandbox because their process-tree tests require read-only `ps` access; the sandbox failure was `PermissionError: Operation not permitted`, while the unrestricted rerun is 257/257.
- No remote push was performed.

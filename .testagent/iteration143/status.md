# Iteration 143 — status

- Phase: terminal evidence complete; pending final diff/admission verification, commit, tag and push.
- Parent: `dcbf259a8cefbf20ba012bfbda1651ae25cc25e3`.
- Personal source read: 20 files / 2,285 lines — all eight files in `src/ViciOne.ServiceBus.Initializers`, direct advanced capability contracts/extensions and the three runtime endpoint dispatchers.
- Personal owning-test read: five files / 1,093 lines.
- Capability-project result: no material source defect found; validation, forwarding, generic selection, task/cancellation identity and variable isolation behavior agree with comments and owning tests.
- Confirmed and corrected finding: send, publish and response runtime-contract caches strongly retained collectible types/assemblies. All three now use `ConditionalWeakTable<Type, Lazy<TConverter>>`, preserving live-key reuse and execution-and-publication lazy initialization.
- Frozen source admission: 20 files / 2,285 lines; manifest `1b93f065293b65bf14c5f09b9a7b5f9072c4997723e52117afbd6104650ec179`; chain `c927f785f4727b9d9e81d1f03a1d698c58c7dd9ac991f4d0071b37d2fba9c6d7`.
- Frozen test admission: five files / 1,093 lines; manifest `c663654f8ab40b097678d95d8b3070ad96f06c619f5c8d403a9ccc2e506f3f69`; chain `e0675caa2eff1c4e90cc9a6f59b93960845550d92e996af4ffa9d6869f8aa173`.
- Unchanged-product baseline: 0/3; every dispatcher retained its collectible contract type. Corrected fixture: 3/3. Existing dispatcher regression: 10/10. Capability/variable regressions: 20/20.
- Mutations: three compile-valid, one-at-a-time strong-key mutations killed at 0/1 each and immediately restored; post-mutation fixture 3/3.
- Static pairing: actual packet 20 source / five test files, 14 paired / six heuristic misses; all three corrected dispatchers paired.
- Strict Release builds: product and Unit solutions each 0 warnings / 0 errors. Engineering and Unit format verification: exit 0, no findings.
- Unfiltered inventories: Abstractions 752/752; Core 4,799/4,799. Abstractions removed no parent case and added exactly the three dispatcher forms; Core retained the exact Iteration-142 name multiset.
- Selected Core coverage: 49,679/61,228 lines (81.1377%), 17,161/23,340 branches (73.5261%), 18,155 methods, 121 CRAP scores above 30.
- Targeted Abstractions coverage: 5,332/8,310 lines (64.1637%), 1,865/2,996 branches (62.2497%), 2,762 methods, 51 CRAP scores above 30. The overlapping graphs are not summed.
- Next checkpoint: exact path-limited diff and manifest validation, then commit, annotated tag, atomic push and independent remote verification.

Whole-fork personal source/comment completion, global API/new-parameter behavior coverage, global bidirectional naming/type/file/directory consistency, legacy/dummy/directive absence, whole-fork coverage/CRAP and real durable-provider/external acceptance remain open after this connected subsystem packet.

# Saga ownership — accepted native artifact bindings

Artifact root: `/private/tmp/vsb-iteration119-saga-pairing.iEltCW`.
All paths below are relative to that exact directory. SHA256 binds the observed
native artifacts, not a generated substitute or independent external acceptance.
Canonical source/test/catalogue bytes are separately bound in
[SAGA_OWNERSHIP_PACKET.md](SAGA_OWNERSHIP_PACKET.md).

## Nine individually compiled counterchanges

Only the accepted reruns are bound below where a first observation had masking
cleanup or unobserved build completion. The excluded runs remain disclosed in the
packet report and working journal. Each build exits 0 with zero warnings/errors;
each accepted mutant native execution exits 2 with the report's exact case counts.

| Mutation | Native artifact | SHA256 |
|---|---|---|
| M1 | `mutation01-consume-dispose-build.log` | `a8f99fd0dc365eeb1ab3d6f702dd3e74810e4f07d47037b2a8262da65a403ed2` |
| M1 | `mutation01-consume-dispose-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_01_10.603936.ctrf` | `501b330d60112936cce7ba1e375f25e2d6f0141873bb1cecc68096c95b483c30` |
| M2 | `mutation02-removed-load-build.log` | `5f935c6fafab8211c0e6e346f6727cfc37652b0edca79d0d68bae2d7b052a307` |
| M2 | `mutation02-removed-load-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_02_02.512628.ctrf` | `2e89ea33c50205f2a6d69deaed9264371959f33ae646ae52b83382364868782b` |
| M3 | `mutation03-final-user-boundary-rerun-build.log` | `0897682c8280572104ddeaaf40ca18c2186f5357986441c4f64b7f186a3d6989` |
| M3 | `mutation03-final-user-boundary-rerun-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_09_56.440724.ctrf` | `1880101e9e7db27e45e4d354e4120551e6398a1598fab9b235f91e12de1b5650` |
| M4 | `mutation04-stale-delete-build.log` | `85071703ef7345317f43a67fc3d9024e59a22ec9e27406b5c907021d1f5fc6f5` |
| M4 | `mutation04-stale-delete-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_10_45.379536.ctrf` | `ea46db223cca78a2c952ab2666d7fcfe5a1a7e2d834a4353f824a127c923ea90` |
| M5 | `mutation05-execute-precancellation-build.log` | `60a18c3abeb14dff1ef76ab5e61fe37b295395eb5da12886643f8c0699d71e8b` |
| M5 | `mutation05-execute-precancellation-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_11_31.179890.ctrf` | `5a14ceaf0d3365a5088389b64898332c98ed3538a47ee2f17f0df8c9fa0d8911` |
| M6 | `mutation06-save-null-guard-build.log` | `c41de50545f56d4da64f5ce2495ce8acf75f5548073e1957ff78439caa383169` |
| M6 | `mutation06-save-null-guard-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_12_16.840756.ctrf` | `5762fdabd6e4c6e6f51b60bd80a0598ef33713d374a065640314a6697d0c6c24` |
| M7 | `mutation07-null-task-build.log` | `be52635b828ceb2499a06536ac470db0828d3c90c6cc6bf5312dba0811d4ec95` |
| M7 | `mutation07-null-task-rerun-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_13_26.813270.ctrf` | `d71de179afc370046b1e26ae0f9c4e4664bae3cc89e083e0a17f03fccd0bc4a4` |
| M8 | `mutation08-active-dispose-rerun-build.log` | `c6c5d48f6d1f9aeb8e05cf567d66252c59e6b1397fede51fe120a6e30420683e` |
| M8 | `mutation08-active-dispose-rerun-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_18_52.511554.ctrf` | `c2418796ee2ae94bd2a10f3e8fcd561010569472347f64fcf6910d8ebd67407b` |
| M9 | `mutation09-invalidation-recovery-build.log` | `2ea746c5fcb38424bf667702119bdd46da80d2d719c3742cef874c46200ce121` |
| M9 | `mutation09-invalidation-recovery-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_19_33.560907.ctrf` | `609b03812df6ac8dbf659f7499e3f0d8c9a05287599fcb3afb43933a80cc7ca8` |

## Restored closing runs

The first focused closing run precedes only the ten nonsemantic indentation changes
disclosed and exact-hash reversed in the packet. The final complete Core execution
uses the final formatted test bytes and includes all 82 focused cases passing.
No earlier partial execution substitutes for final coverage or full tests. Every
corresponding owned build/test/format process has reached observed terminal status.

| Evidence | Native artifact | SHA256 |
|---|---|---|
| Final strict build | `post-format-final-core-build.log` | `6c0f0131731891270c62ea3d805a64e70efebb293704acf092612982e4a9fecd` |
| Restored focused 82/82 | `final-restored-focused-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_20_31.610337.ctrf` | `f4342e3e210f84bfada4a5c16cbf6af7730928e1fe52c1547f00d2ca93679f0a` |
| Final complete Core 3,910/3,910 | `post-format-core-final-results/edgar.liebold_Edgars-iMac-2_2026-09-15_11_24_30.861818.ctrf` | `ea0f3d5476ccf24ac9173913b3bcc38ddd7a451b82e79ad692f505cfe6d28efd` |
| Final Core source-only coverage | `post-format-core-final-results/post-format-core-final.cobertura.xml` | `b766644caa27399a9b5712ca4464b97fe61e00ce6cb3fd413ef5f57711f3910c` |
| Separate strict Abstractions build | `abstractions-final-build.log` | `8a780af173438d0786d3651a7fa4ed6a86d3f08f201949d3cb3fd2b2e912dfb9` |
| Separate Abstractions 749/749 | `abstractions-final-results/edgar.liebold_Edgars-iMac-2_2026-09-15_10_53_56.033292.ctrf` | `e6397e780fd47b161534808e369075addf9d58746432e4341648db7cba200787` |
| Separate Abstractions source-only coverage | `abstractions-final-results/abstractions-final.cobertura.xml` | `f32e0368ae61f1b321e52c7371b70cc50b4ade8fc8865b21f21ed8df540af7c6` |
| Product scoped whitespace, exit 0 | `final-product-whitespace.log` | `df093edb6658e6dce69dbca17325c7b1a6c8c6a4bc93830a9ef448c4af677f56` |
| Corrected Unit scoped whitespace, exit 0 | `post-correction-final-unit-whitespace.log` | `df093edb6658e6dce69dbca17325c7b1a6c8c6a4bc93830a9ef448c4af677f56` |
| Parse-only pairing, heuristic not behavioral | `pairing.json` | `216797b0daaae1e8fc46a452b0bae8001d5aee975c0d8a5bbc6b5e881cd4d4aa` |

Both whitespace logs contain exactly the same known workspace-load warning;
identical hashes are expected, not reused substitute observations. The separate
native coverage profile is the repository file `tools/ci/coverage.settings.xml`,
SHA256 `3838fc1b6b73f21d8fb447beecad11a5c91cca053bd6c31f906a6036f248464d`.
Its include/exclude/auto-property semantics are described in the packet. The
28 raw bindings and 14 canonical source/test/catalogue bindings are checked before
Git capture. These artifacts measure their exact loaded graphs, not global API
correctness, CRAP or real cloud-provider acceptance.

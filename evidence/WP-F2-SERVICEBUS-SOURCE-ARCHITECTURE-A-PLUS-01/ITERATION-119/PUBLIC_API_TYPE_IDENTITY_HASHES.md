# Iteration 119 — public API type identity artifact hashes

SHA-256 bindings are recorded manually from observed read-only hashing. Canonical
paths are relative to the product repository. Raw paths are relative to
`/private/tmp/vsb-iteration119-api-identity.mFjlT2`; their bytes remain local,
not uploaded to Git. This manifest does not certify live or unexecuted work.

## Canonical executable and contract inputs

| Path | SHA-256 |
|---|---|
| tools/public-api-baseline/PublicApiBaseline.cs | b2e42d62b8dbe025696acc3fa8836d6c24bb06f81806ee99f93ad1586c322036 |
| tools/public-api-baseline/ViciOne.ServiceBus.Build.PublicApiBaseline.csproj | 0feb21ceb1a7d28920bcdde2570f668bd2307b4cdf838bf455692369fe91c6c4 |
| tools/public-api-baseline/packages.lock.json | 03eeadc5ef377c17f787ab65f41fb4c8a9c936bb7f7f4171111fdeec8a81cb46 |
| tools/ci/verify_developer_journeys.sh | f7bf8eeeecc9fdc62b5cc62b53da96bc0ac5ad58b833c25c335c10ae50bdaf00 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Tooling/PublicApiBaselineTests.cs | 9b169f88ad57ceec31c5955d6d6e2cfb8f1ae8a9f71f9ea0b7f34abf66da496f |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Product/DeveloperJourneyArchitectureTests.cs | c95c41f5363322845a94db53a03d51a6fe8945085d994e52d24b533dceed353a |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/RepositoryGraphTests.cs | 0666ff674181a8b694f9c9018fe610a479af3f59478da3d0c4fec3b892636968 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj | 917a1cfeb7a7ec52dc0f317b5f7068b26441f7f30855f06b9d73d6654f24da0c |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Requirements/ArchitectureFoundationRequirements.json | 7be5c2d12835032ae59caae5208992bb6933997b1922f89c5ae6a3196d284a22 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/packages.lock.json | 60d4ee23a6c0dfc2697ee4f2f1d56f5a131babfee43ba1bdd1b86b228ec635db |
| ViciOne.ServiceBus.Engineering.slnx | bc11986b349179fc182a6cf8ef222f269d09d8f3361b27a72c2db675ec3fee72 |
| ViciOne.ServiceBus.Tests.Unit.slnx | ac102688f92ab8ee214af159e05703a6d0bc43dde268efe78c4ca41e82f45091 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/PipeConfigurator.cs | 1eff3899a69d32da3d002a42cb96146c7b08de35e0cb52f8ed9cab4ed1b0bad1 |

## Terminal raw evidence

| Path | SHA-256 |
|---|---|
| red-build.log | c58bb281f9867dba0885477b0c67af07ac1f87158584367ea2794dcfe0dc8d71 |
| red-build-corrected.log | dfc2bd7ffa7ea47e066634130a392d18f0b7eef9f0e1d24a924a3789d5bbd663 |
| red-tests.log | 3d358665037e7647f660b1f1b162fb61f409bd78c513a0cea49506686989689e |
| red-tests-corrected.log | d749b4074f7bed4fcc7bc10a951afdea62effc2d4a77492738e75d93a83fc8e1 |
| green-build.log | 3c19c16304a9a1ad525723944bd5b9cdaa2ea1efd7491d466e24f3a1a8a413dd |
| green-tests.log | 59b205475fa82a65b3529ea3cdbeb99243a684c36b12cfe40097833075dba0ec |
| M00-cursor-build.log | e2c065e5806fb6f054981df720b86f6329cf65d8635927a27eb3d7761298b11f |
| M00-cursor-tests.log | 5ba6a0bf59ec064f9b4587881668f4705b95b6a63f9d3681090f8c6dbfd919fb |
| M01-segments-build.log | 2b19988517eca757ac081708c74481ceffdd7938f54fd84f47542cb85afb0557 |
| M01-segments-tests.log | 2777044868ad45222f2138536d463f07726a9795a70a25d1910335bae423bc45 |
| M02-array-build.log | 6f842fea3569eac4a8d0110b128627e41a208b8a18437cbfc57c873d9d068611 |
| M02-array-tests.log | cc4e5ccb098eac2c5b279d188e7166fa79644d0bccd498e924b168f36eace868 |
| final-build.log | 05e3bdc86a843fbf8a90c92f8cb8b2da721b1b21f5b1dddc363d298f9f59fac9 |
| final-tests.log | 7558300e992ad90c4227a03016b836fabc942d076f3912ade371bbdaf87f584a |
| tool-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| tests-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| source-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| red-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_20_42.380961.ctrf | f3d49d93af1451191eea5cbdc8c0a84605a334477d0e1711335ce679a4c7760d |
| green-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_22_32.274228.ctrf | 07ef3b5a9d868943a416b6e6a914a216418b7a693d26b8006f0808d61bfbf657 |
| M00-cursor-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_25_15.160155.ctrf | 5c7115dd9d7edf2d3d6d3760165f2bda35cfe74ad1a00feec9cce06881f04de2 |
| M01-segments-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_26_30.155859.ctrf | 9dd4a35129b4239b3837bf1005ea1629018f0124ea8803a50c22ebaacffff4b4 |
| M02-array-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_27_56.506814.ctrf | 3bdec161e6d292dd63e5388c7446f0507a2e2bf82b33ad4213d18e99da988822 |
| final-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_30_10.575940.ctrf | ce9ed17bf54fe46449b2a3173930d84df08d31cb72d1127b97aa326423406e6b |

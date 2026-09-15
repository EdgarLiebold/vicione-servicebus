# Iteration 119 — governed traversal and generic contract hashes

SHA-256 values are manually recorded from observed read-only hashing. Canonical
paths are relative to this repository. Raw paths are relative to
`/private/tmp/vsb-iteration119-api-contract.9mkrjp`; those bytes remain local,
not uploaded to Git. Bindings certify these inputs and results, not whole-goal
acceptance or an unexecuted coverage/cloud gate.

## Canonical source, test and contract inputs

| Path | SHA-256 |
|---|---|
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Build/MsBuildEvaluation.cs | a66a17c405cd379e1e24a4d2164b88e0dfbf4002bfe8434affce761c36c7afe7 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Tooling/PublicApiRuntimeContractTests.cs | 4307b28bcca465a9021b8e6d15a1a8c1ce111c04cc21c8b2397f5a19e6bf1f70 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/RepositoryLayout.cs | 326e412c06597e85b1c47f7557a6dcfc88a3546d736725504b3b2f6cbd726603 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/RepositoryLayoutTests.cs | 11d08bf1faa3fce21dae486ed8fd49d7483f749f44c35806d323e502989836c1 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/RepositoryGraphTests.cs | 15571481c01454521fc6f158a1a30d4b41c3e1b01cf34fb0f4358d12c51bfa3d |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Repository/VerificationCapabilityDispositionTests.cs | b1643cfcb6e6da0bf3480b363192541559df7163f2cd87e48768d289cf641b40 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Tooling/PublicApiGenericContractTests.cs | 38765fbdd06f667cbfdd2734149baffca71815acc72ffe9759e9e5dc4e61b206 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Tooling/PublicApiBaselineTests.cs | 9b169f88ad57ceec31c5955d6d6e2cfb8f1ae8a9f71f9ea0b7f34abf66da496f |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Requirements/ArchitectureFoundationRequirements.json | 2c7efdc9eb2780a0b20b75d0a15f2e0496e81437f47b15b41570033975da43bd |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/ViciOne.ServiceBus.Architecture.Tests.csproj | 917a1cfeb7a7ec52dc0f317b5f7068b26441f7f30855f06b9d73d6654f24da0c |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/packages.lock.json | 60d4ee23a6c0dfc2697ee4f2f1d56f5a131babfee43ba1bdd1b86b228ec635db |
| tools/public-api-baseline/PublicApiBaseline.cs | 860384083a31212ecde0fa0fb98572651d935320963edc60d9fffebc398da447 |
| tools/public-api-baseline/ViciOne.ServiceBus.Build.PublicApiBaseline.csproj | a0d2692676e17995f38c812e2ce7772e40b31833b8404fd8bfdcdbf19c4bcc5f |
| tools/public-api-baseline/packages.lock.json | 03eeadc5ef377c17f787ab65f41fb4c8a9c936bb7f7f4171111fdeec8a81cb46 |
| tools/ci/verify_developer_journeys.sh | f7bf8eeeecc9fdc62b5cc62b53da96bc0ac5ad58b833c25c335c10ae50bdaf00 |
| docs/api/packed-public-api.txt | 59ea05a49d8d99e64715ac60b79bc68f9b657948f0742fd9c3d3e972babd054b |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/PipeConfigurator.cs | 1eff3899a69d32da3d002a42cb96146c7b08de35e0cb52f8ed9cab4ed1b0bad1 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/PipeBuilder.cs | e0f1c207b009b61866d63d367b5f4bc235424e3cf953127a0aa0f1411f98b762 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ChildSpecificationPipeBuilder.cs | 45582ddfa34dd8f0372063950d8fec5668ef870faae2983561f4021ffa0f3a86 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/SpecificationPipeBuilder.cs | 22acf3c7a064de9fdb0bfcadb207b6b7caa1d322cb02a97c16a3f5b1f24f5961 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/Filters/SplitFilterPipeSpecification.cs | cf1db499039c6971e419706abda47b86e170310d2a86501ef9c5ebcdbac10542 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ISpecificationPipeBuilder.cs | 0f6a335f30828000b0f71487baf85d2142cb35be0814d7e7e1463eeb67857871 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/Send/MessageSendPipeSpecification.cs | 32d3ab1e8d12ccc6ea66e95f2ba6d73a15f96b90fb4527a7b27ae62451be1ee7 |
| src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/Publish/MessagePublishPipeSpecification.cs | 5d1388aa301f3238d18bf5230457a46501965875f729997ebbb335e8774f7b79 |
| src/ViciOne.ServiceBus/Configuration/MessageConsumePipeSpecification.cs | f053e7cefb69200a91d37b8d5365841369ea529abef5921cb6b5dee663d020f0 |
| src/ViciOne.ServiceBus/MessageData/Conventions/MessageDataMessageSendTopology.cs | ac4484745b79dcfef8b004ceb61e71f515432ced09449bf294831067ddf170c0 |

## Terminal raw functional, mutation and validation results

| Path | SHA-256 |
|---|---|
| scope-red-build.log | 991211114c807743a0b2cdf7ef820ba3943d29d77bb1bb3da60df9565d1216a5 |
| scope-red-tests.log | e929703475aee98353b96713a3e35e92c15fa748fa1e02f10ade301a40f59e89 |
| scope-red-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_43_32.051236.ctrf | 9479e1e632cf833bd6eba62667aee650e069139dbddc5e6f187c5c051223de68 |
| scope-confirmed-build.log | 1aadea43a0a0ac2a4fbb2ae63ad59d20d20750d6274973583bb7929301742836 |
| scope-expanded-build.log | ad6d6e243a7e4ad91440cf6f10478e9d2c2b89f36d4fdd3e3bf5b356c16b3ce0 |
| scope-expanded-tests.log | bc0f565d07bfda4144624b21bcafa5dc9430de68865ac48555869d70618eb63a |
| scope-expanded-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_57_17.169812.ctrf | 87339579b1b0cc4b1cb5144886f210cf00613a4a29734b7442400f4a9dfb4714 |
| scope-M00-build.log | 3ac051887dcb4385f20e466952ef7eb38a82c366a84e1cc43957c36ce825b85a |
| scope-M00-tests.log | 0139aeaa65c73c3e9740599b0f021151c3b1888e8298bfaa8a22524eb5fdda59 |
| scope-M00-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_53_19.961325.ctrf | 92b3d3bd2c4917063095a9912ce3b17da0539b1cf73e028b401f11362f05f7b3 |
| scope-M01-build.log | 25473386c1d090c5d98b3aa85ecc5ab6c019b8d2bb780601e7efa69c83c4747c |
| scope-M01-tests.log | a25cc4464e46bbd73891dd53384707919ea64de29a9e934815299f97592f98a7 |
| scope-M01-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_54_29.311200.ctrf | e21232feed153cd6db40309e3012ae0da1857dd22526e31c0cdb85bbf26669ef |
| scope-M02-build.log | 24836674adc50aafe3ea794057e7e1a26f3d1e9505fe38d944d4df16680b6921 |
| scope-M02-tests.log | d61f93422b4def2901441871795606a3c485d7c2dcd383f700c3334f7b3974ab |
| scope-M02-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_55_27.858561.ctrf | dd07c1931488da1dade8df222ae11a78e25eab226949a9c41f2705e5f1b35861 |
| scope-M03-build.log | ea5b4a3263f972632f7f893c10e0ec4a04706edc0a02201598fa39a9685948a9 |
| scope-M03-tests.log | 21525a2bbef51f3a3791da2933fc69180e659a0ae5979c32881cf485cd8cdb47 |
| scope-M03-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_13_58_18.001531.ctrf | 0ff84e25b914e511cd83d94906c76d4ddec739c809a8c977cfa1b071c98fa38d |
| generic-red-build.log | 511092ff509979123f8ac4cccc6394950a6763d1747b4d71cf5c698676b173dd |
| generic-red-tests.log | 8d7cbd6813ad55b5efb029960ea27849d440d4649a67af63a86d1e739584e283 |
| generic-red-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_00_43.184575.ctrf | eafd079ce7e24fb05b3c49d6fd49cbb789fe1d917d946b397725d1a71cb30a75 |
| generic-green-build.log | ab69c0d1ec01107d2f7fdf14d69693cf9ec669edc6715c9ce7d7444177cb7b98 |
| combined-green-tests.log | ea809d86450f25bff0c6da4b68116fc178621b64b48c4ac5ecf6e0430361bf43 |
| combined-green-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_02_59.777357.ctrf | d8d0307541c5c820348a37d3d15b320768a861f3569bd99b98c332122152ccd2 |
| generic-M00-build.log | af77ca8082c885726ecb3ff864d5d4d98aaf7fdc8b3d89deceb154e7e65b81fe |
| generic-M00-tests.log | c95deb06157098adfc9a6bc1a23a29109f5f25a3b422aaaac5c932a5c77f11ac |
| generic-M00-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_04_35.044002.ctrf | d040369bc89e357834c6feda5a34d876c0f5f4b505b8feb431b5e7a2950004a8 |
| generic-M01-build.log | fc2f58a102d33e595fa645617b1224e334834c737fbc6435add5ea3609a33f36 |
| generic-M01-tests.log | 27d64a4b012bfe065d49901ade0d5039c6e9820fb16c42870b987b1e18682858 |
| generic-M01-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_05_49.640439.ctrf | 703c17b73ee261a0d613a37df8122b18fa5ca7b4506bad0eb94b2a339b901c3a |
| generic-M02-build.log | 1c3b8eb4a430c62b157ee59a5a8219a830e7618842a50443fe6f06e6d0d87ee7 |
| generic-M02-tests.log | ea709a8b77b0f6dff3e0257a58492b6d50978f31f79d5e4edee9f0fbe0a244cd |
| generic-M02-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_07_10.852808.ctrf | 01760526e6acda0deb2057a8e5d9e1e51349159636987c128c73a9344f7bf4b6 |
| generic-M03-build.log | a4e5d0feea9ffec51fcde39dd78fbcafce2b0dc4b1a61de0afa12b651ff716b2 |
| generic-M03-tests.log | 964d33e52b3ce3a36dfae3ca3e0e97f22aca59d7b51c424d69080921f56aea3a |
| generic-M03-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_08_41.637692.ctrf | 9be7027870a08c2a7d54c1d5d55778bb1d55ada3a4edb74c803a6604c6f6ea93 |
| generic-M04-build.log | 61fa14b0010dbfde61345feee30b24e7766974a72b61e7e5ef3a5bbda18736b7 |
| generic-M04-tests.log | d86e19bcfee830caf1ba028bdb3c3291ed270b19c8c341a3ac06456ba0732741 |
| generic-M04-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_09_48.501520.ctrf | 3592f05a4172356372b3abbe242ce7c616cbb7abaa592b60ab255c28bce82251 |
| generic-M05-build.log | 5b531e45632a1d21aa35c22b275137f8cb97505fa3f4b0c6d7259f07eee7110f |
| generic-M05-tests.log | 28a057ab0469581797e4f8859a9154ed2c3bc85ae282aed1f18ec813428a9c48 |
| generic-M05-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_11_11.511916.ctrf | e54adb2f099a6bb3cb67010a8fdee73c6a2a73d9546be1c012e15b673de995c6 |
| final-owner-build.log | d5b9e084cfc9bfb797d1c07e70d805356b25b52231c64b0b3ff03c2fb604af3c |
| full-architecture-tests.log | f7585e10df0969353e84592b5a4cadfdac900f8974f3db05fbde8a55222e3ed8 |
| full-architecture-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_12_30.532253.ctrf | 6416215fc9e0aa636183625b6081d863871e21a9a2ce98050c794b470be791de |
| tool-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| architecture-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| source-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| tool-static-pairing.json | ef7124091a0a7624f7ae06e7fd81acbcb5b8dc6709b7e57e57157da445cf10a2 |
| tool-static-pairing.log | 5cb9a523b58c3fd22bfe91057d1c63f80832fa40e733e2bd3eb605a68ed12954 |
| framework-red-build.log | 0838039330c41b5819b904cabed7df2e68e4899db95af8725b964ed43d46880b |
| framework-red-tests.log | d2c4640a851a42e7f56ec4f680868ec82a87c3d872a2f34c4193dd2f1c9838f4 |
| framework-red-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_23_44.512835.ctrf | fa00a86efdc130e318460e0e7a973359ac25271e683f7f86d0709817045ca665 |
| framework-functional-red-build.log | e9546f3929c658e13035c0fe33c51991380b8b33fe00ea956f0ba7291153c289 |
| framework-functional-red-tests.log | d14eb93c2e7e45696046a762013fc4d7aac5da2bce2275593afbf6278d56af17 |
| framework-functional-red-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_29_02.846890.ctrf | 6db6286bb9f908e30b944ffacc9cea282d219b6c9978d243db5c2089aa4adc0a |
| framework-restore.log | 864850951b0d7daec96a5e5335ef7911c66dbe0923e2988523c09b48e260c9c5 |
| framework-green-build.log | 6f87599de0a6417ef5525fcb05d8260000df1cecb8c00b566e33087447154264 |
| framework-combined-green-tests.log | 198371ad8f869e0053cd5db1182b9d7c416a4cc8978b600c4fe1cc618cdd65ef |
| framework-combined-green-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_31_19.618109.ctrf | 2d7c201ba21e717f150298af2a5e5736d8829bf6c10e60bb1c6bf29433523da3 |
| framework-tool-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| framework-architecture-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| fresh-package-gate.log | 087f8c7fc5612646931e9a5eaca3bf08d45aa3acbb6a3cec60ebaf9a3e58c3de |
| framework-fresh-package-gate.log | 731068950485d521713f9d3a7086346a2aa3699ae88e3645a364785cb99b5b3c |
| framework-fresh-packed-public-api.txt | 96436e3b855dee3c678da4430a01c0947b14126227faff5a37be040455790676 |
| framework-final-owner-build.log | 7893982b6e54faab5fc7b31153c24a5fa70df20153c19532cdd16cfaa0311b46 |
| framework-full-architecture-tests.log | d2c6212e375e4296609ed0d9ed5be3943c35725fd8d2023a1eaa129830d35f6c |
| framework-full-architecture-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_14_36_23.670648.ctrf | 6efcf3613a808fd32d14ba2d83e8f7bed61892cb0bdf1f93c5629961ecb6856c |

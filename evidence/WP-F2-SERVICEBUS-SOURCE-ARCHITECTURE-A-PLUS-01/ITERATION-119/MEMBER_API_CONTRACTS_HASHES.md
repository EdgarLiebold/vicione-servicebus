# Member API contracts input/output bindings

All paths are relative to the product repository unless explicitly absolute.
Inputs bind the remote-secured 9990fe12490d2330964c4e53945097c0688fbff6 tree.
Current source files are documentation-only changes after complete personal main
reads. This manifest does not claim all-source reading or whole-goal completion.

| File | Starting/current lines | Input SHA256 | Current SHA256 |
| --- | --- | --- | --- |
| src/ViciOne.ServiceBus.Sagas/Sagas/Correlation/ViciOneServiceBusStateMachine.UncorrelatedEventCorrelation.cs | 46/46 | bb1b65498af35bb320eb238ad438b7cdcc664a1bb14fee0489b3e2607cf60b83 | f282372c2b797236544645b78912b964a244b4083b7ad08a41832da50dc191e1 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.BehaviorContextProxy.cs | 205/205 | 966de686aff47f32dabe81c87b121cf88ee3e9d9bdbee73fef54f1a6b4d8885c | ffe763c7363727788c97a88a34c81989a24fc8922862eb4e1a3aebcf1213f61f |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.BehaviorExceptionContextProxy.cs | 78/78 | d86b35be4ed7090e55d2ab8b8511e57a28766ae684ff7098420007fa774ace69 | 7fee578f53c5b0952c862d4c3662a390f09bb9818b7444c6dcfa9989e0524a55 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.EventObservable.cs | 71/71 | 75c4c47ca6a5ae0e8c27e5c6d23de31292425d854b60fc6a1bb6115f04e6b085 | 735f04955de0289304123d11800f8e5150d7ac6ad377e7aa19ca1eca1758eff6 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.SelectedEventObserver.cs | 93/93 | 2edc7ea5a3a0defa28ab7f60a5b5234011ef23ed84c803e1cde0625e0152364e | 0a06116d21287e5d91516499a51c0757cd574760043a5aa178e3574e494738a5 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineRequest.cs | 191/191 | 3458d89591dcfbcd7d1c7bc107ed7f09af2686c5ea1c2ae24c0730b422afd1cf | b6dd149dcf97190b2a4ff698f671a9fd2370480851c3f45d0dc841c65571b687 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineSchedule.cs | 66/66 | 5cee7c23ca51713e3a8941eaf611b68ffde66e6bad11eb543f592e3b6a509ca2 | 7942013618ec7a579b2c366137299638b5f13059687591fec9cfa9c7af752a7b |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateMachineState.cs | 390/390 | 9ddf2ce7117c2d79789a6dee9d40393a997edd20d50b09d15963035dd56c2c61 | 83dbfbfac554a62f53d605222b11ee02abb87a1e540132543982ce59e3e490fd |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.StateObservable.cs | 24/24 | dfa9c0fc8be460aad3ad2e98556f7ae371a90626ce715e23c99e2edbdbdfd8dc | c6e6cab93296c5e3da79b9a7cc7f37b702f29b00e2741909bf6fd1200cd835b9 |

| Current contract input | SHA256 |
| --- | --- |
| tools/public-api-baseline/PublicApiBaseline.cs | a4178ebc8861ef1a029fd28f2107527c88e3978408e1a2b1a2a01d57a305a564 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Tooling/PublicApiMemberModifierTests.cs | 65d5804c725d9ec6d6b7106beb56ab2dc4d0bed4a1760338ce221c9d7d5ccec8 |
| tests/Architecture/ViciOne.ServiceBus.Architecture.Tests/Requirements/ArchitectureFoundationRequirements.json | 548df903479544026a05c3527f3aa2b677679e26302c6bdd36ad8b6b4ce1aa6c |

Raw build/native/check bindings are appended only after actual terminal outcomes.
Every selected mutation must restore the exact current tool checksum above
before another candidate or final acceptance. Historical package/coverage outputs
are not relabeled as outputs from this current repair.

## Terminal raw observations

The following names resolve only under the owned absolute directory
/private/tmp/vsb-iteration119-member-modifiers.EXN6Ek.

| Raw artifact | SHA256 |
| --- | --- |
| red-build.log | 3361630e5e5f1d5c5408502750a600807a4c28e44a0ef1f9c64128d4b34ca5fd |
| red-tests.log | b3de8da8da3c40df836bda67f1eee4efd3a3ed332af492c17969c2b20e175980 |
| green-build.log | 645183d35e5ec5bbd996560d985738f3643864906ffef234c345a1e10acc3683 |
| green-tests.log | cfc2b854cf6ddcefae4e3fc6c6fdcfe805c0515ecf58a348be63973edff52010 |
| final-expanded-red-build.log | e1a380933090c5337d325abf35fe2f30642510dc8def4c46d9ba22f585b2aa4f |
| expanded-red-tests.log | 6954f945fb20c9bf3264c05c2b8356813d199148cc751d3e8b37229f212c5b24 |
| expanded-green-build.log | 50721549c3e329620f76429659a0f502e4b6ef35f5189860f13d90351a2e0fc9 |
| expanded-green-tests.log | d65913fe6db1bec0eccf64c09901acc8d58efeae32f13b2ce515c505a8a4b85a |
| final-architecture-build.log | 7f391eec9dc945143990052eb72550ec199e9a58f18d3b6877f6b1301ddee30e |
| final-core-build.log | 567a70175e7a21ab7d3ed564ad2b2d964eeb61ad400c5d19733bdeeead6bae7a |
| final-abstractions-build.log | 4fe7ef8d7a1706d190c3a72886be196d2a0e5b78890875945aa0838794ba1759 |
| final-core-tests.log | 363369f285c2a951413408d7d515ac3928610b55c702f853ca8fa4bca6e82455 |
| final-abstractions-tests.log | 52ff6bae083ed213bd3a8597825516d501c067481ad80fd3093dc6349ce694ce |
| final-source-documentation-check.log | 389e894a200b571fdb64386dbe0b34173cb6d5bf7fe76f5a3f721380bd275713 |
| catalogue-source-check.log | a746a86e567d599f588440774947b33f3812e9df77949761ec98268bb88439d5 |
| mutation-build-check.log | 18017c5928b41f5b45be6d7fe85cea67122fb6a3177f476af266976c86d82b1d |
| m01-build.log | 57ab40bfe3025aa3742f0cc67b06ce7054a395f7c7c47257cd7b6371c74c7f26 |
| m01-tests.log | 95f7ce051e4ea0532dd364d91195af466a3c69b41d62fbec0257d1a01cd056ff |
| m02-build.log | e04a87ee1472677852fddc720a8599e556117ecd32aa0dea05f5014fdaa56d01 |
| m02-tests.log | 5a0bef208601cb96a3102183df00855252c4bef6cbac73a5272e1f9b51b8a9e9 |
| m03-build.log | 58909b5ddd37039d90dee1e8458f96c879a130908a673847f3eb3046b08b139e |
| m03-tests.log | 4c436baf89871b2c2ecd27f6a0a2817fb5fe0be3bbafde6cc4f2755b5a81edf0 |
| m04-build.log | 929c0a301676fa35f319ff47e5fdc94a8a125c3ad49d020a44907f143cb4115c |
| m04-tests.log | f22326fa17814664b2577f96100b508fa85cf690b4d8f8eb2f64483b0ddd848a |
| m05-build.log | 96b17aa988b87c5d7ee3b83c4f7ccbe9a98407d9c66fae749ae1bf7e5a7c2fe8 |
| m05-tests.log | 4e917d4d088040a022325b4d2390ca4e472a0ee0d10c330d178545d296e135c3 |
| m06-build.log | 9d5048840d8fd0180ad9bfa8d4c8133e05f012f142bd49d8ca0b9520ccbc789c |
| m06-tests.log | c2eef6ecbf8f06756bd4f123677ef9878cc4ac3b706a76ff4f093cb21a5cc70b |
| m07-build.log | eefe39cf1a19f15697a7933971cc042586f14ed70a983c2af38013c191f486e5 |
| m07-tests.log | 2432a74d856657da1675866483a2c81fb7815abbe95f4e2b6cfb866d07a5cf57 |
| m08-build.log | ff0ab1d83f4393966d48b797a566a2c85b52c5c3dad5625c62111bbee3466758 |
| m08-tests.log | dbd9aa0cf16b8f92503b9a828745390db28fa1fd5f85d325e6e97e2874a26491 |
| m09-build.log | 8fc942b7e24a73011bf005076465d4a8d09c6ec1ec9c10c7f087c7cdd090a164 |
| m09-tests.log | eaa9bf0a220154ff6540a601707224c66866e93c9cae39d341b9b2e610a45df2 |
| m10-build.log | 73dda393915e2adbab473750ccefbf2d8f23c60a486a319eb2c907ba1b2dbd63 |
| m10-tests.log | 1aff5880caaf0164f66471124cdbc1f08daf50c37a3b7505f563087632fe0048 |
| final-source-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| final-tool-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| final-test-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| final-architecture-tests.log | 22750c8c03994b672a78f3e6b7155c3fe750c46d037cf81db8d6cc9be4aafa37 |
| fresh-package-gate.log | 3be227ab01826e59835ea141e4b5165d3f4b9616af4cf1323f8dc60dad1655b6 |
| fresh-packed-public-api.txt | ed29376e3e214ede55083913ddd8d12d0dcea080075d7472d0f51ca8b4d7ddde |
| final-tool-restore-check.log | 1bac0d61067fedde53d1f5d38d9a8fbcd374a67f9b8ff3e6488f6c167590b20d |
| final-architecture-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_15_48_15.335011.ctrf | 69aceabd39a7bb62c77d2cdf26a7ba039a195c7bbf965c5d1234dad903ab6563 |
| final-core-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_15_48_36.283454.ctrf | 4b8d94f53bcbf28b6e9e5deaeca4003c13dc7c490875c3809b7fa5e8a24b08a9 |
| final-abstractions-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_15_48_40.726226.ctrf | a1830758d0d7d8511dbb089f1e5701cd016de7dcb1db3cf66dd8a43084ec472a |

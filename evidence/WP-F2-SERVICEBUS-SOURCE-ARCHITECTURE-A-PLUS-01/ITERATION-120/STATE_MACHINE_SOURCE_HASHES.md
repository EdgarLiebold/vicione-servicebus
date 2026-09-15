# State-machine source input/output bindings

Inputs bind remote-secured commit 3a7386ca27250bffaf482b828833be643c554e23.
Source paths are relative to the product repository. The main personally
completely reads all six files; the bounded internal Sol advisor separately
reads the exact six current files and verifies unchanged entry/exit hashes.
Neither that internal advice nor these bindings certify the whole-product goal.

| Source file | Starting/current lines | Input SHA256 | Current SHA256 |
| --- | --- | --- | --- |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.cs | 2266/2227 | 2ee6d249f0d6bfb0c9574e638c09114ebf08d91c6a99e00ff232aa69694e8b06 | b34154afbdcca7bef514d35fd589519f2ae14c7b9d2bdb1f940cb87f5856eeae |
| src/ViciOne.ServiceBus.Sagas/SagaStateMachine/Activities/CompositeEventActivity.cs | 121/121 | 3854fb0131758679682bb780b5d460280e09ee08d06d24533637be7b957dfe07 | 324b8b06286d6a5771961a0d8f3d3bb6ca60af40851fa7324efd21d58a7f20fe |
| src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.InitialIfNullStateAccessor.cs | 53/53 | 17ede1d4d92a66469eca366106de44527edb0e4bed157b266f2104dcc7386f45 | 17ede1d4d92a66469eca366106de44527edb0e4bed157b266f2104dcc7386f45 |
| src/ViciOne.ServiceBus.Sagas/Sagas/Accessors/ViciOneServiceBusStateMachine.StateAccessorIndex.cs | 57/57 | c1f0257eb824756c6869cc272979491d87e046aed148db9a0f0933a9f959dc13 | c1f0257eb824756c6869cc272979491d87e046aed148db9a0f0933a9f959dc13 |
| src/ViciOne.ServiceBus.Sagas/Sagas/ViciOneServiceBusStateMachine.NonTransitionEventObserver.cs | 74/74 | 5e367e6b06600609a1366fee6b9017a9a9206039d8cf17e893176b507a69ba32 | 5e367e6b06600609a1366fee6b9017a9a9206039d8cf17e893176b507a69ba32 |
| src/ViciOne.ServiceBus.Sagas/SagaStateMachine/Activities/TransitionActivity.cs | 176/176 | 667e77ebc54860691ad367d58bd1a5d806ad34bc9dbbf71837f7d7b71b611c55 | 667e77ebc54860691ad367d58bd1a5d806ad34bc9dbbf71837f7d7b71b611c55 |

## Secured input remote receipts

Both commands actually terminate 0. Independent reference-keyed validation
matches the exact branch, annotated tag object and peeled commit; the shared
branch/peeled commit hash is not collapsed into a hash-keyed map.

| Absolute owned raw artifact | SHA256 |
| --- | --- |
| /private/tmp/vsb-iteration119-member-modifiers.EXN6Ek/checkpoint-push.log | f8ff7df1aaaecdd4e7d6f34ae1aeccd82c4344d989e803ef4d5150403cbdc204 |
| /private/tmp/vsb-iteration119-member-modifiers.EXN6Ek/checkpoint-remote-references.log | a7d389217362e723d82882953a49e6c601ffd84748b9444565831783733899a5 |

## Current terminal observations

Relative raw names in this section resolve only under the owned directory
/private/tmp/vsb-iteration120-state-machine-read.58YZjf.
Actual build/native/format/comment-comparison artifacts are bound only after
terminal outcomes; protected product review/result trees are never searched.

| Relative raw artifact | SHA256 |
| --- | --- |
| architecture-build.log | 63e40d04f95509dcfc011b19720f90f4b4cab7e13a8c118800c2eb20b59dfd13 |
| core-build.log | 1c9562a6685bc1b97c174d46232f599b2b136205c560157a431a0aae06bb736c |
| core-tests.log | bd4342cb10aecfd83515cd5b4e029b35dd82349f4fdffd41791ab55f0feb0df0 |
| core-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_16_08_39.762786.ctrf | bda7f14ee09917777935cd22471175f5d698579713243448821d06ce6b86cb50 |
| source-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| source-comment-equivalence.log | a895a0758f0fef3781875ce1787c99a0cff97378eb0feb6a389fc72322c15dd3 |
| architecture-tests.log | 0ec0a1f5d1f39fe980f084c7e498cbea58a15080ae354a4a617c3d48d002fbbb |
| architecture-tests/edgar.liebold_Edgars-iMac-2_2026-09-15_16_08_34.230371.ctrf | 9ed1360c152f27368312ff1bcb56d620663161dbe5a514e4a056b0553a9c4368 |

# System.Text.Json contract-shape mutation validation

The five valid one-cause mutations below ran in a disposable copy of the complete candidate. Every
mutant compiled before its unfiltered native MTP project run. Each run exited with code 2 and failed
for the intended reason; no mutation touched the canonical working tree.

| Mutation | Expected owner | Result |
|---|---|---|
| remove `StringDecimalJsonConverter` registration | maximum-decimal wire contract | one decimal case failed while reading the required quoted value |
| suppress `CustomMessageTypeJsonConverter<T>` registration | envelope/raw extension-data configuration | both extension-data cases failed: envelope produced `ReceiveFault`; raw exposed unrelated envelope fields |
| remove `JsonDerivedTypeAttribute` from the test contract | declared polymorphism contract | all three scalar/array/list cases observed `ReceiveFault` instead of a message |
| replace the consumer-supplied response cost with zero | immutable response verdict | exactly the constructor-bound response case failed on `5000` versus `0` |
| remove the decimal row from the embedded requirement projection | requirement completeness | exactly `CoreRequirements_MatchCompiledRequirementMetadata` rejected the unprojected compiled method |

Compiler failures and infrastructure failures are not counted as mutation evidence.

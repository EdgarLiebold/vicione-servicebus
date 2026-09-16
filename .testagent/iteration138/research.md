# Iteration 138 — initializer provider lifetime research

## Scope and acceptance

Whole requested A+ API/source/architecture completion remains the goal. This connected packet repairs five implementations in four personally read files: ProviderPropertyInitializer, ProviderHeaderInitializer, SetHeaderInitializer and both AsyncPropertyProvider generic variants. InputPropertyProvider and TaskPropertyProvider are personally understood unchanged collaborators. No source/test/comment authoring generator is permitted; every productive file and comment is manually read and authored. Features, signatures and caller-owned task-value cancellation must be preserved.

Required behavior: started provider and converter tasks remain observed to their original success, ordinary fault or provider cancellation even after local caller cancellation; exact context/input/token and exception identity; property/header assignment only after successful resolution; synchronous guards before effects; precancellation starts no collaborator; missing input/inner task preserves default value; null outer/converter tasks retain exact diagnostics; caller-owned inner values remain locally cancellable and unsettled; TaskPropertyProvider exports the exact original task without awaiting it; runtime message ownership prevents assignment to derived message instances.

## Existing evidence and conventions

Parent is the normally committed/tagged/pushed and exact-three-ref verified iteration 137 commit 0667256e619d3f0434ec3887425bb15e11b6229d. Its 4545 passed Core cases must remain as an exact name/multiplicity multiset. Existing manually read fixtures include MessageInitializerContractTests, MessageInitializerLifetimeTests, PropertyInitializerContractTests, HeaderInitializerContractTests, PropertyProviderContractTests, PropertyProviderStateMatrixTests, AsyncPropertyProviderTests and TaskPropertyProviderTests. They use xUnit v3/native MTP, requirement attributes plus explicit additive CoreRequirements bindings, controlled original tasks, independent cancellation tokens and actual BaseInitializeContext/MessageSendContext objects. One property-initializer state assertion currently requires abandonment and must be corrected, not removed.

The static pairing analyzer was invoked once against the narrow initializer root and terminated with exit 2 because tree-sitter-language-pack is absent. It produced no JSON classification or suggested path. No dependency/SDK/restore change is made to run it and no test-gap conclusion is inferred from this failure; direct scoped symbol references identify the owning fixtures above. Static pairing would not prove behavioral coverage in any event.

## Source findings and related queue

All three provider-backed assignment implementations use caller-cancelable waits on their own provider operation. Both AsyncPropertyProvider implementations use the same outer abandonment; the converting variant also abandons its converter. Their inner task-valued properties are values supplied by input and retain local WaitAsync cancellation. TaskPropertyProvider deliberately transfers the original operation as the task-valued result and must not silently await it.

Scoped source searches also expose potentially related waits in nullable, object, variable, ordinary conversion, task/fallback, nested-message and collection converters. They are not certified by this packet and remain a connected queue requiring complete code/ownership reads and their own causal cases. PropertyConverterPropertyProvider and VariablePropertyProvider have already been personally read to establish that queue; their existing cancellation/cleanup assertions will be addressed with their productive ownership correction, not preemptively weakened here.

Native detection is unchanged .NET 10.0.302, global.json MTP runner, xunit.v3.mtp-v2 4.0.0 and CodeCoverage 18.10.0. Every Release compiler uses no restore, warning-as-error, disabled build servers/shared compilation, one worker, approved SDK-IPC execution and a unique binlog. Native execution is conditional on successful fresh compilation. Excluded review/TestResults/legacy trees are not scanned or edited.

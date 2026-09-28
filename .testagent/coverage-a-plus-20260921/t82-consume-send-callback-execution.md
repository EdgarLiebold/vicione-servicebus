# T82 consume-scope send callback execution

The frozen T74 profile showed unexecuted callback-overload paths in
`SendConsumeContextExecuteExtensions`. Existing tests already covered public
argument admission and common send-core cancellation, but did not execute
callbacks through the endpoint's send-context pipe.

One new test class exercises all eight callback forms against a recording
endpoint that executes the supplied pipe. It asserts exact generic/runtime/
initializer endpoint method signatures, original message or initializer
value, the caller token, one callback invocation, shared send-context
identity, metadata mutation, and transport acceptance. A four-row theory
holds each asynchronous callback pending and proves the send stays pending
until release. Sync and async callback failures propagate as the exact
exception instance without transport acceptance.

The read-only Red Team initially found two P2 survivors: confusing direct
typed send with the initializer overload, and dropping the task returned by
three asynchronous callback overloads. Exact method-parameter assertions and
the four-form pending theory kill both. Final re-review was PASS with no
remaining concrete P1/P2 gap. Three requirement metadata rows match their
compiled tests.

The Abstractions xUnit v3/MTP project passes 954/954 without failures or skips
on the exact test commit `e762e97a7`. Product code did not change. This is
an affected-project verification; T74 remains the latest complete
Line/Branch/CRAP profile. The next full profile is reserved for the agreed
multi-packet checkpoint unless a cross-project contract change needs it
sooner.

# Iteration 191 research

## Scope

Twenty-five current saga policy, state-accessor and delegate-contract sources, totaling 969
physical lines. Thirteen are new to the cumulative personal-read inventory; twelve are deliberately
re-read current versions of sources admitted before the expanded manifest chain:

- Policy/context/query: existing-or-new policy selection, consume-context construction, lazy saga
  predicates and the associated public interfaces/delegate.
- State accessors: default, initial-if-null, raw, string and integer storage plus state indexing.
- Factory/provider delegates: synchronous/asynchronous event and exception message factories,
  destination/service addresses, and relative/absolute scheduling.

The lead read every selected source and current comment before authorizing changes. Three agents own
non-overlapping source and test areas; requirements, integration, mutations, coverage/CRAP,
manifests, evidence and publication remain with the lead.

The twelve re-admitted sources are the two existing/new policies, both consume-context-factory
contracts/implementation, `SagaQuery`, `ISagaQuery`, and all six state-accessor files. They remain in
this iteration's content manifest but are not counted twice in cumulative unique-file progress.

## Confirmed work

- Policy paths need deterministic required-owner, null-result and null-task contracts.
- State accessors need direct lifecycle/index/default-property tests and explicit invalid-input and
  observer-task boundaries; indexer properties must not qualify as default state storage.
- Delegate-only public APIs need direct variance, constraint, context-identity and result-shape
  evidence. Exception-produced messages must use the same reference-message constraint as ordinary
  event messages unless an actual supported value-type consumer is found.

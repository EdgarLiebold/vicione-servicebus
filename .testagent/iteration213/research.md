# Iteration 213 research

## Scope

Lead read the complete six-file `StateMachineInterfaceType<TInstance, TData>` subsystem (389
initial lines) and its complete `MessageEventCorrelation<TSaga, TMessage>` policy owner:

- public state-machine interface adapter and event connector factory;
- message-correlation-id builders for ordinary and fault events;
- state-machine saga message connector; and
- the event-correlation configurator and builder.
- the lazy event-correlation policy selection and validation owner.

None of the seven paths appears in an earlier admission source manifest. On admission, cumulative
lead-read progress becomes 758/4,118 sources (18.407%).

## Initial findings

- Constructor dependencies are accepted without consistent owner-level null validation, so several
  invalid inputs currently fail later or through unrelated collaborators.
- Message-correlation-id builders need direct success, missing-id, original-fault-message and
  exception-identity evidence.
- Connector creation must prove exact saga generic identity, state-machine/event/filter dependency
  identity, optional message-filter order and topology selection.
- A null saga-filter-factory result and null correlation collaborators need stable diagnostics.
- The correlation configurator has multiple overload families whose selector, expression, query,
  saga-factory and missing-pipe semantics require direct positive and negative proof.
- Existing-correlation inheritance, repeated mutation/build snapshot behavior, optional missing
  pipelines and all default flags need explicit lifecycle tests.
- Direct correlation construction must reject each required owner in signature order, and lazy
  policy selection must preserve exact type, collaborator, configuration and stable identity.
- Writable validation must not inspect initial-state membership; combined read-only conflicts must
  remain independent and report both failures.
- Public surface, generic constraints, nullability, comments and synchronous naming must reconcile
  bidirectionally with tests and requirement variants.

The packet is synchronous composition code and must not acquire asynchronous names.

# Iteration 211 research

## Scope

Lead read the six remaining previously unadmitted saga message-filter sources (349 initial lines):

- filter contract and state-machine event execution: `ISagaMessageFilter`,
  `StateMachineSagaMessageFilter`;
- initiating paths: `InitiatedBySagaMessageFilter`,
  `InitiatedByOrOrchestratesSagaMessageFilter`;
- existing-instance paths: `ObservesSagaMessageFilter`, `OrchestratesSagaMessageFilter`.

The direct connector factories and existing boundary/cancellation tests were also read. On
admission, cumulative lead-read progress becomes 742/4,118 sources (18.019%).

## Initial findings

- The four consumer filters validate send arguments but their probe boundary accepts null, and
  collaborator-returned null tasks currently fail through incidental await behavior.
- Pre-canceled contexts can enter user saga code; cancellation after the saga completes can still
  enter the continuation unless ownership checkpoints are explicit.
- The duplicated consumer execution paths require identical ordering, failure identity,
  instrumentation completion and exactly-once continuation guarantees.
- State-machine execution validates its constructor/send boundaries and pre-cancellation, but its
  event, completion, state-accessor, terminal completion and instrumentation cleanup paths need
  direct behavioral proof, including unhandled-event translation and failure identity.
- The marker interface has no independent behavior, but its public constraints and inheritance
  are part of the compile-visible contract.

All genuinely asynchronous members already carry the `Async` suffix; probe and construction
members are synchronous.

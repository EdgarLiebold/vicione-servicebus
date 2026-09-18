# Iteration 212 research

## Scope

Lead read the complete nine-file generic `SagaConnector` composition, cache and specification
subsystem (563 initial lines):

- connector discovery and caching: `SagaConnector`, `SagaConnectorCache`;
- correlated/query pipeline composition: `CorrelatedSagaMessageConnector`,
  `QuerySagaMessageConnector`, `SagaMessageConnector`;
- message/saga specification composition and adapters: `SagaMessageSpecification`,
  `SagaMessageSplitFilterSpecification`, `SagaPipeSpecificationProxy`,
  `SagaSplitFilterSpecification`.

None of the nine paths appears in an earlier admission source manifest. On admission, cumulative
lead-read progress becomes 751/4,118 sources (18.237%).

## Initial findings

- Discovery category precedence is semantic and already documented, but empty-contract, duplicate,
  generic-mismatch, reflection-failure and concurrent cache publication need direct proof.
- Multi-connector setup owns partially acquired handles. A later connection or cleanup failure must
  dispose every acquired handle, retain causal ordering and never accept a null handle silently.
- Connector and specification constructors, public methods, builders, callbacks and collaborator
  results have inconsistent null boundaries; several invalid inputs currently fail incidentally.
- Pipeline composition must prove exact correlation/query filter order, repository/specification
  identity, topology-option selection and build/connect call cardinality.
- Split-filter adapters must preserve filter identity, validation order and context ownership while
  rejecting invalid wrapped specifications, builders, filters and validation sequences.
- Observer notification and repeated validation/build behavior require lifecycle proof; comments
  must describe the final ownership and repetition semantics exactly.

All genuinely asynchronous APIs in the packet already use the `Async` suffix; the packet itself is
synchronous composition code and must not acquire asynchronous names.

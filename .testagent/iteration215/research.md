# Iteration 215 research

## Scope

Lead read the complete seven-file state-machine graph packet (554 initial lines):

- immutable graph snapshots and the public `GetGraph` extension;
- graph node and edge types with their exact kind enums; and
- the internal state-machine visitor that projects states, events, transitions, exceptions,
  composite contributions and inheritance.

None of the seven paths appears in an earlier admission source manifest. On admission, cumulative
lead-read progress becomes 772/4,118 sources (18.747%).

## Initial findings

- Existing graph tests establish the basic factories, endpoint compatibility, snapshot behavior and
  one representative machine projection, but exact public shape and adversarial enumerable behavior
  remain under-specified.
- Graph membership is identity-sensitive while states are visitor-canonicalized by ordinal name and
  event bindings by state-node/event identity; those boundaries need explicit evidence.
- Edge equality, hashing, enum values and every valid/invalid kind-to-endpoint matrix require exact,
  mutation-resistant coverage.
- Visitor ordering, repeated graph reads, disconnected elements, duplicate traversal, nested
  exception activities and context restoration after a throwing continuation need lifecycle proof.
- The visitor constructor and visit collaborators need clear null/illegal-order diagnostics rather
  than incidental null-reference or sequence failures.
- Public surface, nullability, XML comments and requirement variants must reconcile bidirectionally
  with the behavior tests.

The three production/test packets are disjoint. Requirement projection, integration, compiled
mutation checks and final admission evidence remain lead-owned.

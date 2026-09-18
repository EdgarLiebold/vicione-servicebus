# Iteration 214 research

## Scope

Lead read the complete seven-file state-machine event, correlation-expression and message-factory
packet (1,670 initial lines):

- correlation-expression conversion and query creation;
- typed and untyped state-machine event metadata; and
- task-, context- and overload-rich state-machine message factories.

None of the seven paths appears in an earlier admission source manifest. On admission, cumulative
lead-read progress becomes 765/4,118 sources (18.577%).

## Initial findings

- Required constructor, conversion, probe, callback, context, task, factory, message and pipeline
  inputs are not consistently rejected at their owning boundary.
- Correlation conversion currently has only one positive example; nested context access, captured
  values, input immutability, failure identity and query/probe behavior need direct proof.
- Event metadata needs exact surface, generic identity, visitor/probe collaboration, ordinal
  comparison, cross-type equality, hash and singleton evidence.
- The message factory exposes a large symmetric overload matrix across typed/untyped behavior and
  exception contexts. Sync/completed and deferred paths must preserve the same message, pipeline,
  callback and context semantics.
- Pre-cancellation, task/result/failure/cancellation identity, null task results, callback ordering
  and optional callback behavior require explicit lifecycle evidence.
- Public surface, generic constraints, nullability, XML comments and `Async` suffixes must
  reconcile bidirectionally with tests and requirement variants.

The three production/test packets are disjoint and centrally integrated only through the immutable
requirement projection and final evidence.

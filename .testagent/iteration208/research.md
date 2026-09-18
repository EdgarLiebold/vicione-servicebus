# Iteration 208 research

## Scope

Lead read ten previously unadmitted saga sources:

- `Advanced/ICorrelatedBy.cs`
- `Advanced/ILoadSagaRepository.cs`
- `Advanced/ISaga.cs`
- `Components/RequestState.cs`
- `Components/RequestStateMachine.cs`
- `Context/SagaConsumeContextProxy.cs`
- `Contracts/IRequestCompleted.cs`
- `Contracts/IRequestFaulted.cs`
- `Contracts/IRequestStarted.cs`
- `Contracts/IRequestTimeoutExpired.cs`

On admission, cumulative lead-read progress becomes 717/4,118 sources (17.411%).

## Initial findings

- The public advanced and request-contract surfaces require exact inheritance, exclusion attributes,
  variance, constraints, task/cancellation shapes, members and nullable annotations.
- `RequestStateMachine` owns correlation and request lifecycle routing; initialization must preserve
  envelope identity, addresses, expiration and the required source-address boundary.
- `SagaConsumeContextProxy` changes correlation ownership to the saga while forwarding completion
  and state through the supplied saga context; both constructor dependencies require explicit
  ownership proof.

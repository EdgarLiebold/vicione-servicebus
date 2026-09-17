# Iteration 197 research

## Scope

Three previously unadmitted saga outbound-message extension sources totaling 1,421 physical lines
and 73 public overloads. The lead read every selected source completely before delegation.
Admission moves cumulative exact unique source coverage to 637/4,118 current source files
(15.469%).

- Publish: normal, data-bearing and faulted binders across message, task, synchronous factory,
  asynchronous factory and initialized-message factory forms.
- Respond: data-bearing, faulted and data-faulted binders across the same response factory forms.
- Send: normal, data-bearing, faulted and data-faulted binders with fixed/provider destination plus
  message, task and factory forms.

## Existing conventions and risks

- The owning xUnit v3/Microsoft Testing Platform project is
  `tests/ViciOne.ServiceBus.Tests/ViciOne.ServiceBus.Tests.csproj`.
- Iteration 194's Roslyn pairing scan remains the repository-wide static baseline. Focused search
  finds no dedicated direct tests naming these extension owners.
- The 73-overload matrix currently delegates directly into binder `Add`, destination closures and
  `MessageFactory.Create`; receiver and destination ownership is therefore inconsistently exposed
  to downstream implementation diagnostics.
- Several message/factory inputs may already be validated by `MessageFactory`; those equivalent
  downstream guards must be distinguished from material boundary gaps rather than inflating
  mutation counts.
- Fixed destination addresses are lifted into closures. Tests must prove captured URI identity and
  must distinguish it from caller-supplied destination providers.
- Publish callbacks are uplifted from `PublishContext<T>` to `SendContext<T>` through payload
  retrieval; null and non-null callback behavior must be direct and must not invoke early.
- `Awaited` methods synchronously configure an activity that will later await a task/factory; they
  are not task-returning public APIs and therefore do not take the `Async` suffix.

## Acceptance checklist

1. All 73 overloads preserve exact extension/public shape, generic constraints, binder family,
   return type and sync-vs-awaiting naming.
2. Required receivers and destination inputs fail immediately with exact parameter names before
   binder effects; message/factory boundaries have an explicit material-or-equivalent disposition.
3. Fixed/provider destinations preserve identity and are not invoked during configuration.
4. Message, task, sync factory, async factory and initialized-message factory forms select the
   exact activity/message-factory shape and preserve input identity.
5. Publish callback uplift retrieves the exact publish payload, invokes once when present and
   remains null when absent; respond/send callbacks preserve exact identity.
6. Normal, data, faulted and data-faulted binders select the exact corresponding activity family
   and return the binder result unchanged.
7. Focused evidence is assertion-rich, mutation-sensitive, warning-free and reconciled into the
   central requirement projection before complete Core/EF gates.

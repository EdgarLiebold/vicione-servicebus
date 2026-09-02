# V4 explicit receive-terminality validation

## Bound inputs

- Integration baseline commit: `f395904737bf489df94982df3db4c162489783f8`
- Integration baseline tree: `35332272955c85ad5ea3fad78a4057592adf622c`
- Protected review aggregate SHA-256:
  `371bf21331f0fc3316be271bce04ab37b3c54c50e13f443789d94c1f6eca1f18`
- V4 bundle SHA-256:
  `e8f28736562bf7c4fa8ffca4dfd662cd5105d3124e26d2ba424fe1ac0d192b87`
- V4 bundle head:
  `f8050928715e536b60c42d800d1cbb81c085818f`
- Semantic donor commit:
  `ad9d4e27a294fc9f061a3142e164da94293d77ec`
- `review/**` remained unchanged and untracked throughout the integration.

## Integrated behavior

Receive transport and endpoint faults now carry a non-null exact exception and an explicit
`IsTerminal` decision made by the retry owner. Per-attempt failures remain observable without
prematurely failing endpoint readiness. Retry exhaustion publishes one terminal event after invoking
the retry context's failure callback. Requested stop cancels retry backoff without publishing a false
terminal fault.

Endpoint start rejects a pre-cancelled token before starting the transport, preserves that exact token,
and rolls back synchronous start failures so a later healthy start is possible. Pending readiness is
cancelled with its initiating token, while a terminal transport failure faults it with the original
exception. The bus-endpoint waiter trusts explicit terminality rather than provider-specific exception
classification, preserves the first terminal cause, and isolates cancellation callback failures.

The in-memory receive transport owns its startup task. Startup dependency failures publish one terminal
fault with their exact cause, and stop waits for startup observation before completing. The donor was not
copied mechanically: focused tests were added to the current native suite, the bus waiter received a
friend-assembly test driver instead of a reflection-only oracle, and a masked startup-ownership test was
strengthened to isolate the exact task owner.

## Executing evidence

- Focused `ReceiveLifecycleTerminalityTests`: 9/9 materialized cases passed.
- Focused `InMemoryBusLifecycleTests`: 3/3 passed.
- The two focused classes contain 11 methods, 12 materialized cases, and 69 direct assertions.
- Final complete Unit/Architecture execution: 3,023/3,023 passed, 0 failed, 0 skipped.
- The first complete run had one scheduling-sensitive failure in the unchanged diagnostics test
  `RaisedBudgetCancellationIsNotQuiescence`; the isolated rerun passed, no diagnostics code was changed,
  and the complete unchanged rerun passed 3,023/3,023.
- `CoreRequirements.json` parses successfully.
- `git diff --check` and the scoped changed-C# whitespace gate pass.
- Ten buildable one-cause production mutations were killed and restored before the final green run.

The known macOS workspace sandbox denies .NET/MSBuild/MTP IPC. Authoritative builds and tests therefore
used the established external execution profile with isolated `DOTNET_CLI_HOME`, explicit `DOTNET_ROOT`,
disabled multilevel lookup, disabled node reuse, and the existing package cache. This is an execution
environment boundary, not a product workaround.

## Assertion and anti-pattern review

Every asynchronous test ends at a product-owned task, event, fake-time timer, or stop barrier. No test
uses `Thread.Sleep`, wall-clock `Task.Delay`, polling, a quiet-window verdict, timeout-as-success, skipped
execution, random input, or a console-only oracle. Assertions bind exact exception identity, cancellation
token identity, state, retry trace, event order and count, terminality, attempt count, routing identity,
and post-stop completion. Multiple assertions within a method describe one cohesive lifecycle contract.

The necessary in-memory white-box seam obtains the actual receive handle and disables its independent
executor completion path so the startup task is the only possible stop blocker. It then observes public
observer completion; reflection is setup isolation, not the product verdict. The bus waiter uses the
existing signed friend-assembly test infrastructure and exercises its actual observer contract.

## One-cause mutation evidence

Each mutation was applied independently, compiled by its focused owner, and removed before the next
mutation. The final full run used only restored production sources.

| ID | Single changed cause | Causal native owner and result |
| --- | --- | --- |
| M17 | Ignore the supplied terminality in the transport fault event | the `true` contract row failed |
| M18 | Retain the endpoint handle after a synchronous start failure | the healthy retry was rejected as already started |
| M19 | Fault readiness instead of cancelling it | exact cancellation state and token assertions failed |
| M20 | Invert the endpoint's terminal-fault guard | the nonterminal readiness-pending assertion failed |
| M21 | Publish retry exhaustion as nonterminal | the third-event terminality assertion failed |
| M22 | Omit the retry context's terminal callback | the exact retry trace assertion failed |
| M23 | Mark in-memory startup dependency failure nonterminal | the terminal startup-fault assertion failed |
| M24 | Complete stop without awaiting the owned in-memory startup task | isolated startup-observer completion was still pending when stop returned |
| M25 | Treat requested retry-backoff cancellation as a terminal failure | a second, terminal fault appeared during stop |
| M26 | Invert explicit terminality in the bus-endpoint waiter | a nonterminal fault cancelled the attached waiter |

The initial M24 test shape was deliberately rejected after the mutation survived: another active agent
also blocked stop and masked the missing startup await. The final owner isolates that competing path and
kills the mutation at the causal assertion. No mutation marker remains in the restored tree.

# Iteration 10 Validation

## Scope

Iteration 10 turns the fresh-package public API inventory into an enforced delivery contract:

- the package gate packs every one of the thirty packable product projects;
- the exact expected artifact catalog is bound to the evaluated packable-project catalog;
- the three provider-testing packages continue to execute through isolated package-only consumers;
- a fourth source-reference-free consumer restores all twenty-nine runtime packages directly;
- reflection runs against the assemblies from that isolated restore, including the ASP.NET Core
  shared framework required by the SignalR package;
- the generated 24,000-line surface must match the tracked `docs/api/packed-public-api.txt` file;
  and
- updating the contract requires the explicit `--update-public-api-contract` operation.

The tracked package-consumer locks were refreshed against the current fresh artifacts. This changed
only ViciOne package content hashes in the three provider consumers and the developer-journey
consumer; resolved versions and dependency graphs remain unchanged. The new complete consumer has
twenty-nine direct ViciOne package entries at version `1.0.0`.

## Red/green evidence

The new architecture contract initially failed because no committed packed API contract and no
complete package consumer existed. The first complete package run then exposed a second gap: the
file-based reflector used a console SDK and could not load `Microsoft.AspNetCore.SignalR.Core` while
inspecting the newly included SignalR assembly. Selecting the Web SDK for that reflector supplied
the correct shared-framework closure without adding a product dependency.

After correction, two independent complete package-only runs produced exactly thirty packages,
twenty-nine reflected runtime assemblies, 24,000 output lines, and the same SHA-256:

`28e7a84a58a2cbdbac689f41e193c9f54cc827dcaef80d93701da341458fb0c6`

The default second run compared against the tracked contract and passed without an update option.

## Mutation evidence

Four isolated regressions were introduced and removed:

1. Replacing the byte comparison with unconditional success was killed by the architecture test's
   exact comparison contract.
2. Removing `ViciOne.ServiceBus.StateMachineVisualizer` from the complete consumer was killed by
   the exact twenty-nine-package assertion.
3. Removing `ViciOne.ServiceBus.Analyzers` from the expected artifact catalog was killed by the
   exact evaluated-project/package assertion.
4. Changing one line in the tracked API contract allowed the complete pack, restore, build, and
   reflection stages to finish, then failed the gate with a one-line unified diff.

Every mutation was restored before validation.

## Test-quality review

The architecture tests use exact collection equality for the evaluated packable project catalog,
the complete runtime consumer, and its lock entries. They assert absence of source project
references, exact package versions, a non-empty tracked contract, the comparison and diagnostic
operations, and the required CI workflow entry point. No timing, polling, skip, weakened assertion,
or alternate test runner was introduced.

## Repository validation

| Gate | Result |
|---|---|
| Unit/Architecture solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Complete Unit/Architecture profile | PASS — 3,818 passed, 0 failed, 0 skipped |
| Complete Architecture profile | PASS — 215 passed, 0 failed, 0 skipped |
| Engineering solution Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Fresh-package gate, normal comparison mode | PASS — 18 journeys, 30 packages, 3 executable isolated consumers, 29 runtime package APIs |
| Independent packed API repetitions | PASS — byte-identical, 24,000 lines, hash shown above |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Git whitespace validation | PASS |

This is internal engineering and adversarial-review evidence, not independent external acceptance.

# research — directive 0111 correction round

Scope is **broad** by the code-testing-agent sizing table: five test applications, 68 `.cs` files,
151 test methods, and a package-line change that touches 42 lock files. The `.testagent` artifacts
and the completion contract therefore apply.

## Bounded target inventory

| Application | Path | Cases before | Owner in this round |
|---|---|---|---|
| Core | `tests2/Core/ViciOne.ServiceBus.Tests` | 247 | writer D |
| MessagePack | `tests2/Serialization/ViciOne.ServiceBus.MessagePack.Tests` | 24 | writer E |
| Architecture | `tests2/Architecture/ViciOne.ServiceBus.Architecture.Tests` | 20 | writer A |
| RabbitMQ | `tests2/Transports/ViciOne.ServiceBus.RabbitMqTransport.Tests` | 16 | writer C |
| Containers | `tests2/Testing/ViciOne.ServiceBus.Testing.Containers.Tests` | 12 | writer B |

Support libraries, no cases of their own: `ViciOne.ServiceBus.Testing` (E),
`ViciOne.ServiceBus.Testing.Contracts` (D), `ViciOne.ServiceBus.Testing.Containers` (B).

Of the 319 cases, **296 are inherited pilot obligations** the Lead accepted at record 0110. 23 were
added by the previous round. Three of those 23 — the `Lazy<T>` wrapper cases — are removed by
F-0111-03 and are explicitly **not** inherited obligations.

## Conventions already established in this estate

- .NET 10 SDK 10.0.302, Microsoft Testing Platform v2, xUnit v3 4.0.0. `global.json` under `tests2/`
  carries `"test": { "runner": "Microsoft.Testing.Platform" }`, which per the `run-tests` skill is the
  authoritative MTP signal: SDK 10+ syntax, arguments passed directly, **no `--` separator**, and
  `--report-trx` rather than `--logger trx`. Filters are `--filter-class` / `--filter-method`, not
  `--filter "ClassName=..."` — xUnit v3 on MTP.
- Test names are whole English sentences stating the assurance, not `Method_Scenario_Expected`.
  Failure messages state the business meaning; the `test-anti-patterns` rule against messages that
  merely repeat the assertion is already the house style.
- A new assurance counts as proven only under a mutation probe that is red for its own reason, with a
  byte snapshot restore verified by `cmp`. `git checkout` is never used to restore a probe.
- The test tree mirrors the product tree; cross-cutting support lives under `tests2/Testing`,
  architecture boundaries under `tests2/Architecture`, and no test framework enters a generic support
  library.

## Measured before any edit

`Assert.True` 138 · `Assert.False` 18 · `Assert.Equal` 51 · `Assert.Throws` 44 · `Assert.Contains` 10 ·
`Assert.Same` 4 · `Assert.Null` 3 · `Assert.DoesNotContain` 1 · `Assert.IsType` 1 · `Assert.Fail` 1 ·
`Assert.NotEqual` 0 · `Assert.NotNull` 0 · **`Assert.Single` 0 · `Assert.Empty` 0 · `Assert.NotEmpty` 0 ·
`Assert.All` 0**.

The Lead measured 138 `Assert.True`; counted independently, 138. The four collection assertions being
absent entirely is the sharpest signal: the estate asserts over collections without ever using the
assertion forms that describe collections.

Wall-clock markers found: `Thread.Sleep` once (in the `Lazy<T>` wrapper test that F-0111-03 deletes),
`Task.Delay` in test logic at `Delivery.cs:34` and `:42`, and unbounded `Task.Delay(budget)` timers in
`RunScopedBus.cs:152/165`.

## Acceptance checklist (verbatim from directive 0111)

1. §2.1 replace `TngTech.ArchUnitNET.xUnitV3` with `TngTech.ArchUnitNET`; remove every
   `ArchUnitNET.xUnitV3` namespace/type including `FailedArchRuleException`; preserve diagnostics and
   all positive/negative controls.
2. §2.2 exactly one direct xUnit/MTP entry package per test application: `xunit.v3.mtp-v2` 4.0.0;
   remove direct `xunit.v3`, `xunit.v3.extensibility.core` and obsolete ArchUnit pins once the
   regenerated graph proves no direct consumer.
3. §2.3 remove `GitHubActionsTestLogger` as a `GlobalPackageReference`; regenerate every affected lock
   file; add one architecture boundary proving test frameworks, runners and test-only loggers cannot
   resolve into `src/**`, using an explicit reviewed set of package families.
4. §2.4 the evaluated-MSBuild control uses an isolated fixture under the architecture test project; it
   must not depend on a real defect remaining in the product graph and must not mutate `src/**`.
5. F-0111-01 delete the shell probe; prove the owned reaper contract with a small ordinary C# case.
6. F-0111-02 remove the `ModuleInitializer` and the unused `Enabled` property; explicit enable-and-fail-
   closed before builder construction in both fixtures; zero-warning non-incremental build; no CA2255
   suppression.
7. F-0111-03 delete `SharedOwner<T>` and `SharedEndpointOwnership`; use `Lazy<T>` directly.
8. F-0111-04 release support internal and compact, with eight named properties proven deterministically
   and without stress loops, production scheduling hooks or fixed sleeps.
9. F-0111-05 internal creation seams for `RabbitMqBroker` and `ToxiproxyRelay`, five named branch
   proofs, public fixture API not broadened.
10. F-0111-06 remove the tautological assertion in `StartingAndStoppingTheBus`; the ledger says why.
11. §5.1–5.5 support-library rule moved into the architecture project; one bounded async child-process
    helper; capped evaluation concurrency; run-scoped `PackInto`; positive and negative fixtures for
    `InLibrariesOf`, `DeclaredBy`, `CarriedIn`.
12. §6 assertion and test-code quality across every `tests2/**/*.cs`, with a discriminating
    counter-check per correction.
13. §7 two read-only reviewers, one integrated commit, binlogs, one final MTP run, no self-declared
    open High/Medium gap.

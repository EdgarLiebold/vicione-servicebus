# R0 probe — the fail-closed `RequireCompleteObligationSet` provenance mechanism

Lead plan section 12.1: the root `Directory.Build.props` must capture the value present **before its
own defaults** in a non-overridable provenance property following the `RestoreLockedModeFromCommandLine`
pattern; `Directory.Build.targets` must require **both** the captured and the effectively evaluated
value to be `true` for every run marked as an evidence profile, and abort with a stable `VOSB` error
otherwise. `REQ-TEST-107` repeats this: "missing, false, project-overridden and command-line-disabled
mutants fail with a stable VOSB error."

The reason this needs proving before F1 is that the obvious implementation is fail-**open**: a plain
default plus a plain check passes as soon as anything sets the property, including the project that
the check is supposed to constrain.

Measured in a disposable project under the session scratchpad. The bound candidate was not touched.

## Mechanism under test

`Directory.Build.props`, provenance capture evaluated before any default of ours:

```xml
<RequireCompleteObligationSetFromCommandLine Condition="'$(RequireCompleteObligationSet)' != ''">true</RequireCompleteObligationSetFromCommandLine>
<RequireCompleteObligationSetFromCommandLine Condition="'$(RequireCompleteObligationSetFromCommandLine)' == ''">false</RequireCompleteObligationSetFromCommandLine>
```

`Directory.Build.targets`, two separate errors so the failure names its own reason:

```xml
<Error Condition="'$(RequireCompleteObligationSetFromCommandLine)' != 'true'" Code="VOSB0002" … />
<Error Condition="'$(RequireCompleteObligationSet)' != 'true'"                Code="VOSB0003" … />
```

## Measurements

| # | Case | Command properties | Exit | Result |
|---|---|---|---:|---|
| 1 | Correct evidence run | `EvidenceProfile=true` `RequireCompleteObligationSet=true` | 0 | green |
| 2 | Flag missing | `EvidenceProfile=true` | 1 | **VOSB0002**, captured provenance `false` |
| 3 | Flag disabled on the command line | `EvidenceProfile=true` `RequireCompleteObligationSet=false` | 1 | **VOSB0003**, effective value `false` |
| 4 | **Project grants itself the flag**, no command-line value | `EvidenceProfile=true` | 1 | **VOSB0002** — the project-level `<RequireCompleteObligationSet>true</…>` raises the effective value but cannot forge the provenance |
| 5 | Project self-grant plus correct command line | `EvidenceProfile=true` `RequireCompleteObligationSet=true` | 0 | green — a redundant project value does not break a legitimate run |
| 6 | Project self-grant plus command-line `false` | `EvidenceProfile=true` `RequireCompleteObligationSet=false` | 1 | **VOSB0003** — the MSBuild global property wins over the project value |
| 7 | Focused local iteration, not an evidence profile | none | 0 | green — allowed by the plan, and by construction never evidence |

## Conclusion

Case 4 is the one that matters and it holds: **no project can grant itself the exception.** Because
the provenance property is derived from the state of `RequireCompleteObligationSet` before any
default is applied, a project-level value raises only the effective value, never the captured one,
and the run dies on `VOSB0002`.

Case 6 additionally confirms that an MSBuild global property from the command line cannot be
overridden by a project `PropertyGroup`, so a project cannot re-enable the flag that a gate command
deliberately disabled.

Cases 2, 3, 4 and 6 each fail with the error that names its own reason, and cases 1, 5 and 7 stay
green, so none of the negative probes is red for an unrelated missing precondition.

Four of the section 12.1 mandatory mutants — missing value, command-line `false`, project override,
and the legitimate positive path — are therefore proven at mechanism level before implementation.
The remaining mutants of that section depend on artefacts that do not exist yet and stay F1 work.

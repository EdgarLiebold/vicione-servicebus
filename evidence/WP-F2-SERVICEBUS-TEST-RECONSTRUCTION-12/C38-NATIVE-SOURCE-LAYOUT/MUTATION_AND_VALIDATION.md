# C38 Native Source Layout — Mutation and Validation

## Positive result

- Architecture Release build: 0 warnings, 0 errors.
- Complete architecture project after the structural correction: 85 passed, 0 failed, 0 skipped.
- Requirement projection JSON parses successfully.

## One-cause mutations

An otherwise valid temporary C# file was added at
`tests2/ViciOne.ServiceBus.Tests/Serialization/NamespaceLayoutMutationProbe.cs` with namespace
`ViciOne.ServiceBus.Tests.WrongFolder`.

The run produced 85 results: 84 passed and exactly
`EveryNativeTestSourceNamespace_MirrorsItsProjectFolder` failed. Its diagnostic named the exact
path, expected `ViciOne.ServiceBus.Tests.Serialization`, and found
`ViciOne.ServiceBus.Tests.WrongFolder`. The probe was removed, its absence verified, and the full
architecture project returned to 85/85.

A second temporary file under the same physical folder declared the correct
`ViciOne.ServiceBus.Tests.Serialization` namespace and then a hidden
`ViciOne.ServiceBus.Tests.Hidden` namespace. Again, 84 facts passed and only the layout fact failed,
now reporting `expected exactly one namespace declaration, found 2`. The second probe was removed
and the full architecture project again returned to 85/85.

## Final hashes before full-profile validation

- Layout rule: `1d6dd950d8f14ebcd75b1508ac07ac14576ab92f3ce689e9863572c93c54d1c6`
- MSBuild evaluator: `249afc499a96f81a0bdfc405e3a362777b149b24d031b30f7ba9a74cdfe7bd42`
- Analyzer support project: `1ee2ab413daf535f31186236026bb2e86a479af288cbf3e125f7d01abc12350c`
- Roslyn support project: `5d867999c330d3bb8ec3636ebb667fbb95e3777dfb9d2314953630962991c346`
- Architecture projection: `94b8c823612453830b98967621b0c12741bd59f6f255db41516778ec2ef0da03`

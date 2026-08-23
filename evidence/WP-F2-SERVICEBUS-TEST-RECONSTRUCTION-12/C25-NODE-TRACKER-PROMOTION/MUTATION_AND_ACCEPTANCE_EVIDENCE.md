# C25 Node Tracker Promotion Evidence

## Frozen technical subject

- parent: `472a1ac739785f242f31bccc9aa9b95c5e7320fc`
- commit: `a9ca118f055668a4b04828d4c170617b641071a6`
- tree: `80290d0a2b74655eed28f1adae46bf278a2778bc`
- inherited source SHA-256: `6d14b12cdab0ae501b07e0078c63b75e9f737380e8e2955075d27c3a14ec8042`
- semantic ledger SHA-256: `5b7134255db42f7fdb4276598b4790660059d006e13bd184db8fe5cf6bf51a66`
- `NodeTrackerTests.cs` SHA-256:
  `46e76182808d59004583618ab4ebb444b9f320ed104c04728b81cdb12e5295ea`
- `CoreRequirements.json` SHA-256:
  `5d455b5ac40be54ced449f0e901e42c4e969f76bb23088620a18dcd1cbbaa9a2`
- terminal disposition SHA-256:
  `b5fa33449faf355d743a0b599a9551773573ad4e37474c1d925363d4fd39c8e8`

## Closure

The single inherited obligation `OBL-R0-CORE-B-0336` is terminally mapped to one ordinary xUnit
source-owner fact. The fact proves the exact produced value through the temporary and stored nodes,
promotion to a distinct `BucketNode`, the exact observer payload, and the complete one-operation
statistics delta. The inherited NUnit file was deleted only after closure.

## Independent one-cause mutations

Each mutation was built in Release with no restore and no incremental build, executed through the
native MTP test application, and immediately restored.

| Mutation | Observed result |
| --- | --- |
| report a temporary `FactoryNode` instead of storing/reporting a `BucketNode` | exit 2; exact-type assertion failed |
| record a hit instead of a miss | exit 2; exact miss count failed |
| omit `Statistics.ValueAdded` | exit 2; exact current/total count failed |
| omit the `ValueAdded` observer publication | exit 2; central two-second diagnostic timeout failed the test |
| change the projected variant key | exit 2; native projection test reported one missing and one unexpected entry |

## Acceptance

- formatter gate: exit 0;
- focused C25 fact: 1/1 passed;
- complete Core executable after restoration: 290/290 passed;
- complete Release UnitArchitecture build: 0 warnings, 0 errors;
- two consecutive unfiltered UnitArchitecture runs: 805/805 passed, 0 skipped;
- LocalIntegration Release build: 0 warnings, 0 errors;
- unfiltered LocalIntegration run: 3/3 passed, 0 skipped;
- final product, requirement JSON, disposition, and documentation diff reviewed in full;
- no product API, package, solution, lock file, or test-runner infrastructure changed in C25.

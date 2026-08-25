# C33 validation

## Stationary product and test results

| Gate | Result |
|---|---|
| Abstractions Release build | PASS — 0 warnings, 0 errors |
| Core Release build | PASS — 0 warnings, 0 errors |
| `ViciOne.ServiceBus.Abstractions.Tests` | PASS — 239/239, 0 failed, 0 skipped |
| `ViciOne.ServiceBus.Tests` | PASS — 835/835, 0 failed, 0 skipped |
| Unit/Architecture Release solution build | PASS — 0 warnings, 0 errors |
| `UnitArchitecture` unfiltered profile | PASS — 1466/1466, 0 failed, 0 skipped |
| LocalIntegration Release solution build | PASS — 0 warnings, 0 errors |
| `LocalIntegration` unfiltered profile | PASS — 3/3, 0 failed, 0 skipped |
| Complete serial Engineering Release build | PASS — 0 warnings, 0 errors |
| Seven isolated one-cause mutations | PASS — every mutant rejected for its intended reason |
| JSON syntax and `git diff --check` | PASS |

All final gates use artifacts rebuilt after every mutant was reverted. No commit or push is part of
this autonomous work interval.

## Final source hashes used by the complete-profile replay

```text
f31e903f0e1f89926f9cf44ffdf0e40c379a74b8d0f7c35da988bbe80af24a37  src/ViciOne.ServiceBus/Events/FaultExceptionInfo.cs
f92bfa4a09124c109c870ba1bd2d9614f127d5553b0e95dc78b6aa23a3b06b91  src/ViciOne.ServiceBus/Serialization/JsonConverters/CaseInsensitiveDictionaryStringObjectJsonConverter.cs
75babacedee7385eb4cca6be3ae025c4541b88e5d2d90a8bfd44174a392c23e5  src/ViciOne.ServiceBus.Abstractions/Metadata/BusHostInfo.cs
e0404d01e35533abbcdbd1bf09873e4da41aa096971f2e04909e7c45e9dea694  src/ViciOne.ServiceBus.Abstractions/Metadata/HostMetadataCache.cs
4443fee5073a8f8a37c64c80de9b9c2077ca0f31fa15d24ca1a2449586cdec10  tests2/ViciOne.ServiceBus.Tests/Events/FaultExceptionInfoTests.cs
71c6d6ae98130412dd4c0a79bb3c1e8763985d75339a0af289c9e26ce90fcb86  tests2/ViciOne.ServiceBus.Tests/Serialization/HostMetadataRoundTripTests.cs
46e69e3dcd35aba3e43ef82938daa7487048e010628052ff235018e0519f447e  tests2/ViciOne.ServiceBus.Abstractions.Tests/Metadata/BusHostInfoTests.cs
```

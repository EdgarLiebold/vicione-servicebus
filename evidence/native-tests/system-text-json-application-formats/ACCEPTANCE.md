# System.Text.Json Application-Format Acceptance

Date: 2026-08-23

## Scope

- Four ordinary xUnit facts under the `ViciOne.ServiceBus` source owner prove generated Protobuf and
  opaque XML application values over envelope and raw System.Text.Json.
- The generated test message contains an exact scalar, getter-only `RepeatedField<string>`, and
  `Timestamp`; generation occurs at build time from the minimal test-owned schema.
- XML remains exact string and UTF-8 byte data. No XML serializer, raw-XML serializer, XML media
  type, Protobuf wire serializer, or Protobuf product dependency is introduced.
- Seven inherited fixture/support files are removed after all seven executable ledger identities
  have executing owners; the eighth ledger entry is the PO-resolved cross-format question.

## Executed acceptance

- Native core project: 369 total, 369 passed, 0 failed, 0 skipped.
- UnitArchitecture solution: 886 total, 886 passed, 0 failed, 0 skipped.
- LocalIntegration solution: 3 total, 3 passed, 0 failed, 0 skipped.
- Non-incremental Release build of UnitArchitecture: 0 warnings, 0 errors.
- Non-incremental Release build of LocalIntegration: 0 warnings, 0 errors.
- Remaining inherited core test project after deletion: 0 warnings, 0 errors.
- Locked restores: passed.
- Bounded `dotnet format --verify-no-changes`: passed.
- Four one-cause mutations: all rejected for their intended behavioral or projection reason.

## Static quality review

The four facts were read line by line after the final edits. They use fixed inputs, independent
literal media-type oracles, exact wire-shape and value assertions, no delays, randomness, polling,
skip path, environment dependency, or broad exception-only verdict. The source-owned helper invokes
the real retained serializers and records their actual emitted bytes and content type. The package
graph confines Google.Protobuf and Grpc.Tools to the native test project; Grpc.Tools is private build
tooling. No `src/**` file, product namespace, public API, or product behavior changed.

Verdict: **PASS**.

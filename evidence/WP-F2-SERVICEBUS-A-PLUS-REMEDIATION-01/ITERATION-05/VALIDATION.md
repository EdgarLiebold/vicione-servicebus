# A+ remediation iteration 5 validation

Date: 2026-09-06

## Scope

This iteration removes public contracts and metadata that a .NET 10 greenfield API would not introduce:

- `NewId` formatting and parsing accept read-only spans instead of managed objects passed by readonly reference;
- every formatter validates the exact 16-byte identifier boundary and custom alphabets reject null explicitly;
- formatting and parsing use stack-owned buffers rather than retaining thread-local mutable arrays;
- intentionally unsupported capabilities use `NotSupportedException` with domain-specific messages;
- the public `NotImplementedByDesignException` compatibility type is removed;
- 105 binary-serialization attributes are removed from 103 product files; and
- the former state-machine product identity is removed from source documentation and package metadata.

The entity-name shortener now passes the leading 128 bits of its SHA-256 digest to the explicitly 128-bit z-base-32 formatter. Its externally visible 13-character, 65-bit suffix policy remains unchanged.

## Contract and test evidence

`NewIdApiShapeTests` checks the exact public `ReadOnlySpan<byte>` and `ReadOnlySpan<char>` signatures, rejects readonly-reference managed parameters across the identifier family, validates both adjacent formatter length boundaries for all four formatters, and verifies null custom alphabets.

`GreenfieldApiArchitectureTests` evaluates the real MSBuild `Compile` items of every product project. It rejects the removed custom capability exception, any syntactic `Serializable` attribute, and the former state-machine identity in product source or project metadata. The retry classifier already classifies the replacement `NotSupportedException` as non-retryable.

Existing NewId reference-corpus, round-trip, ordering, byte-layout, formatter, parser, topology-name, Azure subscription-name, and public-failure-classification tests remained green. The complete profile contains nine additional test cases in this iteration.

## Adversarial and mutation evidence

Five isolated contract mutations were killed and restored byte-for-byte:

1. The z-base-32 formatter was changed to accept input longer than 16 bytes; the exact-boundary test failed for 17 bytes.
2. A custom formatter constructor was changed back to `in string`; the reflection shape test reported the managed by-reference parameter.
3. A product throw site was changed back to `NotImplementedByDesignException`; the capability guard reported the exact file.
4. A product type received `[Serializable]`; the syntax guard reported the exact file.
5. The former state-machine brand was restored in package tags; the identity guard reported the exact project.

The mutated files were restored to their recorded SHA-256 values before the final build and test run.

The first complete green run exposed one 30-second timeout in an unrelated state-machine nested-request integration test while the other 3,776 tests passed. The exact test then passed 10 consecutive isolated repetitions in approximately 1.6–1.8 seconds, and the repeated complete profile passed. This is retained as evidence of a load-sensitive test risk for the later test-quality iteration rather than concealed as a clean first attempt.

## Repository validation

| Gate | Result |
|---|---|
| `ViciOne.ServiceBus.Tests.Unit.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Repeated complete Unit/Architecture profile | PASS — 3,777 passed, 0 failed, 0 skipped |
| Isolated nested-request timeout reproduction | PASS — 10/10 |
| `ViciOne.ServiceBus.Engineering.slnx` Release build, warnings as errors | PASS — 0 warnings, 0 errors |
| Engineering whitespace verification | PASS |
| Engineering style verification at warning severity | PASS |
| Git whitespace validation | PASS |
| Greenfield residue scans | PASS — no custom capability exception, serialization attribute, former state-machine brand, or managed by-reference identifier parameter |

This is internal engineering and adversarial-review evidence, not an independent external or Red Team acceptance.

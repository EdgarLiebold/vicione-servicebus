# Abstractions dictionary mutation dead code

## Baseline and disposition

The last complete 36-report profile at exact source/test commit `f7d924f94`
identified three uncovered methods in the internal
`ViciOne.ServiceBus.Internals.DictionaryExtensions` class:

| Removed method | Lines | Reported branches | Complexity | CRAP |
| --- | ---: | ---: | ---: | ---: |
| `SetValue(Dictionary<string, object>, string, object, bool)` | 0/11 | 0/12 | 12 | 156 |
| `SetValue(Dictionary<string, object>, string, string)` | 0/8 | 0/6 | 6 | 42 |
| `SetValues<TValue>` | 0/4 | 0/4 | 4 | 20 |

Repository-wide symbol and text searches across product code, tests, and
samples found no call, `nameof`, method-name string, reflection binding, or
generator reference. The only relationship was the dead `SetValues<TValue>`
method calling the dead object overload. Remaining `.SetValue(...)` calls bind
to `PropertyInfo`, `FieldInfo`, or `Array` APIs. Historical Job Service code
used the helpers before commit `2a061a278`; no current Friend Assembly does.

Adding direct tests would have preserved unused internal implementation only
to improve metrics. Exact commit `01e77bc68` therefore removes both overloads
and `SetValues<TValue>`. `DictionaryExtensions` remains because `GetOrAdd` and
`MergeLeft` are active. Neither the public API nor current source-level Friend
Assembly contracts change. Independent read-only adversarial review returned
PASS and reached the same disposition.

## Verification

- Current-byte Release Unit/Architecture solution build: zero warnings and
  zero errors. This compiled the complete current Friend Assembly graph.
- Current-byte complete Unit/Architecture gate: 10,096/10,096 passed, zero
  failures and skips.
- Current-byte Abstractions project: 759/759 passed, zero failures and skips.
- A clean detached checkout of exact commit `01e77bc68` passed locked restore,
  a zero-warning Release Abstractions-test build, and 759/759 tests with
  Microsoft CodeCoverage and `tools/ci/coverage.settings.xml`.
- Exact-commit report:
  `artifacts/coverage-dictionary-extensions-20260922/exact-01e77bc68.cobertura.xml`,
  SHA-256 `672c304f2da78d6a0eed005dce747a942804c464e4c6b88fd818f68419cd0c46`.
  Its `DictionaryExtensions` method list contains only `GetOrAdd`, `MergeLeft`,
  and the compiler-generated `MergeLeft` local helper; no `SetValue` or
  `SetValues` symbol remains.

The retained methods were already fully covered in the last complete profile:
`GetOrAdd` and `MergeLeft` had CRAP 2, and the generated local helper had CRAP
6. The exact Abstractions-only report does not execute them because their
active behavior is exercised by other repository test projects. This focused
deletion removes one product-wide CRAP-above-30 hotspot and 23 permanently
unreachable instrumented lines. A fresh complete multi-project profile is
still required before updating the global totals; global A+ remains open.

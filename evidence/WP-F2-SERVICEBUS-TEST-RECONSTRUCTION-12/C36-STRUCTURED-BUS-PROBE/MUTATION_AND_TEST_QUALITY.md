# C36 mutation and test-quality review

## Ordinary tests

Two ordinary xUnit facts read `ProbeResult.Results` directly as its structured dictionary graph:

- configured input, internal bus and dynamic endpoint addresses occur exactly once;
- after the dynamic handle stops, exactly the two persistent endpoints remain and the stopped
  address is absent.

The extraction helper is private test observation code, not a product compatibility API. It accepts
the product's documented single-scope dictionary and multiple-scope sequence representations. It
does not serialize, parse JSON, use reflection, inspect source text or copy the old TestFramework
extension. Tests use no sleep, delay, stopwatch, polling, skip or elapsed-time assertion.

## One-cause mutation

The InMemory transport probe's anonymous property was temporarily renamed from `Address` to
`InputAddress`. The Release build remained green. Both focused tests then failed at the exact
missing structured key `address`; the diagnostic showed keys `type`, `inputAddress` and
`prefetchCount`. The one source line was restored, the project rebuilt cleanly, and the focused pair
passed 2/2.

Final guards prove the canonical `Address = _context.InputAddress` assignment present and the
mutated assignment absent.

# T111 — transform operation-token handoff

Implementation commit: `c2a204265`.

`TransformFilter<T>` has four explicit interface paths: send, consume,
execute and compensate. Each created a transform context with the caller's
token, then called `IMessageInitializer<T>.InitializeAsync` without its
explicit cancellation argument. The initializer therefore received
`CancellationToken.None` even though its contract says that argument cancels
property initialization.

The existing completed, pending and fault matrices in
`ActivityTransformAsyncTests` now record and assert the exact token passed to
the three-argument initializer overload. A new four-stage theory,
`PreCanceledTransform_PreservesTheOperationTokenAndSuppressesDownstreamAsync`,
uses a token-aware recording initializer. It asserts exact input and context,
one initializer invocation, the caller's token on the cancellation exception,
and zero downstream calls. On unchanged source, all 40 focused matrix cases
failed at the missing-token assertion. After the four handoff corrections, the
Transformation suite passed 62/62; a zero-warning/error build and the exact
implementation-commit Core run passed 7,075/7,075, with no skips. The
requirements JSON contains 4,391 unique tuples and `git diff --check` passes.

Assertion-quality review: all four pre-canceled variants verify identity,
exception, call count and absence of downstream side effects. The prior
completed/pending/fault tests retain message and proxy identity, envelope
values, awaiting of downstream work and exact failure identity. Gap-analysis
counterprobe: omission of any one of the four token arguments is killed by
the matching pre-canceled variant and its existing stage matrix. The red-first
run on all four omitted arguments confirmed that result empirically.
Independent read-only adversarial review returned **PASS**, no concrete P1/P2.

This packet does not run a new product-wide Line/Branch/CRAP measurement or
claim global A+. T97 `3cb94a285` remains the latest valid 33-profile
checkpoint: 86,639/93,963 lines (92.20544%), 31,312/36,927 conservative
branches (84.79432%) and zero methods above CRAP 30. The PO-approved
larger-packet checkpoint cadence remains in force.

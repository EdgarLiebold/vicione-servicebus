# AWS native closure — final host-input correction

This additive Evidence package binds Technical Commit
`fba681ade996b48877497d71165426e1e92d4b0f`, Tree
`f5a7f4d2851595b605c9a6649b2945ae986d3267`, direct Parent and preceding Evidence
`7aa0ec820cbd24dd133b013631bc71e8f51675f1`.

## Closed review findings

- `AmazonSqsHostAddress(Uri)` now rejects an absolute URI whose host is empty before any settings,
  logging, topology or connection path can consume it.
- `HostUris_RequireAnAbsoluteAddressWithAHost` exercises both the direct address and typed
  configurator boundaries for a hostless absolute URI and a relative URI, with exact stable
  transport-configuration reasons.
- `StringHostConstruction_RejectsMissingHost` exercises null, empty and whitespace input through
  the public string constructor.
- The M32 recipe now replaces the unique complete `finally` block. Its source mutation and red
  result are unchanged, but the Evidence no longer claims that the repeated exchange line itself
  has one occurrence.

## Positive execution

Locked restores preceded full Release builds for Unit, LocalIntegration and Engineering. All builds
finished with zero warnings and zero errors. The exact Technical Commit passed 2,060/2,060 tests in
17 UnitArchitecture modules and 149/149 tests in six LocalIntegration modules, everywhere with zero
failure or skip. The focused Amazon SQS module passed 66/66.

The LocalIntegration runner used run identity `vicione-232e8a72d106` and fresh PostgreSQL, Azurite
and LocalStack resources on dynamically allocated loopback ports. `fixture-findings.json` is empty.
No real AWS account, credential, billable resource or GitHub Actions execution was used.

## Mutation closure

M35 removes only the hostless-URI guard and fails only the hostless axis; M36 removes only the
relative-URI guard and fails that axis through the wrong exception type; M37 removes only the string
host guard and fails all three string inputs. The corrected M32 unique block replacement fails both
failed- and cancelled-completion axes. Four Release mutation builds exit zero with zero warnings and
errors. Their four MTP runs exit 2 with seven causal failures, two controls passing and zero skips.
Every source restores byte-identically and the dedicated mutation worktree is clean at the Technical
Commit.

`SHA256SUMS` binds every other file in this directory. The preceding CORRECTION-02 Evidence remains
immutable and supplies the already independently reviewed M24-M34 and broader AWS correction
closure; this package supersedes only M32's occurrence description and adds M35-M37 plus fresh full
positive execution for the final Technical Commit.

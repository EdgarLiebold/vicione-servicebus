# T32: optional provider results and durable lease transitions — open

Base HEAD: `f35191a7e4bac51da49fdedf8a5eb7df3c0d9cca`.
Authoritative complete measurement remains `6a0aca709`; no new aggregate yet.

## SQS optional result collections

`AmazonSqsBatchIdentityTests.OneSidedResponse_WithAbsentOppositeListPreservesEveryOutcomeAsync`
has two cases: all-success with null Failed and all-failure with null Successful.
Each verifies all four caller outcomes, exact per-body error text where relevant,
reversed response order, one complete SDK request, queue and distinct owner/caller
tokens. The actual returned SDK model is checked for the null counterpart list.
Result and disposal waits are bounded and honor test cancellation.

Read-only adversarial review found no concrete blocker. This exercises the real
batcher and SDK response models, not AWS network behavior.

- MAIN class: 6/6 passed, exit 0; no skipped tests.
- Verify-only whitespace: exit 0, no changes.
- Isolated mutation removes both `?? []` fallbacks in ApplyResponse: exactly
  the two new cases fail with NullReferenceException; four controls pass, exit 2.
- Both fallbacks manually restored; source diff clean; restored class 6/6,
  exit 0 with no skips. No product changes remain.

| Local file | SHA-256 |
| --- | --- |
| MAIN artifacts/t32-sqs-optional.log | 29ae89a43c969bd1c0f6071516d3772c98b2294a5100feb84d839986279202f9 |
| MAIN artifacts/t32-sqs-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| AmazonSqsBatchIdentityTests.cs | c00a8d79837ebb8261af5d3ebdb521854f9a1d9448ee2d7ebdd905f46ee63796 |
| Isolated artifacts/t32-sqs-null-mutant.log | faf7da1d61af6f16e9d3d1e1e6013285123fe008a7db79435a7948886618372e |
| Isolated artifacts/t32-sqs-restored.log | e86e8341acc9326f1832e5f977af1b6c12957c0401069975f19afcd18d2f64f2 |

Isolated directory: `/private/tmp/servicebus-reply-investigation`. Raw logs stay
local; hashes do not publish them.

## EF completion and lease ownership

Two new real-SQLite tests implement the reviewed plan:

- Consumer completion removes the claimed target before the delayed dispatcher
  transition. Require false, absence without resurrection, exact capacity and
  preservation of a neighbor and the same message ID in another store.
- Expiry and reclaim establish a different lease with the same generation.
  Reject the old lease with the exact exception; preserve every persisted field
  and capacity. The current lease then transitions and completes successfully.

The read-only reviewer identified that Assert.Equivalent does not establish byte
order. All four state comparisons now use AssertRecordsUnchanged: ordered keys,
strict record equivalence and explicit sequential Body/Metadata equality.
Follow-up review confirmed this correction without another finding.

MAIN reviewed class: 15/15 passed, no skips; verify-only formatting exit 0.
Three separate isolated mutations each fail exactly the relevant new test while
14 controls pass (exit 2): missing-row false becomes true; stale-lease exception
becomes false; transition update drops its lease predicate. Each was manually
restored; final src diff is clean. Restored control: 15/15 passed, exit 0,
no skips.

These tests prove fixed interleavings using real SQLite, not simultaneous
database races or process restart behavior.

| Local file | SHA-256 |
| --- | --- |
| MAIN artifacts/t32-ef-reviewed.log | 8e92d251b05ddbdd8afb54c4be3af5fc141e8a0e376a2c146832e15785e4c61c |
| MAIN artifacts/t32-ef-final-format.log | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| EntityFrameworkReliableStoreTests.cs | 75b655339215e304fb15ec79b2dee596e787baf7f2b899d872c61022cb7be03c |
| Isolated artifacts/t32-ef-missing-mutant.log | 3f2a12fdc39b6930a288971e24ad77aabfefb14e860d1240d4efb4f2a0cbcdeb |
| Isolated artifacts/t32-ef-stale-mutant.log | 125db9f81201a4c94402981cf68bf81fc8fdc65394ab920cb4e80db42b056abe |
| Isolated artifacts/t32-ef-ownership-mutant.log | 5173ca4521a256efae7e38c6af870e67ee3978c57b9d2312ca14ea0043f898e5 |
| Isolated artifacts/t32-ef-restored.log | ba2d38c2b21c7b78685c913e2bb7df1e57fc230659ae45dbbad4cea6beb19a67 |

## Remaining iteration work

Complete changelog, commit,
all 33 fresh exact-commit profiles, aggregate review and authorized push.
The SQS focused pass alone does not establish closure of its branch gap or A+.

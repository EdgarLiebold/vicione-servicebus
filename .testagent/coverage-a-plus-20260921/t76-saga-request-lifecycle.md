# T76 saga request lifecycle and completion publication

The frozen T74 aggregate selected the request lifecycle as a connected
product area. It showed large uncovered extension-overload surfaces, but
those counts were only a locator. The behavioral inventory covers normal
request construction and dispatch, and original/generated completion
publication. The T75 Roslyn source/test pairing artifact was reused.

Two product defects were reproduced red-first:

- Normal `RequestActivity` accepted missing request, message factory or
  explicit service-address provider. Unlike the faulted variant, the bad
  declaration could enter a state-machine graph and fail only on dispatch.
  The red-first test failed on the first absent `request`. The shared base
  and both normal activity forms now reject each missing dependency with
  its exact argument name before execution.
- Both `RequestCompletedActivity` forms could begin publishing with a
  missing pipeline continuation, and a missing context failed with an
  unhelpful null-reference exception. The red-first test failed on that
  null-context path. Both forms now validate context and continuation
  before generating or publishing a completion.

Normal request tests check service-address override and settings fallback,
exact payload, delayed Send completion before request-ID persistence or
continuation, unchanged prior ID after send failure, and no side effects
from a pre-canceled context. An in-flight canceled send must preserve the
previous ID, exact cancellation token and absent continuation. The last
two counterprobes close a P2 gap identified by independent read-only Red
Team review.

Completion tests inspect the published `IRequestCompleted` envelope at the
activity boundary: exact saga correlation ID, fixed UTC timestamp, payload
object identity, message type identities and caller cancellation token.
They prove the generated response factory and publication must both finish
before continuation. Factory exception, null task, null response and
publication failure retain their exact errors and never continue or publish
when the failure occurs before publication.
The Red Team's second pass found that the invalid-pipeline test observed
publication but not a prematurely invoked generated-response factory. The
test now counts factory calls and requires zero for both invalid inputs.
Independent read-only Red Team final re-review is PASS with no remaining
concrete P1/P2 finding.

Focused xUnit v3/MTP results on the current tree: normal/faulted request
activity class 21/21, completion class 5/5. The exact-commit full Core
receipt is pending. No numeric mutation score or current product-wide
coverage value is claimed. The T74 33-profile report remains the latest
complete Line/Branch/CRAP measurement; the next aggregate follows the
agreed multi-packet interval unless a broad contract change requires it.

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
VOSB5001 | Reliability | Error | Blocking waits and synchronous locking primitives are forbidden in an actual IConsumer&lt;T&gt;.Consume implementation
VOSB5002 | Configuration | Error | Consumer definitions cannot write endpoint transport prefetch settings
VOSB5003 | Configuration | Error | Direct consumer concurrency writes must use the first-class policy
VOSB5004 | Topology | Warning | Consumers of topology-excluded contracts require explicit compatible topology
VOSB5005 | Performance | Warning | Potentially large inline binary members should use MessageData

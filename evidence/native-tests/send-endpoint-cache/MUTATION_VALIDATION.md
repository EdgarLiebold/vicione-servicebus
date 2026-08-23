# Send-endpoint cache mutation validation

The product cache was bypassed so every lookup invoked the endpoint factory directly. The complete
native Core project then executed 316 tests: the new endpoint-identity fact failed, all other 315
tests passed, and Microsoft Testing Platform returned exit code 2. The failure was the expected
`Assert.Same` difference between the cold and warm endpoint instances.

The mutation was reverted before the final Release build and unfiltered acceptance run.

# C32 test-quality review — kill switch

Date: 2026-08-25

## Structure and ownership

- seven configuration facts live at the source-mirrored `Configuration/KillSwitchOptionsTests` owner;
- seventeen state-machine facts and two real InMemory facts live below
  `Transports/Components/KillSwitch`;
- the signed `Testing/ViciOne.ServiceBus.Tests.InternalAccess` assembly is xUnit-free and exposes
  only observations and endpoint effects needed to test internal production state;
- the driver contains no assertion, threshold decision, ratio calculation or copied recovery rule;
- all 26 facts map one-to-one to 26 passive requirement-projection rows.

The test files contain 85 direct xUnit assertions. There is no skipped test, wall-clock wait,
`Thread.Sleep`, random input or process-time oracle. The one `Task.Run` use is confined to the
concurrency attack and is explicitly documented; product synchronization, not thread scheduling,
is the assertion subject. Virtual deadlines use the shared observable `TimeProvider`.

## False-green review

- exact threshold and ratio boundaries have distinct negative cases;
- retry tests prove absence of work before virtual-time advancement as well as eventual work;
- endpoint shutdown proves absence of a late restart and disposal of the active timer;
- health tests cross the real bus and .NET health-check service, then prove continued delivery;
- the instrumentation boundary observes the context actually seen by pause and restart;
- recovery verification proves both failure and success exits;
- fifteen independent one-cause mutations reject missing or inverted decisions.

The inherited tests are treated as historical regression evidence only. They did not determine the
new test structure and were not used as behavioral oracles where the complete product owner showed
stronger boundaries.

Result: PASS. No blocker, major gap, shallow assertion, test-only product reimplementation or known
false-green path remains in the bounded C32 cohort.

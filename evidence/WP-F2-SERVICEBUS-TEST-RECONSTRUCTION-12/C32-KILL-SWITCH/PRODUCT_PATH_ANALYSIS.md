# C32 product-path analysis — kill switch

Date: 2026-08-25

## Scope read in full

- all seven files below `Transports/Components/KillSwitch`;
- `Configuration/KillSwitchConfigurationExtensions.cs`;
- receive-endpoint start, stop, observer, handle and health-result paths;
- host endpoint collection and aggregate bus-health projection;
- all five retained Core, ActiveMQ and RabbitMQ kill-switch fixtures;
- the exact R0 rows `OBL-R0-CORE-B-0189`, `OBL-R0-CORE-B-0190`,
  `OBL-R0-CORE-D-0230`, `OBL-R0-BRK-0162` and `OBL-R0-BRK-0433`;
- imported upstream v8.5.10 source, the available repository history and current official MassTransit
  source/documentation as non-binding design evidence.

## Useful capability that remains

The kill switch protects queued messages from a persistently failing consumer. It observes consume
and routing-slip activity, waits for a minimum attempt population, trips at a configured matching
failure ratio, stops only the affected receive endpoint, leaves it paused for a configured delay,
starts it again, reports the endpoint as degraded while stopped and supports an exception filter.
It can be configured for one endpoint or for every endpoint of a bus.

## Imported design defects

1. Stop and restart are launched through unowned `Task.Run` calls. No product lifetime cancels or
   observes those tasks.
2. Restart failure is logged once and leaves the switch in `RestartingKillSwitchState` forever.
3. Stop failure still arms a raw restart timer even though endpoint state is unknown.
4. Three raw timers/clock mechanisms (`Timer`, `Stopwatch`, `DateTime.UtcNow`) create separate time
   truths and one timer per active state.
5. The recovery state expresses a useful intent: a matching failure after restart must re-trip
   immediately, while a configured population of successful deliveries stabilizes the endpoint.
   The endpoint's `Ready` callback bypasses that state entirely, however, and its successful
   population uses `attempts > threshold`. The implementation therefore cannot be copied.
6. Counter reset races with concurrent `Interlocked` increments because reset uses a different lock.
   The tracking window can lose attempts or failures.
7. Activation uses `attempts > threshold`, although the option describes a minimum population.
8. The integer and double trip-threshold overloads use different units; the API does not say whether
   a value means a percentage or a ratio.
9. `RestartTimeout` is not a timeout; it is a pause/retry delay. The component overloads for days,
   hours, minutes, seconds and milliseconds duplicate `TimeSpan` and permit ambiguous calls.
10. Mutable options are shared across endpoint instances after configuration.
11. Runtime coordination interfaces and all three state implementations are public despite being
    implementation details. Their only non-product consumer is an inherited test.
12. Public validation accepts a null configuration callback silently and the exception-filter setter
    accepts null silently.

The focused static-dependency scan found seven hidden ambient/lifecycle call sites in the seven
runtime files: two `Task.Run`, two raw `Timer`, one `Stopwatch`, one `DateTime.UtcNow` and one direct
ambient-log-context assignment. There are no filesystem, environment, network, console, process,
randomness, culture or serialization dependencies in this bounded owner.

## A+ disposition

- Keep one internal runtime component; remove the public runtime interfaces and state classes.
- Capture an immutable settings snapshot when configuration ends. Bus-level configuration shares
  that immutable snapshot, not mutable options.
- Use one standard .NET `TimeProvider`. Reset the tracking window lazily on the next observation, so
  a healthy endpoint owns no periodic timer.
- Count and evaluate a delivery under one synchronization boundary. The activation threshold is
  reached at `attempts >= threshold`; failure ratio uses `double` arithmetic.
- Start one stored recovery task after yielding out of the fault observer. It owns stop, pause,
  restart, retry, cancellation, logging context and terminal error observation. There is no detached
  transition task.
- Retry failed stop/start work after the same bounded delay; never hot-loop and never remain silently
  stranded.
- External endpoint stop cancels the recovery lifetime. A stop initiated by the kill switch does not
  cancel its own recovery.
- After a successful restart, enter an explicit internal `VerifyingRecovery` state. A matching
  failure re-trips immediately; exactly `ActivationThreshold` successful deliveries return the
  switch to its normal ratio-tracking state. This preserves the useful experienced-author intent
  while removing the dead callback path and its off-by-one boundary.
- Public configuration uses `SetTripThresholdRatio(double)` (`0.0` through `1.0`) and
  `SetRestartDelay(TimeSpan)`. Remove ambiguous percent and time-component overloads.
- Keep `KillSwitchOptions` public with read-only properties and fluent mutation during the callback;
  move it to the configuration owner. Keep the runtime types internal.
- Re-establish the owning log context before every recovery operation.

## Native test plan

The Core owner receives ordinary xUnit/MTP tests under source-mirrored `Configuration/KillSwitch`
and `Transports/Components/KillSwitch` directories. They must cover:

- defaults, exact public surface and every invalid configuration boundary;
- immutable post-callback settings and independent bus/endpoint instances;
- exact activation boundary and exact ratio boundary;
- matching and ignored failures;
- deterministic tracking-window reset under `FakeTimeProvider`;
- one transition under concurrent faults;
- exact stop → pause → restart ordering and no restart before the virtual deadline;
- complete Healthy → Degraded → Healthy product-health sequence;
- stop failure and start failure retry without a hot loop;
- immediate re-trip and exact successful stabilization after restart;
- external stop cancellation and absence of a late restart;
- restart through the component's own log/instrumentation context;
- consumer and routing-slip observer parity;
- compiled API assurance that runtime state types are no longer public;
- passive requirement projection and isolated one-cause mutations.

The three Core inherited fixtures are deleted only after their three R0 rows and the stronger
source-derived matrix are terminal. ActiveMQ and RabbitMQ fixtures remain for their later real-broker
LocalIntegration cohorts; C32 must not claim those two R0 broker rows.

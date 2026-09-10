using System;
using ViciOne.ServiceBus.Courier;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates routing-slip activity harnesses for a bus test harness.</summary>
public static class ActivityTestHarnessExtensions
{
    /// <summary>Registers a default-constructed execute-and-compensate activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute arguments.</typeparam>
    /// <typeparam name="TLog">The compensation log.</typeparam>
    /// <param name="harness">The harness that hosts the activity endpoints.</param>
    /// <returns>The activity harness.</returns>
    public static ActivityTestHarness<TActivity, TArguments, TLog> Activity<TActivity, TArguments, TLog>(this BusTestHarness harness)
        where TActivity : class, IActivity<TArguments, TLog>, new()
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        var activityFactory = new FactoryMethodActivityFactory<TActivity, TArguments, TLog>(static _ => new TActivity(), static _ => new TActivity());

        return new ActivityTestHarness<TActivity, TArguments, TLog>(harness, activityFactory, static _ =>
        {
        }, static _ =>
        {
        });
    }

    /// <summary>Registers an execute-and-compensate activity created by delegates.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute arguments.</typeparam>
    /// <typeparam name="TLog">The compensation log.</typeparam>
    /// <param name="harness">The harness that hosts the activity endpoints.</param>
    /// <param name="executeFactory">The delegate that creates an instance for execution.</param>
    /// <param name="compensateFactory">The delegate that creates an instance for compensation.</param>
    /// <returns>The activity harness.</returns>
    public static ActivityTestHarness<TActivity, TArguments, TLog> Activity<TActivity, TArguments, TLog>(this BusTestHarness harness,
        Func<TArguments, TActivity> executeFactory, Func<TLog, TActivity> compensateFactory)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(executeFactory);
        ArgumentNullException.ThrowIfNull(compensateFactory);
        var activityFactory = new FactoryMethodActivityFactory<TActivity, TArguments, TLog>(executeFactory, compensateFactory);

        return new ActivityTestHarness<TActivity, TArguments, TLog>(harness, activityFactory, static _ =>
        {
        }, static _ =>
        {
        });
    }

    /// <summary>Registers a default-constructed execute-only activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute arguments.</typeparam>
    /// <param name="harness">The harness that hosts the execute endpoint.</param>
    /// <returns>The execute activity harness.</returns>
    public static ExecuteActivityTestHarness<TActivity, TArguments> ExecuteActivity<TActivity, TArguments>(this BusTestHarness harness)
        where TActivity : class, IExecuteActivity<TArguments>, new()
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        var activityFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(static _ => new TActivity());

        return new ExecuteActivityTestHarness<TActivity, TArguments>(harness, activityFactory, static _ =>
        {
        });
    }

    /// <summary>Registers a delegate-created execute-only activity.</summary>
    /// <typeparam name="TActivity">The activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute arguments.</typeparam>
    /// <param name="harness">The harness that hosts the execute endpoint.</param>
    /// <param name="executeFactory">The delegate that creates an activity instance.</param>
    /// <returns>The execute activity harness.</returns>
    public static ExecuteActivityTestHarness<TActivity, TArguments> ExecuteActivity<TActivity, TArguments>(this BusTestHarness harness,
        Func<TArguments, TActivity> executeFactory)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(executeFactory);
        var activityFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactory);

        return new ExecuteActivityTestHarness<TActivity, TArguments>(harness, activityFactory, static _ =>
        {
        });
    }
}

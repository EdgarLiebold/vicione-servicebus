using System;
using ViciOne.ServiceBus.Courier;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for activity test harness.</summary>
public static class ActivityTestHarnessExtensions
{
    /// <summary>Creates an activity test harness.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <returns>The activity test harness produced by the operation.</returns>
    public static ActivityTestHarness<TActivity, TArguments, TLog> Activity<TActivity, TArguments, TLog>(this BusTestHarness harness)
        where TActivity : class, IActivity<TArguments, TLog>, new()
        where TArguments : class
        where TLog : class
    {
        var activityFactory = new FactoryMethodActivityFactory<TActivity, TArguments, TLog>(x => new TActivity(), x => new TActivity());

        return new ActivityTestHarness<TActivity, TArguments, TLog>(harness, activityFactory, x =>
        {
        }, x =>
        {
        });
    }

    /// <summary>Creates an activity test harness.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="executeFactoryMethod">The execute factory method.</param>
    /// <param name="compensateFactoryMethod">The compensate factory method.</param>
    /// <returns>The activity test harness produced by the operation.</returns>
    public static ActivityTestHarness<TActivity, TArguments, TLog> Activity<TActivity, TArguments, TLog>(this BusTestHarness harness,
        Func<TArguments, TActivity> executeFactoryMethod, Func<TLog, TActivity> compensateFactoryMethod)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        var activityFactory = new FactoryMethodActivityFactory<TActivity, TArguments, TLog>(executeFactoryMethod, compensateFactoryMethod);

        return new ActivityTestHarness<TActivity, TArguments, TLog>(harness, activityFactory, x =>
        {
        }, x =>
        {
        });
    }

    /// <summary>Creates an execute-only activity test harness.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <returns>The execute activity test harness produced by the operation.</returns>
    public static ExecuteActivityTestHarness<TActivity, TArguments> ExecuteActivity<TActivity, TArguments>(this BusTestHarness harness)
        where TActivity : class, IExecuteActivity<TArguments>, new()
        where TArguments : class
    {
        var activityFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(x => new TActivity());

        return new ExecuteActivityTestHarness<TActivity, TArguments>(harness, activityFactory, x =>
        {
        });
    }

    /// <summary>Creates an execute-only activity test harness.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="executeFactoryMethod">The execute factory method.</param>
    /// <returns>The execute activity test harness produced by the operation.</returns>
    public static ExecuteActivityTestHarness<TActivity, TArguments> ExecuteActivity<TActivity, TArguments>(this BusTestHarness harness,
        Func<TArguments, TActivity> executeFactoryMethod)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var activityFactory = new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(executeFactoryMethod);

        return new ExecuteActivityTestHarness<TActivity, TArguments>(harness, activityFactory, x =>
        {
        });
    }
}

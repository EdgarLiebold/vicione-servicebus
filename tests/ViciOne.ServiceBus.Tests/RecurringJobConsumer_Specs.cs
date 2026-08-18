namespace ViciOne.ServiceBus.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts.JobService;
using ViciOne.ServiceBus.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;


[TestFixture]
public class Configuring_a_recurring_job_consumer
{
    [Test]
    public async Task Should_support_cancellation_and_continuation_of_the_job()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RecurringJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(10));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();
        try
        {
            IRequestClient<SubmitJob<RecurringJobMessage>> client = harness.GetRequestClient<SubmitJob<RecurringJobMessage>>();

            var jobId = await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), x => x.Every(seconds: 5),
                harness.CancellationToken);

            Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(1).Count(), Is.EqualTo(1));

            await harness.Bus.CancelRecurringJob<RecurringJobMessage>(nameof(RecurringJobConsumer), "Not right now");

            Assert.That(await harness.Published.SelectAsync<JobCanceled>(x => x.Context.Message.JobId == jobId).Take(1).Count(), Is.EqualTo(1));

            await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), x => x.Every(seconds: 5),
                harness.CancellationToken);

            Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(2).Count(), Is.EqualTo(2));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Test]
    public async Task Should_support_configuration_of_the_job()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RecurringJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(15));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();

        IRequestClient<SubmitJob<RecurringJobMessage>> client = harness.GetRequestClient<SubmitJob<RecurringJobMessage>>();

        var jobId = await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), x => x.Every(seconds: 5),
            harness.CancellationToken);

        Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(2).Count(), Is.EqualTo(2));

        await harness.Stop();
    }

    /// <summary>
    /// Several recurring jobs of one message type, told apart by their name, each keep their own identity and their
    /// own schedule.
    ///
    /// The completion of each named job is the barrier: a completion carrying that job identifier and that name can
    /// only be published after that one named job has actually run. Counting completions of the type would prove
    /// only that some job of it ran often enough.
    /// </summary>
    [Test]
    public async Task Should_support_multiple_jobs_of_the_same_type_with_different_names()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<MaintenanceJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(15), testTimeout: TimeSpan.FromSeconds(60));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();
        try
        {
            IRequestClient<SubmitJob<MaintenanceTask>> client = harness.GetRequestClient<SubmitJob<MaintenanceTask>>();

            var schedules = new[] { ("One", 2), ("Two", 3), ("Tree", 4), ("Four", 5) };

            var jobIds = new Dictionary<string, Guid>();

            foreach ((string name, var seconds) in schedules)
            {
                jobIds.Add(name, await client.AddOrUpdateRecurringJob(name, new MaintenanceTask { Name = name },
                    x => x.Every(seconds: seconds), harness.CancellationToken));
            }

            Assert.That(jobIds.Values.Distinct().Count(), Is.EqualTo(schedules.Length),
                "Each name must be scheduled as its own recurring job");

            foreach ((string name, _) in schedules)
            {
                var jobId = jobIds[name];

                var completed = await harness.Published
                    .SelectAsync<JobCompleted<MaintenanceTask>>(x => x.Context.Message.JobId == jobId && x.Context.Message.Job.Name == name)
                    .Take(1)
                    .Count();

                Assert.That(completed, Is.EqualTo(1), $"The recurring job '{name}' did not complete under its own identity");
            }
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Test]
    public async Task Should_support_reconfiguration_of_the_job()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RecurringJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(15), testTimeout: TimeSpan.FromSeconds(30));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();

        IRequestClient<SubmitJob<RecurringJobMessage>> client = harness.GetRequestClient<SubmitJob<RecurringJobMessage>>();

        var jobId = await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), "*/5 * * * * ?",
            harness.CancellationToken);

        Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(1).Count(), Is.EqualTo(1));

        await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), "*/10 * * * * ?",
            harness.CancellationToken);

        Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(2).Count(), Is.EqualTo(2));

        await harness.Stop();
    }

    [Test]
    public async Task Should_support_reconfiguration_of_the_job_with_no_changes()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RecurringJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(15));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();

        IRequestClient<SubmitJob<RecurringJobMessage>> client = harness.GetRequestClient<SubmitJob<RecurringJobMessage>>();

        var jobId = await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), "*/5 * * * * ?",
            harness.CancellationToken);

        Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(1).Count(), Is.EqualTo(1));

        await client.AddOrUpdateRecurringJob(nameof(RecurringJobConsumer), new RecurringJobMessage(), "*/5 * * * * ?",
            harness.CancellationToken);

        Assert.That(await harness.Published.SelectAsync<JobCompleted<RecurringJobMessage>>().Take(2).Count(), Is.EqualTo(2));

        await harness.Stop();
    }

    [Test]
    public async Task Should_support_scheduling_a_single_run_job()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<RecurringJobConsumer>();

                x.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(1));
                x.SetTestTimeouts(testInactivityTimeout: TimeSpan.FromSeconds(15));

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = await provider.StartTestHarness();

        IRequestClient<SubmitJob<RecurringJobMessage>> client = harness.GetRequestClient<SubmitJob<RecurringJobMessage>>();

        var jobId = await client.ScheduleJob(DateTimeOffset.UtcNow.AddSeconds(3), new RecurringJobMessage(), harness.CancellationToken);

        Assert.That(await harness.Published.Any<JobCompleted<RecurringJobMessage>>(), Is.True);

        await harness.Stop();
    }


    public class RecurringJobConsumer :
        IJobConsumer<RecurringJobMessage>
    {
        readonly ILogger<RecurringJobConsumer> _logger;

        public RecurringJobConsumer(ILogger<RecurringJobConsumer> logger)
        {
            _logger = logger;
        }

        public Task Run(JobContext<RecurringJobMessage> context)
        {
            _logger.LogInformation("Every minute");

            return Task.CompletedTask;
        }
    }


    public class MaintenanceJobConsumer :
        IJobConsumer<MaintenanceTask>
    {
        readonly ILogger<MaintenanceJobConsumer> _logger;

        public MaintenanceJobConsumer(ILogger<MaintenanceJobConsumer> logger)
        {
            _logger = logger;
        }

        public Task Run(JobContext<MaintenanceTask> context)
        {
            // Saving job state on cancellation is covered by JobConsumer_Specs, which asserts the saved state.
            _logger.LogInformation("Running MaintenanceTask: {Id} {Name}", context.JobId, context.Job.Name);

            return Task.CompletedTask;
        }
    }
}


public record RecurringJobMessage;


public record MaintenanceTask
{
    public string Name { get; set; }
}

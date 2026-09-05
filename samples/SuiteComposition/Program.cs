using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.Samples.SuiteComposition;

string databasePath = Path.Combine(Path.GetTempPath(), $"vicione-suite-composition-{Guid.NewGuid():N}.db");
string connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";
var observedSchedule = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

try
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddOptions<ViciOneServiceBusHostOptions>()
        .Configure(static options => options.WaitUntilStarted = true);
    services.AddSingleton(observedSchedule);
    services.AddPooledDbContextFactory<SuiteDbContext>(options => options.UseSqlite(connectionString));
    services.AddViciOneServiceBus(configuration =>
    {
        configuration.Limits(MessageLimits.Conservative);
        configuration.AddConsumer<SuiteConsumer>();
        configuration.AddRequestClient<GetSuiteStatus>(
            new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<SuiteConsumer>()}"));
        configuration.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
        configuration.UseReliableMessaging(reliable =>
        {
            reliable.UseEntityFramework<SuiteDbContext>();
            reliable.Store(new ReliableStoreLimits
            {
                MaximumStoredCount = 1_000,
                MaximumStoredBytes = 16 * 1024 * 1024,
            });
            reliable.Delivery(delivery =>
            {
                delivery.MaximumAttempts = 3;
                delivery.PollInterval = TimeSpan.FromMilliseconds(20);
                delivery.InitialRetryDelay = TimeSpan.FromMilliseconds(20);
                delivery.MaximumRetryDelay = TimeSpan.FromMilliseconds(100);
                delivery.RetryJitterFraction = 0;
            });
            reliable.Retention(TimeSpan.FromDays(7));
            reliable.AddMessageContract<GetSuiteStatus>("suite.status.get");
            reliable.AddMessageContract<SuiteStatus>("suite.status");
            reliable.AddMessageContract<SuiteDeadline>("suite.deadline");
        });
    });

    await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    });
    await using (AsyncServiceScope migrationScope = provider.CreateAsyncScope())
    {
        SuiteDbContext database = migrationScope.ServiceProvider.GetRequiredService<SuiteDbContext>();
        await database.Database.EnsureCreatedAsync();
    }

    IHostedService[] hostedServices = provider.GetServices<IHostedService>().ToArray();
    var startedServices = new Stack<IHostedService>();
    try
    {
        foreach (IHostedService hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
            startedServices.Push(hostedService);
        }

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRequestClient<GetSuiteStatus> client = scope.ServiceProvider.GetRequiredService<IRequestClient<GetSuiteStatus>>();
        Response<SuiteStatus> response = await client.Advanced().GetResponseAsync<SuiteStatus>(
            new GetSuiteStatus("composition"),
            RequestTimeout.After(s: 10));
        if (response.Message.Value != "ready:composition")
            throw new InvalidOperationException($"Unexpected request response: {response.Message.Value}");

        IMessageScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        await scheduler.ScheduleSendAsync(
            new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<SuiteConsumer>()}"),
            DateTimeOffset.UtcNow.AddMilliseconds(100),
            new SuiteDeadline("scheduled"));
        string scheduled = await observedSchedule.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (scheduled != "scheduled")
            throw new InvalidOperationException($"Unexpected scheduled value: {scheduled}");

        _ = typeof(RabbitMqBusFactory).Assembly;
        string[] loadedProductAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(static assembly => assembly.GetName().Name)
            .OfType<string>()
            .Where(static name => name.StartsWith("ViciOne.ServiceBus", StringComparison.Ordinal)
                && name != "ViciOne.ServiceBus.Samples.SuiteComposition")
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expectedProductAssemblies =
        [
            "ViciOne.ServiceBus",
            "ViciOne.ServiceBus.Abstractions",
            "ViciOne.ServiceBus.EntityFrameworkCore",
            "ViciOne.ServiceBus.RabbitMq",
        ];
        if (!loadedProductAssemblies.SequenceEqual(expectedProductAssemblies, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unexpected product assembly closure: {string.Join(", ", loadedProductAssemblies)}");
        }

        Console.WriteLine("SuiteComposition passed: limits, SQLite reliable messaging, consumer, request, and schedule are operational.");
    }
    finally
    {
        while (startedServices.TryPop(out IHostedService? hostedService))
            await hostedService.StopAsync(CancellationToken.None);
    }
}
finally
{
    DeleteIfPresent(databasePath);
    DeleteIfPresent(databasePath + "-wal");
    DeleteIfPresent(databasePath + "-shm");
}

static void DeleteIfPresent(string path)
{
    if (File.Exists(path))
        File.Delete(path);
}

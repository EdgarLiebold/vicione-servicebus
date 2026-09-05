using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey15MessageJournal
{
    public static IServiceCollection Configure(IServiceCollection services, string connectionString)
    {
        DbContextOptions<JourneyDbContext> journalDatabase = new DbContextOptionsBuilder<JourneyDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UseMessageJournal(journal => journal
                .UseEntityFramework<JourneyDbContext>(
                    journalDatabase,
                    "MessageJournal",
                    new MessageJournalStoreLimits(
                        maximumEntryBytes: 64 * 1024,
                        maximumEntries: 10_000,
                        retentionPeriod: TimeSpan.FromDays(7)))
                .Policy(new MetadataOnlyJournalPolicy())
                .Options(MessageJournalOptions.ContinueMessageFlow(
                    TimeSpan.FromSeconds(2),
                    TimeProvider.System)));
            configuration.UsingInMemory();
        });
    }

    private sealed class MetadataOnlyJournalPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                capture.ContentType,
                capture.MessageTypes,
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["operation"] = capture.Operation.ToString(),
                    ["outcome"] = capture.Outcome.ToString(),
                }));
        }
    }
}

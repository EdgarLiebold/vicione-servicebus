namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey08MessageData
{
    public static IServiceCollection ConfigureAdmission(IServiceCollection services) =>
        services.AddViciOnePayloadAdmission<IBus>(options =>
        {
            options.WarningBodyBytes = 64 * 1024;
            options.MessageDataOffloadThresholdBytes = 256 * 1024;
            options.MaximumSerializedBodyBytes = 1024 * 1024;
            options.MaximumTransportEnvelopeBytes = 2 * 1024 * 1024;
        });

    public static async Task<LargeOrderDocument> Store(
        IMessageDataRepository repository,
        Guid orderId,
        string document,
        CancellationToken cancellationToken) =>
        new(orderId, await repository.PutString(document, cancellationToken));
}

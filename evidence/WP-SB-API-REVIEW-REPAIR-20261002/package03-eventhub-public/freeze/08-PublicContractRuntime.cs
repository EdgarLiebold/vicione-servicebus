namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;
internal static class PublicContractRuntime
{
    public static TimeSpan Timeout { get; set; }
    public static CancellationToken CancellationToken { get; set; }
}
internal static class Record
{
    public static async Task<Exception?> ExceptionAsync(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception error) { return error; }
    }
}

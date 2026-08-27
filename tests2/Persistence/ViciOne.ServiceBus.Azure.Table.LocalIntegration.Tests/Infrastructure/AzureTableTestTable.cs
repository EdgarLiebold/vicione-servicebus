namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;

using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

internal sealed class AzureTableTestTable : IAsyncDisposable
{
    private readonly TableServiceClient _service;

    private AzureTableTestTable(TableServiceClient service, TableClient table)
    {
        _service = service;
        Table = table;
    }

    public TableServiceClient Service => _service;

    public TableClient Table { get; }

    public static async Task<AzureTableTestTable> CreateAsync(
        string purpose,
        CancellationToken cancellationToken,
        TableClientOptions? clientOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable);
        AzureTableLocalOptions azureTable = options.LocalInfrastructure!.AzureTable!;
        var endpoint = new Uri($"http://{azureTable.Host}:{azureTable.Port}/{azureTable.AccountName}");
        var credential = new TableSharedKeyCredential(azureTable.AccountName, azureTable.AccountKey);
        var service = clientOptions is null
            ? new TableServiceClient(endpoint, credential)
            : new TableServiceClient(endpoint, credential, clientOptions);
        string prefix = new(purpose.Where(char.IsLetterOrDigit).ToArray());
        if (prefix.Length == 0 || !char.IsLetter(prefix[0]))
            prefix = $"T{prefix}";

        string tableName = $"{prefix}{Guid.NewGuid():N}";
        if (tableName.Length > 63)
            tableName = tableName[..63];

        TableClient table = service.GetTableClient(tableName);
        await table.CreateAsync(cancellationToken);
        return new AzureTableTestTable(service, table);
    }

    public async ValueTask DisposeAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.AzureTable)
            .OperationTimeout!.Value;
        using var cleanup = new CancellationTokenSource(timeout);
        await _service.DeleteTableAsync(Table.Name, cleanup.Token);
    }
}

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Infrastructure;

internal sealed class DynamoDbTestTable : IAsyncDisposable
{
    private readonly AmazonDynamoDBClient _client;

    private DynamoDbTestTable(AmazonDynamoDBClient client, string tableName, TimeSpan operationTimeout)
    {
        _client = client;
        TableName = tableName;
        OperationTimeout = operationTimeout;
    }

    public IAmazonDynamoDB Client => _client;

    public string TableName { get; }

    public TimeSpan OperationTimeout { get; }

    public static async Task<DynamoDbTestTable> CreateAsync(string purpose, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.LocalStack);
        LocalStackLocalOptions localStack = testOptions.LocalInfrastructure!.LocalStack!;
        var endpoint = new UriBuilder(Uri.UriSchemeHttp, localStack.Host, localStack.Port!.Value).Uri;
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = endpoint.AbsoluteUri.TrimEnd('/'),
            AuthenticationRegion = localStack.Region,
            MaxErrorRetry = 0,
        };
        string normalizedPurpose = string.Concat(purpose.Where(char.IsAsciiLetterOrDigit));
        if (normalizedPurpose.Length == 0)
            normalizedPurpose = "test";
        if (normalizedPurpose.Length > 24)
            normalizedPurpose = normalizedPurpose[..24];

        string tableName = $"ViciOne-{normalizedPurpose}-{Guid.NewGuid():N}";
        var fixture = new DynamoDbTestTable(
            new AmazonDynamoDBClient(CreateRunCredentials(), config),
            tableName,
            testOptions.OperationTimeout!.Value);

        try
        {
            await fixture._client.CreateTableAsync(
                    new CreateTableRequest
                    {
                        TableName = tableName,
                        BillingMode = BillingMode.PAY_PER_REQUEST,
                        KeySchema =
                        [
                            new KeySchemaElement("PK", KeyType.HASH),
                            new KeySchemaElement("SK", KeyType.RANGE),
                        ],
                        AttributeDefinitions =
                        [
                            new AttributeDefinition("PK", ScalarAttributeType.S),
                            new AttributeDefinition("SK", ScalarAttributeType.S),
                        ],
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            TableDescription description = (await fixture._client.DescribeTableAsync(tableName, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken)).Table;
            if (description.TableStatus != TableStatus.ACTIVE)
                throw new InvalidOperationException($"LocalStack returned table state '{description.TableStatus}' after creation.");
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public IDynamoDBContext CreateContext() => new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => _client)
        .Build();

    public async Task<Dictionary<string, AttributeValue>[]> ScanAsync(CancellationToken cancellationToken)
    {
        ScanResponse response = await _client.ScanAsync(
                new ScanRequest { TableName = TableName, ConsistentRead = true },
                cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);
        return response.Items is null ? [] : [.. response.Items];
    }

    public async ValueTask DisposeAsync()
    {
        using var cleanup = new CancellationTokenSource(OperationTimeout);
        try
        {
            await _client.DeleteTableAsync(TableName, cleanup.Token);
        }
        catch (ResourceNotFoundException)
        {
            // The test failed before table creation completed, or already removed its table.
        }
        finally
        {
            _client.Dispose();
        }
    }

    private static AWSCredentials CreateRunCredentials()
    {
        string accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")
            ?? throw new InvalidOperationException("The canonical LocalStack runner did not project AWS_ACCESS_KEY_ID.");
        string secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
            ?? throw new InvalidOperationException("The canonical LocalStack runner did not project AWS_SECRET_ACCESS_KEY.");
        string? sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

        return string.IsNullOrEmpty(sessionToken)
            ? new BasicAWSCredentials(accessKey, secretKey)
            : new SessionAWSCredentials(accessKey, secretKey, sessionToken);
    }
}

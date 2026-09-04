using System;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

internal sealed class DynamoDbContextFactory<TSaga>
    where TSaga : class, ISagaVersion
{
    readonly Func<IDynamoDBContext> _contextFactory;

    public DynamoDbContextFactory(Func<IDynamoDBContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public IDynamoDBContext Create() =>
        _contextFactory() ?? throw new InvalidOperationException("The DynamoDB context factory returned null.");
}

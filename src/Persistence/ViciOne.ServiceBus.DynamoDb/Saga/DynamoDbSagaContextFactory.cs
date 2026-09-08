using System;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

internal sealed class DynamoDbSagaContextFactory<TSaga>(Func<IDynamoDBContext> contextFactory)
    where TSaga : class, ISagaVersion
{
    readonly Func<IDynamoDBContext> _contextFactory =
        contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    public IDynamoDBContext Create() =>
        _contextFactory() ?? throw new InvalidOperationException("The DynamoDB context factory returned null.");
}

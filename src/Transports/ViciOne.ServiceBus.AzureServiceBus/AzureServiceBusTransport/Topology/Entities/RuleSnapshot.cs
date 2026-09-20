using System;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

internal static class RuleSnapshot
{
    public static CreateRuleOptions? Copy(CreateRuleOptions? source)
    {
        if (source is null)
            return null;

        return new CreateRuleOptions(source.Name, Copy(source.Filter)!)
        {
            Action = Copy(source.Action)
        };
    }

    public static RuleFilter? Copy(RuleFilter? source) => source switch
    {
        null => null,
        TrueRuleFilter => new TrueRuleFilter(),
        FalseRuleFilter => new FalseRuleFilter(),
        SqlRuleFilter sql => CopySqlFilter(sql),
        CorrelationRuleFilter correlation => CopyCorrelationFilter(correlation),
        _ => throw new NotSupportedException($"Unsupported Azure Service Bus rule filter: {source.GetType().FullName}."),
    };

    static RuleAction? Copy(RuleAction? source) => source switch
    {
        null => null,
        SqlRuleAction sql => CopySqlAction(sql),
        _ => throw new NotSupportedException($"Unsupported Azure Service Bus rule action: {source.GetType().FullName}."),
    };

    static SqlRuleFilter CopySqlFilter(SqlRuleFilter source)
    {
        var copy = new SqlRuleFilter(source.SqlExpression);
        foreach (var property in source.Parameters)
            copy.Parameters.Add(property.Key, property.Value);
        return copy;
    }

    static SqlRuleAction CopySqlAction(SqlRuleAction source)
    {
        var copy = new SqlRuleAction(source.SqlExpression);
        foreach (var property in source.Parameters)
            copy.Parameters.Add(property.Key, property.Value);
        return copy;
    }

    static CorrelationRuleFilter CopyCorrelationFilter(CorrelationRuleFilter source)
    {
        var copy = new CorrelationRuleFilter
        {
            CorrelationId = source.CorrelationId,
            MessageId = source.MessageId,
            To = source.To,
            ReplyTo = source.ReplyTo,
            Subject = source.Subject,
            SessionId = source.SessionId,
            ReplyToSessionId = source.ReplyToSessionId,
            ContentType = source.ContentType,
        };
        foreach (var property in source.ApplicationProperties)
            copy.ApplicationProperties.Add(property.Key, property.Value);
        return copy;
    }
}

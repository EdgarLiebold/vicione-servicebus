namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;
    using System.Text;


    public class SqlServerLockStatementFormatter :
        ILockStatementFormatter
    {
        readonly bool _serializable;

        public SqlServerLockStatementFormatter(bool serializable)
        {
            _serializable = serializable;
        }

        public void Create(StringBuilder sb, string schema, string table)
        {
            sb.AppendFormat("SELECT * FROM {0} WITH (UPDLOCK, ROWLOCK", FormatTableName(schema, table));
            if (_serializable)
                sb.Append(", SERIALIZABLE");
            sb.Append(") WHERE ");
        }

        public void AppendColumn(StringBuilder sb, int index, string columnName)
        {
            if (index == 0)
                sb.AppendFormat("{0} = @p0", QuoteIdentifier(columnName));
            else
                sb.AppendFormat(" AND {0} = @p{1}", QuoteIdentifier(columnName), index);
        }

        public void Complete(StringBuilder sb)
        {
        }

        public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string createdColumn, string outboxIdColumn,
            string busKeyColumn, string statusColumn, string nextDeliveryTimeColumn)
        {
            sb.AppendFormat(
                "SELECT TOP 1 * FROM {0} WITH (UPDLOCK, ROWLOCK, READPAST) WHERE {1} = @p0 AND ({2} = @p1 OR ({2} = @p2 AND ({3} IS NULL OR {3} <= @p3)) OR {2} = @p4) ORDER BY {4}, {5}",
                FormatTableName(schema, table), QuoteIdentifier(busKeyColumn), QuoteIdentifier(statusColumn), QuoteIdentifier(nextDeliveryTimeColumn),
                QuoteIdentifier(createdColumn), QuoteIdentifier(outboxIdColumn));
        }

        public void CreateInboxCleanupLockStatement(StringBuilder sb, string schema, string table)
        {
            string resource = $"ViciOne.ServiceBus:InboxCleanup:{schema}.{table}".Replace("'", "''", StringComparison.Ordinal);
            sb.Append("DECLARE @result int; EXEC @result = sys.sp_getapplock ")
                .Append("@Resource = N'").Append(resource).Append("', ")
                .Append("@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0; ")
                .Append("SELECT CASE WHEN @result >= 0 THEN 1 ELSE 0 END");
        }

        static string FormatTableName(string schema, string table)
        {
            return string.IsNullOrEmpty(schema)
                ? QuoteIdentifier(table)
                : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";
        }

        static string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}

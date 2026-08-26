namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;
    using System.Text;


    public class SqliteLockStatementFormatter :
        ILockStatementFormatter
    {
        public void Create(StringBuilder sb, string schema, string table)
        {
            sb.Append($"SELECT * FROM {QuoteIdentifier(table)} WHERE ");
        }

        public void AppendColumn(StringBuilder sb, int index, string columnName)
        {
            sb.Append(index == 0
                ? $"{QuoteIdentifier(columnName)} = @p0"
                : $" AND {QuoteIdentifier(columnName)} = @p{index}");
        }

        public void Complete(StringBuilder sb)
        {
        }

        public void CreateOutboxStatement(StringBuilder sb, string schema, string table, string columnName)
        {
            sb.Append($"SELECT * FROM {QuoteIdentifier(table)} ORDER BY {QuoteIdentifier(columnName)} LIMIT 1");
        }

        static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;


    public class SqlLockStatementProvider :
        ILockStatementProvider
    {
        readonly ILockStatementFormatter _formatter;
        readonly ConditionalWeakTable<IModel, ConcurrentDictionary<LockStatementCacheKey, SchemaTableColumnTrio>> _modelMappings;

        public SqlLockStatementProvider(string defaultSchema, ILockStatementFormatter formatter)
        {
            if (string.IsNullOrWhiteSpace(defaultSchema))
                throw new ArgumentException("The default schema must not be empty.", nameof(defaultSchema));

            ArgumentNullException.ThrowIfNull(formatter);

            DefaultSchema = defaultSchema;
            _formatter = formatter;
            _modelMappings = new ConditionalWeakTable<IModel, ConcurrentDictionary<LockStatementCacheKey, SchemaTableColumnTrio>>();
        }

        public SqlLockStatementProvider(ILockStatementFormatter formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            _formatter = formatter;
            _modelMappings = new ConditionalWeakTable<IModel, ConcurrentDictionary<LockStatementCacheKey, SchemaTableColumnTrio>>();
        }

        string DefaultSchema { get; }

        public virtual string GetRowLockStatement<T>(DbContext context)
            where T : class
        {
            return FormatLockStatement<T>(context, nameof(ISaga.CorrelationId));
        }

        public virtual string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class
        {
            return FormatLockStatement<T>(context, propertyNames);
        }

        public virtual string GetOutboxStatement(DbContext context)
        {
            var schemaTableTrio = GetSchemaAndTableNameAndColumnName(context, typeof(OutboxState),
                nameof(OutboxState.Created), nameof(OutboxState.OutboxId), nameof(OutboxState.BusKey), nameof(OutboxState.Status),
                nameof(OutboxState.NextDeliveryTime));

            var sb = new StringBuilder(256);
            _formatter.CreateOutboxStatement(sb, schemaTableTrio.Schema, schemaTableTrio.Table,
                schemaTableTrio.ColumnNames[0], schemaTableTrio.ColumnNames[1], schemaTableTrio.ColumnNames[2], schemaTableTrio.ColumnNames[3],
                schemaTableTrio.ColumnNames[4]);

            return sb.ToString();
        }

        public virtual string GetInboxCleanupLockStatement(DbContext context)
        {
            var mapping = GetSchemaAndTableNameAndColumnName(context, typeof(InboxState), nameof(InboxState.Delivered));
            var sb = new StringBuilder(192);
            _formatter.CreateInboxCleanupLockStatement(sb, mapping.Schema, mapping.Table);
            return sb.ToString();
        }

        string FormatLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class
        {
            var schemaTableTrio = GetSchemaAndTableNameAndColumnName(context, typeof(T), propertyNames);

            var sb = new StringBuilder(128);
            _formatter.Create(sb, schemaTableTrio.Schema, schemaTableTrio.Table);

            for (var i = 0; i < propertyNames.Length; i++)
                _formatter.AppendColumn(sb, i, schemaTableTrio.ColumnNames[i]);

            _formatter.Complete(sb);

            return sb.ToString();
        }

        SchemaTableColumnTrio GetSchemaAndTableNameAndColumnName(DbContext context, Type type, params string[] propertyNames)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(propertyNames);

            if (propertyNames.Length == 0)
                throw new ArgumentException("At least one mapped property is required.", nameof(propertyNames));

            string[] requestedProperties = propertyNames.ToArray();
            if (requestedProperties.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Mapped property names must not be empty.", nameof(propertyNames));

            IModel model = context.Model;
            var cache = _modelMappings.GetValue(model,
                static _ => new ConcurrentDictionary<LockStatementCacheKey, SchemaTableColumnTrio>());
            var key = new LockStatementCacheKey(type, string.Join('\u001f', requestedProperties));

            return cache.GetOrAdd(key, _ => ResolveMapping(model, type, requestedProperties));
        }

        SchemaTableColumnTrio ResolveMapping(IModel model, Type type, IReadOnlyList<string> propertyNames)
        {
            var entityType = model.FindEntityType(type)
                ?? throw new InvalidOperationException($"Entity type not found: {TypeCache.GetShortName(type)}");

            var schema = entityType.GetSchema();
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ViciOneServiceBusException($"Unable to determine saga table name: {TypeCache.GetShortName(type)} (using model metadata).");

            var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
            var columnNames = new string[propertyNames.Count];

            for (var i = 0; i < propertyNames.Count; i++)
            {
                var property = entityType.FindProperty(propertyNames[i])
                    ?? throw new InvalidOperationException(
                        $"Property not found: {TypeCache.GetShortName(type)}.{propertyNames[i]}");

                var columnName = property.GetColumnName(storeObjectIdentifier);
                if (string.IsNullOrWhiteSpace(columnName))
                    throw new InvalidOperationException(
                        $"Column mapping not found: {TypeCache.GetShortName(type)}.{propertyNames[i]}");

                columnNames[i] = columnName;
            }

            return new SchemaTableColumnTrio(schema ?? DefaultSchema, tableName, columnNames);
        }


        readonly record struct LockStatementCacheKey(Type EntityType, string PropertyKey);

        protected readonly struct SchemaTableColumnTrio
        {
            public SchemaTableColumnTrio(string schema, string table, string[] columnNames)
            {
                Schema = schema;
                Table = table;
                ColumnNames = columnNames;
            }

            public readonly string Schema;
            public readonly string Table;
            public readonly string[] ColumnNames;
        }
    }
}

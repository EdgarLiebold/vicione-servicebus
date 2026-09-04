using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Generates a monospaced text table, useful in trace output formats. Shamelessly inspired by ConsoleTables
/// https://github.com/khalidabuhakmeh/ConsoleTables
/// </summary>
public class TextTable
{
    static readonly HashSet<Type> NumericTypes = new HashSet<Type>
    {
        typeof(int),
        typeof(double),
        typeof(decimal),
        typeof(long),
        typeof(short),
        typeof(sbyte),
        typeof(byte),
        typeof(ulong),
        typeof(ushort),
        typeof(uint),
        typeof(float)
    };

    readonly List<object?> _columns;
    readonly List<object?[]> _rows;
    Type[] _columnTypes = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    public TextTable(params string[] columns)
        : this(new TextTableOptions { Columns = new List<string>(columns) })
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public TextTable(TextTableOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        options.Out ??= TextWriter.Null;

        _rows = new List<object?[]>();
        _columns = new List<object?>(options.Columns);
    }

    /// <summary>
    /// Gets the options value.
    /// </summary>
    public TextTableOptions Options { get; }

    /// <summary>
    /// Adds columns to the configuration.
    /// </summary>
    /// <param name="names">The names value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable AddColumns(params string[] names)
    {
        return AddColumns((IEnumerable<string>)names);
    }

    /// <summary>
    /// Adds columns to the configuration.
    /// </summary>
    /// <param name="names">The names value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable AddColumns(IEnumerable<string> names)
    {
        foreach (var name in names)
            _columns.Add(name);

        return this;
    }

    /// <summary>
    /// Adds row to the configuration.
    /// </summary>
    /// <param name="values">The values value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable AddRow(params object?[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (_columns.Count == 0)
            throw new InvalidOperationException("Columns must be specified before adding rows");
        if (_columns.Count != values.Length)
            throw new ArgumentException(nameof(values), $"Rows must have {_columns.Count} columns, only {values.Length} provided");

        _rows.Add(values);

        return this;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="action">The action value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable Configure(Action<TextTableOptions> action)
    {
        action(Options);

        return this;
    }

    /// <summary>
    /// Create a table from an existing enumerable collection
    /// </summary>
    /// <param name="rows">The collection</param>
    /// <typeparam name="T">The collection element type</typeparam>
    /// <returns></returns>
    public static TextTable Create<T>(IEnumerable<T> rows)
    {
        IReadOnlyPropertyCache<T> properties = TypeCache<T>.ReadOnlyPropertyCache;

        Type[] columnTypes = properties.Select(x => x.Property.PropertyType).ToArray();
        var columnNames = properties.Select(x => x.Property.Name).ToArray();

        var table = new TextTable(columnNames) { _columnTypes = columnTypes };

        foreach (IEnumerable<object?> propertyValues in rows.Select(value => properties.Select(column => column.GetProperty(value))))
            table.AddRow(propertyValues.ToArray());

        return table;
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();

        List<int> columnWidths = CalculateColumnWidths();

        List<string> columnAlignment = Enumerable.Range(0, _columns.Count)
            .Select(GetNumberAlignment)
            .ToList();

        var dataRowFormat = "\x2503 " + string.Join(" \x2502 ", Enumerable.Range(0, _columns.Count)
            .Select(x => "{" + x + "," + columnAlignment[x] + columnWidths[x] + "}")) + " \x2503";

        var columnHeaders = string.Format(dataRowFormat, _columns.ToArray());

        List<string> formattedRows = _rows.Select(row => string.Format(dataRowFormat, row)).ToList();

        var rowSeparator = "\x2520\x2500" + string.Join("\x2500\x253C\x2500", Enumerable.Range(0, _columns.Count)
            .Select(x => new string('\x2500', columnWidths[x]))) + "\x2500\x2528";
        var top = "\x250F\x2501" + string.Join("\x2501\x252F\x2501", Enumerable.Range(0, _columns.Count)
            .Select(x => new string('\x2501', columnWidths[x]))) + "\x2501\x2513";
        var bottom = "\x2517\x2501" + string.Join("\x2501\x2537\x2501", Enumerable.Range(0, _columns.Count)
            .Select(x => new string('\x2501', columnWidths[x]))) + "\x2501\x251B";

        builder.AppendLine(top);
        builder.AppendLine(columnHeaders);
        builder.AppendLine(rowSeparator);

        foreach (var row in formattedRows)
        {
            builder.AppendLine(row);

            if (Options.ShowRowSeparator)
                builder.AppendLine(rowSeparator);
        }

        builder.AppendLine(bottom);

        if (Options.EnableCount)
        {
            builder.AppendLine("");
            builder.AppendFormat("Count: {0}", _rows.Count);
        }

        return builder.ToString();
    }

    string GetNumberAlignment(int column)
    {
        return Options.NumberAlignment == NumberAlignment.Right
            && _columnTypes != null
            && NumericTypes.Contains(_columnTypes[column])
                ? ""
                : "-";
    }

    List<int> CalculateColumnWidths()
    {
        List<int> columnLengths = _columns
            .Select((t, i) => _rows.Select(x => x[i])
                .Union(new[] { _columns[i] })
                .Select(x => x?.ToString()?.Length ?? 0).Max())
            .ToList();
        return columnLengths;
    }

    /// <summary>
    /// Performs the write operation.
    /// </summary>
    public void Write()
    {
        Options.Out.WriteLine(ToString());
    }

    /// <summary>
    /// Sets column.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="columnType">The column type value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable SetColumn(int column, string name, Type? columnType = default)
    {
        if (column < 0 || column >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(column));
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        _columns[column] = name;

        if (columnType != default && _columnTypes != null)
            _columnTypes[column] = columnType;

        return this;
    }

    /// <summary>
    /// Performs the hide row separator operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TextTable HideRowSeparator()
    {
        Options.ShowRowSeparator = false;
        return this;
    }

    /// <summary>
    /// Performs the enable count operation.
    /// </summary>
    /// <param name="enabled">The enabled value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable EnableCount(bool enabled)
    {
        Options.EnableCount = enabled;
        return this;
    }

    /// <summary>
    /// Sets right number alignment.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TextTable SetRightNumberAlignment()
    {
        Options.NumberAlignment = NumberAlignment.Right;
        return this;
    }

    /// <summary>
    /// Performs the output to operation.
    /// </summary>
    /// <param name="textWriter">The text writer value.</param>
    /// <returns>The result of the operation.</returns>
    public TextTable OutputTo(TextWriter textWriter)
    {
        Options.Out = textWriter ?? TextWriter.Null;
        return this;
    }
}


/// <summary>
/// Defines configuration options for text table.
/// </summary>
public class TextTableOptions
{
    /// <summary>
    /// The column names
    /// </summary>
    public IEnumerable<string> Columns { get; set; } = new List<string>();

    /// <summary>
    /// Include the row count at the end of the table
    /// </summary>
    public bool EnableCount { get; set; }

    /// <summary>
    /// Specify the number alignment (defaults to left)
    /// </summary>
    public NumberAlignment NumberAlignment { get; set; } = NumberAlignment.Left;

    /// <summary>
    /// The <see cref="System.IO.TextWriter" /> to write to. Defaults to <see cref="System.Console.Out" />.
    /// </summary>
    public TextWriter Out { get; set; } = Console.Out;

    /// <summary>
    /// Gets or sets the show row separator value.
    /// </summary>
    public bool ShowRowSeparator { get; set; }
}


/// <summary>
/// Specifies the available number alignment values.
/// </summary>
public enum NumberAlignment
{
    /// <summary>
    /// Indicates left.
    /// </summary>
    Left,
    /// <summary>
    /// Indicates right.
    /// </summary>
    Right
}

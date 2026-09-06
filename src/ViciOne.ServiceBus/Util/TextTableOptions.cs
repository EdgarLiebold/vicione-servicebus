using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Defines configuration options for text table.
/// </summary>
public sealed class TextTableOptions
{
    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(Columns);
        ArgumentNullException.ThrowIfNull(Out);
        if (!Enum.IsDefined(NumberAlignment))
            throw new ArgumentOutOfRangeException(nameof(NumberAlignment), NumberAlignment, "Select a defined number-alignment value.");
        if (Columns.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Column names must not be empty.", nameof(Columns));
    }

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

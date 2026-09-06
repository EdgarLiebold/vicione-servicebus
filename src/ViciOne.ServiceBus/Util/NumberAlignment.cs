using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Util;

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

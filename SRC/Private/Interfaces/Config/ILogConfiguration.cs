/********************************************************************************
* ILogConfiguration.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes the required configuration values related to logging
    /// </summary>
    internal interface ILogConfiguration
    {
        /// <summary>
        /// Where to put the generated source files, or null if we don't need them.
        /// </summary>
        string? SourceDump { get; }

        /// <summary>
        /// Where to put the logs, or null if we don't need them.
        /// </summary>
        string? LogDump { get; }

        /// <summary>
        /// Log level to be used. Considered only when <see cref="LogDump"/> is not null.
        /// </summary>
        LogLevel LogLevel { get; }
    }
}

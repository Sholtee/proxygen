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
        /// Where to put the logs, or null if we don't need them.
        /// </summary>
        string? LogDirectory { get; }

        /// <summary>
        /// Log level to be used. Considered only when the <see cref="LogDirectory"/> is set.
        /// </summary>
        LogLevel LogLevel { get; }
    }
}

/********************************************************************************
* ConfigBase.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents the common configuration.
    /// </summary>
    internal abstract class ConfigBase: ILogConfiguration
    {
        /// <summary>
        /// When implemented, reads the value associated with the given <paramref name="name"/> from the underlying config source.
        /// </summary>
        protected abstract string? ReadValue(string name);

        /// <summary>
        /// When implemented returns the containing directory of executable that references the ProxyGen assembly.
        /// </summary>
        protected abstract string BasePath { get; }

        /// <inheritdoc/>
        public string? SourceDump => GetPath(nameof(SourceDump));

        /// <inheritdoc/>
        public string? LogDump => GetPath(nameof(LogDump));

        /// <inheritdoc/>
        public LogLevel LogLevel => Enum.TryParse(ReadValue(nameof(LogLevel)), out LogLevel logLevel)
            ? logLevel
            : LogLevel.Info;

        /// <summary>
        /// Gets the path associated with the given name.
        /// </summary>
        protected string? GetPath(string name)
        {
            string? result = ReadValue(name);

            if (result is not null)
            {
                result = Environment.ExpandEnvironmentVariables(result);

                if (!Path.IsPathRooted(result))
                    result = Path.Combine(BasePath, result);
            }

            return result;
        }
    }
}

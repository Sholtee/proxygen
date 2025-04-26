/********************************************************************************
* ILogger.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// The logger maintains two separate logs. The 1st contains collection of system logs, the 2nd is for the source being crafted.
    /// </summary>
    internal interface ILogger
    {
        /// <summary>
        /// Creates/overwrites the source log.
        /// </summary>
        void WriteSource(CompilationUnitSyntax src);

        /// <summary>
        /// Appends the system log
        /// </summary>
        void Log(LogLevel level, object id, string message, IDictionary<string, object?>? additionalData = null);

        /// <summary>
        /// The minimum log level under which the logger should not dump information or null when logging is turned off.
        /// </summary>
        LogLevel? Level { get; }

        /// <summary>
        /// The scope to be used. For instance when writing physical logs this id will be used in output file names: "{Scope}.log" and "{Scope}.cs"
        /// </summary>
        string Scope { get; }
    }
}

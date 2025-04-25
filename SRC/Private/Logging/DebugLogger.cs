/********************************************************************************
* DebugLogger.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Logger that logs nothing.
    /// </summary>
    internal sealed class DebugLogger(string scope) : LoggerBase(scope, LogLevel.Debug)
    {
        protected override void WriteSourceCore(string src) => Debug.WriteLine(src);

        protected override void LogCore(string message) => Debug.WriteLine(message);
    }
}

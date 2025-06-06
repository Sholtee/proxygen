﻿/********************************************************************************
* DebugLogger.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Logger that writes the input to the debug output. Note that for production builds this logger does nothing since under the hood it uses the <see cref="Debug.WriteLine(string)"/> method.
    /// </summary>
    internal sealed class DebugLogger(string scope) : LoggerBase(scope, LogLevel.Debug)
    {
        protected override void WriteSourceCore(string src) => Debug.WriteLine(src);

        protected override void LogCore(string message) => Debug.WriteLine(message);
    }
}

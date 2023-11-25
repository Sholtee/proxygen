/********************************************************************************
* ProxyEmbedderBase.Log.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using static System.Environment;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyEmbedderBase
    {
        //
        // SourceGenerator should reference as few dependencies as possible since dependencies must be bundled
        // to our sourcegenerator package. That's the reason behind this primitive logging
        //

        internal protected static string? LogException(Exception ex, in CancellationToken cancellation)
        {
            string? logDump = WorkingDirectories.Instance.LogDump;
            if (logDump is not null)
            {
                string logFile = Path.Combine(logDump, $"ProxyGen_{Guid.NewGuid()}.log");

                Directory.CreateDirectory(logDump);

                try
                {
                    using StreamWriter log = File.CreateText(logFile);
                    log.AutoFlush = true;

                    for (Exception? current = ex; current is not null; current = current.InnerException)
                    {
                        if (current != ex) log.Write($"{NewLine}->{NewLine}", cancellation: cancellation);
                        log.Write(current.ToString(), cancellation: cancellation);

                        foreach (object? key in current.Data.Keys)
                        {
                            log.Write($"{NewLine + key}:{NewLine + current.Data[key]}", cancellation: cancellation);
                        }
                    }

                    return logFile;
                }
                catch (IOException exc)
                {
                    Trace.TraceWarning($"File ({logFile}) could not be dumped: ${exc.Message}");
                }
            }
            return null;
        }
    }
}

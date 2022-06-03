/********************************************************************************
* ProxyEmbedderBase.Log.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Threading;

using static System.Environment;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyEmbedderBase
    {
        //
        // A SourceGenerator a leheto legkevesebb fuggoseget kell hivatkozza (mivel azokat mind hivatkozni kell
        // a Roslyn szamara is), ezert a primitiv naplozas.
        //

        internal protected static string? LogException(Exception ex, in CancellationToken cancellation)
        {
            string? logDump = WorkingDirectories.Instance.LogDump;
            if (logDump is not null)
            {
                try
                {
                    Directory.CreateDirectory(logDump);

                    string logFile = Path.Combine(logDump, $"ProxyGen_{Guid.NewGuid()}.log");

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
                #pragma warning disable CA1031 // This method should never throw.
                catch {}
                #pragma warning restore CA1031
            }
            return null;
        }
    }
}

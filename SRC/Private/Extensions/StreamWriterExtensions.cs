/********************************************************************************
* StreamWriterExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal static class StreamWriterExtensions
    {
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "The 'cancellation' is supported in NETSTANDARD2_1+ only")]
        public static void Write(this StreamWriter self, string str, CancellationToken cancellation) =>
#if NETSTANDARD2_0
            self.Write(str);
#else
            self.WriteAsync(str.AsMemory(), cancellation).Wait(CancellationToken.None);
#endif
    }
}

﻿/********************************************************************************
* StreamWriterExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal static class StreamWriterExtensions
    {
        public static void Write(this StreamWriter self, in string str, CancellationToken cancellation)
        {
#if NETSTANDARD2_0
            self.Write(str);
#else
            self.WriteAsync(str.AsMemory(), cancellation).Wait();
#endif
        }
    }
}
/********************************************************************************
* RuntimeConfigReader.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class RuntimeConfigReader : IConfigReader
    {
        //
        // Assembly.GetEntryAssembly() can be NULL in certain circumstances so
        // use BaseDirectory instead
        //

        public string BasePath { get; } = AppDomain.CurrentDomain.BaseDirectory;

        public string? ReadValue(string name) => AppContext.GetData($"ProxyGen.{name}") as string;
    }
}

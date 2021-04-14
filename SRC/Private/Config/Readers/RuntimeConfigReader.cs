/********************************************************************************
* RuntimeConfigReader.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class RuntimeConfigReader : IConfigReader
    {
        public string BasePath { get; } = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public string? ReadValue(string name) => AppContext.GetData($"ProxyGen.{name}") as string;
    }
}

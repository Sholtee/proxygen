/********************************************************************************
* DictionaryConfigReader.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class DictionaryConfigReader(IReadOnlyDictionary<string, string> values, string? basePath = null) : IConfigReader
    {
        public string BasePath => basePath ?? throw new NotSupportedException();

        public string? ReadValue(string name) => values.TryGetValue(name, out string value)
            ? value
            : null;
    }
}

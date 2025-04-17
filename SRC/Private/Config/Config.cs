/********************************************************************************
* Config.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class Config(IConfigReader configReader)
    {
        public string? AssemblyCacheDir { get; } = GetPath(configReader, nameof(AssemblyCacheDir));

        public string? SourceDump { get; } = GetPath(configReader, nameof(SourceDump));

        public string? LogDump { get; } = GetPath(configReader, nameof(LogDump));
#if DEBUG
        public bool DebugGenerator { get; } = configReader
            .ReadValue(nameof(DebugGenerator))?
            .Equals(true.ToString(), StringComparison.OrdinalIgnoreCase) is true;
#endif
        private static string? GetPath(IConfigReader configReader, string name)
        {
            string? result = configReader.ReadValue(name);

            if (result is not null)
            {
                result = Environment.ExpandEnvironmentVariables(result);

                if (!Path.IsPathRooted(result))
                    result = Path.Combine(configReader.BasePath, result);
            }

            return result;
        }
    }
}

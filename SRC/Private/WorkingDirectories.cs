/********************************************************************************
* WorkingDirectories.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    internal static class WorkingDirectories
    {
        public static string? AssemblyCacheDir { get; } = GetFromRuntimeConfig(nameof(AssemblyCacheDir));

        public static string? SourceDump { get; } = GetFromRuntimeConfig
        (
            nameof(SourceDump)
#if DEBUG
            , Path.GetTempPath()
#endif
        );

        public static string LogDump { get; } = GetFromRuntimeConfig(nameof(LogDump), Path.GetTempPath())!;

        private static string? GetFromRuntimeConfig(string name, string? @default = null)
        {
            string? result = AppContext.GetData(name) as string ?? @default;

            if (result is not null) result = Path.GetFullPath
            (
                Environment.ExpandEnvironmentVariables(result)
            );

            return result;
        }
    }
}

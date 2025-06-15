/********************************************************************************
* PlatformAssemblies.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Returns the assembly references to <see href="https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-1">netstandard</see> version supported be the actual runtime.
    /// </summary>
    internal static class PlatformAssemblies
    {
        private static IEnumerable<MetadataReference> Read()
        {
            Assembly netstandard = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Single(static asm => asm.GetName().Name == "netstandard");

            string baseDir = Path.GetDirectoryName(netstandard.Location);

            return netstandard
                .GetReferencedAssemblies()
                .Select(asm => Path.Combine(baseDir, $"{asm.Name}.dll"))
                .Where(File.Exists)
                .Concat([netstandard.Location])
                .OrderBy(Path.GetFileName)
                .Select(static loc => MetadataReference.CreateFromFile(loc));
        }

        /// <summary>
        /// The assembly references wrapped to Roslyn's <see cref="MetadataReference"/>. 
        /// </summary>
        public static IReadOnlyCollection<MetadataReference> References { get; } = [..Read()];
    }
}
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
                .Select(static loc => MetadataReference.CreateFromFile(loc));
        }

        public static IReadOnlyCollection<MetadataReference> References { get; } = [..Read()];
    }
}
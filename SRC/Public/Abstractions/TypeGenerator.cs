/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Implements the <see cref="ITypeGenerator"/> interface.
    /// </summary>
    /// <remarks>Generators can not be instantiated. To access the create type use the <see cref="GeneratedType"/> property.</remarks>
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        private static readonly object FLock = new object();

        private static Type? FType;

        private Type ExtractType(Assembly asm) => asm.GetType(SyntaxFactory.GeneratedClassName, throwOnError: true);

        internal string? CacheFile => CacheDirectory != null 
            ? Path.Combine(CacheDirectory, $"{MD5Hash.CreateFromString(SyntaxFactory.AssemblyName)}.dll")
            : null;

        //
        // "assemblyNameOverride" parameter CSAK a teljesitmeny tesztek miatt szerepel.
        //

        internal Type GenerateType(string? assemblyNameOverride = null) => ExtractType
        (
            Compile.ToAssembly
            (
                root: SyntaxFactory.GenerateProxyUnit(),
                asmName: assemblyNameOverride ?? SyntaxFactory.AssemblyName,
                outputFile: CacheFile,
                references: References
            )
        );

        internal bool TryLoadType(out Type? type) 
        {
            string? cacheFile = CacheFile;

            if (cacheFile != null && File.Exists(cacheFile))
            {
                type = ExtractType(Assembly.LoadFile(cacheFile));
                return true;
            }

            type = null;
            return false;
        }

        /// <summary>
        /// The genrated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is assembled only once so you can read this property multiple times.</remarks>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By this, every concrete generator will have its own generated type")]
        public static Type GeneratedType 
        {
            get 
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null)
                        {
                            var self = new TDescendant();
                            self.DoCheck();
                            if (!self.TryLoadType(out FType)) FType = self.GenerateType();
                        }
                return FType!;
            }
        }

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public abstract IReadOnlyList<Assembly> References { get; }

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public abstract ISyntaxFactory SyntaxFactory { get; }

        /// <summary>
        /// The (optional) cache directory to be used to store the generated assembly.
        /// </summary>
        /// <remarks>Every product version should have its own cache directory to prevent unexpected <see cref="TypeLoadException"/>s.</remarks>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By this, every concrete generator will have its own cache directory")]
        public static string? CacheDirectory { get; set; }

        /// <summary>
        /// Override to invoke your own checks.
        /// </summary>
        protected virtual void DoCheck() { }

        /// <summary>
        /// Throws if the <paramref name="type"/> is not visible for the assembly being created.
        /// </summary>
        protected void CheckVisibility(Type type)
        {
#if !IGNORE_VISIBILITY
            Visibility.Check(type, SyntaxFactory.AssemblyName);
#endif
        }
    }
}

/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Implements the <see cref="ITypeGenerator"/> interface.
    /// </summary>
    /// <remarks>Generators can not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType(string?)"/> or <see cref="GetGeneratedTypeAsync(string?, CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        #region Private
        private static readonly SemaphoreSlim FLock = new SemaphoreSlim(1, 1);

        private static Type? FType;

        private Type ExtractType(Assembly asm) => asm.GetType(SyntaxFactory.Classes.Single(), throwOnError: true);
        #endregion

        #region Internal
        internal string CacheFileName => $"{MD5Hash.CreateFromString(AssemblyName)}.dll";

        //
        // "assemblyNameOverride" parameter CSAK a teljesitmeny tesztek miatt szerepel.
        //

        internal Type GenerateTypeCore(string? outputFile = default, string? assemblyNameOverride = default, CancellationToken cancellation = default)
        {
            SyntaxFactory.Build(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     SyntaxFactory.Unit!,
                     asmName: assemblyNameOverride ?? AssemblyName,
                     outputFile,
                     SyntaxFactory.References.Select(asm => MetadataReference.CreateFromFile(asm.Location!)).ToArray(),
                     cancellation
                 )
            );
        }

        internal static Type GenerateType(string? cacheDir, CancellationToken cancellation = default) 
        {
            var self = new TDescendant();
            self.DoCheck();

            string? cacheFile = null;

            if (!string.IsNullOrEmpty(cacheDir))
            {
                cacheFile = Path.Combine(cacheDir, self.CacheFileName);

                if (File.Exists(cacheFile)) return self.ExtractType
                (
                    Assembly.LoadFile(cacheFile)
                );

                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);
            }

            return self.GenerateTypeCore(cacheFile, null, cancellation);
        }
        #endregion

        #region Protected
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
            Visibility.Check(MetadataTypeInfo.CreateFrom(type), AssemblyName);
#endif
        }
        #endregion

        #region Public
        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static async Task<Type> GetGeneratedTypeAsync(string? cacheDir = default, CancellationToken cancellation = default)
        {
            if (FType != null) return FType;

            await FLock.WaitAsync(cancellation).ConfigureAwait(false);

            try
            {
                return FType ??= GenerateType(cacheDir, cancellation);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static Type GetGeneratedType(string? cacheDir = null) 
        {
            if (FType != null) return FType;

            FLock.Wait();

            try
            {
                return FType ??= GenerateType(cacheDir);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// The name of the assembly that will contain the generated <see cref="Type"/>.
        /// </summary>
        public string AssemblyName { get; protected set; } = $"Generated_{typeof(TDescendant).GetMD5HashCode()}";

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public abstract IUnitSyntaxFactory SyntaxFactory { get; }
        #endregion
    }
}

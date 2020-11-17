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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Implements the <see cref="ITypeGenerator"/> interface.
    /// </summary>
    /// <remarks>Generators can not be instantiated. To access the create type use the <see cref="GeneratedType"/> property.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        //
        // Szal biztos: https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netcore-3.1#thread-safety
        //

        private static readonly SemaphoreSlim FLock = new SemaphoreSlim(1, 1);

        private static Type? FType;

        private Type ExtractType(Assembly asm) => asm.GetType(SyntaxFactory.ProxyClassName, throwOnError: true);

        internal string? CacheFile => CacheDirectory != null 
            ? Path.Combine(CacheDirectory, $"{MD5Hash.CreateFromString(SyntaxFactory.AssemblyName!)}.dll")
            : null;

        //
        // "assemblyNameOverride" parameter CSAK a teljesitmeny tesztek miatt szerepel.
        //

        internal Task<Type> GenerateTypeAsync(string? assemblyNameOverride = default) => Task.Factory.StartNew(() => 
        {
            (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References, _) = SyntaxFactory.GetContext();

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     root: Unit,
                     asmName: assemblyNameOverride ?? SyntaxFactory.AssemblyName!,
                     outputFile: CacheFile,
                     references: References
                 )
            );
        }, default, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        internal static async Task<Type> GetGeneratedTypeAsync()
        {
            if (FType != null) return FType;

            await FLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (FType == null)
                {
                    var self = new TDescendant();
                    self.DoCheck();

                    if (!self.TryLoadType(out FType))
                        FType = await self.GenerateTypeAsync().ConfigureAwait(false);
                }

                return FType!;
            }
            finally { FLock.Release(); }
        }

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
        [Obsolete("Use GeneratedTypeAsync instead")]
        public static Type GeneratedType => GeneratedTypeAsync.GetAwaiter().GetResult();

        /// <summary>
        /// The genrated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is assembled only once so you can read this property multiple times.</remarks>
        public static Task<Type> GeneratedTypeAsync => GetGeneratedTypeAsync();

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public abstract IProxySyntaxFactory SyntaxFactory { get; }

        /// <summary>
        /// The (optional) cache directory to be used to store the generated assembly.
        /// </summary>
        /// <remarks>Every product version should have its own cache directory to prevent unexpected <see cref="TypeLoadException"/>s.</remarks>
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
            Visibility.Check(type, SyntaxFactory.AssemblyName!);
#endif
        }
    }
}

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
    /// <remarks>Generators can not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        #region Private
        private static readonly SemaphoreSlim FLock = new SemaphoreSlim(1, 1);

        private static Type? FType;

        private Type ExtractType(Assembly asm) => asm.GetType(SyntaxFactory.DefinedClasses.Single(), throwOnError: true);

        private Type GenerateOrLoadType(CancellationToken cancellation = default)
        {
            DoCheck();

            string? cacheFile = null;

            if (!string.IsNullOrEmpty(CacheDir))
            {
                cacheFile = Path.Combine(CacheDir, CacheFileName);

                if (File.Exists(cacheFile)) return ExtractType
                (
                    Assembly.LoadFile(cacheFile)
                );

                if (!Directory.Exists(CacheDir))
                    Directory.CreateDirectory(CacheDir);
            }

            return GenerateType(cacheFile, null, cancellation);
        }

        private static Type GetGeneratedType(CancellationToken cancellation)
        {
            TDescendant self;
            try
            {
                self = new TDescendant();
            }

            //
            // "new" operator hivasa Activator.CreateInstace() hivas valojaban
            //

            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }

            return self.GenerateOrLoadType(cancellation);
        }
        #endregion

        #region Internal
        internal string CacheFileName => $"{AssemblyName}.dll"; // tesztekhez

        internal static string? CacheDir { get; set; } = AppContext.GetData("AssemblyCacheDir") as string; // tesztekhez

        //
        // "assemblyNameOverride" parameter CSAK a teljesitmeny tesztek miatt szerepel.
        //

        internal Type GenerateType(string? outputFile = default, string? assemblyNameOverride = default, CancellationToken cancellation = default)
        {
            SyntaxFactory.Build(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     SyntaxFactory.Unit!,
                     asmName: assemblyNameOverride ?? AssemblyName,
                     outputFile,
                     SyntaxFactory
                        .References
                        .Select(asm => MetadataReference.CreateFromFile(asm.Location!))
                        .ToArray(),
                     allowUnsafe: false,
                     cancellation
                 )
            );
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
        public static async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
        {
            if (FType != null) return FType;

            await FLock.WaitAsync(cancellation).ConfigureAwait(false);

            try
            {
                return FType ??= GetGeneratedType(cancellation);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static Type GetGeneratedType() 
        {
            if (FType != null) return FType;

            FLock.Wait();

            try
            {
                return FType ??= GetGeneratedType(default);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// The name of the assembly that will contain the generated <see cref="Type"/>.
        /// </summary>
        public virtual string AssemblyName { get; } = $"Generated_{MetadataTypeInfo.CreateFrom(typeof(TDescendant)).GetMD5HashCode()}";

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public abstract IUnitSyntaxFactory SyntaxFactory { get; }
        #endregion
    }
}

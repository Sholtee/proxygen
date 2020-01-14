/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
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

        private static Type FType;

        //
        // "assemblyNameOverride" parameter CSAK a teljesitmeny tesztek miatt szerepel.
        //

        internal Type GenerateType(string assemblyNameOverride = null) 
        {
            DoCheck();

            return Compile.ToAssembly
            (
                root: SyntaxFactory.GenerateProxyUnit(),
                asmName: assemblyNameOverride ?? SyntaxFactory.AssemblyName,
                references: References
            )
            .GetType(SyntaxFactory.GeneratedClassName, throwOnError: true);
        }

        /// <summary>
        /// The genrated <see cref="Type"/>.
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By this, every concrete generator will have its own generated type")]
        public static Type GeneratedType 
        {
            get 
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null)
                            FType = new TDescendant().GenerateType();
                return FType;
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

/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Implements the <see cref="ITypeGenerator"/> interface.
    /// </summary>
    /// <remarks>Concrete generators are singletons. To access them use the <see cref="Instance"/> property.</remarks>
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        //
        // Mivel szerelveny adott nevvel csak egyszer toltheto be ezert globalisan lokkolunk
        // generatoronkent.
        //

        private static readonly object FLock = new object();

        //
        // Generatoronkent kulombozik (ezert kell a TDescendant-os varazslas, bar baszott ronda).
        //

        private static Type FType;

        private Type GenerateType()
        {
            Debug.Assert(FType == null);

            DoCheck();

            return Compile.ToAssembly
            (
                root: SyntaxFactory.GenerateProxyUnit(),
                asmName: SyntaxFactory.AssemblyName,
                references: References
            )
            .GetType(SyntaxFactory.GeneratedClassName, throwOnError: true);
        }

        /// <summary>
        /// The generator instance.
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "By this every concrete generator will have its own Instance")]
        public static TDescendant Instance { get; } = new TDescendant();

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public Type GeneratedType 
        {
            get 
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null)
                            FType = GenerateType();
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

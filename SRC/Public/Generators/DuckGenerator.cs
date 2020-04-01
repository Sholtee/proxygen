/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Generators
{
    using Abstractions;
    using Internals;
    using Properties;

    /// <summary>
    /// Type generator for creating proxies that let <typeparamref name="TTarget"/> behaves like a <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target who implements all the <typeparamref name="TInterface"/> members.</typeparam>
    public sealed class DuckGenerator<TInterface, TTarget>: TypeGenerator<DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public override IReadOnlyList<Assembly> References { get; } = new[]
            {
                typeof(DuckBase<>).Assembly
            }
            .Concat(typeof(TInterface).GetReferences())
            .Concat(typeof(TTarget).GetReferences())
            .Distinct()
            .ToArray();

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public override ISyntaxFactory SyntaxFactory { get; } = new DuckSyntaxFactory<TInterface, TTarget>();

        /// <summary>
        /// See <see cref="TypeGenerator{T}"/>.
        /// </summary>
        protected override void DoCheck()
        {
            CheckInterface();
            CheckTarget();
        }

        private void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface) throw new InvalidOperationException(Resources.NOT_AN_INTERFACE);
            if (type.ContainsGenericParameters) throw new InvalidOperationException();
        }

        private void CheckTarget()
        {
            //
            // Konstruktor parameterben atadasra kerul -> lathatonak kell lennie.
            //

            CheckVisibility(typeof(TTarget));
        }
    }
}
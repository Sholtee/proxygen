/********************************************************************************
* ClassProxySyntaxFactory.Activator.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using TupleWrapper = Tuple<IInterceptor, object>;

    internal sealed partial class ClassProxySyntaxFactory : ProxyUnitSyntaxFactory
    {
        /// <summary>
        /// <code>
        /// new Proxy(wrapper.Item1, ...);
        /// </code>
        /// </summary>
        protected override IEnumerable<StatementSyntax> ResolveProxyObject(ClassDeclarationSyntax cls, object context, params IEnumerable<ExpressionSyntax> arguments) => base.ResolveProxyObject
        (
            cls,
            null!,
            [(MemberAccessExpressionSyntax) context, ..arguments]
        );

        /// <inheritdoc/>
        protected override IEnumerable<ParameterSyntax> FilterProxyObjectCtorParameters(ConstructorDeclarationSyntax ctor) =>
            //
            // Skip the first argument as it will be taken from the "wrapper"
            //

            base.FilterProxyObjectCtorParameters(ctor).Skip(1);

        /// <summary>
        /// <code>
        /// Tuple&lt;IInterceptor, object&gt; wrapper = (Tuple&lt;IInterceptor, object&gt;) tuple;
        /// switch (wrapper.Item2)
        /// {
        ///     case null: return new Class(wrapper.Item1);
        ///     case Tuple&lt;int, string&gt; t1: return new Class(wrapper.Item1, t1.Item1, t1.Item2);
        ///     default: throw new MissingMethodException("...");
        /// }
        /// </code>
        /// </summary>
        protected override IEnumerable<StatementSyntax> ResolveActivatorBody(ClassDeclarationSyntax cls, ExpressionSyntax tuple, object context)
        {
            LocalDeclarationStatementSyntax wrapper = ResolveLocal<TupleWrapper>
            (
                nameof(wrapper),
                CastExpression
                (
                    ResolveType<TupleWrapper>(),
                    tuple
                )
            );
            yield return wrapper;

            SimpleNameSyntax wrapperName = ResolveIdentifierName(wrapper);

            foreach
            (
                StatementSyntax statement in base.ResolveActivatorBody
                (
                    cls,
                    SimpleMemberAccess
                    (
                        wrapperName,
                        nameof(TupleWrapper.Item2)
                    ),
                    SimpleMemberAccess
                    (
                        wrapperName,
                        nameof(TupleWrapper.Item1)
                    )
                )
            )
                yield return statement;
        }
    }
}
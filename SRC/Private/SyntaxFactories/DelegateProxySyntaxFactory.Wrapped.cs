/********************************************************************************
* DelegateProxySyntaxFactory.Wrapped.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed partial class DelegateProxySyntaxFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IPropertyInfo FWrapped = MetadataPropertyInfo.CreateFrom
        (
            PropertyInfoExtensions.ExtractFrom(static (IDelegateWrapper w) => w.Wrapped)
        );

        /// <summary>
        /// <code>
        /// Delegate Wrapped => (TConcreteDelegate) Invoke;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => base.ResolveProperties
        (
            cls.AddMembers
            (
                ResolveProperty
                (
                    FWrapped,
                    getBody: ArrowExpressionClause
                    (
                        CastExpression
                        (
                            ResolveType(TargetType!),
                            SimpleMemberAccess
                            (
                                ThisExpression(),
                                INVOKE_METHOD_NAME
                            )
                        )
                    ),
                    setBody: null
                )
            ),
            context
        );
    }
}

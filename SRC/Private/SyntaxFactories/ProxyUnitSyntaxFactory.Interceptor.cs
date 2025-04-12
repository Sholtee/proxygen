/********************************************************************************
* ProxyUnitSyntaxFactory.Interceptor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IMethodInfo FInvokeInterceptor = MetadataMethodInfo.CreateFrom
        (
            MethodInfoExtensions.ExtractFrom(static (IInterceptor i) => i.Invoke(null!))
        );

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IPropertyInfo FInterceptor = MetadataPropertyInfo.CreateFrom
        (
            PropertyInfoExtensions.ExtractFrom(static (IInterceptorAccess ia) => ia.Interceptor)
        );

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) =>
        [
            MetadataTypeInfo.CreateFrom(typeof(IInterceptorAccess)),
            ..base.ResolveBases(context)
        ];

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => base.ResolveProperties
        (
            cls.AddMembers
            (
                ResolveProperty(FInterceptor, null, null)
            ),
            context
        );

        protected InvocationExpressionSyntax InvokeInterceptor<TContext>(params IEnumerable<ArgumentSyntax> arguments) where TContext: IInvocationContext => InvokeMethod
        (
            method: FInvokeInterceptor,
            target: MemberAccess
            (
                ThisExpression(),
                FInterceptor,
                castTargetTo: FInterceptor.DeclaringType
            ),
            castTargetTo: null,
            arguments: Argument
            (
                ResolveObject<TContext>
                (
                    arguments
                )
            )
        );
    }
}
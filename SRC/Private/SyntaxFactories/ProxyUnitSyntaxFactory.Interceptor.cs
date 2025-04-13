/********************************************************************************
* ProxyUnitSyntaxFactory.Interceptor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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

        #if DEBUG
        internal
        #endif
        protected IEnumerable<StatementSyntax> ResolveInvokeInterceptor<TContext>(IMethodInfo targetMethod, Func<LocalDeclarationStatementSyntax, IEnumerable<ArgumentSyntax>> contextArgs) where TContext : IInvocationContext
        {
            LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(targetMethod);
            yield return argsArray;

            InvocationExpressionSyntax invokeInterceptor = InvokeMethod
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
                        contextArgs(argsArray)
                    )
                )
            );

            LocalDeclarationStatementSyntax? result;

            if (targetMethod.ReturnValue.Type.Flags.HasFlag(TypeInfoFlags.IsVoid))
            {
                result = null;
                yield return ExpressionStatement(invokeInterceptor);
            }
            else
            {
                result = ResolveLocal<object>
                (
                    EnsureUnused(targetMethod, nameof(result)),
                    invokeInterceptor
                );
                yield return result;
            }

            foreach(ExpressionStatementSyntax expr in AssignByRefParameters(targetMethod, argsArray))
                yield return expr;

            if (result is not null)
               yield return ReturnResult(targetMethod.ReturnValue.Type, result);
        }
    }
}
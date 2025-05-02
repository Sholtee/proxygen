/********************************************************************************
* ProxyUnitSyntaxFactory.Interceptor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory
    {
        private const string INTERCEPTOR_FIELD = "FInterceptor";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IMethodInfo FInvokeInterceptor = MetadataMethodInfo.CreateFrom
        (
            MethodInfoExtensions.ExtractFrom(static (IInterceptor i) => i.Invoke(null!))
        );

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) => base.ResolveMembers
        (
            cls.AddMembers
            (
                ResolveField(MetadataTypeInfo.CreateFrom(typeof(IInterceptor)), INTERCEPTOR_FIELD, @static: false)
            ),
            context,
            cancellation
        );

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(IConstructorInfo ctor, SyntaxToken name) => AugmentConstructor<IInterceptor>
        (
            base.ResolveConstructor(ctor, name),
            "interceptor",
            interceptor => ExpressionStatement
            (
                AssignmentExpression
                (
                    SyntaxKind.SimpleAssignmentExpression,
                    left: SimpleMemberAccess(ThisExpression(), INTERCEPTOR_FIELD),
                    right: IdentifierName(interceptor.Identifier)
                )
            )
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
                target: SimpleMemberAccess
                (
                    ThisExpression(),
                    INTERCEPTOR_FIELD
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
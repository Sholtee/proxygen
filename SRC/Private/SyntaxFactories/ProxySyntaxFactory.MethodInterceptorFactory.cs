/********************************************************************************
* ProxySyntaxFactory.MethodInterceptorFactory.cs                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)               <br/>
        /// {                                                                                                    <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                 <br/>
        ///                                                                                                      <br/>
        ///     InvokeTarget = () =>                                                                             <br/>
        ///     {                                                                                                <br/>
        ///         System.Int32 cb_a = (System.Int32)args[0];                                                   <br/>
        ///         System.String cb_b;                                                                          <br/>
        ///         TT cb_c = (TT)args[2];                                                                       <br/>
        ///         System.Object result;                                                                        <br/>
        ///         result = this.Target.Foo[TT](cb_a, out cb_b, ref cb_c);                                      <br/>
        ///                                                                                                      <br/>
        ///         args[1] = (System.Object)cb_b;                                                               <br/>
        ///         args[2] = (System.Object)cb_c;                                                               <br/>
        ///         return result;                                                                               <br/>
        ///     };                                                                                               <br/>         
        ///                                                                                                      <br/>
        ///     MethodInfo method = ResolveMethod(InvokeTarget);                                                 <br/>
        ///     System.Object result = Invoke(method, args, method);                                             <br/>
        ///                                                                                                      <br/>
        ///     para2 = (T2) args[1];                                                                            <br/>
        ///     para3 = (T3) args[2];                                                                            <br/>
        ///                                                                                                      <br/>
        ///     return (TResult) result;                                                                         <br/>
        /// }
        /// </summary>
        internal sealed class MethodInterceptorFactory : InterceptorFactoryBase
        {
            #region Internals
            private static readonly MethodInfo
                RESOLVE_METHOD = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveMethod(default!));

            /// <summary>
            /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)   <br/>
            /// {                                                                                        <br/>
            ///   ...                                                                                    <br/>
            ///   para2 = (T2) args[1];                                                                  <br/>
            ///   para3 = (T3) args[2];                                                                  <br/>
            ///   ...                                                                                    <br/>
            /// }
            /// </summary>
            internal static IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(IReadOnlyList<ParameterInfo> paramz, LocalDeclarationStatementSyntax argsArray) => paramz
                .Select((param, i) => new { Parameter = param, Index = i })
                .Where(p => new[] { ParameterKind.InOut, ParameterKind.Out }.Contains(p.Parameter.GetParameterKind()))
                .Select
                (
                    p => ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: IdentifierName(p.Parameter.Name),
                            right: CastExpression
                            (
                                type: CreateType(p.Parameter.ParameterType),
                                expression: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                    )
                                )
                            )
                        )
                    )
                );

            /// <summary>
            /// args[0] = (System.Object)cb_a // ref <br/>
            /// args[2] = (TT)cb_c // out
            /// </summary>
            internal static IEnumerable<StatementSyntax> ReassignArgsArray(IReadOnlyList<ParameterInfo> paramz, LocalDeclarationStatementSyntax argsArray, IReadOnlyList<LocalDeclarationStatementSyntax> locals) => paramz
                .Select((param, i) => new { Parameter = param, Index = i })
                .Where(p => new[] { ParameterKind.InOut, ParameterKind.Out }.Contains(p.Parameter.GetParameterKind()))
                .Select
                (
                    p => ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
                            (
                                argumentList: BracketedArgumentList
                                (
                                    SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                )
                            ),
                            right: CastExpression
                            (
                                type: CreateType(typeof(object)),
                                ToIdentifierName(locals[p.Index])
                            )
                        )
                    )
                );

            internal static LambdaExpressionSyntax BuildCallback(MethodInfo method, LocalDeclarationStatementSyntax argsArray) => DeclareCallback(argsArray, method, (locals, result) =>
            {
                InvocationExpressionSyntax invocation = InvokeMethod
                (
                    method,
                    TARGET,
                    castTargetTo: null,
                    arguments: locals.Select
                    (
                        arg => Argument(ToIdentifierName(arg))
                    ).ToArray()
                );

                var body = new List<StatementSyntax>();
                body.Add
                (
                    ExpressionStatement
                    (
                        method.ReturnType != typeof(void)
                            ? AssignmentExpression
                            (
                                SyntaxKind.SimpleAssignmentExpression,
                                ToIdentifierName(result!),
                                CastExpression
                                (
                                    CreateType<object>(),
                                    invocation
                                )
                            )
                            : (ExpressionSyntax) invocation
                    )
                );
                body.AddRange
                (
                    ReassignArgsArray(method.GetParameters(), argsArray, locals)
                );

                return body;
            });

            internal static IEnumerable<StatementSyntax> BuildBody(MethodInfo methodInfo) 
            {
                var statements = new List<StatementSyntax>();

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(methodInfo);

                statements.Add(argsArray);
                statements.Add
                (
                    AssignCallback
                    (
                        BuildCallback(methodInfo, argsArray)
                    )
                );

                LocalDeclarationStatementSyntax method = DeclareLocal<MethodInfo>(EnsureUnused(nameof(method), methodInfo), InvokeMethod
                (
                    RESOLVE_METHOD,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null, null)
                    )
                ));
                statements.Add(method);

                InvocationExpressionSyntax invocation = InvokeMethod
                (
                    INVOKE,
                    target: null,
                    castTargetTo: null,
                    Argument(ToIdentifierName(method)), Argument(ToIdentifierName(argsArray)), Argument(ToIdentifierName(method))
                );

                if (methodInfo.ReturnType != typeof(void))
                {
                    LocalDeclarationStatementSyntax result = DeclareLocal<object>
                    (
                        EnsureUnused(nameof(result), methodInfo),
                        invocation
                    );

                    statements.Add(result);
                    statements.AddRange(AssignByRefParameters(methodInfo.GetParameters(), argsArray));
                    statements.Add
                    (
                        ReturnResult(methodInfo.ReturnType, result)
                    );
                }
                else
                {
                    statements.Add
                    (
                        ExpressionStatement(invocation)
                    );
                    statements.AddRange(AssignByRefParameters(methodInfo.GetParameters(), argsArray));
                }

                return statements;
            }
            #endregion

            public override bool IsCompatible(MemberInfo member) => member is MethodInfo method && method.DeclaringType.IsInterface && !method.IsSpecialName && !AlreadyImplemented(method);

            public override MemberDeclarationSyntax Build(MemberInfo member) 
            {
                MethodInfo method = (MethodInfo) member;

                //
                // "ref" visszateres nem tamogatott.
                //

                if (method.ReturnType.IsByRef)
                    throw new NotSupportedException(Resources.REF_RETURNS_NOT_SUPPORTED);
             
                return DeclareMethod(method).WithBody
                (
                    body: Block
                    (
                        BuildBody(method)
                    )
                );
            }
        }
    }
}
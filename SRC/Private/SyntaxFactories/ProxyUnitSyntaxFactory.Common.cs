/********************************************************************************
* ProxyUnitSyntaxFactory.Common.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract partial class ProxyUnitSyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        protected static readonly IReadOnlyCollection<ParameterKind> ByRefs = new[] { ParameterKind.Ref, ParameterKind.Out };

        protected static string EnsureUnused(string name, IEnumerable<IParameterInfo> parameters)
        {
            while (parameters.Any(param => param.Name == name))
            {
                name = $"_{name}";
            }
            return name;
        }

        protected static string EnsureUnused(string name, IMethodInfo method) => EnsureUnused(name, method.Parameters);

        /// <summary>
        /// <code>
        /// TResult IInterface.Foo&lt;TGeneric&gt;(T1 para1, ref T2 para2, out T3 para3, TGeneric para4) 
        /// {                                                                                
        ///   ...                                                                          
        ///   object[] args = new object[]{para1, para2, default(T3), para4};              
        ///   ...                                                                            
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected
        LocalDeclarationStatementSyntax ResolveArgumentsArray(IMethodInfo method)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            return ResolveLocal<object[]>
            (
                EnsureUnused("args", paramz),
                ResolveArray<object>
                (
                    paramz.Select
                    (
                        param => (ExpressionSyntax)
                        (
                            param.Kind switch
                            {
                                _ when param.Type.RefType is RefType.Ref =>
                                    //
                                    // We cannot cast "ref struct"s to objects
                                    //

                                    throw new NotSupportedException(Resources.BYREF_NOT_SUPPORTED),
                                ParameterKind.Out => DefaultExpression
                                (
                                    ResolveType(param.Type)
                                ),
                                _ => IdentifierName(param.Name)
                            }
                        )
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// return;
        /// // OR
        /// return (T) ...;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected
        ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType?.IsVoid is true
                ? null
                : returnType is not null
                    ? CastExpression
                    (
                        type: ResolveType(returnType),
                        expression: result
                    )
                    : result
        );

        /// <summary>
        /// <code>
        /// return;
        /// // OR
        /// return (T) result;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected
        ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ResolveIdentifierName(result));

        /// <summary>
        /// <code>
        /// return null;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected
        static ReturnStatementSyntax ReturnNull() => ReturnStatement
        (
            LiteralExpression(SyntaxKind.NullLiteralExpression)
        );

        /// <summary>
        /// <code>
        /// args[0] = (System.Object)cb_a // ref
        /// args[2] = (TT)cb_c // out
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        IEnumerable<StatementSyntax> ReassignArgsArray(IMethodInfo method, ParameterSyntax argsArray, IReadOnlyList<LocalDeclarationStatementSyntax> locals)
        {
            int i = 0;
            foreach (IParameterInfo param in method.Parameters)
            {
                if (ByRefs.Any(x => x == param.Kind))
                {
                    yield return ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: ElementAccessExpression
                            (
                                IdentifierName(argsArray.Identifier)
                            )
                            .WithArgumentList
                            (
                                argumentList: BracketedArgumentList
                                (
                                    SingletonSeparatedList
                                    (
                                        Argument
                                        (
                                            i.AsLiteral()
                                        )
                                    )
                                )
                            ),
                            right: CastExpression
                            (
                                type: ResolveType<object>(),
                                ResolveIdentifierName(locals[i])
                            )
                        )
                    );
                }
                i++;
            }
        }

        /// <summary>
        /// <code>
        /// System.String _a;
        /// TT _b = (TT) args[1];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        IEnumerable<LocalDeclarationStatementSyntax> ResolveInvokeTargetLocals(ParameterSyntax argsArray, IMethodInfo method) => method.Parameters.Select
        (
            (p, i) => ResolveLocal
            (
                p.Type,
                $"_{p.Name}",
                p.Kind is ParameterKind.Out ? null : CastExpression
                (
                    type: ResolveType(p.Type),
                    expression: ElementAccessExpression
                    (
                        IdentifierName(argsArray.Identifier)
                    )
                    .WithArgumentList
                    (
                        argumentList: BracketedArgumentList
                        (
                            SingletonSeparatedList
                            (
                                Argument
                                (
                                    i.AsLiteral()
                                )
                            )
                        )
                    )
                )
            )
        );

        /// <summary>
        /// <code>
        /// static (object target, object[] args) =>   
        /// {                                             
        ///     System.Int32 cb_a = (System.Int32) args[0]; 
        ///     System.String cb_b;                   
        ///     TT cb_c = (TT) args[2];
        ///     [object? result = null;]
        ///     ...
        ///     return null|result;
        /// };
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method, Func<ParameterSyntax, ParameterSyntax, LocalDeclarationStatementSyntax?, IReadOnlyList<LocalDeclarationStatementSyntax>, StatementSyntax> invocationFactory)
        {
            ParameterSyntax
                target = Parameter
                (
                    identifier: Identifier(nameof(target))
                )
                .WithType
                (
                    ResolveType<object>()
                ),
                args = Parameter
                (
                    identifier: Identifier(nameof(args))
                )
                .WithType
                (
                    ResolveType<object[]>()
                );

            List<StatementSyntax> statements = new();

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = ResolveInvokeTargetLocals(args, method).ToList();
            statements.AddRange(locals);

            LocalDeclarationStatementSyntax? result = null;
            if (!method.ReturnValue.Type.IsVoid)
            {
                result = ResolveLocal<object>(nameof(result));
                statements.Add(result);
            }

            statements.Add(invocationFactory(target, args, result, locals));
            statements.AddRange(ReassignArgsArray(method, args, locals));
            statements.Add(result is null ? ReturnNull() : ReturnResult(null, result));

            ParenthesizedLambdaExpressionSyntax lambda = ParenthesizedLambdaExpression()
                .WithParameterList
                (
                    ParameterList
                    (
                        new ParameterSyntax[] { target, args }.ToSyntaxList()
                    )
                )
                .WithBody
                (
                    Block(statements)
                );

            return lambda;
        }
    }
}
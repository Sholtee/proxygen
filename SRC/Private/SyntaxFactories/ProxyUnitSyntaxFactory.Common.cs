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

    internal abstract partial class ProxyUnitSyntaxFactory
    {
        protected static readonly IReadOnlyCollection<ParameterKind> ByRefs = [ParameterKind.Ref, ParameterKind.Out];

        protected static string EnsureUnused(IEnumerable<IParameterInfo> parameters, string variable)
        {
            while (parameters.Any(param => param.Name == variable))
            {
                variable = $"_{variable}";
            }
            return variable;
        }

        protected static string EnsureUnused(IMethodInfo method, string variable) => EnsureUnused(method.Parameters, variable);

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
        protected ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType?.Flags.HasFlag(TypeInfoFlags.IsVoid) is true
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
        protected ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ResolveIdentifierName(result));

        /// <summary>
        /// <code>
        /// return null;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected static ReturnStatementSyntax ReturnNull() => ReturnStatement
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
        /// [static] ([object target,] object[] args) =>   
        /// {                                             
        ///     System.Int32 cb_a = (System.Int32) args[0]; 
        ///     System.String cb_b;                   
        ///     TT cb_c = (TT) args[2];
        ///     [object? result =]
        ///     ...
        ///     args[1] = (System.Object) cb_b;                                                                  
        ///     args[2] = (System.Object) cb_c;   
        ///     return null|result;
        /// };
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method, bool hasTarget /*TODO: REMOVE*/, Func<IReadOnlyList<ParameterSyntax>, IReadOnlyList<LocalDeclarationStatementSyntax>, ExpressionSyntax> invocationFactory)
        {
            ParameterSyntax args =
                Parameter
                (
                    identifier: Identifier(nameof(args))
                )
                .WithType
                (
                    ResolveType<object[]>()
                );

            List<ParameterSyntax> paramz = [];
            if (hasTarget) paramz.Add
            (
                Parameter
                (
                    identifier: Identifier("target")
                )
                .WithType
                (
                    ResolveType<object>()
                )
            );
            paramz.Add(args);
        
            List<StatementSyntax> body = [];

            List<LocalDeclarationStatementSyntax> locals = new(ResolveInvokeTargetLocals(args, method));
            body.AddRange(locals);

            LocalDeclarationStatementSyntax? result;
            if (!method.ReturnValue.Type.Flags.HasFlag(TypeInfoFlags.IsVoid))
            {
                result = ResolveLocal<object>
                (
                    nameof(result),
                    invocationFactory(paramz, locals)
                );
                body.Add(result);
            }
            else
            {
                result = null;
                body.Add
                (
                    ExpressionStatement
                    (
                        invocationFactory(paramz, locals)
                    )
                );
            }

            body.AddRange
            (
                ReassignArgsArray(method, args, locals)
            );
            body.Add(result is null ? ReturnNull() : ReturnResult(null, result));

            ParenthesizedLambdaExpressionSyntax lambda = ParenthesizedLambdaExpression()
                .WithParameterList
                (
                    ParameterList
                    (
                        paramz.ToSyntaxList()
                    )
                )
                .WithBody
                (
                    Block(body)
                );

            if (hasTarget && Context.LanguageVersion >= LanguageVersion.CSharp9)
                lambda = lambda.WithModifiers
                (
                    modifiers: TokenList
                    (
                        Token(SyntaxKind.StaticKeyword)
                    )
                );

            return lambda;
        }

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
        protected LocalDeclarationStatementSyntax ResolveArgumentsArray(IMethodInfo method)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            return ResolveLocal<object[]>
            (
                EnsureUnused(paramz, "args"),
                ResolveArray<object>
                (
                    paramz.Select<IParameterInfo, ExpressionSyntax>
                    (
                        param =>
                        {
                            if (param.Type.RefType is RefType.Ref)
                                //
                                // We cannot cast "ref struct"s to objects
                                //

                                throw new NotSupportedException(Resources.REF_VALUE);

                            return param.Kind switch
                            {
                                ParameterKind.Out => DefaultExpression
                                (
                                    ResolveType(param.Type)
                                ),
                                _ => IdentifierName(param.Name)
                            };
                        }
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// TResult IInterface.Foo&lt;TGeneric&gt;(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
        /// {                                                                                  
        ///   ...                                                                             
        ///   para2 = (T2) args[1];                                                             
        ///   para3 = (T3) args[2];                                                               
        ///   ...                                                                                  
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(IMethodInfo method, LocalDeclarationStatementSyntax argsArray)
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
                            left: IdentifierName(param.Name),
                            right: CastExpression
                            (
                                type: ResolveType(param.Type),
                                expression: ElementAccessExpression(ResolveIdentifierName(argsArray)).WithArgumentList
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
                }
                i++;
            }
        }

    }
}
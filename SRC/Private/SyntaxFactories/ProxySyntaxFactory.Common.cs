/********************************************************************************
* ProxySyntaxFactory.Common.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory
    {
        #if DEBUG
        internal
        #else
        private
        #endif
        static string EnsureUnused(string name, IEnumerable<IParameterInfo> parameters) 
        {
            while (parameters.Some(param => param.Name == name))
            {
                name = $"_{name}";
            }
            return name;
        }

        #if DEBUG
        internal
        #else
        private
        #endif
        static string EnsureUnused(string name, IMethodInfo method) => EnsureUnused(name, method.Parameters);

        //
        // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
        //

        #if DEBUG
        internal
        #else
        private
        #endif
        bool AlreadyImplemented(IMemberInfo member) => InterceptorType
            .Interfaces
            .Some(iface => iface.EqualsTo(member.DeclaringType));

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)  <br/>
        /// {                                                                                       <br/>
        ///   ...                                                                                   <br/>
        ///   object[] args = new object[]{para1, para2, default(T3), para4};                       <br/>
        ///   ...                                                                                   <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalDeclarationStatementSyntax ResolveArgumentsArray(IMethodInfo method)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            return ResolveLocal<object[]>
            (
                EnsureUnused("args", paramz),
                ResolveArray<object>
                (
                    paramz.ConvertAr
                    (
                        param => (ExpressionSyntax) (param.Kind switch
                        {
                            _ when param.Type.RefType is RefType.Ref =>
                                //
                                // "ref struct" cast-olhato oljektumma
                                //

                                throw new NotSupportedException(Resources.BYREF_NOT_SUPPORTED),
                            ParameterKind.Out => DefaultExpression
                            (
                                ResolveType(param.Type)
                            ),
                            _ => IdentifierName(param.Name)
                        })
                    )
                )
            );
        }

        /// <summary>
        /// return;          <br/>
        ///                  <br/>
        /// OR               <br/>
        ///                  <br/>
        /// return (T) ...;
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
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
        /// return;             <br/>
        ///                     <br/>
        /// OR                  <br/>
        ///                     <br/>
        /// return (T) result;
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ToIdentifierName(result));

        /// <summary>
        /// return null;
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        static ReturnStatementSyntax ReturnNull() => ReturnStatement
        (
            LiteralExpression(SyntaxKind.NullLiteralExpression)
        );

        /// <summary>
        /// System.String _a;    <br/>
        /// TT _b = (TT)args[1];
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalDeclarationStatementSyntax[] ResolveInvokeTargetLocals(ParameterSyntax argsArray, IMethodInfo method) => method.Parameters.ConvertAr
        (
            (p, i) => ResolveLocal
            (
                p.Type,
                $"_{p.Name}", // statikus metodusban kerulnek felhasznalasra -> nem kell EnsureUnused(), csak "target" es "args"-al akadhatna ossze
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
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i))
                                )
                            )
                        )
                    )
                )
            )
        );

        /// <summary>
        /// (object target, object[] args) =>                         <br/>
        /// {                                                         <br/>
        ///     System.Int32 cb_a = (System.Int32)args[0];            <br/>
        ///     System.String cb_b;                                   <br/>
        ///     TT cb_c = (TT)args[2];                                <br/>
        ///     ...                                                   <br/>
        /// };
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method, Action<ParameterSyntax, ParameterSyntax, IReadOnlyList<LocalDeclarationStatementSyntax>, List<StatementSyntax>> invocationFactory)
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

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = ResolveInvokeTargetLocals(args, method);
            statements.AddRange(locals);

            invocationFactory(target, args, locals, statements);

            return 
                ParenthesizedLambdaExpression()
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
        }

        /// <summary>
        /// private static readonly MethodContext FXxX = new MethodContext((object target, object[] args) => <br/>
        /// {                                                                                                <br/>
        ///     System.Int32 cb_a = (System.Int32)args[0];                                                   <br/>
        ///     System.String cb_b;                                                                          <br/>
        ///     TT cb_c = (TT)args[2];                                                                       <br/>
        ///     ...                                                                                          <br/>
        /// });
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        FieldDeclarationSyntax ResolveMethodContext(ParenthesizedLambdaExpressionSyntax lambda) => ResolveStaticGlobal<MethodContext>
        (
            $"F{lambda.GetMD5HashCode()}",
            ResolveObject<MethodContext>
            (
                Argument(lambda)
            )     
        );
    }
}
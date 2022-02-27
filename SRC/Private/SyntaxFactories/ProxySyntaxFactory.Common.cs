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
        LocalDeclarationStatementSyntax CreateArgumentsArray(IMethodInfo method)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            return DeclareLocal<object[]>
            (
                EnsureUnused("args", paramz), CreateArray<object>
                (
                    paramz.Convert
                    (
                        param => param.Kind is ParameterKind.Out
                            ? DefaultExpression(CreateType(param.Type))
                            : (ExpressionSyntax) IdentifierName(param.Name)
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
                        type: CreateType(returnType),
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
        /// System.String cb_a;    <br/>
        /// TT cb_b = (TT)args[1];
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalDeclarationStatementSyntax[] DeclareCallbackLocals(LocalDeclarationStatementSyntax argsArray, IEnumerable<IParameterInfo> paramz) => paramz.Convert
        (
            //
            // Az osszes parametert az "args" tombbol vesszuk mert lehet az Invoke() override-ja modositana vmelyik bemeno
            // erteket.
            //

            (p, i) => DeclareLocal
            (
                p.Type,
                EnsureUnused($"cb_{p.Name}", paramz),
                p.Kind is ParameterKind.Out ? null : CastExpression
                (
                    type: CreateType(p.Type),
                    expression: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
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
        /// () =>                                          <br/>
        /// {                                              <br/>
        ///     System.Int32 cb_a = (System.Int32)args[0]; <br/>
        ///     System.String cb_b;                        <br/>
        ///     TT cb_c = (TT)args[2];                     <br/>
        ///     ...                                        <br/>
        /// };
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LambdaExpressionSyntax DeclareCallback(LocalDeclarationStatementSyntax argsArray, IMethodInfo method, Action<IReadOnlyList<LocalDeclarationStatementSyntax>, List<StatementSyntax>> invocationFactory)
        {
            List<StatementSyntax> statements = new();

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = DeclareCallbackLocals(argsArray, method.Parameters);
            statements.AddRange(locals);

            invocationFactory(locals, statements);

            return ParenthesizedLambdaExpression
            (
                Block(statements)
            );
        }
    }
}
/********************************************************************************
* ProxySyntaxFactory.MethodInterceptorFactory.cs                                *
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
        #endif
        protected override IEnumerable<MethodDeclarationSyntax> ResolveMethods(object context)
        {
            foreach (IMethodInfo met in InterfaceType.Methods)
            {
                if (AlreadyImplemented(met) || met.IsSpecial)
                    continue;

                //
                // "ref" visszateres nem tamogatott.
                //

                if (met.ReturnValue.Kind >= ParameterKind.Ref)
                    throw new NotSupportedException(Resources.BYREF_NOT_SUPPORTED);

                yield return ResolveMethod(null!, met);
            }

            foreach (MethodDeclarationSyntax extra in base.ResolveMethods(context))
            {
                yield return extra;
            }
        }

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)                    <br/>
        /// {                                                                                                         <br/>
        ///     static object InvokeTarget(ITarget target, object[] args)                                             <br/>
        ///     {                                                                                                     <br/>
        ///         System.Int32 cb_a = (System.Int32) args[0];                                                       <br/>
        ///         System.String cb_b;                                                                               <br/>
        ///         TT cb_c = (TT) args[2];                                                                           <br/>
        ///         System.Object result;                                                                             <br/>
        ///         result = target.Foo[TT](cb_a, out cb_b, ref cb_c);                                                <br/>
        ///                                                                                                           <br/>
        ///         args[1] = (System.Object) cb_b;                                                                   <br/>
        ///         args[2] = (System.Object) cb_c;                                                                   <br/>
        ///         return result;                                                                                    <br/>
        ///     }                                                                                                     <br/>
        ///                                                                                                           <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                      <br/>
        ///                                                                                                           <br/>
        ///     System.Object result = Invoke(new InvocationContext(args, InvokeTarget));                             <br/>
        ///                                                                                                           <br/>
        ///     para2 = (T2) args[1];                                                                                 <br/>
        ///     para3 = (T3) args[2];                                                                                 <br/>
        ///                                                                                                           <br/>
        ///     return (TResult) result;                                                                              <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override MethodDeclarationSyntax ResolveMethod(object context, IMethodInfo method)
        {
            return ResolveMethod(method).WithBody
            (
                body: Block
                (
                    BuildBody()
                )
            );

            IEnumerable<StatementSyntax> BuildBody()
            {
                List<StatementSyntax> statements = new();

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(method);
                LocalFunctionStatementSyntax invokeTarget = ResolveInvokeTarget(method);

                statements.Add(invokeTarget);
                statements.Add(argsArray);

                InvocationExpressionSyntax invocation = InvokeMethod
                (
                    Invoke,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        ResolveObject<InvocationContext>
                        (
                            ToArgument(argsArray),
                            Argument
                            (
                                IdentifierName(invokeTarget.Identifier)
                            )
                        )
                    )
                );

                if (!method.ReturnValue.Type.IsVoid)
                {
                    LocalDeclarationStatementSyntax result = ResolveLocal<object>
                    (
                        EnsureUnused(nameof(result), method),
                        invocation
                    );

                    statements.Add(result);
                    statements.AddRange
                    (
                        AssignByRefParameters(method, argsArray)
                    );
                    statements.Add
                    (
                        ReturnResult(method.ReturnValue.Type, result)
                    );
                }
                else
                {
                    statements.Add
                    (
                        ExpressionStatement(invocation)
                    );
                    statements.AddRange
                    (
                        AssignByRefParameters(method, argsArray)
                    );
                }

                return statements;
            }
        }

        private static readonly IReadOnlyCollection<ParameterKind> ByRefs = new[] { ParameterKind.Ref, ParameterKind.Out };

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)   <br/>
        /// {                                                                                        <br/>
        ///   ...                                                                                    <br/>
        ///   para2 = (T2) args[1];                                                                  <br/>
        ///   para3 = (T3) args[2];                                                                  <br/>
        ///   ...                                                                                    <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(IMethodInfo method, LocalDeclarationStatementSyntax argsArray)
        {
            int i = 0;
            foreach (IParameterInfo param in method.Parameters)
            {
                if (ByRefs.Some(x => x == param.Kind))
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
                                expression: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList
                                        (
                                            Argument
                                            (
                                                LiteralExpression
                                                (
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(i)
                                                )
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

        /// <summary>
        /// args[0] = (System.Object)cb_a // ref <br/>
        /// args[2] = (TT)cb_c // out
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
                if (ByRefs.Some(x => x == param.Kind))
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
                                            LiteralExpression
                                            (
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(i)
                                            )
                                        )
                                    )
                                )
                            ),
                            right: CastExpression
                            (
                                type: ResolveType<object>(),
                                ToIdentifierName(locals[i])
                            )
                        )
                    );
                }
                i++;
            }
        }

        /// <summary>                                                   <br/>
        /// static object InvokeTarget(ITarget target, object[] args)   <br/>
        /// {                                                           <br/>
        ///    System.Int32 cb_a = (System.Int32) args[0];              <br/>
        ///    System.String cb_b;                                      <br/>
        ///    TT cb_c = (TT) args[2];                                  <br/>
        ///    System.Object result;                                    <br/>
        ///    result = target.Foo[TT](cb_a, out cb_b, ref cb_c);       <br/>
        ///                                                             <br/>
        ///    args[1] = (System.Object) cb_b;                          <br/>
        ///    args[2] = (System.Object) cb_c;                          <br/>
        ///    return result;                                           <br/>
        /// }                                                           <br/>
        /// </summary>   
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalFunctionStatementSyntax ResolveInvokeTarget(IMethodInfo method) => ResolveInvokeTarget(method, (target, args, locals, body) =>
        {
            InvocationExpressionSyntax invocation = InvokeMethod
            (
                method,
                target: IdentifierName(target.Identifier),
                castTargetTo: method.DeclaringType,
                arguments: locals.ConvertAr(ToArgument)
            );

            IEnumerable<StatementSyntax> argsArrayReassignment = ReassignArgsArray(method, args, locals);

            if (method.ReturnValue.Type.IsVoid)
            {
                body.Add
                (
                    ExpressionStatement(invocation)
                );
                body.AddRange(argsArrayReassignment);
                body.Add
                (
                    ReturnNull()
                );
            }
            else
            {
                LocalDeclarationStatementSyntax result = ResolveLocal<object>
                (
                    EnsureUnused(nameof(result), method),
                    CastExpression
                    (
                        ResolveType<object>(),
                        invocation
                    )
                );
                body.Add(result);
                body.AddRange(argsArrayReassignment);
                body.Add
                (
                    ReturnResult(null, result)
                );
            } 
        });
    }
}
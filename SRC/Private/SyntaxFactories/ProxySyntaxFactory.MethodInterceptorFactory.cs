/********************************************************************************
* ProxySyntaxFactory.MethodInterceptorFactory.cs                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

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
                    throw new NotSupportedException(Resources.REF_RETURNS_NOT_SUPPORTED);

                yield return ResolveMethod(null!, met);
            }
        }

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)               <br/>
        /// {                                                                                                    <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                 <br/>
        ///                                                                                                      <br/>
        ///     Func[object] = invokeTarget () =>                                                                <br/>
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
        ///     System.Object result = Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Method));    <br/>
        ///                                                                                                      <br/>
        ///     para2 = (T2) args[1];                                                                            <br/>
        ///     para3 = (T3) args[2];                                                                            <br/>
        ///                                                                                                      <br/>
        ///     return (TResult) result;                                                                         <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override MethodDeclarationSyntax ResolveMethod(object context, IMethodInfo method)
        {
            return DeclareMethod(method).WithBody
            (
                body: Block
                (
                    BuildBody()
                )
            );

            IEnumerable<StatementSyntax> BuildBody()
            {
                List<StatementSyntax> statements = new();

                LocalDeclarationStatementSyntax
                    argsArray = CreateArgumentsArray(method),
                    invokeTarget = DeclareLocal<Func<object>>
                    (
                        EnsureUnused(nameof(invokeTarget), method),
                        BuildMethodInterceptorCallback(method, argsArray)
                    );

                statements.Add(argsArray);
                statements.Add(invokeTarget);

                InvocationExpressionSyntax invocation = InvokeMethod
                (
                    Invoke,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        CreateObject<InvocationContext>
                        (
                            ToArgument(argsArray),
                            ToArgument(invokeTarget),
                            Argument
                            (
                                EnumAccess(MemberTypes.Method)
                            )
                        )
                    )
                );

                if (!method.ReturnValue.Type.IsVoid)
                {
                    LocalDeclarationStatementSyntax result = DeclareLocal<object>
                    (
                        EnsureUnused(nameof(result), method),
                        invocation
                    );

                    statements.Add(result);
                    statements.AddRange
                    (
                        AssignByRefParameters(method.Parameters, argsArray)
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
                        AssignByRefParameters(method.Parameters, argsArray)
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
        IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(IReadOnlyList<IParameterInfo> paramz, LocalDeclarationStatementSyntax argsArray)
        {
            int i = 0;
            foreach (IParameterInfo param in paramz)
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
                                type: CreateType(param.Type),
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
        IEnumerable<StatementSyntax> ReassignArgsArray(IReadOnlyList<IParameterInfo> paramz, LocalDeclarationStatementSyntax argsArray, IReadOnlyList<LocalDeclarationStatementSyntax> locals)
        {
            int i = 0;
            foreach (IParameterInfo param in paramz)
            {
                if (ByRefs.Some(x => x == param.Kind))
                {
                    yield return ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
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
                                type: CreateType<object>(),
                                ToIdentifierName(locals[i])
                            )
                        )
                    );
                }
                i++;
            }
        }

        #if DEBUG
        internal
        #else
        private
        #endif
        LambdaExpressionSyntax BuildMethodInterceptorCallback(IMethodInfo method, LocalDeclarationStatementSyntax argsArray) => DeclareCallback(argsArray, method, (locals, body) =>
        {
            InvocationExpressionSyntax invocation = InvokeMethod
            (
                method,
                MemberAccess(null, Target),
                castTargetTo: null,
                arguments: locals.ConvertAr(ToArgument)
            );

            IEnumerable<StatementSyntax> argsArrayReassignment = ReassignArgsArray(method.Parameters, argsArray, locals);

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
                LocalDeclarationStatementSyntax cb_result = DeclareLocal<object> // ne siman "result" legyen a neve mert a callback-en kivul is lehet ilyen nevu valtozo
                (
                    EnsureUnused(nameof(cb_result), method),
                    CastExpression
                    (
                        CreateType<object>(),
                        invocation
                    )
                );
                body.Add(cb_result);
                body.AddRange(argsArrayReassignment);
                body.Add
                (
                    ReturnResult(null, cb_result)
                );
            }
        });
    }
}
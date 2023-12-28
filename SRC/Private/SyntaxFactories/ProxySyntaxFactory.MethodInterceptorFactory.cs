/********************************************************************************
* ProxySyntaxFactory.MethodInterceptorFactory.cs                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

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
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context)
        {
            foreach (IMethodInfo ifaceMethod in InterfaceType.Methods)
            {
                //
                // Starting from .NET Core 5.0 interface methods may have visibility
                //

                if (AlreadyImplemented(ifaceMethod, InterceptorType.Methods, SignatureEquals) || ifaceMethod.IsSpecial || ifaceMethod.AccessModifiers <= AccessModifiers.Protected)
                    continue;

                //
                // "ref return"s not supported
                //

                if (ifaceMethod.ReturnValue.Kind >= ParameterKind.Ref)
                    throw new NotSupportedException(Resources.BYREF_NOT_SUPPORTED);

                cls = ResolveMethod(cls, context, ifaceMethod);
            }

            return cls;

            static bool SignatureEquals(IMethodInfo targetMethod, IMethodInfo ifaceMethod) =>
                targetMethod.SignatureEquals(ifaceMethod);
        }

        /// <summary>
        /// <code>
        /// private static readonly MethodContext FXxX = new MethodContext((object target, object[] args) =>       
        /// {                                                                                                    
        ///     System.Int32 cb_a = (System.Int32) args[0];                                                       
        ///     System.String cb_b;                                                                               
        ///     TT cb_c = (TT) args[2];                                                                          
        ///     System.Object result;                                                                                
        ///     result = ((TInterface) target).Foo&lt;TT&gt;(cb_a, out cb_b, ref cb_c);                                  
        ///                                                                                                        
        ///     args[1] = (System.Object) cb_b;                                                                  
        ///     args[2] = (System.Object) cb_c;                                                                     
        ///     return result;                                                                                       
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                   
        ///                                                                                                          
        /// TResult IInterface.Foo&lt;TGeneric&gt;(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)            
        /// {                                                                                                     
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                   
        ///                                                                                                         
        ///     System.Object result = Invoke(new InvocationContext(args, FXxX));                                   
        ///                                                                                                       
        ///     para2 = (T2) args[1];                                                                            
        ///     para3 = (T3) args[2];                                                                               
        ///     return (TResult) result;                                                                         
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method)
        {
            //
            // For now, we only have call-index of 0
            //

            const int CALL_INDEX = 0;

            MemberDeclarationSyntax methodCtx = method is IGenericMethodInfo genericMethod
                ? ResolveMethodContext
                (
                    ResolveInvokeTarget(method),
                    CALL_INDEX,
                    genericMethod.GenericArguments,
                    genericMethod.GenericConstraints
                )
                : ResolveMethodContext
                (
                    ResolveInvokeTarget(method),
                    CALL_INDEX
                );

            return cls.AddMembers
            (
                new MemberDeclarationSyntax[]
                {
                    methodCtx,
                    ResolveMethod(method).WithBody
                    (
                        body: Block
                        (
                            BuildBody()
                        )
                    )
                }
            );

            IEnumerable<StatementSyntax> BuildBody()
            {
                List<StatementSyntax> statements = new();

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(method);
                statements.Add(argsArray);

                MemberAccessExpressionSyntax accessContext = StaticMemberAccess(cls, methodCtx);
                if (method is IGenericMethodInfo) accessContext = SimpleMemberAccess
                (
                    accessContext,
                    ResolveIdentifierName
                    (
                        (FieldDeclarationSyntax) ((ClassDeclarationSyntax) methodCtx).Members.Single()
                    )
                );

                InvocationExpressionSyntax invocation = InvokeMethod
                (
                    Invoke,
                    arguments: Argument
                    (
                        ResolveObject<InvocationContext>
                        (
                            ResolveArgument(argsArray),
                            Argument(accessContext)
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
        #else
        private
        #endif
        IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(IMethodInfo method, LocalDeclarationStatementSyntax argsArray)
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
        /// (ITarget target, object[] args) =>                         
        /// {                                                           
        ///    System.Int32 cb_a = (System.Int32) args[0];         
        ///    System.String cb_b;                                      
        ///    TT cb_c = (TT) args[2];                               
        ///    System.Object result;                                  
        ///    result = target.Foo&lt;TT&gt;(cb_a, out cb_b, ref cb_c);      
        ///                                                             
        ///    args[1] = (System.Object) cb_b;                         
        ///    args[2] = (System.Object) cb_c;                         
        ///    return result;                                          
        /// }   
        /// </code>
        /// </summary>   
        #if DEBUG
        internal
        #else
        private
        #endif
        ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method) => ResolveInvokeTarget(method, (target, args, locals, body) =>
        {
            InvocationExpressionSyntax invocation = InvokeMethod
            (
                method,
                target: IdentifierName(target.Identifier),
                castTargetTo: method.DeclaringType,
                arguments: locals.Select(ResolveArgument).ToArray()
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
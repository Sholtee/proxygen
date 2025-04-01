/********************************************************************************
* InterfaceProxySyntaxFactory.MethodInterceptorFactory.cs                       *
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

    internal partial class InterfaceProxySyntaxFactory
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
        /// private static readonly InterfaceInterceptionContext FXxX = new InterfaceInterceptionContext((object target, object[] args) =>       
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
            // Starting from .NET 5.0 interfaces may have visibility.
            //

            Visibility.Check(method, ContainingAssembly);

            //
            // For now, we only have call-index of 0
            //

            const int CALL_INDEX = 0;

            MemberDeclarationSyntax methodCtx = method is IGenericMethodInfo genericMethod
                ? ResolveMethodContext
                (
                    method.GetMD5HashCode(),
                    ResolveInvokeTarget(method),
                    CALL_INDEX,
                    genericMethod.GenericArguments,
                    genericMethod.GenericConstraints
                )
                : ResolveMethodContext
                (
                    method.GetMD5HashCode(),
                    ResolveInvokeTarget(method),
                    CALL_INDEX
                );

            return cls.AddMembers
            (
                methodCtx,
                ResolveMethod(method).WithBody
                (
                    body: Block
                    (
                        BuildBody()
                    )
                )
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
                        ResolveObject<InterfaceInvocationContext>
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

        /// <summary>    
        /// <code>
        /// args =>
        /// {
        ///     TGeneric1 cb_a = (TGeneric1) args[0];
        ///     T1 cb_b;                                                                               
        ///     T2 cb_c = (T2) args[2];
        ///     
        ///     [result =] target.Foo&lt;TT&gt;(cb_a, out cb_b, ref cb_c);
        ///     
        ///     args[1] = (System.Object) cb_b;                                                                  
        ///     args[2] = (System.Object) cb_c;   
        ///                 
        ///     return [result|null];
        /// }
        /// </code>
        /// </summary>   
        #if DEBUG
        internal
        #else
        private
        #endif
        ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method) => ResolveInvokeTarget
        (
            method,
            hasTarget: true,
            (paramz, locals) => InvokeMethod
            (
                method,
                target: IdentifierName(paramz[0].Identifier),
                castTargetTo: method.DeclaringType,
                arguments: locals.Select(ResolveArgument).ToArray()
            )
        ); 
    }
}
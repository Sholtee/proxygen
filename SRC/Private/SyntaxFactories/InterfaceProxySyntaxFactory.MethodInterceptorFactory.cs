/********************************************************************************
* InterfaceProxySyntaxFactory.MethodInterceptorFactory.cs                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context)
        {
            foreach (IMethodInfo ifaceMethod in TargetType!.Methods)
                cls = ResolveMethod(cls, context, ifaceMethod);

            return cls;
        }

        /// <summary>
        /// <code>
        /// private static ExtendedMemberInfo FXxX;
        /// TResult IInterface.Foo&lt;TGeneric&gt;(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
        /// {
        ///     CurrentMethod.GetImplementedInterfaceMethod(ref FXxX);
        ///     
        ///     object[] args = new object[] {para1, para2, default(T3), para4};
        ///     
        ///     System.Object result = FInterceptor.Invoke
        ///     (
        ///         new ClassInvocationContext
        ///         (
        ///             this,
        ///             FXxX,
        ///             args =>
        ///             {
        ///                 TGeneric1 cb_a = (T1) args[0];
        ///                 T2 cb_b = (T2) args[1];                                                                                 
        ///                 T3 cb_c;
        ///                 cb_d = (TGeneric) args[3]
        ///                 
        ///                 System.Object result;                                                                                
        ///                 result = this.Target.Foo&lt;TGeneric&gt;(cb_a, ref cb_b, out cb_c, cb_d);                                  
        ///                                                                                                        
        ///                 args[1] = (System.Object) cb_b;                                                                  
        ///                 args[2] = (System.Object) cb_c;   
        ///                 
        ///                 return result;    
        ///             },
        ///             args,
        ///             new Type[] {typeof(TGeneric)}
        ///         )
        ///     );
        ///     
        ///     para2 = (T1) args[1];                                                                            
        ///     para3 = (T2) args[2];                                                                               
        ///     return (TResult) result;   
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method)
        {
            FieldDeclarationSyntax memberInfo = ResolveField<ExtendedMemberInfo>
            (
                $"F{method.GetMD5HashCode()}",
                @readonly: false
            );

            return cls.AddMembers
            (
                memberInfo,
                ResolveMethod(method).WithBody
                (
                    Block
                    (
                        (StatementSyntax[])
                        [
                            ExpressionStatement
                            (
                                InvokeMethod
                                (
                                    FGetImplementedInterfaceMethod,
                                    arguments: Argument
                                    (
                                        StaticMemberAccess(cls, memberInfo)
                                    )
                                )
                            ),
                            ..ResolveInvokeInterceptor<InvocationContext>
                            (
                                method,
                                argsArray =>
                                [
                                    Argument
                                    (
                                        ThisExpression()
                                    ),
                                    Argument
                                    (
                                        StaticMemberAccess(cls, memberInfo)
                                    ),
                                    Argument
                                    (
                                        ResolveInvokeTarget
                                        (
                                            method,
                                            (_, locals) => InvokeMethod
                                            (
                                                method,
                                                target: GetTarget(),
                                                castTargetTo: null,
                                                arguments: locals.Select(ResolveArgument).ToArray()
                                            )
                                        )
                                    ),
                                    Argument
                                    (
                                        ResolveIdentifierName(argsArray)
                                    ),
                                    Argument
                                    (
                                        ResolveArray<Type>
                                        (
                                            (method as IGenericMethodInfo)?.GenericArguments.Select
                                            (
                                                static ga => TypeOfExpression
                                                (
                                                    IdentifierName(ga.Name)
                                                )
                                            ) ?? []
                                        )
                                    )
                                ]
                            )
                        ]
                    )
                )
            );
        }
    }
}
/********************************************************************************
* ClassProxySyntaxFactory.MethodInterceptorFactory.cs                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context)
        {
            foreach (IMethodInfo method in FBaseType.Methods)
            {
                if (method.IsSpecial || (!method.IsAbstract && !method.IsVirtual))
                    continue;

                cls = ResolveMethod(cls, context, method);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// private static ExtendedMemberInfo FXxX;
        /// public override TResult TGeneric1&gt;.Bar&lt;TGeneric2&gt;(TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4)
        /// {
        ///     CurrentMethod.GetBase(ref FXxX);
        ///     
        ///     object[] args = new object[] {para1, para2, default(T3), para4};
        ///     
        ///     System.Object result = FInterceptor.Invoke
        ///     (
        ///         new InvocationContext
        ///         (
        ///             this,
        ///             FXxX,
        ///             args =>
        ///             {
        ///                 TGeneric1 cb_a = (TGeneric1) args[0];
        ///                 T1 cb_b;                                                                               
        ///                 T2 cb_c = (T2) args[2];   
        ///                 
        ///                 System.Object result;                                                                                
        ///                 result = base.Bar&lt;TGeneric2&gt;(cb_a, out cb_b, ref cb_c);                                  
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
        protected override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo targetMethod)
        {
            //
            // Check if the method is visible.
            //

            if (!IsVisible(targetMethod))
                return cls;

            FieldDeclarationSyntax memberInfo = ResolveField<ExtendedMemberInfo>
            (
                $"F{targetMethod.GetMD5HashCode()}",
                @readonly: false
            );

            return cls.AddMembers
            (
                memberInfo,
                ResolveMethod(targetMethod, allowProtected: true).WithBody
                (
                    Block
                    (
                        (StatementSyntax[])
                        [
                            ExpressionStatement
                            (
                                InvokeMethod
                                (
                                    FGetBase,
                                    arguments: Argument
                                    (
                                        StaticMemberAccess(cls, memberInfo)
                                    )
                                )
                            ),
                            ..ResolveInvokeInterceptor<InvocationContext>
                            (
                                targetMethod,
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
                                        targetMethod.IsAbstract ? ResolveNotImplemented() : ResolveInvokeTarget
                                        (
                                            targetMethod,
                                            (_, locals) => InvokeMethod
                                            (
                                                targetMethod,
                                                target: BaseExpression(),
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
                                            (targetMethod as IGenericMethodInfo)?.GenericArguments.Select
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

/********************************************************************************
* DelegateProxySyntaxFactory.Invoke.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed partial class DelegateProxySyntaxFactory
    {
        /// <summary>
        /// <code>
        /// private TResult Invoke(T0 para1, ref T1 para2, out T2 para3)
        /// {
        ///     object[] args = new object[] {para1, para2, default(T3), para4};
        ///     
        ///     System.Object result = FInterceptor.Invoke
        ///     (
        ///         new ClassInvocationContext
        ///         (
        ///             new ExtendedMemberInfo(FTarget.Method),
        ///             args =>
        ///             {
        ///                 T0 cb_a = (T0) args[0];
        ///                 T1 cb_b;                                                                               
        ///                 T2 cb_c = (T2) args[2];   
        ///                 
        ///                 System.Object result;                                                                                
        ///                 result = FTarget.Invoke(cb_a, out cb_b, ref cb_c);                                  
        ///                                                                                                        
        ///                 args[1] = (System.Object) cb_b;                                                                  
        ///                 args[2] = (System.Object) cb_c;   
        ///                 
        ///                 return result;    
        ///             },
        ///             args,
        ///             new Type[] {}
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
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context) => base.ResolveMethods
        (
            cls.AddMembers
            (
                ResolveMethodCore(FInvokeDelegate)
                    .WithModifiers
                    (
                        TokenList
                        (
                            Token(SyntaxKind.PrivateKeyword)
                        )
                    )
                    .WithBody
                    (
                        Block
                        (
                            ResolveInvokeInterceptor<ClassInvocationContext>
                            (
                                FInvokeDelegate,
                                argsArray =>
                                [
                                    Argument
                                    (
                                        ResolveObject<ExtendedMemberInfo>
                                        (
                                            Argument
                                            (
                                                SimpleMemberAccess(GetTarget(), nameof(Action.Method))
                                            )
                                        )
                                    ),
                                    Argument
                                    (
                                        ResolveInvokeTarget
                                        (
                                            FInvokeDelegate,
                                            hasTarget: false,
                                            (_, locals) => InvokeMethod
                                            (
                                                FInvokeDelegate,
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
                                        ResolveArray<Type>([])
                                    )
                                ]
                            )
                        )
                    )
            ),
            context
        );
    }
}

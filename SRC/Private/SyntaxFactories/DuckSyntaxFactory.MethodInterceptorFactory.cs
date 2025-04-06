/********************************************************************************
* DuckSyntaxFactory.MethodInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
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

                if (ifaceMethod.IsSpecial || ifaceMethod.AccessModifiers <= AccessModifiers.Protected)
                    continue;

                IMethodInfo targetMethod = GetTargetMember
                (
                    ifaceMethod,
                    TargetType.Methods,
                    static (targetMethod, ifaceMethod) => targetMethod.SignatureEquals(ifaceMethod, ignoreVisibility: true)
                );

                cls = ResolveMethod(cls, ifaceMethod, targetMethod);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// ref TResult IFoo&lt;TGeneric1&gt;.Bar&lt;TGeneric2&gt;(TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo&lt;TGeneric2&gt;(para1, ref para2, out para3, para4);
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo targetMethod)
        {
            IMethodInfo ifaceMethod = (IMethodInfo) context;

            Visibility.Check(targetMethod, ContainingAssembly);
            Visibility.Check(ifaceMethod, ContainingAssembly);

            //
            // Explicit members cannot be accessed directly
            //

            ITypeInfo castTargetTo = targetMethod.AccessModifiers is AccessModifiers.Explicit
                    ? targetMethod.DeclaringInterfaces.Single()
                    : TargetType;

            ExpressionSyntax invocation = InvokeMethod
            (
                ifaceMethod,
                GetTarget(),
                castTargetTo,
                arguments: [..ifaceMethod.Parameters.Select(static para => para.Name)]
            );

            if (ifaceMethod.ReturnValue.Kind >= ParameterKind.Ref)
                invocation = RefExpression(invocation);

            return cls.AddMembers
            (
                ResolveMethod(ifaceMethod)
                    .WithExpressionBody
                    (
                        expressionBody: ArrowExpressionClause(invocation)
                    )
                    .WithSemicolonToken
                    (
                        Token(SyntaxKind.SemicolonToken)
                    )
            );
        }
    }
}

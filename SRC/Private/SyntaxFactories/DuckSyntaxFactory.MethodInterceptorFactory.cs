﻿/********************************************************************************
* DuckSyntaxFactory.MethodInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

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
        protected override IEnumerable<MethodDeclarationSyntax> ResolveMethods(object context)
        {
            foreach (IMethodInfo ifaceMethod in InterfaceType.Methods)
            {
                if (ifaceMethod.IsSpecial)
                    continue;

                IMethodInfo targetMethod = GetTargetMember(ifaceMethod, TargetType.Methods, SignatureEquals);

                //
                // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check(targetMethod, ContainingAssembly);

                yield return ResolveMethod(ifaceMethod, targetMethod);

            }

            foreach (MethodDeclarationSyntax extra in base.ResolveMethods(context))
            {
                yield return extra;
            }

            static bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember) =>
                targetMember is IMethodInfo targetMethod &&
                ifaceMember is IMethodInfo ifaceMethod &&
                targetMethod.SignatureEquals(ifaceMethod, ignoreVisibility: true);
        }

        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override MethodDeclarationSyntax ResolveMethod(object context, IMethodInfo targetMethod)
        {
            IMethodInfo ifaceMethod = (IMethodInfo) context;

            //
            // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface generikusok
            // maskepp vannak elnvezve.
            //

            ExpressionSyntax invocation = InvokeMethod
            (
                ifaceMethod,
                MemberAccess(null, Target),
                castTargetTo: targetMethod.AccessModifiers is AccessModifiers.Explicit
                    ? targetMethod.DeclaringInterfaces.Single() // explicit metodushoz biztosan csak egy deklaralo interface tartozik
                    : null,
                arguments: ifaceMethod
                    .Parameters
                    .ConvertAr(para => para.Name)
            );

            if (ifaceMethod.ReturnValue.Kind >= ParameterKind.Ref)
                invocation = RefExpression(invocation);

            return ResolveMethod(ifaceMethod, forceInlining: true)
                .WithExpressionBody
                (
                    expressionBody: ArrowExpressionClause(invocation)
                )
                .WithSemicolonToken
                (
                    Token(SyntaxKind.SemicolonToken)
                );
        }
    }
}

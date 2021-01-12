/********************************************************************************
* DuckSyntaxFactory.MethodInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        internal sealed class MethodInterceptorFactory : DuckMemberSyntaxFactory
        {
            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember) =>
                targetMember is IMethodInfo targetMethod && 
                ifaceMember is IMethodInfo ifaceMethod && 
                targetMethod.SignatureEquals(ifaceMethod, ignoreVisibility: true);

            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation)
            {
                foreach(IMethodInfo ifaceMethod in Context.InterfaceType.Methods.Where(m => !m.IsSpecial))
                {
                    cancellation.ThrowIfCancellationRequested();

                    IMethodInfo targetMethod = GetTargetMember(ifaceMethod, Context.TargetType.Methods);

                    //
                    // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check(targetMethod, Context.AssemblyName);

                    //
                    // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface generikusok
                    // maskepp vannak elnvezve.
                    //

                    ExpressionSyntax invocation = InvokeMethod
                    (
                        ifaceMethod,
                        MemberAccess(null, TARGET),
                        castTargetTo: targetMethod.AccessModifiers == AccessModifiers.Explicit
                            ? targetMethod.DeclaringInterfaces.Single() // explicit metodushoz biztosan csak egy deklaralo interface tartozik
                            : null,
                        arguments: ifaceMethod
                            .Parameters
                            .Select(para => para.Name)
                            .ToArray()
                    );

                    if (ifaceMethod.ReturnValue.Kind >= ParameterKind.Ref) 
                        invocation = RefExpression(invocation);

                    yield return DeclareMethod(ifaceMethod, forceInlining: true)
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

            public MethodInterceptorFactory(IDuckContext context) : base(context) { }
        }
    }
}

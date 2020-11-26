/********************************************************************************
* DuckSyntaxFactory.MethodInterceptorFactory.cs                                 *
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
    internal partial class DuckSyntaxFactory
    {
        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        internal sealed class MethodInterceptorFactory : DuckMemberSyntaxFactory
        {
            //
            // - int A.Foo([in|ref|out] int p) == int B.Foo([in|ref|out] int cica)
            // - TCica A.Bar<TCica>() == TKutya B.Bar<TKutya>()
            // - object A.Baz<TCica>(TCica cica, int x) == object B.Baz<TKutya>(TKutya kutya, int y)
            // - void A.FooBar<TCica, TMica>(TCica a, TCica b) != B.FooBar<TKutya, TMutya>(TKutya a, TMutya b)
            // - TCica A.BarBaz<TCica, TMica>() != TMutya B.BarBaz<TKutya, TMutya>()
            //

            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember) 
            {
                return
                    targetMember.Name == ifaceMember.Name &&
                    !targetMember.IsStatic &&
                    ExtractParameters((IMethodInfo) targetMember).SequenceEqual(ExtractParameters((IMethodInfo) ifaceMember));

                static IEnumerable<object> ExtractParameters(IMethodInfo method) 
                {
                    IReadOnlyDictionary<ITypeInfo, string> unifiedGenerics;

                    if (method is IGenericMethodInfo genericMethod) 
                        unifiedGenerics = genericMethod
                            .GenericArguments
                            .Select((type, i) => new
                            {
                                Type = type,
                                Name = $"T{i}"
                            })
                            .ToDictionary(x => x.Type, x => x.Name);
                    else
                        unifiedGenerics = new Dictionary<ITypeInfo, string>();

                    return new[] { method.ReturnValue }.Concat(method.Parameters).Select(param => new 
                    {
                        //
                        // List<T> es IList<T> eseten typeof(List<T>).GetGenericArguments[0] != typeof(IList<T>).GetGenericArguments[0] 
                        //
                        // "FullName" lehet NULL ha a parameter tipusa nyilt generikus. Ne a "Name" tulajdonsagot
                        // hasznaljuk h eltero nevu nyilt generikusokat is tudjunk vizsgalni.
                        //

                        Name = unifiedGenerics.TryGetValue(param.Type, out string name) 
                            ? name 
                            : param.Type.Name,

                        param.Kind

                        //
                        // Parameter neve nem erdekel bennunket (azonos tipussal es attributumokkal ket parametert
                        // azonosnak veszunk).
                        //
                    });
                }
            }

            protected override IEnumerable<MemberDeclarationSyntax> Build()
            {
                foreach(IMethodInfo ifaceMethod in Owner.InterfaceType.Methods.Where(m => !m.IsSpecial))
                {
                    IMethodInfo targetMethod = GetTargetMember(ifaceMethod, Owner.TargetType.Methods);

                    //
                    // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check(targetMethod, Owner.AssemblyName);

                    //
                    // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface generikusok
                    // maskepp vannak elnvezve.
                    //

                    ExpressionSyntax invocation = InvokeMethod
                    (
                        ifaceMethod,
                        MemberAccess(null, TARGET),
                        castTargetTo: targetMethod.AccessModifiers == AccessModifiers.Explicit
                            ? targetMethod.DeclaringType
                            : null,
                        arguments: ifaceMethod
                            .Parameters
                            .Select(para => para.Name)
                            .ToArray()
                    );

                    if (ifaceMethod.ReturnValue.Type.IsByRef) invocation = RefExpression(invocation);

                    yield return DeclareMethod(ifaceMethod, forceInlining: true)
                        .WithExpressionBody
                        (
                            expressionBody: ArrowExpressionClause(invocation)
                        )
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                }
            }

            public MethodInterceptorFactory(DuckSyntaxFactory owner) : base(owner) { }
        }
    }
}

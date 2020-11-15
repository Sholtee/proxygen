/********************************************************************************
* DuckSyntaxFactory.MethodInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget>
    {
        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        internal sealed class MethodInterceptorFactory : InterceptorFactoryBase
        {
            public MethodInterceptorFactory(DuckSyntaxFactory<TInterface, TTarget> owner) : base(owner) {}

            //
            // - int A.Foo([in|ref|out] int p) == int B.Foo([in|ref|out] int cica)
            // - TCica A.Bar<TCica>() == TKutya B.Bar<TKutya>()
            // - object A.Baz<TCica>(TCica cica, int x) == object B.Baz<TKutya>(TKutya kutya, int y)
            // - void A.FooBar<TCica, TMica>(TCica a, TCica b) != B.FooBar<TKutya, TMutya>(TKutya a, TMutya b)
            // - TCica A.BarBaz<TCica, TMica>() != TMutya B.BarBaz<TKutya, TMutya>()
            //

            protected override bool SignatureEquals(MemberInfo targetMember, MemberInfo ifaceMember) 
            {
                return
                    targetMember.StrippedName() == ifaceMember.StrippedName() &&
                    ExtractParameters((MethodInfo) targetMember).SequenceEqual(ExtractParameters((MethodInfo) ifaceMember));

                static IEnumerable<object> ExtractParameters(MethodInfo method) 
                {
                    IReadOnlyDictionary<Type, string> unifiedGenerics = method
                        .GetGenericArguments()
                        .Select((type, i) => new 
                        { 
                            Type = type, 
                            Name = $"T{i}" 
                        })
                        .ToDictionary(x => x.Type, x => x.Name);

                    return new[] { method.ReturnParameter }.Concat(method.GetParameters()).Select(param => new 
                    {
                        //
                        // List<T> es IList<T> eseten typeof(List<T>).GetGenericArguments[0] != typeof(IList<T>).GetGenericArguments[0] 
                        //
                        // "FullName" lehet NULL ha a parameter tipusa nyilt generikus. Ne a "Name" tulajdonsagot
                        // hasznaljuk h eltero nevu nyilt generikusokat is tudjunk vizsgalni.
                        //

                        Name = unifiedGenerics.TryGetValue(param.ParameterType, out string name) 
                            ? name 
                            : param.ParameterType.FullName,
                        param.Attributes // IN, OUT, stb

                        //
                        // Parameter neve nem erdekel bennunket (azonos tipussal es attributumokkal ket parametert
                        // azonosnak veszunk).
                        //
                    });
                }
            }

            public override MemberDeclarationSyntax Build(MemberInfo member)
            {
                MethodInfo
                    ifaceMethod = (MethodInfo) member,
                    targetMethod = GetTargetMember(ifaceMethod);

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
                    TARGET,
                    castTargetTo: targetMethod.GetAccessModifiers() == AccessModifiers.Explicit
                        ? targetMethod.GetDeclaringType()
                        : null,
                    arguments: ifaceMethod
                        .GetParameters()
                        .Select(para => para.Name)
                        .ToArray()
                );

                if (ifaceMethod.ReturnType.IsByRef) invocation = RefExpression(invocation);

                return DeclareMethod(ifaceMethod, forceInlining: true)
                    .WithExpressionBody
                    (
                        expressionBody: ArrowExpressionClause(invocation)
                    )
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            }

            public override bool IsCompatible(MemberInfo member) => member is MethodInfo method && method.DeclaringType.IsInterface && !method.IsSpecialName;
        }
    }
}

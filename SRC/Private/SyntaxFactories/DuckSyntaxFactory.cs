/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget> : ProxySyntaxFactoryBase
    {
        //
        // this.Target
        //

        private readonly MemberAccessExpressionSyntax TARGET;

        public DuckSyntaxFactory() =>
            TARGET = MemberAccess(null, MetadataPropertyInfo.CreateFrom((PropertyInfo) MemberInfoExtensions.ExtractFrom<DuckBase<TTarget>>(ii => ii.Target!)));

        protected override MemberDeclarationSyntax GenerateProxyClass(CancellationToken cancellation)
        {
            ITypeInfo 
                interfaceType = MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                @base = MetadataTypeInfo.CreateFrom(typeof(DuckBase<TTarget>));

            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(interfaceType is not IGenericTypeInfo genericIface || !genericIface.IsGenericDefinition);
            Debug.Assert(@base is not IGenericTypeInfo genericBase || !genericBase.IsGenericDefinition);

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: ProxyClassName
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Az osztaly ne publikus legyen h "internal" lathatosagu tipusokat is hasznalhassunk
                    //

                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    new[] { @base, interfaceType }.ToSyntaxList(t => (BaseTypeSyntax) SimpleBaseType
                    (
                        CreateType(t)
                    ))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>
            (
                @base.Constructors.Select(DeclareCtor)
            );

            members.AddRange(BuildMembers<MethodInterceptorFactory>(interfaceType.Methods, cancellation));
            members.AddRange(BuildMembers<PropertyInterceptorFactory>(interfaceType.Properties, cancellation));
            members.AddRange(BuildMembers<EventInterceptorFactory>(interfaceType.Events, cancellation));

            return cls.WithMembers
            (
                List(members)
            );
        }

        public override string AssemblyName => $"{GetSafeTypeName<TTarget>()}_{GetSafeTypeName<TInterface>()}_Duck";
    }
}

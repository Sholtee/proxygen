/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory: ClassSyntaxFactory
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo InterceptorType { get; }

        public ProxySyntaxFactory(ITypeInfo interfaceType, ITypeInfo interceptorType) 
        {
            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(interceptorType.Bases.Any(b => 
                b is IGenericTypeInfo generic && 
                generic.GenericDefinition.Equals(MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<>))) &&
                generic.GenericArguments.Single().Equals(interfaceType)));

            InterfaceType = interfaceType;
            InterceptorType = interceptorType;

            MemberSyntaxFactories = new IMemberSyntaxFactory[] 
            {
                new ConstructorFactory(this),
                new MethodInterceptorFactory(this),
                new PropertyInterceptorFactory(this),
                new EventInterceptorFactory(this)
            };
        }

        protected override MemberDeclarationSyntax GenerateClass(IEnumerable<MemberDeclarationSyntax> members)
        {
            ClassDeclarationSyntax cls = ClassDeclaration(Classes.Single())
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
                    new[] { InterceptorType, InterfaceType }.ToSyntaxList
                    (
                        t => (BaseTypeSyntax) SimpleBaseType
                        (
                            CreateType(t)
                        )
                    )
                )
            );

            return cls.WithMembers
            (
                List(members)
            );
        }
    }
}
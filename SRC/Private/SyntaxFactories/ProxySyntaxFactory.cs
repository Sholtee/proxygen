/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
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
    using Properties;

    internal partial class ProxySyntaxFactory: ClassSyntaxFactory
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo InterceptorType { get; }

        public ProxySyntaxFactory(ITypeInfo interfaceType, ITypeInfo interceptorType) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));

            //
            // Append() hivas azon percerz esetre ha nem szarmaztunk le az InterfaceInterceptor-bol
            //

            if (!interceptorType.Bases.Append(interceptorType).Any(
                b =>
                    b is IGenericTypeInfo genericBase &&
                    genericBase
                        .GenericDefinition
                        .Equals(MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<>))) &&
                    genericBase.GenericArguments.Single().Equals(interfaceType)))
                throw new ArgumentException(Resources.NOT_AN_INTERCEPTOR, nameof(interceptorType));

            if (interceptorType is IGenericTypeInfo genericInterceptor && genericInterceptor.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_GENERATOR, nameof(interceptorType));

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
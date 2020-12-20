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

    internal partial class ProxySyntaxFactory: ClassSyntaxFactory, IProxyContext
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo InterceptorType { get; }

        public ITypeInfo BaseInterceptorType { get; }  // ebbol kerdezzuk le az Invoke() stb tagokat, igy biztosan nem lesz utkozes

        public override IReadOnlyCollection<IMemberSyntaxFactory> MemberSyntaxFactories { get; }

        public override string ClassName { get; }

        public ProxySyntaxFactory(ITypeInfo interfaceType, ITypeInfo interceptorType, OutputType outputType): base(outputType) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));

            //
            // Append() hivas azon perverz esetre ha nem szarmaztunk le az InterfaceInterceptor-bol
            //

            BaseInterceptorType = (ITypeInfo) ((IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(InterfaceInterceptor<>))).Close(interfaceType);

            if (!interceptorType.Bases.Append(interceptorType).Any(BaseInterceptorType.Equals))
                throw new ArgumentException(Resources.NOT_AN_INTERCEPTOR, nameof(interceptorType));

            if (interceptorType is IGenericTypeInfo genericInterceptor && genericInterceptor.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_GENERATOR, nameof(interceptorType));

            InterfaceType = interfaceType;        
            InterceptorType = interceptorType;
            ClassName = $"GeneratedClass_{InterceptorType.GetMD5HashCode()}";

            MemberSyntaxFactories = new IMemberSyntaxFactory[] 
            {
                new ConstructorFactory(this),
                new InvokeFactory(this),
                new MethodInterceptorFactory(this),
                new PropertyInterceptorFactory(this),
                new EventInterceptorFactory(this)
            };
        }

        protected override MemberDeclarationSyntax GenerateClass(IEnumerable<MemberDeclarationSyntax> members)
        {
            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: ClassName
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
﻿/********************************************************************************
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

        public override IReadOnlyCollection<IMemberSyntaxFactory> MemberSyntaxFactories { get; }

        public override string ClassName { get; }

        private static readonly string BaseInterceptorName = typeof(InterfaceInterceptor<>).FullName;

        public ProxySyntaxFactory(ITypeInfo interfaceType, ITypeInfo interceptorType, OutputType outputType): base(outputType) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));

            //
            // - Append() hivas azon perverz esetre ha nem szarmaztunk le az InterfaceInterceptor-bol
            // - A "FullName" nem veszi figyelembe a generikus argumentumokat, ami nekunk pont jo
            //

            if (!interceptorType.Bases.Append(interceptorType).Any(ic => ic.FullName == BaseInterceptorName))
                throw new ArgumentException(Resources.NOT_AN_INTERCEPTOR, nameof(interceptorType));

            if (interceptorType is IGenericTypeInfo genericInterceptor && genericInterceptor.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_INTERCEPTOR, nameof(interceptorType));

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
            if (InterceptorType.IsFinal)
                throw new InvalidOperationException(Resources.SEALED_INTERCEPTOR);

            if (InterceptorType.IsAbstract)
                throw new InvalidOperationException(Resources.ABSTRACT_INTERCEPTOR);

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
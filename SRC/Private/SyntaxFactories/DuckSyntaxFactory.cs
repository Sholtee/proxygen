/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class DuckSyntaxFactory : ClassSyntaxFactory, IDuckContext
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo TargetType { get; }

        public ITypeInfo BaseType { get; }

        public string AssemblyName { get; }

        public override string ClassName { get; }

        public override IReadOnlyCollection<IMemberSyntaxFactory> MemberSyntaxFactories { get; }

        public DuckSyntaxFactory(ITypeInfo interfaceType, ITypeInfo targetType, string assemblyName, OutputType outputType): base(outputType) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            InterfaceType = interfaceType;
            TargetType = targetType;
            BaseType = (ITypeInfo) ((IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(targetType);
            AssemblyName = assemblyName;
            ClassName = $"GeneratedClass_{BaseType.GetMD5HashCode()}";

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
            ITypeInfo @base = (ITypeInfo) ((IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(TargetType);

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
                    new[] { @base, InterfaceType }.ToSyntaxList(t => (BaseTypeSyntax) SimpleBaseType
                    (
                        CreateType(t)
                    ))
                )
            );

            return cls.WithMembers
            (
                List(members)
            );
        }
    }
}

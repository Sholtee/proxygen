/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
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
    internal partial class DuckSyntaxFactory : ClassSyntaxFactory
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo TargetType { get; }

        public string AssemblyName { get; }

        public DuckSyntaxFactory(ITypeInfo interfaceType, ITypeInfo targetType, string assemblyName) 
        {
            Debug.Assert(interfaceType.IsInterface);

            InterfaceType = interfaceType;
            TargetType = targetType;
            AssemblyName = assemblyName;

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
            ITypeInfo @base = (ITypeInfo) ((IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(InterfaceType);

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: Classes.Single()
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

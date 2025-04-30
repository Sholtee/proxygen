/********************************************************************************
* ProxyUnitSyntaxFactoryBase.Constructor.cs                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxyUnitSyntaxFactoryBase
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
            ResolveConstructor
            (
                MetadataTypeInfo.CreateFrom(typeof(object))
                    .Constructors
                    .Single(),
                cls.Identifier
            )
        );

        #if DEBUG
        internal
        #endif
        protected ConstructorDeclarationSyntax AugmentConstructor<TParam>(ConstructorDeclarationSyntax ctor, string newParam, Func<IdentifierNameSyntax, StatementSyntax> bodyExtensionFactory)
        {
            ParameterSyntax param =
                Parameter
                (
                    Identifier
                    (
                        EnsureUnused(ctor, newParam)
                    )
                )
                .WithType
                (
                    ResolveType<TParam>()
                );

            return ctor
                .WithParameterList
                (
                    ParameterList
                    (
                        [param, .. ctor.ParameterList.Parameters]
                    )
                )
                .WithBody
                (
                    Block
                    (
                        SeparatedList
                        (
                            [
                                ..ctor.Body!.Statements,
                                bodyExtensionFactory(IdentifierName(param.Identifier))
                            ]
                        )
                    )
                );
        }
    }
}
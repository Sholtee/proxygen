/********************************************************************************
* SyntaxFactoryBase.Attribute.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class SyntaxFactoryBase
    {
        protected internal AttributeSyntax CreateAttribute<TAttribute>(ExpressionSyntax param) where TAttribute : Attribute => Attribute
        (
            (NameSyntax) CreateType(MetadataTypeInfo.CreateFrom(typeof(TAttribute)))
        )
        .WithArgumentList
        (
            argumentList: AttributeArgumentList
            (
                arguments: SingletonSeparatedList
                (
                    AttributeArgument(param)
                )
            )
        );
    }
}
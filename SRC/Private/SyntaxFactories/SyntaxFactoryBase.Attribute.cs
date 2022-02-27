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
        #if DEBUG
        internal
        #endif
        protected AttributeSyntax CreateAttribute<TAttribute>(params ExpressionSyntax[] paramz) where TAttribute : Attribute
        {
            AttributeSyntax attr = Attribute
            (
                (NameSyntax) CreateType(MetadataTypeInfo.CreateFrom(typeof(TAttribute)))
            );

            if (paramz.Length > 0) attr = attr.WithArgumentList
            (
                argumentList: AttributeArgumentList
                (
                    arguments: paramz.ToSyntaxList(AttributeArgument)
                )
            );

            return attr;
        }
    }
}
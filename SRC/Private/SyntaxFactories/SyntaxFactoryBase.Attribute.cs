/********************************************************************************
* SyntaxFactoryBase.Attribute.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class SyntaxFactoryBase
    {
        #if DEBUG
        internal
        #endif
        protected AttributeSyntax ResolveAttribute(Type attribute, params ExpressionSyntax[] paramz)
        {
            Debug.Assert(typeof(Attribute).IsAssignableFrom(attribute));

            AttributeSyntax attr = Attribute
            (
                (NameSyntax) ResolveType(MetadataTypeInfo.CreateFrom(attribute))
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

        #if DEBUG
        internal
        #endif
        protected AttributeSyntax ResolveAttribute<TAttribute>(params ExpressionSyntax[] paramz) where TAttribute : Attribute => ResolveAttribute(typeof(TAttribute), paramz);
    }
}
/********************************************************************************
* MemberSyntaxFactory.Attribute.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class MemberSyntaxFactory
    {
        private SyntaxList<AttributeListSyntax> DeclareMethodImplAttributeToForceInlining() => SingletonList
        (
            node: AttributeList
            (
                attributes: SingletonSeparatedList
                (
                    node: CreateAttribute<MethodImplAttribute>
                    (
                        SimpleMemberAccess
                        (
                            CreateType(MetadataTypeInfo.CreateFrom(typeof(MethodImplOptions))),
                            nameof(MethodImplOptions.AggressiveInlining)
                        )
                    )                 
                )
            )
        );
    }
}

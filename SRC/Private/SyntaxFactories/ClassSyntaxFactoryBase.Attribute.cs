/********************************************************************************
* ClassSyntaxFactoryBase.Attribute.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        private SyntaxList<AttributeListSyntax> ResolveMethodImplAttributeToForceInlining() => SingletonList
        (
            node: AttributeList
            (
                attributes: SingletonSeparatedList
                (
                    node: ResolveAttribute<MethodImplAttribute>
                    (
                        SimpleMemberAccess
                        (
                            ResolveType
                            (
                                MetadataTypeInfo.CreateFrom(typeof(MethodImplOptions))
                            ),
                            nameof(MethodImplOptions.AggressiveInlining)
                        )
                    )                 
                )
            )
        );
    }
}

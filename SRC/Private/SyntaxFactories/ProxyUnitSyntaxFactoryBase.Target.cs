/********************************************************************************
* ProxyUnitSyntaxFactoryBase.Target.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactoryBase
    {
        private const string TARGET_FIELD = "FTarget";

        protected static ExpressionSyntax GetTarget() => SimpleMemberAccess
        (
            ThisExpression(),
            TARGET_FIELD
        );

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) => base.ResolveMembers
        (
            TargetType is null ? cls : cls.AddMembers
            (
                ResolveField(TargetType, TARGET_FIELD, @static: false)
            ),
            context,
            cancellation
        );

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(IConstructorInfo ctor, SyntaxToken name)
        {
            ConstructorDeclarationSyntax resolved = base.ResolveConstructor(ctor, name);

            return TargetType is null ? resolved : AugmentConstructor<object>
            (
                resolved,
                "target",
                target => ExpressionStatement
                (
                    AssignmentExpression
                    (
                        SyntaxKind.SimpleAssignmentExpression,
                        left: SimpleMemberAccess(ThisExpression(), TARGET_FIELD),
                        right: CastExpression(ResolveType(TargetType), IdentifierName(target.Identifier))
                    )
                )
            );
        }
    }
}
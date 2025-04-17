/********************************************************************************
* ProxyUnitSyntaxFactoryBase.Target.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactoryBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IPropertyInfo FTarget = MetadataPropertyInfo.CreateFrom
        (
            PropertyInfoExtensions.ExtractFrom(static (ITargetAccess ta) => ta.Target!)
        );

        private const string TARGET_FIELD = nameof(FTarget);

        protected static MemberAccessExpressionSyntax GetTarget() => SimpleMemberAccess
        (
            ThisExpression(),
            TARGET_FIELD
        );

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases
        {
            get
            {
                List<ITypeInfo> result = [];
                if (TargetType is not null) result.Add
                (
                    MetadataTypeInfo.CreateFrom(typeof(ITargetAccess))
                );
                return result;
            }
        }

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) => base.ResolveMembers
        (
            TargetType is null ? cls : cls.AddMembers
            (
                ResolveField(TargetType, TARGET_FIELD, @static: false, @readonly: false)
            ),
            context,
            cancellation
        );

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => TargetType is null ? cls : cls.AddMembers
        (
            ResolveProperty
            (
                FTarget,
                getBody: ArrowExpressionClause
                (
                    SimpleMemberAccess(ThisExpression(), TARGET_FIELD)
                ),
                setBody: ArrowExpressionClause
                (
                    AssignmentExpression
                    (
                        SyntaxKind.SimpleAssignmentExpression,
                        left: SimpleMemberAccess(ThisExpression(), TARGET_FIELD),
                        right: CastExpression(ResolveType(TargetType), FValue)
                    )
                )
            )
        );
    }
}
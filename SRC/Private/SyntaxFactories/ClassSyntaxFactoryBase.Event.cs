/********************************************************************************
* ClassSyntaxFactoryBase.Event.cs                                               *
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
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// event TDelegate IInterface.EventName <br/>
        /// {                                    <br/>
        ///   add{...}                           <br/>
        ///   remove{...}                        <br/>
        /// }                                    <br/>
        /// </summary>
        protected internal EventDeclarationSyntax DeclareEvent(IEventInfo @event, CSharpSyntaxNode? addBody = null, CSharpSyntaxNode? removeBody = null, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            EventDeclarationSyntax result = EventDeclaration
            (
                type: CreateType(@event.Type),
                identifier: Identifier(@event.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax)CreateType(@event.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (@event.AddMethod != null && addBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.AddAccessorDeclaration, addBody, forceInlining));

            if (@event.RemoveMethod != null && removeBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// target.Event [+|-]= ...;
        /// </summary>
        protected internal AssignmentExpressionSyntax RegisterEvent(IEventInfo @event, ExpressionSyntax? target, bool add, ExpressionSyntax right, ITypeInfo? castTargetTo = null) => AssignmentExpression
        (
            kind: add ? SyntaxKind.AddAssignmentExpression : SyntaxKind.SubtractAssignmentExpression,
            left: MemberAccess
            (
                target,
                @event,
                castTargetTo
            ),
            right: right
        );
    }
}

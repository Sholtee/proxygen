/********************************************************************************
* ProxySyntaxFactoryBase.Event.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactoryBase
    {
        /// <summary>
        /// event TDelegate IInterface.EventName <br/>
        /// {                                    <br/>
        ///   add{...}                           <br/>
        ///   remove{...}                        <br/>
        /// }                                    <br/>
        /// </summary>
        protected internal virtual EventDeclarationSyntax DeclareEvent(EventInfo @event, CSharpSyntaxNode? addBody = null, CSharpSyntaxNode? removeBody = null, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            EventDeclarationSyntax result = EventDeclaration
            (
                type: CreateType(@event.EventHandlerType),
                identifier: Identifier(@event.StrippedName())
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
        protected internal AssignmentExpressionSyntax RegisterEvent(EventInfo @event, ExpressionSyntax? target, bool add, ExpressionSyntax right, Type? castTargetTo = null) => AssignmentExpression
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

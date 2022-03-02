/********************************************************************************
* ClassSyntaxFactoryBase.Event.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

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
        #if DEBUG
        internal
        #endif
        protected EventDeclarationSyntax DeclareEvent(IEventInfo @event, CSharpSyntaxNode? addBody, CSharpSyntaxNode? removeBody, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            EventDeclarationSyntax result = EventDeclaration
            (
                type: CreateType(@event.Type),
                identifier: Identifier(@event.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                (
                    (NameSyntax) CreateType(@event.DeclaringType)
                )
            );

            List<AccessorDeclarationSyntax> accessors = new(2);

            if (@event.AddMethod is not null && addBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.AddAccessorDeclaration, addBody, forceInlining));

            if (@event.RemoveMethod is not null && removeBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody, forceInlining));

            return !accessors.Some() ? result : result.WithAccessorList
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
        #if DEBUG
        internal
        #endif
        protected AssignmentExpressionSyntax RegisterEvent(IEventInfo @event, ExpressionSyntax? target, bool add, ExpressionSyntax right, ITypeInfo? castTargetTo = null) => AssignmentExpression
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

        #if DEBUG
        internal
        #endif
        protected abstract IEnumerable<EventDeclarationSyntax> ResolveEvents(object context);

        #if DEBUG
        internal
        #endif
        protected abstract EventDeclarationSyntax ResolveEvent(object context, IEventInfo evt);
    }
}

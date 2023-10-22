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
        /// <code>
        /// event TDelegate IInterface.EventName
        /// {                                    
        ///   add{...}                           
        ///   remove{...}                       
        /// }                                    
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected EventDeclarationSyntax ResolveEvent(IEventInfo @event, CSharpSyntaxNode? addBody, CSharpSyntaxNode? removeBody, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            EventDeclarationSyntax result = EventDeclaration
            (
                type: ResolveType(@event.Type),
                identifier: Identifier(@event.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                (
                    (NameSyntax) ResolveType(@event.DeclaringType)
                )
            );

            List<AccessorDeclarationSyntax> accessors = new(2);

            if (@event.AddMethod is not null && addBody is not null)
                accessors.Add
                (
                    ResolveAccessor(SyntaxKind.AddAccessorDeclaration, addBody, forceInlining)
                );

            if (@event.RemoveMethod is not null && removeBody is not null)
                accessors.Add
                (
                    ResolveAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody, forceInlining)
                );

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// <code>
        /// target.Event [+|-]= ...;
        /// </code>
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
        protected abstract ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context);

        #if DEBUG
        internal
        #endif
        protected abstract ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt);
    }
}

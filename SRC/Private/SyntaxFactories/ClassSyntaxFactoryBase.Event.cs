/********************************************************************************
* ClassSyntaxFactoryBase.Event.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
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
        ///   add {...}                           
        ///   remove {...}                       
        /// }                                    
        /// </code>
        /// or
        /// <code>
        /// public override event TDelegate EventName
        /// {                                    
        ///   add {...}                           
        ///   remove {...}                       
        /// }                                    
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected EventDeclarationSyntax ResolveEvent(IEventInfo @event, CSharpSyntaxNode addBody, CSharpSyntaxNode removeBody)
        {
            EventDeclarationSyntax result = EventDeclaration
            (
                type: ResolveType(@event.Type),
                identifier: Identifier(@event.Name)
            );

            if (@event.DeclaringType.IsInterface)
                result = result.WithExplicitInterfaceSpecifier
                (
                    explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                    (
                        (NameSyntax) ResolveType(@event.DeclaringType)
                    )
                );
            else
            {
                //
                // Events must have add and remove accessors defined
                //

                IMethodInfo backingMethod = @event.AddMethod!;

                List<SyntaxKind> tokens = [.. ResolveAccessModifiers(backingMethod)];

                tokens.Add(backingMethod.IsVirtual || backingMethod.IsAbstract ? SyntaxKind.OverrideKeyword : SyntaxKind.NewKeyword);

                result = result.WithModifiers
                (
                    modifiers: TokenList
                    (
                        tokens.Select(Token)
                    )
                );
            }

            return result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List
                    (
                        [
                            //
                            // Modifiers cannot be placed on event declarations
                            //

                            ResolveAccessor(SyntaxKind.AddAccessorDeclaration, addBody),
                            ResolveAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody)
                        ]
                    )
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
        protected virtual ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt) => cls;
    }
}

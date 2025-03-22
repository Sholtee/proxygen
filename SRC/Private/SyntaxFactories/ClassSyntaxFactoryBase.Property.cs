/********************************************************************************
* ClassSyntaxFactoryBase.Property.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
        /// target.Property                           
        /// // OR                                         
        /// target.Property[index]
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ExpressionSyntax PropertyAccess(IPropertyInfo property, ExpressionSyntax? target, ITypeInfo? castTargetTo = null) => PropertyAccess
        (
            property, 
            target, 
            castTargetTo, 
            property.Indices.Select(static param => Argument(IdentifierName(param.Name)))
        );

        /// <summary>
        /// <code>
        /// target.Property         
        /// // OR        
        /// target.Property[index]
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ExpressionSyntax PropertyAccess(IPropertyInfo property, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, IEnumerable<ArgumentSyntax>? indices = null) => !property.Indices.Any()
            ? MemberAccess
            (
                target,
                property,
                castTargetTo
            )
            : (ExpressionSyntax) ElementAccessExpression
            (
                AmendTarget(target, property, castTargetTo),
                BracketedArgumentList
                (
                    arguments: indices!.ToSyntaxList()
                )
            );

        /// <summary>
        /// <code>
        /// int IInterface[T].Prop
        /// {                      
        ///   get{...}            
        ///   set{...}           
        /// }                    
        /// </code>
        /// or
        /// <code>
        /// public override int Prop
        /// {                      
        ///   get{...}            
        ///   protected set{...}           
        /// }                    
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected PropertyDeclarationSyntax ResolveProperty(IPropertyInfo property, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody, bool forceInlining = false)
        {
            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: ResolveType(property.Type),
                identifier: Identifier(property.Name)
            );

            AccessModifiers declaredVisibility;

            if (property.DeclaringType.IsInterface)
            {
                declaredVisibility = AccessModifiers.Explicit;

                result = result.WithExplicitInterfaceSpecifier
                (
                    explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                    (
                        (NameSyntax) ResolveType(property.DeclaringType)
                    )
                );
            }
            else
            {
                declaredVisibility = (AccessModifiers) Math.Max
                (
                    (int) (property.GetMethod?.AccessModifiers ?? AccessModifiers.Unknown),
                    (int) (property.SetMethod?.AccessModifiers ?? AccessModifiers.Unknown)
                );

                List<SyntaxKind> tokens = AmToSyntax(declaredVisibility).ToList();

                IMemberInfo underlyingMethod = property.GetMethod ?? property.SetMethod!;

                tokens.Add(underlyingMethod.IsVirtual || underlyingMethod.IsAbstract ? SyntaxKind.OverrideKeyword : SyntaxKind.NewKeyword);

                result = result.WithModifiers
                (
                    TokenList
                    (
                        tokens.Select(Token)
                    )
                );
            }

            List<AccessorDeclarationSyntax> accessors = new(2);

            if (property.GetMethod is not null) accessors.Add
            (
                ResolveAccessor(property.GetMethod, getBody!, SyntaxKind.GetAccessorDeclaration)
            );

            if (property.SetMethod is not null) accessors.Add
            (
                ResolveAccessor(property.SetMethod, setBody!, SyntaxKind.SetAccessorDeclaration)
            );

            return result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );

            AccessorDeclarationSyntax ResolveAccessor(IMethodInfo backingMethod, CSharpSyntaxNode body, SyntaxKind kind)
            {
                Debug.Assert(backingMethod is not null, "Backing method cannot be null");

                IEnumerable<SyntaxKind> modifiers = backingMethod!.AccessModifiers < declaredVisibility
                    ? AmToSyntax(backingMethod.AccessModifiers)
                    : [];

                return this.ResolveAccessor(kind, body, forceInlining, modifiers);
            }

            static IEnumerable<SyntaxKind> AmToSyntax(AccessModifiers am) => am.SetFlags().Select
            (
                static am =>
                {
                    switch (am)
                    {
                        case AccessModifiers.Public: return SyntaxKind.PublicKeyword;
                        case AccessModifiers.Protected: return SyntaxKind.ProtectedKeyword;
                        case AccessModifiers.Internal: return SyntaxKind.InternalKeyword;
                        default:
                            Debug.Fail("Method not visible");
                            return SyntaxKind.None;
                    }
                }
            );
        }

        /// <summary>
        /// <code>
        /// int IInterface.this[string index, ...]
        /// {                                     
        ///   get{...}                            
        ///   set{...}                           
        /// }            
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected IndexerDeclarationSyntax ResolveIndexer(IPropertyInfo property, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);
            Debug.Assert(property.Indices.Any());

            IndexerDeclarationSyntax result = IndexerDeclaration
            (
                type: ResolveType(property.Type)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                (
                    (NameSyntax) ResolveType(property.DeclaringType)
                )
            )
            .WithParameterList
            (
                parameterList: BracketedParameterList
                (
                    parameters: property.Indices.ToSyntaxList
                    (
                        index => Parameter
                        (
                            identifier: Identifier(index.Name)
                        )
                        .WithType
                        (
                            type: ResolveType(index.Type)
                        )
                    )
                )
            );

            List<AccessorDeclarationSyntax> accessors = new(2);

            if (property.GetMethod is not null && getBody is not null)
                accessors.Add
                (
                    ResolveAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining)
                );

            if (property.SetMethod is not null && setBody is not null)
                accessors.Add
                (
                    ResolveAccessor(SyntaxKind.SetAccessorDeclaration, setBody, forceInlining)
                );

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property) => cls;
    }
}

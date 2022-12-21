/********************************************************************************
* ClassSyntaxFactoryBase.Property.cs                                            *
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
        /// <code>
        /// target.Property                           
        /// // OR                                         
        /// target.Propery[index]
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
            property.Indices.Convert(param => Argument(IdentifierName(param.Name)))
        );

        /// <summary>
        /// <code>
        /// target.Property         
        /// // OR        
        /// target.Propery[index]
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ExpressionSyntax PropertyAccess(IPropertyInfo property, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, IEnumerable<ArgumentSyntax>? indices = null) => !property.Indices.Some()
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
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected PropertyDeclarationSyntax ResolveProperty(IPropertyInfo property, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);

            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: ResolveType(property.Type),
                identifier: Identifier(property.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                (
                    (NameSyntax) ResolveType(property.DeclaringType)
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

            return !accessors.Some() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
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
            Debug.Assert(property.Indices.Some());

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

            return !accessors.Some() ? result : result.WithAccessorList
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
        protected abstract ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context);

        #if DEBUG
        internal
        #endif
        protected abstract ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property);
    }
}

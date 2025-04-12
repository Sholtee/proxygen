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

        private TDeclaration ResolveProperty<TDeclaration>(IPropertyInfo property, Func<IPropertyInfo, TDeclaration> fact, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody) where TDeclaration : BasePropertyDeclarationSyntax
        {
            TDeclaration result = fact(property);

            IMethodInfo backingMethodHavingHigherVisibility = (property.GetMethod?.AccessModifiers ?? AccessModifiers.Unknown) > (property.SetMethod?.AccessModifiers ?? AccessModifiers.Unknown)
                ? property.GetMethod!
                : property.SetMethod!;

            if (property.DeclaringType.IsInterface)
            {
                result = (TDeclaration) result.WithExplicitInterfaceSpecifier
                (
                    explicitInterfaceSpecifier: ExplicitInterfaceSpecifier
                    (
                        (NameSyntax) ResolveType(property.DeclaringType)
                    )
                );
            }
            else
            {
                List<SyntaxKind> tokens = [..ResolveAccessModifiers(backingMethodHavingHigherVisibility)];

                IMemberInfo underlyingMethod = property.GetMethod ?? property.SetMethod!;

                tokens.Add(underlyingMethod.IsVirtual || underlyingMethod.IsAbstract ? SyntaxKind.OverrideKeyword : SyntaxKind.NewKeyword);

                result = (TDeclaration) result.WithModifiers
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
                ResolveAccessor(property.GetMethod, getBody, SyntaxKind.GetAccessorDeclaration)
            );

            if (property.SetMethod is not null) accessors.Add
            (
                ResolveAccessor(property.SetMethod, setBody, SyntaxKind.SetAccessorDeclaration)
            );

            return (TDeclaration) result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );

            AccessorDeclarationSyntax ResolveAccessor(IMethodInfo backingMethod, CSharpSyntaxNode? body, SyntaxKind kind)
            {
                Debug.Assert(backingMethod is not null, "Backing method cannot be null");

                //
                // Accessor cannot have higher visibility than the property's
                //

                IEnumerable<SyntaxKind> modifiers = backingMethod!.AccessModifiers < backingMethodHavingHigherVisibility.AccessModifiers
                    ? ResolveAccessModifiers(backingMethod)
                    : [];

                return ClassSyntaxFactoryBase.ResolveAccessor(kind, body, modifiers);
            }
        }

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
        protected PropertyDeclarationSyntax ResolveProperty(IPropertyInfo property, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody) => ResolveProperty
        (
            property,
            property => PropertyDeclaration
            (
                type: ResolveType(property.Type),
                identifier: Identifier(property.Name)
            ),
            getBody,
            setBody
        );

        /// <summary>
        /// <code>
        /// int IInterface.this[string index, ...]
        /// {                                     
        ///   get{...}                            
        ///   set{...}                           
        /// }            
        /// </code>
        /// or
        /// <code>
        /// public override int this[string index, ...]
        /// {                                     
        ///   get{...}                            
        ///   protected set{...}                           
        /// }            
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected IndexerDeclarationSyntax ResolveIndexer(IPropertyInfo property, CSharpSyntaxNode? getBody, CSharpSyntaxNode? setBody) => ResolveProperty
        (
            property,
            property => 
                IndexerDeclaration
                (
                    type: ResolveType(property.Type)
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
                ),
            getBody,
            setBody
        );

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property) => throw new NotImplementedException();
    }
}

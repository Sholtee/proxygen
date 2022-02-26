/********************************************************************************
* ClassSyntaxFactoryBase.Property.cs                                            *
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
        /// target.Property           <br/>
        ///                           <br/>
        /// OR                        <br/>
        ///                           <br/>
        /// target.Propery[index]
        /// </summary>
        protected internal ExpressionSyntax PropertyAccess(IPropertyInfo property, ExpressionSyntax? target, ITypeInfo? castTargetTo = null) => PropertyAccess
        (
            property, 
            target, 
            castTargetTo, 
            property
                .Indices
                .Select(param => Argument(IdentifierName(param.Name)))
        );

        /// <summary>
        /// target.Property           <br/>
        ///                           <br/>
        /// OR                        <br/>
        ///                           <br/>
        /// target.Propery[index]
        /// </summary>
        protected internal ExpressionSyntax PropertyAccess(IPropertyInfo property, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, IEnumerable<ArgumentSyntax>? indices = null) => !property.Indices.Any()
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
        /// int IInterface[T].Prop <br/>
        /// {                      <br/>
        ///   get{...}             <br/>
        ///   set{...}             <br/>
        /// }                      <br/>
        /// </summary>
        protected internal PropertyDeclarationSyntax DeclareProperty(IPropertyInfo property, CSharpSyntaxNode? getBody = null, CSharpSyntaxNode? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);

            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: CreateType(property.Type),
                identifier: Identifier(property.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.GetMethod is not null && getBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.SetMethod is not null && setBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// int IInterface.this[string index, ...] <br/>
        /// {                                      <br/>
        ///   get{...}                             <br/>
        ///   set{...}                             <br/>
        /// }                                      <br/>
        /// </summary>
        protected internal IndexerDeclarationSyntax DeclareIndexer(IPropertyInfo property, CSharpSyntaxNode? getBody = null, CSharpSyntaxNode? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);
            Debug.Assert(property.Indices.Any());

            IndexerDeclarationSyntax result = IndexerDeclaration
            (
                type: CreateType(property.Type)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
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
                            type: CreateType(index.Type)
                        )
                    )
                )
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.GetMethod is not null && getBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.SetMethod is not null && setBody is not null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }
    }
}

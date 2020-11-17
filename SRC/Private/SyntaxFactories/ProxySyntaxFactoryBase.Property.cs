/********************************************************************************
* ProxySyntaxFactoryBase.Property.cs                                            *
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
        /// target.Property           <br/>
        ///                           <br/>
        /// OR                        <br/>
        ///                           <br/>
        /// target.Propery[index]
        /// </summary>
        protected internal ExpressionSyntax PropertyAccess(PropertyInfo property, ExpressionSyntax? target, Type? castTargetTo = null) => PropertyAccess(property, target, castTargetTo, property.GetIndexParameters().Select(param => Argument(IdentifierName(param.Name))));

        /// <summary>
        /// target.Property           <br/>
        ///                           <br/>
        /// OR                        <br/>
        ///                           <br/>
        /// target.Propery[index]
        /// </summary>
        protected internal ExpressionSyntax PropertyAccess(PropertyInfo property, ExpressionSyntax? target, Type? castTargetTo = null, IEnumerable<ArgumentSyntax>? indices = null) => !property.IsIndexer()
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
        protected internal virtual PropertyDeclarationSyntax DeclareProperty(PropertyInfo property, CSharpSyntaxNode? getBody = null, CSharpSyntaxNode? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);

            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: CreateType(property.PropertyType),
                identifier: Identifier(property.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax)CreateType(property.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.CanWrite && setBody != null)
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
        protected internal virtual IndexerDeclarationSyntax DeclareIndexer(PropertyInfo property, CSharpSyntaxNode? getBody = null, CSharpSyntaxNode? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);
            Debug.Assert(property.IsIndexer());

            ParameterInfo[] indices = property.GetIndexParameters();

            IndexerDeclarationSyntax result = IndexerDeclaration
            (
                type: CreateType(property.PropertyType)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax)CreateType(property.DeclaringType))
            )
            .WithParameterList
            (
                parameterList: BracketedParameterList
                (
                    parameters: indices.ToSyntaxList
                    (
                        index => Parameter
                        (
                            identifier: Identifier(index.Name)
                        )
                        .WithType
                        (
                            type: CreateType(index.ParameterType)
                        )
                    )
                )
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.CanWrite && setBody != null)
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

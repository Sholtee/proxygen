/********************************************************************************
* DuckSyntaxFactory.PropertyInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        /// <summary>
        /// System.Int32 IFoo[System.Int32].Prop         <br/>
        /// {                                            <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   get => Target.Prop;                        <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   set => Target.Prop = value;                <br/>
        /// }
        /// </summary>
        internal sealed class PropertyInterceptorFactory : DuckMemberSyntaxFactory
        {
            protected override IEnumerable<MemberDeclarationSyntax> Build()
            {
                foreach(IPropertyInfo ifaceProperty in Owner.InterfaceType.Properties)
                {
                    IPropertyInfo targetProperty = GetTargetMember(ifaceProperty, Owner.TargetType.Properties);

                    //
                    // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check(targetProperty, Owner.AssemblyName, checkGet: ifaceProperty.GetMethod != null, checkSet: ifaceProperty.SetMethod != null);

                    IMethodInfo accessor = targetProperty.GetMethod ?? targetProperty.SetMethod!;

                    //
                    // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
                    // maskepp vannak elnvezve a parameterei.
                    //

                    ExpressionSyntax propertyAccess = PropertyAccess
                    (
                        ifaceProperty,
                        MemberAccess(null, TARGET),
                        castTargetTo: accessor.AccessModifiers == AccessModifiers.Explicit
                            ? accessor.DeclaringType
                            : null
                    );

                    //
                    // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
                    // figyelmen kivul lesz hagyva.
                    //

                    ArrowExpressionClauseSyntax
                        getBody = ArrowExpressionClause
                        (
                            expression: propertyAccess
                        ),
                        setBody = ArrowExpressionClause
                        (
                            expression: AssignmentExpression
                            (
                                kind: SyntaxKind.SimpleAssignmentExpression,
                                left: propertyAccess,
                                right: IdentifierName(Value)
                            )
                        );

                    yield return ifaceProperty.Indices.Any()
                        ? DeclareIndexer
                        (
                            property: ifaceProperty,
                            getBody: getBody,
                            setBody: setBody,
                            forceInlining: true
                        )
                        : (MemberDeclarationSyntax) DeclareProperty
                        (
                            property: ifaceProperty,
                            getBody: getBody,
                            setBody: setBody,
                            forceInlining: true
                        );
                }
            }

            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
            {
                IPropertyInfo
                    targetProp = (IPropertyInfo) targetMember,
                    ifaceProp  = (IPropertyInfo) ifaceMember;

                return
                    targetProp.Type.Equals(ifaceProp.Type) &&

                    !targetProp.IsStatic &&

                    //
                    // Megengedjuk azt az esetet ha az interface pl csak irhato de a target engedelyezne
                    // az olvasast is.
                    //

                    (ifaceProp.SetMethod == null || targetProp.SetMethod != null) &&
                    (ifaceProp.GetMethod == null || targetProp.GetMethod != null) &&

                    //
                    // Indexer property-knel meg kell egyezniuk az index parameterek
                    // sorrendjenek es tipusanak.
                    //

                    targetProp.Indices.Select(p => p.Type)
                        .SequenceEqual
                        (
                            ifaceProp.Indices.Select(p => p.Type)
                        );
            }

            public PropertyInterceptorFactory(DuckSyntaxFactory owner) : base(owner) { }
        }
    }
}

/********************************************************************************
* DuckSyntaxFactory.PropertyInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget>
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
        internal sealed class PropertyInterceptorFactory : InterceptorFactoryBase
        {
            public PropertyInterceptorFactory(DuckSyntaxFactory<TInterface, TTarget> owner) : base(owner) { }

            public override MemberDeclarationSyntax Build(MemberInfo member)
            {
                PropertyInfo
                    ifaceProperty = (PropertyInfo) member,
                    targetProperty = GetTargetMember(ifaceProperty);

                //
                // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check(targetProperty, Owner.AssemblyName, checkGet: ifaceProperty.CanRead, checkSet: ifaceProperty.CanWrite);

                MethodInfo accessor = targetProperty.GetAccessors(nonPublic: true).First();

                //
                // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
                // maskepp vannak elnvezve a parameterei.
                //

                ExpressionSyntax propertyAccess = Owner.PropertyAccess
                (
                    ifaceProperty,
                    Owner.TARGET,
                    castTargetTo: accessor.GetAccessModifiers() == AccessModifiers.Explicit
                        ? accessor.GetDeclaringType()
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

                return ifaceProperty.IsIndexer()
                    ? Owner.DeclareIndexer
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody,
                        forceInlining: true
                    )
                    : (MemberDeclarationSyntax) Owner.DeclareProperty
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody,
                        forceInlining: true
                    );
            }

            public override bool IsCompatible(MemberInfo member) => member is PropertyInfo prop && prop.DeclaringType.IsInterface;

            protected override bool SignatureEquals(MemberInfo targetMember, MemberInfo ifaceMember)
            {
                PropertyInfo
                    targetProp = (PropertyInfo) targetMember,
                    ifaceProp = (PropertyInfo) ifaceMember;

                return
                    targetProp.PropertyType == ifaceProp.PropertyType &&

                    //
                    // Megengedjuk azt az esetet ha az interface pl csak irhato de a target engedelyezne
                    // az olvasast is.
                    //

                    (!ifaceProp.CanWrite || targetProp.CanWrite) &&
                    (!ifaceProp.CanRead || targetProp.CanRead) &&

                    //
                    // Indexer property-knel meg kell egyezniuk az index parameterek
                    // sorrendjenek es tipusanak.
                    //

                    targetProp
                        .GetIndexParameters()
                        .Select(p => p.ParameterType)
                        .SequenceEqual
                        (
                            ifaceProp.GetIndexParameters().Select(p => p.ParameterType)
                        );
            }
        }
    }
}

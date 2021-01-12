/********************************************************************************
* DuckSyntaxFactory.PropertyInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation)
            {
                foreach(IPropertyInfo ifaceProperty in Context.InterfaceType.Properties)
                {
                    cancellation.ThrowIfCancellationRequested();

                    IPropertyInfo targetProperty = GetTargetMember(ifaceProperty, Context.TargetType.Properties);

                    //
                    // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check
                    (
                        targetProperty, 
                        Context.AssemblyName, 
                        checkGet: ifaceProperty.GetMethod is not null, 
                        checkSet: ifaceProperty.SetMethod is not null
                    );

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
                            ? accessor.DeclaringInterfaces.Single() // explicit tulajdonsaghoz biztosan csak egy deklaralo interface tartozik
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
                if (targetMember is not IPropertyInfo targetProp || ifaceMember is not IPropertyInfo ifaceProp)
                    return false;

                //
                // Megengedjuk azt az esetet ha az interface pl csak irhato de a target engedelyezne
                // az olvasast is.
                //

                if (ifaceProp.GetMethod is not null) 
                {
                    if (targetProp.GetMethod is null || !targetProp.GetMethod.SignatureEquals(ifaceProp.GetMethod, ignoreVisibility: true))
                        return false;
                }

                if (ifaceProp.SetMethod is not null) 
                {
                    if (targetProp.SetMethod is null || !targetProp.SetMethod.SignatureEquals(ifaceProp.SetMethod, ignoreVisibility: true))
                        return false;
                }

                return true;
            }

            public PropertyInterceptorFactory(IDuckContext context) : base(context) { }
        }
    }
}

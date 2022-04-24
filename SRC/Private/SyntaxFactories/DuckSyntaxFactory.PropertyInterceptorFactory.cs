/********************************************************************************
* DuckSyntaxFactory.PropertyInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context)
        {
            foreach (IPropertyInfo ifaceProperty in InterfaceType.Properties)
            {
                IPropertyInfo targetProperty = GetTargetMember(ifaceProperty, TargetType.Properties, SignatureEquals);

                //
                // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check
                (
                    targetProperty,
                    ContainingAssembly,
                    checkGet: ifaceProperty.GetMethod is not null,
                    checkSet: ifaceProperty.SetMethod is not null
                );

                cls = ResolveProperty(cls, ifaceProperty, targetProperty);
            }

            return cls;

            [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "There is not dead code.")]
            static bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
            {
                if (targetMember is not IPropertyInfo targetProp || ifaceMember is not IPropertyInfo ifaceProp)
                    return false;

                //
                // Megengedjuk azt az esetet ha az interface pl csak irhato de a target engedelyezne
                // az olvasast is.
                //

                if (ifaceProp.GetMethod is not null)
                {
                    if (targetProp.GetMethod?.SignatureEquals(ifaceProp.GetMethod, ignoreVisibility: true) is not true)
                        return false;
                }

                if (ifaceProp.SetMethod is not null)
                {
                    if (targetProp.SetMethod?.SignatureEquals(ifaceProp.SetMethod, ignoreVisibility: true) is not true)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// System.Int32 IFoo[System.Int32].Prop         <br/>
        /// {                                            <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   get => Target.Prop;                        <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   set => Target.Prop = value;                <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo targetProperty)
        {
            IPropertyInfo ifaceProperty = (IPropertyInfo) context;

            IMethodInfo accessor = targetProperty.GetMethod ?? targetProperty.SetMethod!;

            //
            // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
            // maskepp vannak elnvezve a parameterei.
            //

            ExpressionSyntax propertyAccess = PropertyAccess
            (
                ifaceProperty,
                MemberAccess(null, Target),
                castTargetTo: accessor.AccessModifiers is AccessModifiers.Explicit
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

            return cls.AddMembers
            (
                ifaceProperty.Indices.Some()
                    ? ResolveIndexer
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody,
                        forceInlining: true
                    )
                    : ResolveProperty
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody,
                        forceInlining: true
                    )
            );
        }
    }
}

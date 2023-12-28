/********************************************************************************
* DuckSyntaxFactory.PropertyInterceptorFactory.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

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
                // Check if the property is visible
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

            static bool SignatureEquals(IPropertyInfo targetProp, IPropertyInfo ifaceProp)
            {
                //
                // We allow the implementation to declare a getter or setter that is not required by the interface.
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
        /// <code>
        /// System.Int32 IFoo&lt;System.Int32&gt;.Prop 
        /// {                                         
        ///   [MethodImplAttribute(AggressiveInlining)] 
        ///   get => Target.Prop;                     
        ///   [MethodImplAttribute(AggressiveInlining)] 
        ///   set => Target.Prop = value;              
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo targetProperty)
        {
            IPropertyInfo ifaceProperty = (IPropertyInfo) context;

            IMethodInfo accessor = targetProperty.GetMethod ?? targetProperty.SetMethod!;

            //
            // Invoke the interface property to make sure that all indexer parameter names will match.
            //

            ExpressionSyntax propertyAccess = PropertyAccess
            (
                ifaceProperty,
                MemberAccess(null, Target),
                castTargetTo: accessor.AccessModifiers is AccessModifiers.Explicit
                    ? accessor.DeclaringInterfaces.Single() // Explicit properties belong to exactly one interface definition
                    : null
            );

            //
            // Accessors not defined on the interface will be ignored.
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
                ifaceProperty.Indices.Any()
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

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
            foreach (IPropertyInfo ifaceProperty in FInterfaceType.Properties)
            {
                IPropertyInfo targetProperty = GetTargetMember(ifaceProperty, TargetType!.Properties, SignatureEquals);

                cls = ResolveProperty(cls, ifaceProperty, targetProperty);
            }

            return base.ResolveProperties(cls, context);

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
        ///   get => Target.Prop;                      
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

            Visibility.Check(targetProperty, ContainingAssembly);

            //
            // Starting from .NET 5.0 interfaces may have visibility.
            //

            Visibility.Check(ifaceProperty, ContainingAssembly);

            IMethodInfo accessor = targetProperty.GetMethod ?? targetProperty.SetMethod!;

            //
            // Explicit members cannot be accessed directly
            //

            ITypeInfo? castTargetTo = accessor.AccessModifiers is AccessModifiers.Explicit
                ? accessor.DeclaringInterfaces.Single()
                : null;

            ExpressionSyntax propertyAccess = PropertyAccess
            (
                ifaceProperty,
                GetTarget(),
                castTargetTo
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
                        right: FValue
                    )
                );

            return cls.AddMembers
            (
                ifaceProperty.Indices.Any()
                    ? ResolveIndexer
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody
                    )
                    : ResolveProperty
                    (
                        property: ifaceProperty,
                        getBody: getBody,
                        setBody: setBody
                    )
            );
        }
    }
}

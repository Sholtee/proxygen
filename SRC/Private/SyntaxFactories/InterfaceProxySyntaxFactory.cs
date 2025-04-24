/********************************************************************************
* InterfaceProxySyntaxFactory.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class InterfaceProxySyntaxFactory: ProxyUnitSyntaxFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IMethodInfo FGetImplementedInterfaceMethod = MetadataMethodInfo.CreateFrom
        (
            MethodInfoExtensions.ExtractFrom<ExtendedMemberInfo>(static output => CurrentMember.GetImplementedInterfaceMethod(ref output))
        );

        protected override ExpressionSyntax GetTarget() => ParenthesizedExpression
        (
            BinaryExpression
            (
                SyntaxKind.CoalesceExpression,
                base.GetTarget(),
                ThrowExpression
                (
                    ResolveObject<InvalidOperationException>()
                )
            )
        );

        public override string ExposedClass => $"Proxy_{TargetType!.GetMD5HashCode()}";

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases => [TargetType!, ..base.Bases];

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        public InterfaceProxySyntaxFactory(ITypeInfo interfaceType, SyntaxFactoryContext context) : base(interfaceType, context) 
        {
            if (!interfaceType.Flags.HasFlag(TypeInfoFlags.IsInterface))
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));
        }

        #if DEBUG
        internal
        #endif
        protected override CompilationUnitSyntax ResolveUnitCore(object context, CancellationToken cancellation)
        {
            Visibility.Check(TargetType!, ContainingAssembly);

            return base.ResolveUnitCore(context, cancellation);
        }
    }
}
/********************************************************************************
* ClassProxySyntaxFactory.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class ClassProxySyntaxFactory : ProxyUnitSyntaxFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IMethodInfo FGetBase = MetadataMethodInfo.CreateFrom
        (
            MethodInfoExtensions.ExtractFrom<ExtendedMemberInfo>(static output => CurrentMember.GetBase(ref output))
        );

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IReadOnlyCollection<ITypeInfo> FReservedTypes =
        [
            ..new Type[] {typeof(Array), typeof(Delegate), typeof(ValueType)}.Select(MetadataTypeInfo.CreateFrom)
        ];

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ITypeInfo FBaseType;

        /// <summary>
        /// <code>
        /// (object[] _) => throw new NotImplementedException();
        /// </code>
        /// </summary>
        private ParenthesizedLambdaExpressionSyntax ResolveNotImplemented() => ParenthesizedLambdaExpression()
            .WithParameterList
            (
                ParameterList
                (
                    new ParameterSyntax[]
                    {
                        Parameter
                        (
                            identifier: Identifier("_")
                        )
                        .WithType
                        (
                            ResolveType<object[]>()
                        )
                    }.ToSyntaxList()
                )
            )
            .WithExpressionBody
            (
                ThrowExpression
                (
                    ResolveObject<NotImplementedException>()
                )
            );

        private bool IsVisible(IMethodInfo method)
        {
            try
            {
                Visibility.Check(method, ContainingAssembly, allowProtected: true);
                return true;
            }
            
            //
            // Abstract internal members cannot be skipped
            //
            
            catch(MemberAccessException) when (!method.IsAbstract)
            {
                Trace.TraceWarning($"Method not visible: {method}");
                return false;
            }
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases => [FBaseType, ..base.Bases];

        public override string ExposedClass => $"ClsProxy_{FBaseType.GetMD5HashCode()}";

        #if DEBUG
        internal
        #endif
        protected override CompilationUnitSyntax ResolveUnitCore(object context, CancellationToken cancellation)
        {
            if (FBaseType.Flags.HasFlag(TypeInfoFlags.IsFinal))
                throw new InvalidOperationException(Resources.SEALED_TARGET);

            Visibility.Check(FBaseType, ContainingAssembly);

            return base.ResolveUnitCore(context, cancellation);
        }

        public ClassProxySyntaxFactory(ITypeInfo baseType, SyntaxFactoryContext context) : base(null, context)
        {
            if (!baseType.Flags.HasFlag(TypeInfoFlags.IsClass))
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(baseType));

            if (baseType is IGenericTypeInfo generic && generic.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_TARGET, nameof(baseType));

            if (FReservedTypes.Any(rt => rt.IsAccessibleFrom(baseType)))
                throw new ArgumentException(Resources.RESERVED_TYPE, nameof(baseType));

            FBaseType = baseType;
        }
    }
}
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class ClassProxySyntaxFactory : ProxyUnitSyntaxFactory
    {
        private static readonly IMethodInfo
            FGetBase = MetadataMethodInfo.CreateFrom
            (
                MethodInfoExtensions.ExtractFrom<ExtendedMemberInfo>(static output => CurrentMember.GetBase(ref output))
            ),
            FInvoke = MetadataMethodInfo.CreateFrom
            (
                MethodInfoExtensions.ExtractFrom<IInterceptor>(static i => i.Invoke(null!))
            );

        private static readonly IPropertyInfo
            FInterceptor = MetadataPropertyInfo.CreateFrom
            (
                PropertyInfoExtensions.ExtractFrom(static (IInterceptorAccess ia) => ia.Interceptor)
            );

        private static string ResolveClassName(ITypeInfo targetType) => $"ClsProxy_{targetType.GetMD5HashCode()}";

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

        private InvocationExpressionSyntax InvokeInterceptor(params IEnumerable<ArgumentSyntax> arguments) => InvokeMethod
        (
            method: FInvoke,
            target: MemberAccess
            (
                ThisExpression(),
                FInterceptor,
                castTargetTo: FInterceptor.DeclaringType
            ),
            castTargetTo: null,
            arguments: Argument
            (
                ResolveObject<ClassInvocationContext>
                (
                    arguments
                )
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

        public ITypeInfo TargetType { get; }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => [TargetType, MetadataTypeInfo.CreateFrom(typeof(IInterceptorAccess))];

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
        protected override string ResolveClassName(object context) => ResolveClassName(TargetType);

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            if (TargetType.IsFinal)
                throw new InvalidOperationException(Resources.SEALED_TARGET);

            Visibility.Check(TargetType, ContainingAssembly);

            return base.ResolveUnit(context, cancellation);
        }

        private static IReadOnlyCollection<ITypeInfo> ReservedTypes { get; } =
        [
            ..new Type[] {typeof(Array), typeof(Delegate), typeof(ValueType)}.Select(MetadataTypeInfo.CreateFrom)
        ]; 

        public ClassProxySyntaxFactory
        (
            ITypeInfo targetType,
            string? containingAssembly,
            OutputType outputType,
            ReferenceCollector? referenceCollector = null,
            LanguageVersion languageVersion = LanguageVersion.Latest
        )
        : base
        (
            outputType,
            containingAssembly ?? ResolveClassName(targetType),
            referenceCollector,
            languageVersion
        )
        {
            if (!targetType.IsClass)
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(targetType));

            if (targetType is IGenericTypeInfo generic && generic.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_TARGET, nameof(targetType));

            if (ReservedTypes.Any(rt => rt.IsAccessibleFrom(targetType)))
                throw new ArgumentException(Resources.RESERVED_TYPE, nameof(targetType));

            TargetType = targetType;
        }
    }
}
/********************************************************************************
* ClassProxySyntaxFactory.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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

        private static string ResolveClassName(ITypeInfo targetType) => $"ClsProxy_{targetType.GetMD5HashCode()}";

        private readonly FieldDeclarationSyntax FInterceptor;

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

        public ITypeInfo TargetType { get; }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => [TargetType];

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

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) =>
            base.ResolveMembers(cls.AddMembers(FInterceptor), context, cancellation);

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

            TargetType = targetType;

            FInterceptor = ResolveField<IInterceptor>(nameof(FInterceptor), @static: false);
        }
    }
}
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

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ClassProxySyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        private static string ResolveClassName(ITypeInfo targetType) => $"ClsProxy_{targetType.GetMD5HashCode()}";

        public ITypeInfo TargetType { get; }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => new[] { TargetType };

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
        }
    }
}
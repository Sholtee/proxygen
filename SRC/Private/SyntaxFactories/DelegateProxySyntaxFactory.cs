/********************************************************************************
* DelegateProxySyntaxFactory.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class DelegateProxySyntaxFactory : ProxyUnitSyntaxFactory
    {
        private readonly IMethodInfo FInvokeDelegate;

        private static string ResolveClassName(ITypeInfo targetType) => $"DelegateProxy_{targetType.GetMD5HashCode()}";

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => base.ResolveBases(context).Concat
        (
            [
                MetadataTypeInfo.CreateFrom(typeof(ITargetAccess))
            ]
        );

        #if DEBUG
        internal
        #endif
        protected override string ResolveClassName(object context) => ResolveClassName(TargetType!);

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        public DelegateProxySyntaxFactory
        (
            ITypeInfo targetType,
            string? containingAssembly,
            OutputType outputType,
            ReferenceCollector? referenceCollector = null,
            LanguageVersion languageVersion = LanguageVersion.Latest
        )
        : base
        (
            targetType,
            outputType,
            containingAssembly ?? ResolveClassName(targetType),
            referenceCollector,
            languageVersion
        )
        {
            if (!MetadataTypeInfo.CreateFrom(typeof(Delegate)).IsAccessibleFrom(targetType) || (FInvokeDelegate = targetType.Methods.SingleOrDefault(static m => m.Name == nameof(Action.Invoke))) is null)
                throw new ArgumentException(Resources.NOT_A_DELEGATE, nameof(targetType));

            if (targetType is IGenericTypeInfo generic && generic.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_TARGET, nameof(targetType));
        }
    }
}
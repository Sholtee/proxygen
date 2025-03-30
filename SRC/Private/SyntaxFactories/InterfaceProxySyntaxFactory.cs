/********************************************************************************
* InterfaceProxySyntaxFactory.cs                                                *
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

    internal sealed partial class InterfaceProxySyntaxFactory: ProxyUnitSyntaxFactory
    {
        private static string ResolveClassName(ITypeInfo interceptorType) => $"Proxy_{interceptorType.GetMD5HashCode()}";

        public ITypeInfo InterfaceType { get; }

        public ITypeInfo InterceptorType { get; }

        public ITypeInfo TargetType { get; }

        public IMethodInfo Invoke { get; }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context) => new[] { InterceptorType, InterfaceType };

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
        protected override string ResolveClassName(object context) => ResolveClassName(InterceptorType);

        public InterfaceProxySyntaxFactory
        (
            ITypeInfo interfaceType,
            ITypeInfo interceptorType,
            string? containingAssembly,
            OutputType outputType,
            ReferenceCollector? referenceCollector = null,
            LanguageVersion languageVersion = LanguageVersion.Latest
        )
        : base
        (
            outputType,
            containingAssembly ?? ResolveClassName(interceptorType),
            referenceCollector,
            languageVersion
        ) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));

            if (interceptorType is IGenericTypeInfo genericInterceptor && genericInterceptor.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_INTERCEPTOR, nameof(interceptorType));

            string baseInterceptorName = typeof(InterfaceInterceptor<,>).FullName;

            IGenericTypeInfo? baseInterceptor = (IGenericTypeInfo?)
            (
                interceptorType.QualifiedName == baseInterceptorName
                    ? interceptorType
                    : interceptorType.GetBaseTypes().SingleOrDefault(ic => ic.QualifiedName == baseInterceptorName)
            );

            bool validInterceptor =
                baseInterceptor is not null &&
                baseInterceptor.GenericArguments[0].Equals(interfaceType) &&
                interfaceType.IsAccessibleFrom(baseInterceptor.GenericArguments[1]);

            if (!validInterceptor)
                throw new ArgumentException(Resources.NOT_AN_INTERCEPTOR, nameof(interceptorType));

            InterfaceType = interfaceType;        
            InterceptorType = interceptorType;
            TargetType = baseInterceptor!.GenericArguments[1];

            IMethodInfo invoke = MetadataMethodInfo.CreateFrom
            (
                MethodInfoExtensions.ExtractFrom<InterfaceInterceptor<object>>(static ic => ic.Invoke(default!))
            );

            Invoke = InterceptorType.Methods.Single
            (
                met => met.SignatureEquals(invoke)
            )!;
        }

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            if (InterceptorType.IsFinal)
                throw new InvalidOperationException(Resources.SEALED_INTERCEPTOR);

            if (InterceptorType.IsAbstract)
                throw new InvalidOperationException(Resources.ABSTRACT_INTERCEPTOR);

            Visibility.Check(InterfaceType, ContainingAssembly);
            Visibility.Check(InterceptorType, ContainingAssembly);

            return base.ResolveUnit(context, cancellation);
        }
    }
}
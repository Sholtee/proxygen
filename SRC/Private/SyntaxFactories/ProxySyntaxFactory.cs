/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory: ProxyUnitSyntaxFactory
    {
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
        protected override string ResolveClassName(object context) => $"Proxy_{InterceptorType.GetMD5HashCode()}";

        public ProxySyntaxFactory(
            ITypeInfo interfaceType,
            ITypeInfo interceptorType,
            string? containingAssembly,
            OutputType outputType,
            ReferenceCollector? referenceCollector = null,
            LanguageVersion languageVersion = LanguageVersion.Latest): base(outputType, containingAssembly ?? $"Proxy_{interceptorType.GetMD5HashCode()}", referenceCollector, languageVersion) 
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
                    : interceptorType.GetBaseTypes().Single(ic => ic.QualifiedName == baseInterceptorName, throwOnEmpty: false)
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

            Invoke = InterceptorType.Methods.Single
            (
                static met => met.SignatureEquals
                (
                    MetadataMethodInfo.CreateFrom
                    (
                        (MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<object>>(ic => ic.Invoke(default!))
                    )
                )
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
/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory: ProxyUnitSyntaxFactory
    {
        public ITypeInfo InterfaceType { get; }

        public ITypeInfo InterceptorType { get; }

        public IPropertyInfo Target { get; }

        public IPropertyInfo Proxy { get; }

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
            ReferenceCollector? referenceCollector): base(outputType, containingAssembly ?? $"Proxy_{interceptorType.GetMD5HashCode()}", referenceCollector) 
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(interfaceType));

            if (interfaceType is IGenericTypeInfo genericIface && genericIface.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_IFACE, nameof(interfaceType));

            //
            // - Append() hivas azon perverz esetre ha nem szarmaztunk le az InterfaceInterceptor-bol
            // - A "FullName" nem veszi figyelembe a generikus argumentumokat, ami nekunk pont jo
            //

            string iiFullName = typeof(InterfaceInterceptor<>).FullName;
            if (interceptorType.QualifiedName != iiFullName && !interceptorType.GetBaseTypes().Some(ic => ic.QualifiedName == iiFullName))
                throw new ArgumentException(Resources.NOT_AN_INTERCEPTOR, nameof(interceptorType));

            if (interceptorType is IGenericTypeInfo genericInterceptor && genericInterceptor.IsGenericDefinition)
                throw new ArgumentException(Resources.GENERIC_INTERCEPTOR, nameof(interceptorType));

            InterfaceType = interfaceType;        
            InterceptorType = interceptorType;

            Proxy = InterceptorType.Properties.Single
            (
                prop => prop.Name == nameof(InterfaceInterceptor<object>.Proxy)
            )!;
            Target = InterceptorType.Properties.Single
            (
                prop => prop.Name == nameof(InterfaceInterceptor<object>.Target)
            )!;
            Invoke = InterceptorType.Methods.Single
            (
                met => met.SignatureEquals
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
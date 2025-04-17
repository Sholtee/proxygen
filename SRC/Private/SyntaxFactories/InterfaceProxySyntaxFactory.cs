/********************************************************************************
* InterfaceProxySyntaxFactory.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal sealed partial class InterfaceProxySyntaxFactory: ProxyUnitSyntaxFactory
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ITypeInfo
            FInterfaceType,
            FInterceptorType,
            FTargetType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IMethodInfo FInvoke;

        public override string ExposedClass => $"Proxy_{FInterceptorType.GetMD5HashCode()}";

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases => [FInterceptorType, FInterfaceType];

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        public InterfaceProxySyntaxFactory(ITypeInfo interfaceType, ITypeInfo interceptorType, SyntaxFactoryContext context) : base(null!, context) 
        {
            if (!interfaceType.Flags.HasFlag(TypeInfoFlags.IsInterface))
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

            FInterfaceType = interfaceType;        
            FInterceptorType = interceptorType;
            FTargetType = baseInterceptor!.GenericArguments[1];

            IMethodInfo invoke = MetadataMethodInfo.CreateFrom
            (
                MethodInfoExtensions.ExtractFrom<InterfaceInterceptor<object>>(static ic => ic.Invoke(default!))
            );

            FInvoke = FInterceptorType.Methods.Single
            (
                met => met.SignatureEquals(invoke)
            )!;
        }

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            if (FInterceptorType.Flags.HasFlag(TypeInfoFlags.IsFinal))
                throw new InvalidOperationException(Resources.SEALED_INTERCEPTOR);

            if (FInterceptorType.Flags.HasFlag(TypeInfoFlags.IsAbstract))
                throw new InvalidOperationException(Resources.ABSTRACT_INTERCEPTOR);

            Visibility.Check(FInterfaceType, ContainingAssembly);
            Visibility.Check(FInterceptorType, ContainingAssembly);

            return base.ResolveUnit(context, cancellation);
        }
    }
}
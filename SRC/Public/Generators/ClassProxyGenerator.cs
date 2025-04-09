/********************************************************************************
* ClassProxyGenerator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Tuple =
    #if NETSTANDARD2_1_OR_GREATER
    System.Runtime.CompilerServices.ITuple;
    #else
    object;
    #endif

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that intercept class method calls.
    /// </summary>
    public sealed class ClassProxyGenerator(Type @class) : Generator(id: GenerateId(nameof(ClassProxyGenerator), @class))
    {
        /// <summary>
        /// The target class
        /// </summary>
        public Type Class { get; } = @class ?? throw new ArgumentNullException(nameof(@class));

        /// <summary>
        /// Activates the proxy type.
        /// </summary>
        public async Task<object> ActivateAsync(IInterceptor interceptor, Tuple ctorParamz, CancellationToken cancellation = default)
        {
            IInterceptorAccess interceptorAccess = (IInterceptorAccess) await ActivateAsync(ctorParamz, cancellation);
            interceptorAccess.Interceptor = interceptor;
            return interceptorAccess;
        }

        /// <summary>
        /// Activates the underlying proxy type.
        /// </summary>
        public object Activate(IInterceptor interceptor, Tuple ctorParamz) => ActivateAsync(interceptor, ctorParamz, CancellationToken.None).GetAwaiter().GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(string? asmName, ReferenceCollector referenceCollector) => new ClassProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Class),
            asmName,
            OutputType.Module,
            referenceCollector: referenceCollector
        );
    }
}
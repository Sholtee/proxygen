/********************************************************************************
* EmbeddedTypeResolutionStrategy.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;

    internal sealed class EmbeddedTypeResolutionStrategy : ITypeResolution
    {
        public Type GeneratorType { get; }

        public EmbeddedTypeResolutionStrategy(Type generatorType) => GeneratorType = generatorType;

        public Type? TryResolve(CancellationToken cancellation)
        {
            StackTrace trace = new();

            Assembly inspectedAssembly = typeof(EmbeddedTypeResolutionStrategy).Assembly;

            for (int i = 1; i < trace.FrameCount; i++) 
            {
                cancellation.ThrowIfCancellationRequested();

                Type? containingType = trace
                    .GetFrame(i)
                    .GetMethod()
                    .DeclaringType;

                if (containingType is not null) // delegatumoknak nincs deklaralo tipusa
                {
                    Assembly callingAssembly = containingType.Assembly;

                    if (callingAssembly != inspectedAssembly)
                    {
                        if (callingAssembly.GetCustomAttributes<EmbedGeneratedTypeAttribute>().Any(egta => egta.Generator == GeneratorType))
                        {
                            return callingAssembly
                                .GetTypes()
                                .Single(t => t.GetCustomAttribute<RelatedGeneratorAttribute>(inherit: false)?.Generator == GeneratorType);
                        }
                    }

                    inspectedAssembly = callingAssembly;
                }
            }

            return null;
        }
    }
}

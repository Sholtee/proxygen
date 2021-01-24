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

        private readonly Type? FResolvedType;

        public EmbeddedTypeResolutionStrategy(Type generatorType)
        {
            StackTrace trace = new();

            Assembly inspectedAssembly = typeof(EmbeddedTypeResolutionStrategy).Assembly;

            for (int i = 1; i < trace.FrameCount; i++) 
            {
                Type? containingType = trace
                    .GetFrame(i)
                    .GetMethod()
                    .DeclaringType;

                if (containingType is not null) // delegatumoknak nincs deklaralo tipusa
                {
                    Assembly callingAssembly = containingType.Assembly;

                    if (callingAssembly != inspectedAssembly)
                    {
                        if (callingAssembly.GetCustomAttributes<EmbedGeneratedTypeAttribute>().Any(egta => egta.Generator == generatorType))
                        {
                            FResolvedType = callingAssembly
                                .GetTypes()
                                .Single(t => t.GetCustomAttribute<RelatedGeneratorAttribute>(inherit: false)?.Generator == generatorType);
                            break;
                        }
                    }

                    inspectedAssembly = callingAssembly;
                }
            }

            GeneratorType = generatorType;
        }

        public Type Resolve(CancellationToken cancellation) => FResolvedType!;

        public bool ShouldUse => FResolvedType is not null;

        public string ClassName => FResolvedType?.FullName!;

        public string ContainingAssembly => FResolvedType?.Assembly.GetName().Name!;
    }
}

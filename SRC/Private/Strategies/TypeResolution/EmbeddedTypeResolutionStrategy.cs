/********************************************************************************
* EmbeddedTypeResolutionStrategy.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class EmbeddedTypeResolutionStrategy : ITypeResolutionStrategy
    {
        private static readonly ConcurrentDictionary<Type, Type> FEmbeddedTypes = new ConcurrentDictionary<Type, Type>();

        //
        // Az osszes ProxyGen-t hivatkozo szerelvenyt megvizsgaljuk betolteskor (nyilvan 
        // mind ezutan a szerelveny utan toltodnek be).
        //

        [ModuleInitializer]
        public static void ModuleInit() => AppDomain.CurrentDomain.AssemblyLoad += (sender, args) => 
        {
            var embeddedTypes =
            (
                from embeddedType in args.LoadedAssembly.GetTypes()
                let rga = embeddedType.GetCustomAttribute<RelatedGeneratorAttribute>(inherit: false)
                where rga is not null
                select new 
                {
                    EmbeddedType = embeddedType,
                    RelatedGenerator = rga
                }
            );

            foreach (var t in embeddedTypes)
            {
                FEmbeddedTypes.TryAdd(t.RelatedGenerator.Generator, t.EmbeddedType);
            }
        };

        public Type GeneratorType { get; }

        public EmbeddedTypeResolutionStrategy(Type generatorType) => ShouldUse = FEmbeddedTypes.ContainsKey(GeneratorType = generatorType);

        public OutputType Type { get; } = OutputType.Unit;

        public Type Resolve(IUnitSyntaxFactory syntaxFactory, CancellationToken cancellation) => FEmbeddedTypes[GeneratorType];

        public bool ShouldUse { get; }

        public string AssemblyName => FEmbeddedTypes[GeneratorType].Assembly.GetName().Name;
    }
}

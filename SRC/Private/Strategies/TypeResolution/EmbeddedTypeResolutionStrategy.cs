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

        public EmbeddedTypeResolutionStrategy(Type generatorType) =>
            //
            // Ez jol kezeli azt az esetet ha az EmbedGeneratedTypeAttribute a szerelvenyen van de a
            // forras nem lett bovitve forditaskor (pl VB-ben irtuk a kodunkat).
            //

            ShouldUse = FEmbeddedTypes.ContainsKey(GeneratorType = generatorType);

        public Type Resolve(CancellationToken cancellation) => FEmbeddedTypes[GeneratorType];

        public bool ShouldUse { get; }

        public string ContainingAssembly => FEmbeddedTypes[GeneratorType].Assembly.GetName().Name;
    }
}

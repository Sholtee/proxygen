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
    using Abstractions;
    using Attributes;

    internal sealed class EmbeddedTypeResolutionStrategy : ITypeResolutionStrategy
    {
        private static readonly ConcurrentDictionary<Type, Assembly> FGenerators = new ConcurrentDictionary<Type, Assembly>();

        //
        // Az osszes ProxyGen-t hivatkozo szerelvenyt megvizsgaljuk betolteskor (nyilvan 
        // mind ezutan a szerelveny utan toltodnek be). Ez a modszer addig mukodik amig
        // a EmbedGeneratedTypeAttribute ebben a szerelvenyben talalhato.
        //

        [ModuleInitializer]
        public static void ModuleInit() => AppDomain.CurrentDomain.AssemblyLoad += (sender, args) => 
        {
            foreach (EmbedGeneratedTypeAttribute egta in args.LoadedAssembly.GetCustomAttributes<EmbedGeneratedTypeAttribute>())
            {
                FGenerators.TryAdd(egta.Generator, args.LoadedAssembly);
            }
        };

        public ITypeGenerator Generator { get; }

        public EmbeddedTypeResolutionStrategy(ITypeGenerator generator) => Generator = generator;

        public OutputType Type { get; } = OutputType.Unit;

        public Type Resolve(CancellationToken cancellation) => FGenerators[Generator.GetType()].GetType
        (
            Generator.SyntaxFactory.DefinedClasses.Single(), 
            throwOnError: true
        );

        public bool ShouldUse => FGenerators.ContainsKey(Generator.GetType());

        public string AssemblyName => FGenerators[Generator.GetType()]
            .GetName()
            .Name;
    }
}

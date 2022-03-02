/********************************************************************************
* LoadedTypeResolutionStrategy.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class LoadedTypeResolutionStrategy : ITypeResolution
    {
        public IUnitDefinition Unit { get; }

        public LoadedTypeResolutionStrategy(IUnitDefinition unit) => Unit = unit;

        public Type? TryResolve(CancellationToken cancellation) => GeneratedClass.Instances.TryGetValue(Unit.DefinedClasses.Single()!, out Type result)
            ? result
            : null;
    }
}

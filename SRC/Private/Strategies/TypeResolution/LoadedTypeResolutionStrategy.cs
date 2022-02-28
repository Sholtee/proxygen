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

        public Type? TryResolve(CancellationToken cancellation) =>
            //
            // TODO: FIXME: A verzionak nem kene bedrotozva lennie.
            //

            Type.GetType($"{Unit.DefinedClasses.Single()}, {Unit.ContainingAssembly}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", throwOnError: false);
    }
}

/********************************************************************************
* SupportsSourceGenerationAttribute.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class SupportsSourceGenerationAttributeBase : Attribute
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public IUnitFactory SourceFactory { get; protected set; }
        #pragma warning restore CS8618
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class SupportsSourceGenerationAttribute<TSourceFactory> : SupportsSourceGenerationAttributeBase where TSourceFactory : IUnitFactory, new()
    {
        public SupportsSourceGenerationAttribute() => SourceFactory = new TSourceFactory();
    }
}

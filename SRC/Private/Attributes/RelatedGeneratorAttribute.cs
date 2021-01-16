/********************************************************************************
* RelatedGeneratorAttribute.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public sealed class RelatedGeneratorAttribute: Attribute
    {
        public Type Generator { get; }

        public RelatedGeneratorAttribute(Type generator) => Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }
}

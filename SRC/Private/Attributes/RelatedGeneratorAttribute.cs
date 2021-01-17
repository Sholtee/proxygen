/********************************************************************************
* RelatedGeneratorAttribute.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;

    /// <summary>
    /// Binds the related <see cref="TypeGenerator{TDescendant}"/> descendant to the generated proxy <see cref="Type"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RelatedGeneratorAttribute: Attribute
    {
        /// <summary>
        /// The related <see cref="TypeGenerator{TDescendant}"/> descendant.
        /// </summary>
        public Type Generator { get; }

        /// <summary>
        /// Creates a new <see cref="RelatedGeneratorAttribute"/> instance.
        /// </summary>
        public RelatedGeneratorAttribute(Type generator) => Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }
}

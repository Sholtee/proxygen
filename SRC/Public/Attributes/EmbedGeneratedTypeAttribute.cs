/********************************************************************************
* EmbedGeneratedTypeAttribute.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.Proxy.Attributes
{
    using Internals;
    using Properties;

    /// <summary>
    /// Instructs the system to embed the generated type into the assembly being compiled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class EmbedGeneratedTypeAttribute: Attribute
    {
        /// <summary>
        /// The related <see cref="Generator{TInterface, TDescendant}"/>.
        /// </summary>
        public Type Generator { get; }

        /// <summary>
        /// Creates a new <see cref="EmbedGeneratedTypeAttribute"/> instance.
        /// </summary>
        /// <param name="generator"></param>
        public EmbedGeneratedTypeAttribute(Type generator) 
        {
            if (generator is null)
                throw new ArgumentNullException(nameof(generator));

            if (!generator.GetBaseTypes().Any(@base => @base.IsGenericType && @base.GetGenericTypeDefinition().FullName == typeof(Generator<,>).FullName))
                throw new ArgumentException(Resources.NOT_A_GENERATOR, nameof(generator));

            Generator = generator;
        }
    }
}

/********************************************************************************
* GeneratedClass.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents the base of all the generated classes.
    /// </summary>
    public abstract class GeneratedClass
    {
        /// <summary>
        /// Registers a generated class.
        /// </summary>
        internal protected static void RegisterInstance(Type instance) => TypeEmitter.RegisterInstance(instance ?? throw new ArgumentNullException(nameof(instance)));
    }
}

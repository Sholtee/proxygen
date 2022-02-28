/********************************************************************************
* GeneratedClass.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents the base of all the generated classes.
    /// </summary>
    public abstract class GeneratedClass
    {
        private static readonly ConcurrentDictionary<string, Type> FInstances = new();

        /// <summary>
        /// Registers a generated class.
        /// </summary>
        internal protected static void RegisterInstance(Type instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            FInstances[instance.FullName] = instance;
        }

        /// <summary>
        /// The loaded classes.
        /// </summary>
        public static IReadOnlyDictionary<string, Type> Instances => FInstances;
    }
}

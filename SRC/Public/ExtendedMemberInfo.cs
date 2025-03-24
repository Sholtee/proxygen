/********************************************************************************
* ExtendedMemberInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Member info containing the backing method
    /// </summary>
    public sealed class ExtendedMemberInfo
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<MethodInfo, MemberInfo>> FMethodMemberBindings = new();

        /// <summary>
        /// Creates a new <see cref="ExtendedMemberInfo"/> instance.
        /// </summary>
        public ExtendedMemberInfo(MethodInfo method)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));

            if (method.IsSpecialName)
            {
                IReadOnlyDictionary<MethodInfo, MemberInfo> bindings = FMethodMemberBindings.GetOrAdd(method.DeclaringType, static t =>
                {
                    BindingFlags flags = t.IsInterface
                        ? BindingFlags.Instance | BindingFlags.Public
                        : BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

                    Dictionary<MethodInfo, MemberInfo> bindings = new();

                    foreach (MemberInfo member in t.GetMembers(flags))
                    {
                        switch (member)
                        {
                            case PropertyInfo prop:
                                if (prop.GetMethod is not null)
                                    bindings.Add(prop.GetMethod, member);
                                if (prop.SetMethod is not null)
                                    bindings.Add(prop.SetMethod, member);
                                break;
                            case EventInfo evt:
                                //
                                // Events always have "add" and "remove" assigned
                                //

                                bindings.Add(evt.AddMethod, member);
                                bindings.Add(evt.RemoveMethod, member);
                                break;
                        }
                    }

                    return bindings;
                });

                Member = bindings[method];
            }
            else
                Member = method;
        }

        /// <summary>
        /// The backing method.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// The member that is backed by the <see cref="Method"/>
        /// </summary>
        public MemberInfo Member { get; }
    }
}

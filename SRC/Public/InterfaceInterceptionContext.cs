/********************************************************************************
* InterfaceInterceptionContext.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Internals;

    /// <summary>
    /// Describes a method context.
    /// </summary>
    public class InterfaceInterceptionContext 
    {
        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptionContext"/> instance.
        /// </summary>
        /// <remarks>Calling this constructor is time consuming operation. It is strongly advised to cache the created instances.</remarks>
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public InterfaceInterceptionContext(Func<object, object?[], object?> dispatch, int callIndex, IReadOnlyDictionary<MethodInfo, MethodInfo>? mappings)
        #pragma warning restore CS8618
        {
            Member = MemberInfoExtensions.ExtractFrom(dispatch ?? throw new ArgumentNullException(nameof(dispatch)), callIndex);
            Debug.Assert(Member.Method.DeclaringType.IsInterface, "Invocation should be done on interface member");

            Dispatch = dispatch;

            if (mappings is not null)
            {
                if (mappings.TryGetValue(Member.Method.IsGenericMethod ? Member.Method.GetGenericMethodDefinition() : Member.Method, out MethodInfo targetMethod))
                {
                    if (targetMethod.IsGenericMethod)
                        targetMethod = targetMethod.MakeGenericMethod(Member.Method.GetGenericArguments());

                    Target = new ExtendedMemberInfo(targetMethod, MemberInfoExtensions.ExtractFrom(targetMethod));
                    return;
                }
                Debug.Assert(false, $"Cannot get the target method for: {Member.Method}");
            }
            else
            {
                Target = Member;
            }
        }

        /// <summary>
        /// Creates a copy from the given source.
        /// </summary>
        protected InterfaceInterceptionContext(InterfaceInterceptionContext src)
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            Member  = src.Member;
            Target = src.Target;
            Dispatch      = src.Dispatch;
        }

        /// <summary>
        /// The member (property, event or method) that is being proxied.
        /// </summary>
        public ExtendedMemberInfo Member { get; }

        /// <summary>
        /// The concrete implementation if available, the interface member otherwise.
        /// </summary>
        public ExtendedMemberInfo Target { get; }

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object, object?[], object?> Dispatch { get; }
    }
}

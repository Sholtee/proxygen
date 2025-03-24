/********************************************************************************
* InterfaceInterceptionContext.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Internals;
    using Properties;

    /// <summary>
    /// Describes a method context.
    /// </summary>
    public class InterfaceInterceptionContext 
    {
        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptionContext"/> instance.
        /// </summary>
        /// <remarks>Calling this constructor is time consuming operation. It is strongly advised to cache the created instances.</remarks>
        public InterfaceInterceptionContext(Func<object, object?[], object?> dispatch, int callIndex, IReadOnlyDictionary<MethodInfo, MethodInfo>? mappings)
        {
            Member = MemberInfoExtensions.ExtractFrom(dispatch ?? throw new ArgumentNullException(nameof(dispatch)), callIndex);
            if (!Member.Method.DeclaringType.IsInterface)
                throw new InvalidOperationException(Resources.INVALID_DISPATCH_FN);

            Dispatch = dispatch;

            if (mappings is not null)
            {
                MethodInfo targetMethod = mappings[Member.Method.IsGenericMethod ? Member.Method.GetGenericMethodDefinition() : Member.Method];
                if (targetMethod.IsGenericMethod)
                    targetMethod = targetMethod.MakeGenericMethod(Member.Method.GetGenericArguments());

                Target = new ExtendedMemberInfo(targetMethod);
            }
            else
                Target = Member;
        }

        /// <summary>
        /// Creates a copy from the given source.
        /// </summary>
        protected InterfaceInterceptionContext(InterfaceInterceptionContext src)
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            Member = src.Member;
            Target = src.Target;
            Dispatch = src.Dispatch;
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

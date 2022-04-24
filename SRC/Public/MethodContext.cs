/********************************************************************************
* MethodContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Internals;

    /// <summary>
    /// Describes a method context.
    /// </summary>
    public class MethodContext 
    {
        /// <summary>
        /// Creates a new <see cref="MethodContext"/> instance.
        /// </summary>
        /// <remarks>Calling this constructor is time consuming operation. It is strongly advised to cache the created instances.</remarks>
        public MethodContext(Func<object, object?[], object?> dispatch)
        {
            if (dispatch is null)
                throw new ArgumentNullException(nameof(dispatch));

            ExtendedMemberInfo memberInfo = MemberInfoExtensions.ExtractFrom(dispatch);

            Member = memberInfo.Member;
            Method = memberInfo.Method;
            Dispatch = dispatch;
        }

        /// <summary>
        /// Creates a copy from the given source.
        /// </summary>
        protected MethodContext(MethodContext src)
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            Member = src.Member;
            Method = src.Method;
            Dispatch = src.Dispatch;
        }

        /// <summary>
        /// The concrete method behind the <see cref="Member"/>.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// The member  (property, event or method) that is being invoked.
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object, object?[], object?> Dispatch { get; }
    }
}

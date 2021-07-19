/********************************************************************************
* InvocationContext.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Internals;

    /// <summary>
    /// Describes the method invocation context.
    /// </summary>
    public class InvocationContext 
    {
        private readonly MethodInfo FMethod;

        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(object?[] args, Func<object?> invokeTarget, MemberTypes memberType)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            if (invokeTarget is null)
                throw new ArgumentNullException(nameof(invokeTarget));

            Member = MemberInfoExtensions.ExtractFrom(invokeTarget.Method, memberType, out FMethod);
            Args = args;
            InvokeTarget = invokeTarget;
        }

        /// <summary>
        /// The interface method being invoked.
        /// </summary>
        public MethodInfo Method => FMethod;

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }

        /// <summary>
        /// The concrete member that is being invoked (e.g.: property or event)
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Invokes the original method.
        /// </summary>
        public Func<object?> InvokeTarget { get; }
    }
}

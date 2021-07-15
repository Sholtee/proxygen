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
    /// <summary>
    /// Describes the method invocation context.
    /// </summary>
    public class InvocationContext 
    {
        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(MethodInfo method, object?[] args, MemberInfo member, Func<object?> invokeTarget)
        {
            Method       = method ?? throw new ArgumentNullException(nameof(method));
            Args         = args ?? throw new ArgumentNullException(nameof(args));
            Member       = member ?? throw new ArgumentNullException(nameof(member));
            InvokeTarget = invokeTarget ?? throw new ArgumentNullException(nameof(invokeTarget));
        }

        /// <summary>
        /// The interface method being invoked.
        /// </summary>
        public MethodInfo Method { get; }

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

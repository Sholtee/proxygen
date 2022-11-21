/********************************************************************************
* InvocationContext.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Describes a method invocation context.
    /// </summary>
    public class InvocationContext : MethodContext
    {
#if DEBUG
        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(object?[] args, Func<object, object?[], object?> dispatch): base(dispatch)
            => Args = args ?? throw new ArgumentNullException(nameof(args));
#endif
        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(object?[] args, MethodContext methodContext): base(methodContext)
            => Args = args ?? throw new ArgumentNullException(nameof(args));

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>Before the <see cref="InterfaceInterceptor{TInterface, TTarget}.Target"/> gets called you may use this property to inspect or modify parameters passed by the caller. After it you can read or amend the "by ref" parameters set by the target method.</remarks>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }
    }
}

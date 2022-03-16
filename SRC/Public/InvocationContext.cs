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
        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(object?[] args, Func<object?> invokeTarget, MemberTypes memberType)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            if (invokeTarget is null)
                throw new ArgumentNullException(nameof(invokeTarget));

            //
            // Delegate.Method fuggetlen a korulzart valtozoktol (lasd: UnderlyingMethodOfDelegate_ShouldBeIndependentFromTheEnclosedVariables)
            //

            (MemberInfo member, MethodInfo method) = MemberInfoExtensions.ExtractFrom(invokeTarget.Method, memberType);

            Member = member;
            Method = method;
            Args = args;
            InvokeTarget = invokeTarget;
        }

        /// <summary>
        /// The interface method being invoked.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>Before the <see cref="InvokeTarget"/> gets called you may use this property to inspect or modify parameters passed by the caller. After it you can read or amend the "by ref" parameters set by the target method.</remarks>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }

        /// <summary>
        /// The concrete member that is being invoked (e.g.: property or event)
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Invokes the original method.
        /// </summary>
        /// <remarks>In most of cases you are not supposed to call this function directly. It is done in the base implementation of <see cref="InterfaceInterceptor{TInterface}.Invoke(InvocationContext)"/> method.</remarks>
        public Func<object?> InvokeTarget { get; }
    }
}

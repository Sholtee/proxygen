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
    using Properties;

    /// <summary>
    /// Describes the method invocation context.
    /// </summary>
    public class InvocationContext 
    {
        /// <summary>
        /// Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        public InvocationContext(object?[] args, Func<object, object?[], object?> dispatch, MemberTypes memberType)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            if (dispatch is null)
                throw new ArgumentNullException(nameof(dispatch));

            if (dispatch.Target is not null)
                throw new ArgumentException(Resources.NOT_STATIC, nameof(dispatch));

            //
            // Delegate.Method fuggetlen a korulzart valtozoktol (lasd: UnderlyingMethodOfDelegate_ShouldBeIndependentFromTheEnclosedVariables)
            //

            ExtendedMemberInfo memberInfo = MemberInfoExtensions.ExtractFrom(dispatch, memberType);

            Member = memberInfo.Member;
            Method = memberInfo.Method;
            Args = args;
            Dispatch = dispatch;
        }

        /// <summary>
        /// The interface method being invoked.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>Before the <see cref="InterfaceInterceptor{TInterface}.Target"/> gets called you may use this property to inspect or modify parameters passed by the caller. After it you can read or amend the "by ref" parameters set by the target method.</remarks>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }

        /// <summary>
        /// The concrete member that is being invoked (e.g.: property or event)
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object, object?[], object?> Dispatch { get; }
    }
}

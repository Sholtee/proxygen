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
        /// [Obsolete] Creates a new <see cref="InvocationContext"/> instance.
        /// </summary>
        /// <remarks>This constructor is present only for backward compatibility and will throw.</remarks>
        [Obsolete("This constructor is present only for backward compatibility and will throw.")]
        public InvocationContext(object?[] args, Func<object?> invokeTarget, MemberTypes memberType) =>
            throw new NotSupportedException();

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
        /// <remarks>This constructor is present only for backward compatibility and will throw.</remarks>
        [Obsolete($"{nameof(InvokeTarget)} is obsolete and will throw. Use the {nameof(Dispatch)} instead!")]
        public Func<object?> InvokeTarget => throw new NotSupportedException();

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object, object?[], object?> Dispatch { get; }
    }
}

/********************************************************************************
* IInvocationContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Abstract invocation context
    /// </summary>
    public interface IInvocationContext
    {
        /// <summary>
        /// The proxy object on which the call was made.
        /// </summary>
        object Proxy { get; }

        /// <summary>
        /// The member (property, event or method) that is being proxied.
        /// </summary>
        ExtendedMemberInfo Member { get; }

        /// <summary>
        /// Generic arguments supplied by the caller.
        /// </summary>
        IReadOnlyList<Type> GenericArguments { get; }

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>Before the target gets called you may use this property to inspect or modify parameters passed by the caller. After it you can read or amend the "by ref" parameters set by the target method.</remarks>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        object?[] Args { get; }

        /// <summary>
        /// Dispatches the current call to the target.
        /// </summary>
        object? Dispatch();
    }
}

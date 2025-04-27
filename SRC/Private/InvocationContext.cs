/********************************************************************************
* InvocationContext.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// The default implementation of the <see cref="IInvocationContext"/> interface.
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="InvocationContext"/> instance.
    /// </remarks>
    public sealed class InvocationContext(object proxy, ExtendedMemberInfo targetMember, Func<object?[], object?> dispatch, object?[] args, IReadOnlyList<Type> genericArguments) : IInvocationContext  // this class is referenced by the generated proxies so it must be public
    {
        /// <inheritdoc/>
        public object Proxy { get; } = proxy ?? throw new ArgumentNullException(nameof(proxy));

        /// <inheritdoc/>
        public ExtendedMemberInfo Member { get; } = targetMember ?? throw new ArgumentNullException(nameof(targetMember));

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object?[], object?> Dispatch { get; } = dispatch ?? throw new ArgumentNullException(nameof(dispatch));

        /// <inheritdoc/>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; } = args ?? throw new ArgumentNullException(nameof(args));

        /// <inheritdoc/>
        public IReadOnlyList<Type> GenericArguments { get; } = genericArguments ?? throw new ArgumentNullException(nameof(genericArguments));

        object? IInvocationContext.Dispatch() => Dispatch(Args);
    }
}

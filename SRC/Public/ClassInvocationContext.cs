/********************************************************************************
* ClassInvocationContext.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Describes a method invocation context.
    /// </summary>
    /// <remarks>
    /// Creates a new <see cref="ClassInvocationContext"/> instance.
    /// </remarks>
    public sealed class ClassInvocationContext(ExtendedMemberInfo targetMember, Func<object?[], object?> dispatch, object?[] args, IReadOnlyList<Type> genericArguments) : IInvocationContext
    {
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

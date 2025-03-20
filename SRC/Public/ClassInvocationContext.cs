/********************************************************************************
* ClassInvocationContext.cs                                                 *
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
    public sealed class ClassInvocationContext: IInvocationContext
    {
        /// <summary>
        /// Creates a new <see cref="ClassInvocationContext"/> instance.
        /// </summary>
        public ClassInvocationContext(ExtendedMemberInfo targetMember, Func<object?[], object?> dispatch, object?[] args)
        {
            TargetMember = targetMember ?? throw new ArgumentNullException(nameof(targetMember));
            Dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            Args = args ?? throw new ArgumentNullException(nameof(args));
        }

        /// <inheritdoc/>
        public ExtendedMemberInfo TargetMember { get; }

        /// <summary>
        /// Gets the dispatcher function.
        /// </summary>
        public Func<object?[], object?> Dispatch { get; }

        /// <inheritdoc/>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }
    }
}

/********************************************************************************
* IInvocationContext.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Abstract invocation context
    /// </summary>
    public interface IInvocationContext
    {
        /// <summary>
        /// The member (property, event or method) that is being proxied.
        /// </summary>
        public ExtendedMemberInfo Member { get; }

        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>Before the target gets called you may use this property to inspect or modify parameters passed by the caller. After it you can read or amend the "by ref" parameters set by the target method.</remarks>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "End user is allowed to modify the argument list.")]
        public object?[] Args { get; }
    }
}

/********************************************************************************
* ISyntaxFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ISyntaxFactory
    {
        IReadOnlyCollection<ITypeInfo> Types { get; }
        IReadOnlyCollection<IAssemblyInfo> References { get; }
        bool Build(CancellationToken cancellation = default);
    }
}

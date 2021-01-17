/********************************************************************************
* ISyntaxFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ISyntaxFactory
    {
        IReadOnlyCollection<ITypeInfo> Types { get; }
        IReadOnlyCollection<IAssemblyInfo> References { get; }
        bool Build(CancellationToken cancellation = default);
    }
}

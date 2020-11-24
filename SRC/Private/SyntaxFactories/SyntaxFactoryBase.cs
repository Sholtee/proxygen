/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class SyntaxFactoryBase : ISyntaxFactory
    {
        public IReadOnlyCollection<ITypeInfo> Types => FTypes;

        public IReadOnlyCollection<IAssemblyInfo> References => FReferences;

        public virtual bool Build(CancellationToken cancellation) => throw new NotImplementedException();
    }
}
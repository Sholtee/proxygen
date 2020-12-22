/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class SyntaxFactoryBase : ReferenceCollector, ISyntaxFactory
    {
        public virtual bool Build(CancellationToken cancellation) => throw new NotImplementedException();
    }
}
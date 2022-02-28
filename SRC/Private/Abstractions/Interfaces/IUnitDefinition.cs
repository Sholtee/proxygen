/********************************************************************************
* IUnitDefinition.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IUnitDefinition
    {
        string ContainingAssembly { get; }

        IReadOnlyCollection<string> DefinedClasses { get; }
    }
}

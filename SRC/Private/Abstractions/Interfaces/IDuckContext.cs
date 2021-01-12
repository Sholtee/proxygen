/********************************************************************************
* IDuckContext.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IDuckContext
    {
        ITypeInfo InterfaceType { get; }

        ITypeInfo TargetType { get; }

        ITypeInfo BaseType { get; }

        string ClassName { get; }

        string AssemblyName { get; }
    }
}

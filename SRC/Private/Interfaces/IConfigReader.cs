/********************************************************************************
* IConfigReader.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IConfigReader
    {
        string? ReadValue(string name);

        string BasePath { get; }
    }
}

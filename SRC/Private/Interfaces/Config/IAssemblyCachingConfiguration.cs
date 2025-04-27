/********************************************************************************
* IAssemblyCachingConfiguration.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IAssemblyCachingConfiguration 
    {
        string? AssemblyCacheDir { get; }
    }
}

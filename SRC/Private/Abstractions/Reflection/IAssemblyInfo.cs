/********************************************************************************
* IAssemblyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IAssemblyInfo 
    {
        string? Location { get; }
        bool IsDynamic { get; }
        bool IsFriend(string asmName);
    }
}

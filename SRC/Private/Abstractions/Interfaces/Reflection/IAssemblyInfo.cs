/********************************************************************************
* IAssemblyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IAssemblyInfo
    {
        string? Location { get; }
        bool IsDynamic { get; }
        string Name { get; }
        bool IsFriend(string asmName);
    }
}

﻿/********************************************************************************
* IAssemblyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IAssemblyInfo 
    {
        string? Location { get; }
        bool IsDynamic { get; }
        AssemblyName Name { get; }
        bool IsFriend(string asmName);
    }
}

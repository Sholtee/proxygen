﻿/********************************************************************************
* ParameterKind.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal enum ParameterKind
    {
        None,
        Params,
        In,
        Out,
        Ref,
        RefReadonly
    }
}

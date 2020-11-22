﻿/********************************************************************************
* IInterceptorFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IInterceptorFactory
    {
        MemberDeclarationSyntax Build(IMemberInfo member);
        bool IsCompatible(IMemberInfo member);
    }
}

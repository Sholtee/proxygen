/********************************************************************************
* IInterceptorFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IInterceptorFactory
    {
        bool IsCompatible(MemberInfo member);
        MemberDeclarationSyntax Build(MemberInfo member);
    }
}

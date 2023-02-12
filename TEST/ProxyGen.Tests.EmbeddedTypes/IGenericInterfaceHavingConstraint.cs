/********************************************************************************
* IGenericInterfaceHavingConstraint.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using System;

namespace Solti.Utils.Proxy.Tests.EmbeddedTypes
{
    internal interface IGenericInterfaceHavingConstraint
    {
        void Foo<T>() where T : class, IDisposable;
        void Bar<T, TT>() where T : new() where TT : struct;
    }
}

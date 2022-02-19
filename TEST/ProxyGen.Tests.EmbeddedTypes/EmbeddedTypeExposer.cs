/********************************************************************************
* EmbeddedTypeExposer.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy.Tests.EmbeddedTypes
{
    public static class EmbeddedTypeExposer
    {
        //
        // A beagyzott tipushoz csak a befoglalo szerelvenybol ferhetunk hozza.
        //

        public static Type GetGeneratedTypeByGenerator<TGenerator>() where TGenerator: new() => (Type) typeof(TGenerator).InvokeMember
        (
            "GetGeneratedType", 
            BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, 
            null, 
            null, 
            new object[] { }
        );
    }
}

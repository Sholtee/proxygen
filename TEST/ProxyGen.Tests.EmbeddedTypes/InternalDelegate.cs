/********************************************************************************
* InternalDelegate.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Tests.EmbeddedTypes
{
    internal delegate int InternalDelegate<T>(string a, ref T[] b, out object c);
}

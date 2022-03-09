/********************************************************************************
* MetadataRuntimeContext.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataRuntimeContext : IRuntimeContext
    {
        private MetadataRuntimeContext() { }

        public static IRuntimeContext Create() => new MetadataRuntimeContext();

        public ITypeInfo? GetTypeByQualifiedName(string name)
        {
            Type? type = Type.GetType(name, throwOnError: false);
            return type is not null
                ? MetadataTypeInfo.CreateFrom(type)
                : null;
        }
    }
}

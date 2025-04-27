/********************************************************************************
* MetadataAssemblyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataAssemblyInfo : IAssemblyInfo
    {
        private Assembly UnderlyingAssembly { get; }

        private MetadataAssemblyInfo(Assembly assembly) => UnderlyingAssembly = assembly;

        public override bool Equals(object obj) => obj is MetadataAssemblyInfo that && UnderlyingAssembly.Equals(that.UnderlyingAssembly);

        public override int GetHashCode() => UnderlyingAssembly.GetHashCode();

        public override string ToString() => UnderlyingAssembly.ToString();

        public static IAssemblyInfo CreateFrom(Assembly assembly) => new MetadataAssemblyInfo(assembly);
        
        public bool IsFriend(string asmName)
        {
            //
            // TODO: strong name support
            //

            if (StringComparer.OrdinalIgnoreCase.Equals(asmName, UnderlyingAssembly.GetName().Name))
                return true;

            foreach (InternalsVisibleToAttribute ivt in UnderlyingAssembly.GetCustomAttributes<InternalsVisibleToAttribute>())
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(asmName, ivt.AssemblyName))
                    return true;
            }

            return false;
        }

        public ITypeInfo? GetType(string fullName)
        {
            Type type = UnderlyingAssembly.GetType(fullName, throwOnError: false);

            return type is not null
                ? MetadataTypeInfo.CreateFrom(type)
                : null;
        }

        public string? Location => IsDynamic ? null : UnderlyingAssembly.Location;

        public bool IsDynamic => UnderlyingAssembly.IsDynamic;

        public string Name => UnderlyingAssembly.GetName().ToString();
    }
}

/********************************************************************************
* MetadataAssemblyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataAssemblyInfo : IAssemblyInfo
    {
        private Assembly UnderlyingAssembly { get; }

        private MetadataAssemblyInfo(Assembly assembly) => UnderlyingAssembly = assembly;

        public override bool Equals(object obj) => obj is MetadataAssemblyInfo that && UnderlyingAssembly.Equals(that.UnderlyingAssembly);

        public override int GetHashCode() => UnderlyingAssembly.GetHashCode();

        public override string ToString() => UnderlyingAssembly.ToString();

        public static IAssemblyInfo CreateFrom(Assembly assembly) => new MetadataAssemblyInfo(assembly);

        public bool IsFriend(string asmName) => asmName == UnderlyingAssembly.GetName().Name || UnderlyingAssembly // TODO: strong name support
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Any(ivt => ivt.AssemblyName == asmName);

        public ITypeInfo? GetType(string fullName)
        {
            Type type = UnderlyingAssembly.GetType(fullName, throwOnError: false);

            return type is not null
                ? MetadataTypeInfo.CreateFrom(type)
                : null;
        }

        public string? Location => UnderlyingAssembly.IsDynamic ? null : UnderlyingAssembly.Location;

        public bool IsDynamic => UnderlyingAssembly.IsDynamic;

        public string Name => UnderlyingAssembly.GetName().ToString();
    }
}

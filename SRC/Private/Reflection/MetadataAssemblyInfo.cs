﻿/********************************************************************************
* MetadataAssemblyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataAssemblyInfo : IAssemblyInfo
    {
        private Assembly UnderLyingAssembly { get; }

        private MetadataAssemblyInfo(Assembly assembly) => UnderLyingAssembly = assembly;

        public override bool Equals(object obj) => obj is MetadataAssemblyInfo self && UnderLyingAssembly.Equals(self.UnderLyingAssembly);

        public override int GetHashCode() => UnderLyingAssembly.GetHashCode();

        public static IAssemblyInfo CreateFrom(Assembly assembly) => new MetadataAssemblyInfo(assembly);

        public bool IsFriend(string asmName) => UnderLyingAssembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Any(ivt => ivt.AssemblyName == asmName);

        public string? Location => UnderLyingAssembly.IsDynamic ? null : UnderLyingAssembly.Location;

        public bool IsDynamic => UnderLyingAssembly.IsDynamic;

        public AssemblyName Name => UnderLyingAssembly.GetName();
    }
}

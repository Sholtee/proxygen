/********************************************************************************
* TargetFramework.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class TargetFramework : ConfigBase<TargetFramework>
    {
        public IReadOnlyCollection<string> PlatformAssemblies { get; private set; } = Array.Empty<string>();

        public string? PlatformAssembliesDir { get; private set; }

        protected override void Init(IConfigReader configReader)
        {
            PlatformAssembliesDir = GetPath(configReader, nameof(PlatformAssembliesDir));
            PlatformAssemblies = configReader
                .ReadValue(nameof(PlatformAssemblies))?
                .Split(';')
                .Select(static asm => asm.Trim())
                .ToArray() ?? new string[] { "netstandard.dll", "System.Runtime.dll" };
        }

        protected override void InitWithDefaults() => Init(new RuntimeConfigReader());
    }
}

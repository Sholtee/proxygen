/********************************************************************************
* SourceGeneratorConfig.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SourceGeneratorConfig: ConfigBase<SourceGeneratorConfig>
    {
        protected override void Init(IConfigReader configReader)
        {
            DebugGenerator = configReader
                .ReadValue(nameof(DebugGenerator))?
                .Equals(true.ToString(), StringComparison.OrdinalIgnoreCase) == true;
        }

        public bool DebugGenerator { get; private set; }

        protected override void InitWithDefaults() {}
    }
}

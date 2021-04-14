/********************************************************************************
* WorkingDirectories.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class WorkingDirectories: ConfigBase<WorkingDirectories>
    {
        public string? AssemblyCacheDir { get; private set; }

        public string? SourceDump { get; private set; }

        public string? LogDump { get; private set; }

        protected override void Init(IConfigReader configReader)
        {
            AssemblyCacheDir = GetPath(nameof(AssemblyCacheDir));
            SourceDump       = GetPath(nameof(SourceDump));
            LogDump          = GetPath(nameof(LogDump));

            string? GetPath(string name)
            {
                string? result = configReader.ReadValue(name);

                if (result is not null)
                {
                    result = Environment.ExpandEnvironmentVariables(result);

                    if (!Path.IsPathRooted(result))
                        result = Path.Combine(configReader.BasePath, result);
                }

                return result;
            }
        }

        protected override void InitWithDefaults() => Init(new RuntimeConfigReader());
    }
}

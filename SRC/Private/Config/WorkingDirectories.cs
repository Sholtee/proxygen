/********************************************************************************
* WorkingDirectories.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal sealed class WorkingDirectories: ConfigBase<WorkingDirectories>
    {
        public string? AssemblyCacheDir { get; private set; }

        public string? SourceDump { get; private set; }

        public string? LogDump { get; private set; }

        protected override void Init(IConfigReader configReader)
        {
            AssemblyCacheDir = GetPath(configReader, nameof(AssemblyCacheDir));
            SourceDump       = GetPath(configReader, nameof(SourceDump));
            LogDump          = GetPath(configReader, nameof(LogDump));
        }

        protected override void InitWithDefaults() => Init(new RuntimeConfigReader());
    }
}

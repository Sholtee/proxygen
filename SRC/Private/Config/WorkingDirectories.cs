/********************************************************************************
* WorkingDirectories.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal class WorkingDirectories
    {
        //
        // Nem talaltam semmilyen dokumentaciot arrol h parhuzamosan futhatnak e 
        // SourceGenerator-ok, ezert feltetelezem h igen -> ThreadLocal
        //

        private static readonly ThreadLocal<WorkingDirectories> FInstance = new ThreadLocal<WorkingDirectories>(() => new WorkingDirectories(new RuntimeConfigReader()));

        public string? AssemblyCacheDir { get; }

        public string? SourceDump { get; }

        public string? LogDump { get; }

        public static WorkingDirectories Instance => FInstance.Value;

        public static void Setup(IConfigReader configReader) => FInstance.Value = new WorkingDirectories(configReader);

        private WorkingDirectories(IConfigReader configReader)
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
    }
}

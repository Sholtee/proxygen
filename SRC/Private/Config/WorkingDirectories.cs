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

        public string LogDump { get; }

        public static WorkingDirectories Instance => FInstance.Value;

        public static void Setup(IConfigReader configReader) => FInstance.Value = new WorkingDirectories(configReader);

        private WorkingDirectories(IConfigReader configReader)
        {
            AssemblyCacheDir = GetPath(configReader, nameof(AssemblyCacheDir));
            SourceDump       = GetPath(configReader, nameof(SourceDump));
            LogDump          = GetPath(configReader, nameof(LogDump)) ?? Path.GetTempPath();

            static string? GetPath(IConfigReader configReader, string name)
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

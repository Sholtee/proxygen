/********************************************************************************
* ConfigBase.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ConfigBase<TDescendant> where TDescendant : ConfigBase<TDescendant>, new()
    {
        //
        // It seems not documented if SourceGenerators can run parallel or not, so lets assume
        // they can -> ThreadLocal
        //

        private static readonly ThreadLocal<TDescendant> FInstance = new
        (
            static () => 
            {
                TDescendant instance = new();
                instance.InitWithDefaults();
                return instance;
            },
            trackAllValues: false
        );

        protected static string? GetPath(IConfigReader configReader, string name)
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

        protected abstract void Init(IConfigReader configReader);

        protected abstract void InitWithDefaults();

        public static void Setup(IConfigReader configReader) 
        {
            TDescendant instance = new();
            instance.Init(configReader);
            FInstance.Value = instance;
        }

        public static TDescendant Instance => FInstance.Value;
    }
}

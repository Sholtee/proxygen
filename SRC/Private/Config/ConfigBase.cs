/********************************************************************************
* ConfigBase.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ConfigBase<TDescendant> where TDescendant : ConfigBase<TDescendant>, new()
    {
        //
        // Nem talaltam semmilyen dokumentaciot arrol h parhuzamosan futhatnak e 
        // SourceGenerator-ok, ezert feltetelezem h igen -> ThreadLocal
        //

        private static readonly ThreadLocal<TDescendant> FInstance = new(() => 
        {
            TDescendant instance = new();
            instance.InitWithDefaults();
            return instance;
        });

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

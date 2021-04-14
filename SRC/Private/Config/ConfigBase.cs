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
            var instance = new TDescendant();
            instance.InitWithDefaults();
            return instance;
        });

        protected abstract void Init(IConfigReader configReader);

        protected abstract void InitWithDefaults();

        public static void Setup(IConfigReader configReader) 
        {
            var instance = new TDescendant();
            instance.Init(configReader);
            FInstance.Value = instance;
        }
#if DEBUG
        public static void Fake(TDescendant fake) => FInstance.Value = fake;
#endif
        public static TDescendant Instance => FInstance.Value;
    }
}

/********************************************************************************
* SourceCode.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public record SourceCode 
    {
        private readonly string FValue;
        public ref readonly string Value => ref FValue;

        public string Hint { get; }

        public SourceCode(string hint, in string value) 
        {
            Hint = hint;
            FValue = value;
        }

        public bool Dump()
        {
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), Hint), FValue);
                return true;
            }
            catch 
            {
                return false;
            }
        }
    }
}

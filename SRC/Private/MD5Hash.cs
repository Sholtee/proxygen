/********************************************************************************
* MD5Hash.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MD5Hash
    {
        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is only used to generate short file names")]
        public static string CreateFromString(string str) 
        {
            using (var core = MD5.Create()) 
            {
                byte[] data = core.ComputeHash(Encoding.UTF8.GetBytes(str));

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2", null));
                }

                return sb.ToString();
            }
        }
    }
}

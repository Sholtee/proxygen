/********************************************************************************
* ITypeInfoExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ITypeInfoExtensions
    {
        //
        // A "GUID" property generikus tipus lezart es nyitott valtozatanal ugyanaz
        //

        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
        public static string GetMD5HashCode(this ITypeInfo src)
        {
            using MD5 md5 = MD5.Create();

            Hash(src, md5);

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < md5.Hash.Length; i++)
            {
                sb.Append(md5.Hash[i].ToString("X2", null));
            }

            return sb.ToString();

            static void Hash(ITypeInfo t, ICryptoTransform transform)
            {
                if (t.AssemblyQualifiedName is null) // TODO: FIXME: ez latszolag nyitot generikusoknal is NULL nem csak generikus parametereknel
                    return;

                byte[] inputBuffer = Encoding.UTF8.GetBytes(t.AssemblyQualifiedName);

                transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, inputBuffer, 0);

                if (t is IGenericTypeInfo generic)
                    foreach (ITypeInfo ga in generic.GenericArguments)
                    {
                        Hash(ga, transform);
                    }
            }
        }
    }
}

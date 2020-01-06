/********************************************************************************
* CompilationOptionsFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if IGNORE_VISIBILITY
using System;
using System.Diagnostics;
using System.Reflection;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal static class CompilationOptionsFactory
    {
#if IGNORE_VISIBILITY
        private static readonly PropertyInfo TopLevelBinderFlagsProperty = GetTopLevelBinderFlags();

        private static readonly uint IgnoreAccessibility = GetIgnoreAccessibilityFlag();

        private static PropertyInfo GetTopLevelBinderFlags() 
        {
            PropertyInfo result = typeof(CSharpCompilationOptions)
                .GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(result != null);
            return result;
        }

        private static uint GetIgnoreAccessibilityFlag() 
        {
            Type binderFlagsType = typeof(CSharpCompilationOptions)
                .Assembly()
                .GetType("Microsoft.CodeAnalysis.CSharp.BinderFlags");
            Debug.Assert(binderFlagsType != null);

            FieldInfo ignoreAccessibility = binderFlagsType
                .GetField("IgnoreAccessibility", BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(ignoreAccessibility != null);

            return (uint) ignoreAccessibility.GetValue(null);
        }
#endif
        public static CSharpCompilationOptions Create()
        {
            var options = new CSharpCompilationOptions
            (
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions:
#if IGNORE_VISIBILITY
                    MetadataImportOptions.All,
#else
                    MetadataImportOptions.Public,
#endif
                optimizationLevel: OptimizationLevel.Release
            );
#if IGNORE_VISIBILITY
            TopLevelBinderFlagsProperty.SetValue(options, IgnoreAccessibility);
#endif
            return options;
        }
    }
}

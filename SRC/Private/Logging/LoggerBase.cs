/********************************************************************************
* LoggerBase.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class LoggerBase(string scope, LogLevel? level) : ILogger
    {
        protected virtual string StringifyObject(IDictionary<string, object?> additionalData)
        {
            DataContractJsonSerializer serializer = new
            (
                typeof(IDictionary<string, object>),
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                    KnownTypes = [typeof(List<string>)]
                }
            );

            using MemoryStream stm = new();

            serializer.WriteObject(stm, additionalData);

            return Encoding.UTF8.GetString(stm.GetBuffer(), 0, (int) stm.Length);
        }

        protected abstract void LogCore(string message);

        protected abstract void WriteSourceCore(string src);

        public LogLevel? Level { get; } = level;

        public string Scope { get; } = scope;

        public void Log(LogLevel level, object id, string message, IDictionary<string, object?>? additionalData)
        {
            if (level < Level)
                return;

            string msg = $"{DateTime.UtcNow:o} [{level}] {id} - {message}";
            if (additionalData is not null)
                msg += $"{Environment.NewLine}    {StringifyObject(additionalData)}";

            LogCore(msg);
        }

        public void WriteSource(CompilationUnitSyntax src) => WriteSourceCore
        (
            src
                .NormalizeWhitespace(eol: Environment.NewLine)
                .ToFullString()
        );

        public virtual void Dispose() { }
    }
}

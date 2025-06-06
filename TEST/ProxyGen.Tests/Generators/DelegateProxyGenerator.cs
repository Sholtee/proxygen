﻿/********************************************************************************
* DelegateProxyGenerator.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Internals;

    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class DelegateProxyGeneratorTests
    {
        public delegate int MyDelegate<T>(string a, ref T[] b, out object c);

        public interface IContextAccess: IInterceptor
        {
            IInvocationContext Context { get; }
        }

        private sealed class InterceptorChangingTheReturnValue : IContextAccess
        {
            public IInvocationContext Context { get; private set; }

            public object Invoke(IInvocationContext context)
            {
                Context = context;
                return 1986;
            }
        }

        private sealed class InterceptorNotChangingTheReturnValue : IContextAccess
        {
            public IInvocationContext Context { get; private set; }

            public object Invoke(IInvocationContext context)
            {
                Context = context;
                return context.Dispatch();
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        private sealed class MyAnnotationAttribute : Attribute { }


        public static IEnumerable<object[]> Interceptors
        {
            get
            {
                yield return [new InterceptorChangingTheReturnValue(), 1986];
                yield return [new InterceptorNotChangingTheReturnValue(), 0];
            }
        }

        [TestCaseSource(nameof(Interceptors))]
        public async Task GeneratedProxy_ShouldHookOnDelegates(IContextAccess interceptor, int retVal)
        {
            MyDelegate<int> proxy = await DelegateProxyGenerator<MyDelegate<int>>.ActivateAsync(interceptor, (string a, ref int[] b, out object c) => { c = null; return 0; });

            int[] ar = [];
            Assert.That(proxy("cica", ref ar , out object x), Is.EqualTo(retVal));
            Assert.That(interceptor.Context.Args[0], Is.EqualTo("cica"));
            Assert.That(interceptor.Context.Args[1], Is.SameAs(ar));
            Assert.That(interceptor.Context.Args[2], Is.Null);
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheDelegateTargetMember()
        {
            InterceptorNotChangingTheReturnValue interceptor = new();
            MyDelegate<int> proxy = await DelegateProxyGenerator<MyDelegate<int>>.ActivateAsync(interceptor, [MyAnnotation] (string a, ref int[] b, out object c) => { c = null; return 0; });

            int[] ar = [];
            proxy("cica", ref ar, out object x);

            Assert.That(interceptor.Context.Member.Method.GetCustomAttribute<MyAnnotationAttribute>(), Is.Not.Null);
        }

        [TestCaseSource(nameof(Interceptors))]
        public async Task GeneratedProxy_ShouldHookOnFuncs(IContextAccess interceptor, int retVal)
        {
            Func<string, int> proxy = await DelegateProxyGenerator<Func<string, int>>.ActivateAsync(interceptor, s => 0);

            Assert.That(proxy("cica"), Is.EqualTo(retVal));
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheFuncTargetMember()
        {
            InterceptorNotChangingTheReturnValue interceptor = new();
            Action proxy = await DelegateProxyGenerator<Action>.ActivateAsync(interceptor, [MyAnnotation] () => { });

            proxy();

            Assert.That(interceptor.Context.Member.Method.GetCustomAttribute<MyAnnotationAttribute>(), Is.Not.Null);
        }

        public static IEnumerable<Type> GeneratedProxy_AgainstSystemType_Params => typeof(object)
            .Assembly
            .GetExportedTypes()
            .Except
            ([
#if NET6_0_OR_GREATER
                typeof(System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal.UnhandledExceptionPropagationHandler)
#endif
            ])
            .Where
            (
                et => 
                    et.IsDelegate() && 
                    (!et.IsGenericType || !et.GetGenericArguments().SelectMany(ga => ga.GetGenericParameterConstraints()).Any())
            )
            .Select
            (
                et =>
                    et.IsGenericType &&
                    et.IsGenericTypeDefinition
                        ? et.MakeGenericType(et.GetGenericArguments().Length.Times(() => typeof(object)).ToArray())
                        : et
            )
            .Where
            (
                et =>
                {
                    return et.GetMethod("Invoke").GetParameters().All
                    (
                        p => Valid(p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType)
                    );

                    static bool Valid(Type t) => t.GetRefType() switch
                    {
                        RefType.Array => Valid(t.GetElementType()),
                        RefType.None => true,
                        _ => false
                    };
                }
            );

        [TestCaseSource(nameof(GeneratedProxy_AgainstSystemType_Params))]
        public void GeneratedProxy_AgainstSystemType(Type type) =>
            Assert.DoesNotThrow(() => new DelegateProxyGenerator(type).GetGeneratedType());
    }
}
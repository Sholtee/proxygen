/********************************************************************************
* ClassProxyGenerator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Security;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Proxy.Generators.Tests
{
    using Internals;

    [TestFixture, Parallelizable(ParallelScope.All)]
    public sealed class ClassProxyGeneratorTests
    {
        public abstract class Foo(int myParam)
        {
            public abstract int Bar<T>(ref T x, out string y, in List<T> z) where T: struct;

            public virtual int Prop { get; protected set; }

            public virtual event Action Event;

            public int Param { get; } = myParam;
        }

        private sealed class FooInterceptor : IInterceptor
        {
            public IInvocationContext Context { get; private set; }

            public object Invoke(IInvocationContext context)
            {
                Context = context;
                return 1;
            }
        }

        [Test]
        public async Task GeneratedProxy_ShouldHook()
        {
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(new FooInterceptor(), Tuple.Create(1986));

            int i = 0;
            Assert.That(proxy.Bar(ref i, out _, default), Is.EqualTo(1));

            Assert.That(proxy.Prop, Is.EqualTo(1));
        }

        [Test]
        public async Task GeneratedProxy_ShouldHandleCtorParams()
        {
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(new FooInterceptor(), Tuple.Create(1986));

            Assert.That(proxy.Param, Is.EqualTo(1986));
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheGenericArguments()
        {
            FooInterceptor interceptor = new();
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(interceptor, Tuple.Create(1986));

            int i = 0;
            proxy.Bar(ref i, out _, default);

            Assert.That(interceptor.Context.GenericArguments.SequenceEqual([typeof(int)]));

            _ = proxy.Prop;
            Assert.That(interceptor.Context.GenericArguments, Is.Empty);
        }

        [Test]
        public async Task GeneratedProxy_ShouldExposeTheTargetMember()
        {
            FooInterceptor interceptor = new();
            Foo proxy = await ProxyGenerator<Foo>.ActivateAsync(interceptor, Tuple.Create(1986));

            int i = 0;
            proxy.Bar(ref i, out _, default);

            string s;
            Assert.That(interceptor.Context.Member.Member, Is.EqualTo(MethodInfoExtensions.ExtractFrom<Foo>(f => f.Bar(ref i, out s ,default)).GetGenericMethodDefinition()));

            _ = proxy.Prop;
            Assert.That(interceptor.Context.Member.Member, Is.EqualTo(PropertyInfoExtensions.ExtractFrom((Foo f) => f.Prop)));
        }

        private static readonly HashSet<Type> TypesToSkip = 
        [
            typeof(Array), typeof(Delegate), typeof(Enum), typeof(MulticastDelegate), typeof(ValueType), // special types, can't derive from them
            typeof(SecurityException),  // too many ctors
#if NETFRAMEWORK
            typeof(ObjectAccessRule), typeof(ObjectAuditRule),  // too many ctors
            typeof(RuntimeEnvironment),  // obsolete class
#endif
#if NET5_0_OR_GREATER
            typeof(ComWrappers),
#endif
#if !NET5_0 && !NETCOREAPP3_1
            typeof(GenericSecurityDescriptor),  // abstract internal property
#endif
            typeof(FieldInfo)  // [not annotated] ref struct parameter 
        ];

        public static IEnumerable<Type> GeneratedProxy_AgainstSystemType_Params => typeof(object)
            .Assembly
            .GetExportedTypes()
            .Where
            (
                t =>
                {
                    IEnumerable<MethodInfo> virtualMethods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where
                    (
                        m => m.GetAccessModifiers() is not AccessModifiers.Private or AccessModifiers.Internal && (m.IsAbstract || m.IsVirtual)
                    );

                    IEnumerable<ConstructorInfo> ctors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where
                    (
                        c => c.GetAccessModifiers() is AccessModifiers.Public or AccessModifiers.Protected
                    );

                    return t.IsClass &&
                        !t.IsSealed &&
                        !t.IsSpecialName &&
                        !t.GetGenericArguments().Any() &&
                        !TypesToSkip.Contains(t) &&
                        !virtualMethods.Any(m => m.GetParameters().Any(p => p.ParameterType.GetRefType() is RefType.Ref or RefType.Pointer)) &&
                        virtualMethods.Count() > 3 &&
                        !ctors.Any(c => c.GetParameters().Any(p => p.IsOut || p.IsIn || p.ParameterType.GetRefType() is RefType.Ref or RefType.Pointer)) &&
                        ctors.Any();

                }
            );

        [TestCaseSource(nameof(GeneratedProxy_AgainstSystemType_Params))]
        public void GeneratedProxy_AgainstSystemType(Type type) =>
            Assert.DoesNotThrow(() => new ClassProxyGenerator(type).GetGeneratedType());
    }
}
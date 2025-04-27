# ProxyGen.NET [![Build status](https://ci.appveyor.com/api/projects/status/caw7qqtf5tbaa1fq/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/proxygen/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/proxygen/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/proxygen/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/proxygen?branch=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/proxygen.net)](https://www.nuget.org/packages/proxygen.net) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/sholtee/proxygen/master)
> .NET proxy generator powered by [Roslyn](https://github.com/dotnet/roslyn )

**This documentation refers the version 10.X of the library**

*ATTENTION:* This version represents a complete revamp of the library. It provides a brand new (more generic) API so it is NOT compatible with the previous versions at all. 
If you are interested in the legacy -v9- API you can find its documentation [here](https://github.com/Sholtee/proxygen/blob/65539b78a9772c8541fb5f643ce51cea47f02bd5/README.MD).
## Features
This library currently supports generating [proxies](https://en.wikipedia.org/wiki/Proxy_pattern ) for *interface*, *virtual method* and *delegate* interception as well as [duck typing](https://en.wikipedia.org/wiki/Duck_typing ).

### To hook into interface member calls:
1. Implement the `IInterceptor` class:
  ```csharp
  using Solti.Utils.Proxy;
  ...
  public class MyInterceptor: IInterceptor
  {
    public object Invoke(IInvocationContext context) // Invoking the generated proxy instance will trigger this method
    {
      if (suppressOriginalMethod)
      {
        return something;    
      }

      context.Args[0] = someNewVal; // "someNewVal" will be forwarded to the target method
	  
      object? result = context.Dispatch(); // call the target

      context.Args[1] = outputVal;  // modify the "ref" or "out" parameters set by the target method

      return result;
    }  
  }
  ```

  The `IInvocationContext` provides the following essential properties:
  - `Proxy`: The generated proxy instance that called the `Invoke()` method on the interceptor
  - `Member`: The member (property, event or method) being invoked by the caller
  - `GenericArguments`: Since the `Member` property always points to the generic definition, the user code should use this value to grab the generic arguments.
  - `Args`: Argument passed by the caller. The user code is allowed to modify this array. This is to let the user code change the input and output (`ref` or `out`) parameters

2. Generate a proxy instance:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...
  IMyInterface target = new MyClass();
  ...
  IMyInterface proxy;

  // create a proxy without target
  proxy = InterfaceProxyGenerator<IMyInterface>.Activate(new MyInterceptor());  // or ActivateAsync()

  // create a proxy with target
  proxy = InterfaceProxyGenerator<IMyInterface>.Activate(new MyInterceptor(), target);  // or ActivateAsync()
  ```
3. Enjoy

For further usage examples see [this](https://github.com/Sholtee/proxygen/blob/3740e6d608d30aec0f5a88aabfc89f8897cbd211/TEST/ProxyGen.Tests/Generators/InterfaceProxyGenerator.cs ).
### To hook into virtual member invocations
Lets suppose we have a virtual/abstract class like this:
```charp
public class MyClass(int ctorParameter1, string ctorParameter2)
{
  public virtual void DoSomeStuff<T>(T param) {...}  // it could be abstract as well
}
```
1. Create an interceptor in the same way as you could see in case of interface interception
2. Activate the proxy instance
  ```csharp
  using Solti.Utils.Proxy.Generators;

  // you can pick the desired constructor by providing the corresponding parameters.
  // pass null when you want to call the parameterless constructor
  MyClass proxy = ClassProxyGenerator<MyClass>.Activate(Tuple.Create(1986, "abc"));  // or ActivateAsync()
  ```
3. Enjoy

For further usage examples see [this](https://github.com/Sholtee/proxygen/blob/3740e6d608d30aec0f5a88aabfc89f8897cbd211/TEST/ProxyGen.Tests/Generators/ClassProxyGenerator.cs ).
### To create delegate proxies
1. Create an interceptor in the same way as you could see it above
2. Activate the proxy instance
  ```csharp
  using Solti.Utils.Proxy.Generators;

  Func<int, string> proxy = DelegateProxyGenerator<Func<int, string>>.Activate(i => string.Emtpy);  // or ActivateAsync()
  ```
For further usage examples see [this](https://github.com/Sholtee/proxygen/blob/3740e6d608d30aec0f5a88aabfc89f8897cbd211/TEST/ProxyGen.Tests/Generators/DelegateProxyGenerator.cs ).
### To create ducks:
1. Declare an interface that covers all the desired members of the target class:
  ```csharp
  public class TargetClass // does not implement IDuck
  {
    public void Foo() {...}
  }
  ...
  public interface IDuck 
  {
    void Foo();
  }
  ```
2. Generate the duck instance:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...
  TargetClass target = ...;
  IDuck duck = DuckGenerator<IDuck, TargetClass>.Activate(target); // or ActivateAsync()
  ```
3. Quack
  
Related tests can be seen [here](https://github.com/Sholtee/proxygen/blob/3740e6d608d30aec0f5a88aabfc89f8897cbd211/TEST/ProxyGen.Tests/Generators/DuckGenerator.cs ).
## Caching the generated assembly
By setting the `ProxyGen.AssemblyCacheDir` property in [YourApp.runtimeconfig.json](https://docs.microsoft.com/en-us/dotnet/core/run-time-config/ ) you can make the system cache the generated assembly, so next time your app starts and requests the proxy there won't be time consuming emitting operation.

You can do it easily by creating a template file named `runtimeconfig.template.json` in your project folder:
```json
{
  "configProperties": {
    "ProxyGen.AssemblyCacheDir": "GeneratedAssemblies"
  }
}
```
## Embedding the generated type
This library can be used as a [source generator](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/ ) so you can embed the generated proxy type into the assembly that uses it. This is simply done by the `Solti.Utils.Proxy.Attributes.EmbedGeneratedTypeAttribute`:
```csharp
[assembly: EmbedGeneratedType(typeof(InterfaceProxyGenerator<IMyInterface>))]
[assembly: EmbedGeneratedType(typeof(DuckGenerator<IMyInterface, MyClass>))]

```
The `xXxGenerator.GetGeneratedType()` method returns the embedded type if it is present in the assembly in which the `GetGeneratedType()` was called. Since all the time consuming operations already happened in compile time, requesting embedded types can significantly improve the performance.

Note that:
- Open generics are not supported.
- [coveralls.io](https://www.nuget.org/packages/coveralls.io/ ) (and other coverage reporters) may crash if your project was augmented by a source generator. To work this issue around:
  - Ignore the generated sources in your coverage app (e.g.: in [OpenCover](https://www.nuget.org/packages/OpenCover/ ) use the `-filter:-[*]Proxies.GeneratedClass_*` switch)
  - Create an empty file for each generated class (e.g.: `YourProject\Solti.Utils.Proxy\Solti.Utils.Proxy.Internals.ProxyEmbedder\Proxies.GeneratedClass_XxX.cs`)
  - Exclude these files from your project:
  ```xml
  <ItemGroup>
    <Compile Remove="Solti.Utils.Proxy\**" />
    <EmbeddedResource Remove="Solti.Utils.Proxy\**" />
    <None Remove="Solti.Utils.Proxy\**" />
  </ItemGroup>
  ```  
## Inspecting the generated code
*ProxyGen* is able to dump the generated sources. Due to performance considerations it is disabled by default. To enable 
- In runtime:

  Set the `ProxyGen.LogDirectory` property (in the same way you could see [above](#caching-the-generated-assembly)) to the desired directory (note that environment variables are supported):
  ```json
  {
    "configProperties": {
      "ProxyGen.LogDirectory": "%TEMP%"
    }
  }
  ```
  
- In compile time (source generator):

  Extend your `.csproj` with the following:
  ```xml
  <PropertyGroup>
    <ProxyGen_LogDirectory>$(OutputPath)Logs</ProxyGen_LogDirectory>
  </PropertyGroup>
  ```

The output should look like [this](https://github.com/Sholtee/proxygen/blob/bc4169b9d5c1727a4d612ef65183ccfae84979f8/TEST/ProxyGen.Tests/IfaceProxySrcModule.txt ).
## Migrating from version 
- 2.X
  - Delete all the cached assemblies (if the `[Proxy|Duck]Generator.CacheDirectory` is set somewhere)
  - `InterfaceInterceptor.Invoke()` returns the result of the original method (instead of `CALL_TARGET`) so in the override you may never need to invoke the `method` parameter directly.
- 3.X
  - `[Proxy|Duck]Generator.GeneratedType[Async]` property has been removed. To get the generated proxy type call the `[Proxy|Duck]Generator.GetGeneratedType[Async]()` method.
  - `[Proxy|Duck]Generator.CacheDirectory` property has been removed. To set the cache directory tweak the [runtimeconfig.json](#caching-the-generated-assembly) file.
- 4.X
  - The layout of the `InterfaceInterceptor<>.Invoke()` has been changed. Invocation parameters can be grabbed from the `InvocationContext` passed to the `Invoke()` method.
  - The `ConcurrentInterfaceInterceptor<>` class has been dropped since the `InterfaceInterceptor<>` class was rewritten in a thread safe manner.
- 5.X
  - You don't need to manually activate the generated proxy type, instead you may use the built-in `Generator.Activate()` method.
- 6.X
  - The `InvocationContext.InvokeTarget` property has been removed but you should not be affected by it
  - As proxy embedder has been reimplemented using the [v2](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md ) Source Generator API, this feature now requires VS 2022
- 7.X
  - `InterfaceInterceptor<TInterface>.Member|Method` has been renamed to `InterfaceMember|InterfaceMethod`
- 8.X
  - `Generator`s have been demoted to `class`. To compare `Generator` instances use their `Id` property.
- 9.X
  TODO
## Resources
- [API Docs](https://sholtee.github.io/proxygen )
- [Benchmark Results](https://sholtee.github.io/proxygen/perf )
- [Version History](https://github.com/Sholtee/proxygen/blob/master/history.md )

## Supported frameworks
This project currently targets `netstandard2.0` as well as `netstandard2.1` and had been tested against `net472`, `netcoreapp3.1`, `net5.0`, `net6.0`, `net7.0`, `net8.0` and `net9.0`.
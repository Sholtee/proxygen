# ProxyGen [![Build status](https://ci.appveyor.com/api/projects/status/caw7qqtf5tbaa1fq?svg=true)](https://ci.appveyor.com/project/Sholtee/proxygen) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/proxygen/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/proxygen?branch=master) ![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/proxygen.net)
> .NET proxy generator powered by Roslyn
## Purposes
This library currently supports generating [proxies](https://en.wikipedia.org/wiki/Proxy_pattern ) for interface interception and [duck typing](https://en.wikipedia.org/wiki/Duck_typing ).
### To hook into interface method calls:
1. Create the interceptor class (which is an [InterfaceInterceptor](https://sholtee.github.io/proxygen/doc/Solti.Utils.Proxy.InterfaceInterceptor-1.html ) descendant):
  ```csharp
  using Solti.Utils.Proxy;
  ...
  public class MyInterceptor: InterfaceInterceptor<IMyInterface>
  {
    public MyInterceptor(IMyInterface target) : base(target){}  // Nothing to do here

    public override object Invoke(MethodInfo method, object[] args, MemberInfo extra) // Invoked on every method call on generated proxy type
    {
	  if (needToModifyRetval)
	  {
	    return something;
        // ref|out parameters can be assigned by setting the corresponding "args[]" item 
	  }
	  return base.Invoke(method, args, extra); // Let the original method do its work
    }  
  }
  ```
2. Generate the proxy type:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...  
  Type proxyType = ProxyGenerator<IMyInterface, MyInterceptor>.GeneratedType; // The returned proxy type is assembled only once
  ```
3. Activate the proxy:
  ```csharp
  using System;
  ...
  IMyInterface target = ...;
  ...
  IMyInterface proxy = (IMyInterface) Activator.CreateInstence(proxyType, new object[]{target});
  ```
4. Enjoy

For further usage examples see [this](https://github.com/Sholtee/proxygen/blob/master/TEST/Generators/ProxyGenerator.cs ) or [that](https://github.com/Sholtee/injector#decorating-services ).
### To create ducks:
1. Take an interface that covers the desired members of the target class:
  ```csharp
  public class TargetClass // Not implements IDuck
  {
    public void Foo(){...}
  }
  ...
  public interface IDuck 
  {
    void Foo();
  }
  ```
2. Generate the duck type:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...
  Type duckType = DuckGenerator<IDuck, TargetClass>.GeneratedType; // The returned duck type is assembled only once
  ```
3. Quack:
  ```csharp
  using System;
  ...
  TargetClass target = ...;
  ...
  IDuck duck = (IDuck) Activator.CreateInstence(duckType, new object[]{target});
  ```
  
If you don't get bored (yet) check out the related [tests](https://github.com/Sholtee/proxygen/blob/master/TEST/Generators/DuckGenerator.cs ).
## Resources
- [API Docs](https://sholtee.github.io/proxygen ) (WIP).
- [Benchmark Results](https://sholtee.github.io/proxygen/perf )

## Supported frameworks
This project currently targets *.NET Standard* 1.6, 2.0 and 2.1.
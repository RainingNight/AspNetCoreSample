# ASP.NET Core 微服务初探[1]：服务发现之Consul

在传统单体架构中，由于应用动态性不强，不会频繁的更新和发布，也不会进行自动伸缩，我们通常将所有的服务地址都直接写在项目的配置文件中，发生变化时，手动改一下配置文件，也不会觉得有什么问题。但是在微服务模式下，服务会更细的拆分解耦，微服务会被频繁的更新和发布，根据负载情况进行动态伸缩，以及受资源调度影响而从一台服务器迁移到另一台服务器等等。总而言之，在微服务架构中，微服务实例的网络位置变化是一种常态，服务发现也就成了微服务中的一个至关重要的环节。

## 服务发现是什么

其实，服务发现可以说自古有之，我们每天在不知不觉中就一直在使用服务发现。比如，我们在浏览器中输入域名，DNS服务器会根据我们的域名解析出一个Ip地址，然后去请求这个Ip来获取我们想要的数据，又或是我们使用网络打印机的时候，首先要通过WS-Discovery或者Bonjour协议来发现并连接网络中存在的打印服务等。这都是服务发现，它可以让我们只需说我想要什么服务即可，而不必去关心服务提供者的具体网络位置（IP 地址、端口等）。

目前，服务发现主要分为两种模式，客户端模式与服务端模式，两者的本质区别在于，客户端是否保存服务列表信息，比如DNS就属于服务端模式。

在客户端模式下，如果要进行微服务调用，首先要到服务注册中心获取服务列表，然后使用本地的负载均衡策略选择一个服务进行调用。

而在服务端模式下，客户端直接向服务注册中心发送请求，服务注册中心再通过自身负载均衡策略对微服务进行调用后返回给客户端。

客户端模式相对来说比较简单，也比较容易实现，本文就先来介绍一下基于Consul的客户端服务发现。

## Consul简介

Consul是HashiCorp公司推出的使用go语言开发的开源工具，用于实现分布式系统的服务发现与配置，内置了服务注册与发现框架、分布一致性协议实现、健康检查、Key/Value存储、多数据中心方案，使用起来较为简单。

Consul的安装包仅包含一个可执行文件，部署非常方便，直接从 [官网](https://www.consul.io/downloads.html)) 下载即可。

![consul](https://img2018.cnblogs.com/blog/347047/201812/347047-20181207193655991-996490717.png)

如图，可以看出Consul的集群是由N个Server，加上M个Client组成的。而不管是Server还是Client，都是Consul的一个节点，所有的服务都可以注册到这些节点上，正是通过这些节点实现服务注册信息的共享。

Consule的核心概念：

* **Server**：表示Consul的server模式，它会把所有的信息持久化的本地，这样遇到故障，信息是可以被保留的。

* **Client**：表示consul的client模式，就是客户端模式。在这种模式下，所有注册到当前节点的服务会被转发到server，本身不持久化这些信息。

* **ServerLeader**：上图那个Server下面有LEADER标识的，表明这个Server是它们的老大，它和其它Server不一样的是，它需要负责同步注册的信息给其它的Server，同时也要负责各个节点的健康监测。

关于Consul集群搭建等文章非常之多，本文就不再啰嗦，简单使用开发模式来演示，运行如下命令：

```bash
./consul agent -dev

# 输出
==> Starting Consul agent...
==> Consul agent running!
           Version: 'v1.4.0'
           Node ID: '21ec5df7-f11d-3a4e-ad1b-5ca445f8149b'
         Node name: 'Cosmos'
        Datacenter: 'dc1' (Segment: '<all>')
            Server: true (Bootstrap: false)
       Client Addr: [127.0.0.1] (HTTP: 8500, HTTPS: -1, gRPC: 8502, DNS: 8600)
      Cluster Addr: 127.0.0.1 (LAN: 8301, WAN: 8302)
           Encrypt: Gossip: false, TLS-Outgoing: false, TLS-Incoming: false
```

如上，可以看到Consul默认的几个端口，如`8500`是客户端基于Http调用的，也是我们最常用的，另外再补充一下常用的几个参数的含义：

* **-dev**：创建一个开发环境下的server节点，不会有任何持久化操作，不建议在生产环境中使用。
* **-bootstrap-expect**：该命令通知consul server准备加入的server节点个数，延迟日志复制的启动，直到指定数量的server节点成功的加入后才启动。
* **-client**: 用于客户端通过RPC, DNS, HTTP 或 HTTPS访问，默认127.0.0.1。
* **-bind**: 用于集群间通信，默认0.0.0.0。
* **-advertise**: 通告地址，通告给集群中其他节点，默认使用 `-bind` 地址。

## 注册服务

我们首先创建一个ASP.NET Core WebAPI程序，命名为ServiceA。

然后引入Cosnul的官方Nuge包：

```bash
dotnet add package Consul
```

Consul包中提供了一个`IConsulClient`类，我们可以通过它来调用Consul进行服务的注册，以及发现等。

首先在`Startup`的`ConfigureServices`方法中来配置`IConsulClient`到ASP.NET Core的依赖注入系统中：

```csharp
services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://localhost:8500");
}));
```

我们需要在服务启动的时候，将自身的地址等信息注册到Consul中，并在服务关闭的时候从Consul撤销。这种行为就非常适合使用 [IHostedService](https://www.cnblogs.com/RainingNight/p/hosting-configure-in-asp-net-core.html#ihostedservice) 来实现。

1.启动时注册服务：

```csharp
public async Task StartAsync(CancellationToken cancellationToken)
{
    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    var features = _server.Features;
    var address = features.Get<IServerAddressesFeature>().Addresses.First();
    var uri = new Uri(address);

    _serviceId = "Service-v1-" + Dns.GetHostName() + "-" + uri.Authority;

    var registration = new AgentServiceRegistration()
    {
        ID = _serviceId,
        Name = "Service",
        Address = uri.Host,
        Port = uri.Port,
        Tags = new[] { "api" }
    };

    // 首先移除服务，避免重复注册
    await _consulClient.Agent.ServiceDeregister(registration.ID, _cts.Token);
    await _consulClient.Agent.ServiceRegister(registration, _cts.Token);
}
```

这里要注意的是，我们需要保证`_serviceId`对于同一个实例的唯一，避免重复性的注册。

2.关闭时撤销服务：

```csharp
public async Task StopAsync(CancellationToken cancellationToken)
{
    _cts.Cancel();
    await _consulClient.Agent.ServiceDeregister(_serviceId, cancellationToken);
}
```

我们可以复制一份ServiceA的代码，命名为ServiceB，修改一下端口，分别为5001和5002，运行后，打开Consul的管理UI [http://localhost:8500](http://localhost:8500)：

![consul-healthy](https://img2018.cnblogs.com/blog/347047/201812/347047-20181207181234552-915191810.png)

如果我们关闭其中一个服务的，会调用`StopAsync`方法，撤销其注册的服务，然后刷新浏览器，可以看到只剩下一个节点了。

Consul是支持健康检查，我们可以在注册服务的时候指定健康检查地址，修改上面`AgentServiceRegistration`中的信息如下：

```csharp
var registration = new AgentServiceRegistration()
{
    ID = _serviceId,
    Name = "Service",
    Address = uri.Host,
    Port = uri.Port,
    Tags = new[] { "api" }
    Check = new AgentServiceCheck()
    {
        // 心跳地址
        HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}/healthz",
        // 超时时间
        Timeout = TimeSpan.FromSeconds(2),
        // 检查间隔
        Interval = TimeSpan.FromSeconds(10)
    }
};
```

对于上面的`healthz`地址，我使用了ASP.NET Core 2.2中自带的健康检查，它需要在`Startup`中添加如下配置：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks();
}

public void Configure(IApplicationBuilder app)
{
    app.UseHealthChecks("/healthz");
}
```

关于健康检查更详细的介绍可以查看：[ASP.NET Core 2.2.0-preview1: Healthchecks](https://blogs.msdn.microsoft.com/webdev/2018/08/22/asp-net-core-2-2-0-preview1-healthcheck)。

现在，我们重新运作这两个服务，等待注册成功后，使用任务管理器杀掉其中的一个进程（阻止`StopAsync`的执行），可以看到Consul会将其移动到不健康的节点，显示如下：

![consul-unhealthy](https://img2018.cnblogs.com/blog/347047/201812/347047-20181207181252601-2046337279.png)

## 发现服务

现在来看看服务消费者如何从Consul来获取可用的服务列表。

我们创建一个ConsoleApp，做为服务的调用端，添加`Consul`Nuget包,然后，创建一个`ConsulServiceProvider`类，实现如下：

```csharp
public class ConsulServiceProvider : IServiceDiscoveryProvider
{
    public async Task<List<string>> GetServicesAsync()
    {
        var consuleClient = new ConsulClient(consulConfig =>
        {
            consulConfig.Address = new Uri("http://localhost:8500");
        });
        var queryResult = await consuleClient.Health.Service("Service", string.Empty, true);
        var result = new List<string>();
        foreach (var serviceEntry in queryResult.Response)
        {
            result.Add(serviceEntry.Service.Address + ":" + serviceEntry.Service.Port);
        }
        return result;
    }
}
```

如上，我们创建一个`ConsulClient`实例，直接调用`consuleClient.Health.Service`就可以获取到可用的服务列表了，然后使用HttpClient就可以发起对服务的调用。

但我们需要思考一个问题，我们什么时候从Consul获取服务呢？

最为简单的便是在每次调用服务时，都先从Consul来获取一下服务列表，这样做的好处是我们得到的服务列表是最新的，能及时获取到新注册的服务以及过滤掉挂掉的服务。但是这样每次请求都增加了一次对Consul的调用，对性能有稍微的损耗，不过我们可以在每个调用端的机器上都部署一个Consul Agent，这样对性能的影响就微乎其微了。

另外一种方式，可以在调用端做服务列表的本地缓存，并定时与Consul同步，具体实现如下：

```bash
public class PollingConsulServiceProvider : IServiceDiscoveryProvider
{
    private List<string> _services = new List<string>();
    private bool _polling;

    public PollingConsulServiceProvider()
    {
        var _timer = new Timer(async _ =>
        {
            if (_polling) return;

            _polling = true;
            await Poll();
            _polling = false;

        }, null, 0, 1000);
    }

    public async Task<List<string>> GetServicesAsync()
    {
        if (_services.Count == 0) await Poll();
        return _services;
    }

    private async Task Poll()
    {
        _services = await new ConsulServiceProvider().GetServicesAsync();
    }
}
```

其实现也非常简单，通过一个Timer来定时从Consul拉取最新的服务列表。

现在我们获取到服务列表了，还需要设计一种负载均衡机制，来实现服务调用的最优化。

## 负载均衡

如何将不同的用户的流量分发到不同的服务器上面呢，早期的方法是使用DNS做负载，通过给客户端解析不同的IP地址，让客户端的流量直接到达各个服务器。但是这种方法有一个很大的缺点就是延时性问题，在做出调度策略改变以后，由于DNS各级节点的缓存并不会及时的在客户端生效，而且DNS负载的调度策略比较简单，无法满足业务需求，因此就出现了负载均衡器。

常见的负载均衡算法有如下几种：

* 随机算法：每次从服务列表中随机选取一个服务器。

* 轮询及加权轮询：按顺序依次调用服务列表中的服务器，也可以指定一个加权值，来增加某个服务器的调用次数。

* 最小连接：记录每个服务器的连接数，每次选取连接数最少的服务器。

* 哈希算法：分为普通哈希与一致性哈希等。

* IP地址散列：通过调用端Ip地址的散列，将来自同一调用端的分组统一转发到相同服务器的算法。

* URL散列：通过管理调用端请求URL信息的散列，将发送至相同URL的请求转发至同一服务器的算法。

本文中简单模拟前两种来介绍一下。

### 随机均衡

随机均衡是最为简单粗暴的方式，我们只需根据服务器数量生成一个随机数即可：

```bash
public class RandomLoadBalancer : ILoadBalancer
{
    private readonly IServiceDiscoveryProvider _sdProvider;

    public RandomLoadBalancer(IServiceDiscoveryProvider sdProvider)
    {
        _sdProvider = sdProvider;
    }

    private Random _random = new Random();

    public async Task<string> GetServiceAsync()
    {
        var services = await _sdProvider.GetServicesAsync();
        return services[_random.Next(services.Count)];
    }
}
```

其中`IServiceDiscoveryProvider`是上文介绍的Consule服务提供者者，定义如下：

```bash
public interface IServiceDiscoveryProvider
{
    Task<List<string>> GetServicesAsync();
}
```

而`ILoadBalancer`的定义如下：

```bash
public interface ILoadBalancer
{
    Task<string> GetServiceAsync();
}
```

### 轮询均衡

再来看一下最简单的轮询实现：

```bash
public class RoundRobinLoadBalancer : ILoadBalancer
{
    private readonly IServiceDiscoveryProvider _sdProvider;

    public RoundRobinLoadBalancer(IServiceDiscoveryProvider sdProvider)
    {
        _sdProvider = sdProvider;
    }

    private readonly object _lock = new object();
    private int _index = 0;

    public async Task<string> GetServiceAsync()
    {
        var services = await _sdProvider.GetServicesAsync();
        lock (_lock)
        {
            if (_index >= services.Count)
            {
                _index = 0;
            }
            return services[_index++];
        }
    }
}
```

如上，使用lock控制并发，每次请求，移动一下服务索引。

最后，便可以直接使用HttpClient来完成服务的调用了：

```bash
var client = new HttpClient();
ILoadBalancer balancer = new RoundRobinLoadBalancer(new PollingConsulServiceProvider());

// 使用轮询算法调用
for (int i = 0; i < 10; i++)
{
    var service = await balancer.GetServiceAsync();
    Console.WriteLine(DateTime.Now.ToString() + "-RoundRobin:" +
        await client.GetStringAsync("http://" + service + "/api/values") + " --> " + "Request from " + service);
}

// 使用随机算法调用
balancer = new RandomLoadBalancer(new PollingConsulServiceProvider());
for (int i = 0; i < 10; i++)
{
    var service = await balancer.GetServiceAsync();
    Console.WriteLine(DateTime.Now.ToString() + "-Random:" +
        await client.GetStringAsync("http://" + service + "/api/values") + " --> " + "Request from " + service);
}
```

## 总结

本文从服务注册，到服务发现，再到负载均衡，演示了一个最简单的服务间调用的流程。看起来还不错，但是还有一个很严重的问题，就是当我们获取到服务列表时，服务都还是健康的，但是在我们发起请求中，服务突然挂了，这会导致调用端的异常。那么能不能在某一个服务调用失败时，自动切换到下一个服务进行调用呢？下一章就来介绍一下熔断降级，完美的解决了服务调用失败以及重试的问题。

附本文源码地址：[https://github.com/RainingNight/AspNetCoreSample/tree/master/src/Microservice/ServiceDiscovery/ConsulDemo](https://github.com/RainingNight/AspNetCoreSample/tree/master/src/Microservice/ServiceDiscovery/ConsulDemo)

## 参考资料

* [Using Consul for Service Discovery with ASP.NET Core](https://cecilphillip.com/using-consul-for-service-discovery-with-asp-net-core)

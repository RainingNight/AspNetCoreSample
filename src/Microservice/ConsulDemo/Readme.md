# .NET Core 微服务初探[2]：服务发现之Consul

Consul是HashiCorp公司推出的开源工具，用于实现分布式系统的服务发现与配置。与其他分布式服务注册与发现的方案，比如 Airbnb的SmartStack等相比，Consul的方案更“一站式”，内置了服务注册与发现框 架、分布一致性协议实现、健康检查、Key/Value存储、多数据中心方案，不再需要依赖其他工具（比如ZooKeeper等），使用起来也较为简单。Consul用Golang实现，因此具有天然可移植性(支持Linux、windows和Mac OS X)；安装包仅包含一个可执行文件，方便部署，与Docker等轻量级容器可无缝配合。

# Consul部署

## 常用命令

* agent: 运行一个consul agent
* join: 将agent加入到consul集群
* members: 列出consul cluster集群中的members
* leave: 将节点移除所在集群

参数详解：

* -data-dir
    * 作用：指定agent储存状态的数据目录
    * 这是所有agent都必须的
    * 对于server尤其重要，因为他们必须持久化集群的状态

* -config-dir
    * 作用：指定service的配置文件和检查定义所在的位置
    * 通常会指定为"某一个路径/consul.d"（通常情况下，.d表示一系列配置文件存放的目录）

* -config-file
    * 作用：指定一个要装载的配置文件
    * 该选项可以配置多次，进而配置多个配置文件（后边的会合并前边的，相同的值覆盖）

* -dev
    * 作用：创建一个开发环境下的server节点
    * 该参数配置下，不会有任何持久化操作，即不会有任何数据写入到磁盘
    * 这种模式不能用于生产环境（因为第二条）

* -bootstrap-expect
    * 作用：该命令通知consul server我们现在准备加入的server节点个数，该参数是为了延迟日志复制的启动直到我们指定数量的server节点成功的加入后启动。

* -node
    * 作用：指定节点在集群中的名称
    * 该名称在集群中必须是唯一的（默认采用机器的host）
    * 推荐：直接采用机器的IP

* -bind
    * 作用：指明节点的IP地址
    * 有时候不指定绑定IP，会报Failed to get advertise address: Multiple private IPs found. Please configure one. 的异常

* -server
    * 作用：指定节点为server
    * 每个数据中心（DC）的server数推荐至少为1，至多为5
    * 所有的server都采用raft一致性算法来确保事务的一致性和线性化，事务修改了集群的状态，且集群的状态保存在每一台server上保证可用性
    * server也是与其他DC交互的门面（gateway）

* -client
    * 作用：指定节点为client，指定客户端接口的绑定地址，包括：HTTP、DNS、RPC
    * 默认是127.0.0.1，只允许回环接口访问
    * 若不指定为-server，其实就是-client

* -join
    * 作用：将节点加入到集群

* -datacenter
    * 作用：指定机器加入到哪一个数据中心

## Docker 部署

```bash
# 若不指定为-server，其实就是-client
docker run -d -p 8301:8301 -p 8302:8302 -p 8400:8400 -p 8500:8500 -p 53:8600/udp consul

# ==> Starting Consul agent...
# 
# ==> Consul agent running!
#            Version: 'v1.3.0'
#            Node ID: 'ee81f4a6-92b9-6fab-6786-536d9c07edaf'
#          Node name: 'ac83d5b0a078'
#         Datacenter: 'dc1' (Segment: '<all>')
#             Server: true (Bootstrap: false)
#        Client Addr: [0.0.0.0] (HTTP: 8500, HTTPS: -1, gRPC: 8502, DNS: 8600)
#       Cluster Addr: 127.0.0.1 (LAN: 8301, WAN: 8302)
#            Encrypt: Gossip: false, TLS-Outgoing: false, TLS-Incoming: false
```

## 参考资料

* [Using Consul for Service Discovery with ASP.NET Core](https://cecilphillip.com/using-consul-for-service-discovery-with-asp-net-core)

* [Using Consul for Health Checks with ASP.NET Core](https://cecilphillip.com/using-consul-for-health-checks-with-asp-net-core)

* [Service Discovery And Health Checks In ASP.NET Core With Consul](http://michaco.net/blog/ServiceDiscoveryAndHealthChecksInAspNetCoreWithConsul)
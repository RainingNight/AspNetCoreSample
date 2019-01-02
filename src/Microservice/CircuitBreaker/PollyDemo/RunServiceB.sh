#/bin/bash
cd ../../ServiceDiscovery/ConsulDemo/

dotnet restore
dotnet build --no-restore

dotnet run --project ./src/ServiceB --no-build &
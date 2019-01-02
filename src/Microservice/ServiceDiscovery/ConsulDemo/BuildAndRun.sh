#/bin/bash

dotnet restore
dotnet build --no-restore

dotnet run --project ./src/ServiceA --no-build & 
dotnet run --project ./src/ServiceB --no-build &

sleep 5

dotnet run --project ./src/ClientDemo --no-build &
#/bin/bash

dotnet restore
dotnet build --no-restore

dotnet run --project ./src/ServiceA --no-build & 
dotnet run --project ./src/ServiceA --no-build &

sleep 2

dotnet run --project ./src/ClientDemo --no-build &
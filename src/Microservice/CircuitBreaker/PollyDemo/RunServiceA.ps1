# Uncomment the following to clear the nuget cache if rebuilding the packages doesn't seem to take effect.
#dotnet nuget locals all --clear

cd ../../ServiceDiscovery/ConsulDemo/

dotnet restore
if ($LastExitCode -ne 0) { return; }

dotnet build --no-restore
if ($LastExitCode -ne 0) { return; }

Start-Process "dotnet" -ArgumentList "run --project src/ServiceA --no-build"
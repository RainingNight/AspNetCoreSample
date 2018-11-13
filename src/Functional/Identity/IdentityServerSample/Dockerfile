FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY IdentityServerSample/IdentityServerSample.csproj IdentityServerSample/
RUN dotnet restore IdentityServerSample/IdentityServerSample.csproj
COPY . .
WORKDIR /src/IdentityServerSample
RUN dotnet build IdentityServerSample.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish IdentityServerSample.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

EXPOSE 4000/tcp
ENV ASPNETCORE_URLS http://*:4000
ENV ASPNETCORE_ENVIRONMENT Production

ENTRYPOINT ["dotnet", "IdentityServerSample.dll"]

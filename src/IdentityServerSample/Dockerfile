FROM microsoft/dotnet:latest
COPY . /app
WORKDIR /app
 
RUN dotnet restore
RUN dotnet build
 
EXPOSE 4000/tcp
ENV ASPNETCORE_URLS http://*:4000
ENV ASPNETCORE_ENVIRONMENT Production
 
ENTRYPOINT dotnet run
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /api

COPY . .

WORKDIR /api
RUN df -h
RUN pwd
RUN ls -la
RUN dotnet restore "./artifact-exchange-api/Artifact_Service_Api/Artifact_Service_Api.csproj"

RUN dotnet build "./artifact-exchange-api/Artifact_Service_Api/Artifact_Service_Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./artifact-exchange-api/Artifact_Service_Api/Artifact_Service_Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Artifact_Service_Api.dll"]
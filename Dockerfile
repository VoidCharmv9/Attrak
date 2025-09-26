FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the necessary files for the web API project
COPY ServerAtrrak/ ServerAtrrak/
COPY AttrackSharedClass/ AttrackSharedClass/
COPY Attrak.sln .

# Restore and publish only the web API project
RUN dotnet restore ServerAtrrak/ServerAtrrak.csproj
RUN dotnet publish ServerAtrrak/ServerAtrrak.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ServerAtrrak.dll"]

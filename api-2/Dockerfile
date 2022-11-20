FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy app csproj and restore as distinct layers
COPY api-2.csproj .
RUN dotnet restore
    
# Copy everything else and build
COPY . . 
RUN dotnet publish -c Release -o out
    
# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "api-2.dll"]
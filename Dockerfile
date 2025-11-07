# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DockWatch/*.csproj ./DockWatch/
RUN dotnet restore ./DockWatch/DockWatch.csproj

COPY . .
# Let publish do the restore
RUN dotnet publish ./DockWatch/DockWatch.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DockWatch.dll"]

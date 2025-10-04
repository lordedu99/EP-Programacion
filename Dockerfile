# ---- build & publish ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos solo el csproj para aprovechar la cache en el restore
COPY PortalAcademico.csproj ./
RUN dotnet restore PortalAcademico.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish PortalAcademico.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render inyecta $PORT; exponemos 8080 por convención
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PortalAcademico.dll"]

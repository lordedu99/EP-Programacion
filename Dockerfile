# Etapa base (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Render asigna el puerto via $PORT, pero EXPOSE ayuda localmente
EXPOSE 8080
# Valor por defecto local; en Render se sobrescribe con ASPNETCORE_URLS env
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Etapa build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia csproj y restaura
COPY ["PortalAcademico.csproj", "./"]
RUN dotnet restore "PortalAcademico.csproj"

# Copia el resto y compila
COPY . .
RUN dotnet build "PortalAcademico.csproj" -c Release -o /app/build

# Publica
FROM build AS publish
RUN dotnet publish "PortalAcademico.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PortalAcademico.dll"]

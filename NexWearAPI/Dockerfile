
# ── Etapa 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
 
# Copiar y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore
 
# Copiar todo y compilar
COPY . ./
RUN dotnet publish -c Release -o /app/publish
 
# ── Etapa 2: Runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
 
COPY --from=build /app/publish .
 
EXPOSE 8080
 
ENV ASPNETCORE_URLS=http://+:8080
 
ENTRYPOINT ["dotnet", "NexWearAPI.dll"]
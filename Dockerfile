FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY Sipitex.slnx ./
COPY src/Sipitex.Domain/Sipitex.Domain.csproj src/Sipitex.Domain/
COPY src/Sipitex.Application/Sipitex.Application.csproj src/Sipitex.Application/
COPY src/Sipitex.Infrastructure/Sipitex.Infrastructure.csproj src/Sipitex.Infrastructure/
COPY src/Sipitex.Web/Sipitex.Web.csproj src/Sipitex.Web/
RUN dotnet restore src/Sipitex.Web/Sipitex.Web.csproj
COPY src/ src/
ARG RENDER_GIT_COMMIT=
ENV RENDER_GIT_COMMIT=${RENDER_GIT_COMMIT}
RUN dotnet publish src/Sipitex.Web/Sipitex.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.9 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
# Render sobreescribe PORT (por defecto 10000). En local el valor es 8080.
# La app lee PORT al arrancar y escucha en 0.0.0.0. No fijar ASPNETCORE_URLS aquí:
# un valor fijo en 8080 hace que Render marque el deploy como fallido y deje el anterior.
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
ARG RENDER_GIT_COMMIT=
ENV RENDER_GIT_COMMIT=${RENDER_GIT_COMMIT}
ENV ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex"
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS "http://127.0.0.1:${PORT}/healthz" || exit 1
ENTRYPOINT ["dotnet", "Sipitex.Web.dll"]

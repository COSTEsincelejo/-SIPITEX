FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Sipitex.slnx ./
COPY src/Sipitex.Domain/Sipitex.Domain.csproj src/Sipitex.Domain/
COPY src/Sipitex.Application/Sipitex.Application.csproj src/Sipitex.Application/
COPY src/Sipitex.Infrastructure/Sipitex.Infrastructure.csproj src/Sipitex.Infrastructure/
COPY src/Sipitex.Web/Sipitex.Web.csproj src/Sipitex.Web/
RUN dotnet restore src/Sipitex.Web/Sipitex.Web.csproj
COPY src/ src/
RUN dotnet publish src/Sipitex.Web/Sipitex.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
VOLUME ["/app/data"]
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/sipitex.db"
ENTRYPOINT ["dotnet", "Sipitex.Web.dll"]

# syntax=docker/dockerfile:1
# -----------------------------------------------------------------------------
# QLDonHang Backend - .NET 9 Web API
# Multi-stage build for Railway deployment.
# -----------------------------------------------------------------------------

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + props + csproj first to leverage layer cache for `dotnet restore`.
COPY OrderMgmt.sln Directory.Build.props ./
COPY src/OrderMgmt.Domain/OrderMgmt.Domain.csproj           src/OrderMgmt.Domain/
COPY src/OrderMgmt.Application/OrderMgmt.Application.csproj src/OrderMgmt.Application/
COPY src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj src/OrderMgmt.Infrastructure/
COPY src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj           src/OrderMgmt.WebApi/

RUN dotnet restore src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj

# Copy the rest of the source and publish.
COPY src/ ./src/
RUN dotnet publish src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# LibreOffice is only required if QuotationExport PDF rendering is used.
# Skip it to keep the image small — set QuotationExport__LibreOfficePath only
# if you mount/install a renderer at runtime.

COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_GCServer=1 \
    PORT=8080

# Railway injects $PORT at runtime. ASP.NET Core listens on ASPNETCORE_URLS,
# so we expand $PORT into the URL at container start.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT} exec dotnet OrderMgmt.WebApi.dll"]

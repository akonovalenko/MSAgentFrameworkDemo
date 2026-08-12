# Multi-stage Dockerfile for building and running the BitcoinAgent.Api web project (net10.0)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files to improve layer caching
COPY ["BitcoinAgent.slnx", "./"]
COPY ["BitcoinAgent.Api/BitcoinAgent.Api.csproj", "BitcoinAgent.Api/"]
COPY ["BitcoinAgent.Application/BitcoinAgent.Application.csproj", "BitcoinAgent.Application/"]
COPY ["BitcoinAgent.Infrastructure/BitcoinAgent.Infrastructure.csproj", "BitcoinAgent.Infrastructure/"]
COPY ["BitcoinAgent.Domain/BitcoinAgent.Domain.csproj", "BitcoinAgent.Domain/"]

RUN dotnet restore "BitcoinAgent.Api/BitcoinAgent.Api.csproj"

# Copy remaining source and publish
COPY . .
ARG CONFIGURATION=Release
RUN dotnet publish "BitcoinAgent.Api/BitcoinAgent.Api.csproj" -c $CONFIGURATION -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install openssl for certificate generation
RUN apt-get update && apt-get install -y openssl && rm -rf /var/lib/apt/lists/*

# Create certificate directory
RUN mkdir -p /app/certs

# Generate self-signed certificate for HTTPS with SAN
RUN openssl req -x509 -newkey rsa:2048 -keyout /app/certs/key.pem -out /app/certs/cert.pem \
    -days 365 -nodes -subj "/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,DNS:127.0.0.1,IP:127.0.0.1" && \
    openssl pkcs12 -export -out /app/certs/aspnetapp.pfx -inkey /app/certs/key.pem \
    -in /app/certs/cert.pem -passout pass:

# Set environment variables for HTTPS on port 443 (standard HTTPS)
ENV ASPNETCORE_URLS=https://+:443
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish .
EXPOSE 443

ENTRYPOINT ["dotnet", "BitcoinAgent.Api.dll"]

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
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .
EXPOSE 80

ENTRYPOINT ["dotnet", "BitcoinAgent.Api.dll"]
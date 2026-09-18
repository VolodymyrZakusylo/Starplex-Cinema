FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/StarPlex.API/StarPlex.API.csproj", "src/StarPlex.API/"]
COPY ["src/StarPlex.Application/StarPlex.Application.csproj", "src/StarPlex.Application/"]
COPY ["src/StarPlex.Infrastructure/StarPlex.Infrastructure.csproj", "src/StarPlex.Infrastructure/"]
COPY ["src/StarPlex.Domain/StarPlex.Domain.csproj", "src/StarPlex.Domain/"]

RUN dotnet restore "src/StarPlex.API/StarPlex.API.csproj"

COPY src/ src/
WORKDIR /src/src/StarPlex.API
RUN dotnet publish "StarPlex.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StarPlex.API.dll"]

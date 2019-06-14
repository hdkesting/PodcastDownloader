FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["PodcastDownloader.Docker/PodcastDownloader.Docker.csproj", "PodcastDownloader.Docker/"]
RUN dotnet restore "PodcastDownloader.Docker/PodcastDownloader.Docker.csproj"
COPY . .
WORKDIR "/src/PodcastDownloader.Docker"
RUN dotnet build "PodcastDownloader.Docker.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "PodcastDownloader.Docker.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PodcastDownloader.Docker.dll"]
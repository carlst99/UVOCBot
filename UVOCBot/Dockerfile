FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", ""]
COPY ["UVOCBot/UVOCBot.csproj", "UVOCBot/"]
COPY ["UVOCBot.Core/UVOCBot.Core.csproj", "UVOCBot.Core/"]
COPY ["UVOCBot.Discord.Core/UVOCBot.Discord.Core.csproj", "UVOCBot.Discord.Core/"]
COPY ["Plugins/UVOCBot.Plugins.ApexLegends/UVOCBot.Plugins.ApexLegends.csproj", "Plugins/UVOCBot.Plugins.ApexLegends/"]
COPY ["Plugins/UVOCBot.Plugins.Feeds/UVOCBot.Plugins.Feeds.csproj", "Plugins/UVOCBot.Plugins.Feeds/"]
COPY ["Plugins/UVOCBot.Plugins.Greetings/UVOCBot.Plugins.Greetings.csproj", "Plugins/UVOCBot.Plugins.Greetings/"]
COPY ["Plugins/UVOCBot.Plugins.Planetside/UVOCBot.Plugins.Planetside.csproj", "Plugins/UVOCBot.Plugins.Planetside/"]
COPY ["Plugins/UVOCBot.Plugins.Roles/UVOCBot.Plugins.Roles.csproj", "Plugins/UVOCBot.Plugins.Roles/"]
COPY ["Plugins/UVOCBot.Plugins.SpaceEngineers/UVOCBot.Plugins.SpaceEngineers.csproj", "Plugins/UVOCBot.Plugins.SpaceEngineers/"]
RUN dotnet restore "UVOCBot/UVOCBot.csproj"
COPY . .
WORKDIR "/src/UVOCBot"
RUN dotnet build "UVOCBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UVOCBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UVOCBot.dll"]

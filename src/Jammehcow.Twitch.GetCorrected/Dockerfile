FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Jammehcow.Twitch.GetCorrected/Jammehcow.Twitch.GetCorrected.csproj", "Jammehcow.Twitch.GetCorrected/"]
RUN dotnet restore "Jammehcow.Twitch.GetCorrected/Jammehcow.Twitch.GetCorrected.csproj"
COPY . .
WORKDIR "/src/Jammehcow.Twitch.GetCorrected"
RUN dotnet build "Jammehcow.Twitch.GetCorrected.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jammehcow.Twitch.GetCorrected.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jammehcow.Twitch.GetCorrected.dll"]

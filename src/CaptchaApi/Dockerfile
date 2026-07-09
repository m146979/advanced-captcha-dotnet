FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CaptchaApi/CaptchaApi.csproj", "src/CaptchaApi/"]
COPY ["src/CaptchaCore/CaptchaCore.csproj", "src/CaptchaCore/"]
COPY ["src/CaptchaBehavioral/CaptchaBehavioral.csproj", "src/CaptchaBehavioral/"]
COPY ["src/CaptchaChallenges/CaptchaChallenges.csproj", "src/CaptchaChallenges/"]
RUN dotnet restore "src/CaptchaApi/CaptchaApi.csproj"
COPY . .
WORKDIR "/src/src/CaptchaApi"
RUN dotnet build "CaptchaApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CaptchaApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/models
ENTRYPOINT ["dotnet", "CaptchaApi.dll"]

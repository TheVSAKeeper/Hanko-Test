﻿FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7002
EXPOSE 7003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HankoTest.FirstApi/HankoTest.FirstApi.csproj", "HankoTest.FirstApi/"]
RUN dotnet restore "HankoTest.FirstApi/HankoTest.FirstApi.csproj"
COPY . .
WORKDIR "/src/HankoTest.FirstApi"
RUN dotnet build "HankoTest.FirstApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HankoTest.FirstApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HankoTest.FirstApi.dll"]

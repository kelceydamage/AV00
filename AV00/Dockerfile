#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AV00/AV00/AV00.csproj", "AV00/AV00/"]
COPY ["AV00-Shared/AV00-Shared.csproj", "AV00-Shared/"]
COPY ["AV00-Transport/AV00-Transport.csproj", "AV00-Transport/"]
RUN dotnet restore "AV00/AV00/AV00.csproj"
COPY . .
RUN ls -l
WORKDIR "/src/AV00/AV00"
RUN dotnet build "AV00.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AV00.csproj" -c Release -o /app/publish /p:UseAppHost=false

#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "AV00.dll"]

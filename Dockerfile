FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#RUN apt-get update && apt-get install -y libgdiplus lua5.4 liblua5.4-dev
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Administrator/Administrator.csproj", "Administrator/"]
COPY ["Administrator.Bot/Administrator.Bot.csproj", "Administrator.Bot/"]
COPY ["Administrator.Core/Administrator.Core.csproj", "Administrator.Core/"]
COPY ["Administrator.Database/Administrator.Database.csproj", "Administrator.Database/"]
COPY nuget.config .
RUN dotnet restore "Administrator/Administrator.csproj"
COPY . .
WORKDIR "/src/Administrator"
RUN dotnet build "Administrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Administrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#RUN ln -s /usr/lib/x86_64-linux-gnu/liblua5.4.so /usr/lib/x86_64-linux-gnu/liblua54.so
ENTRYPOINT ["dotnet", "Administrator.dll"]

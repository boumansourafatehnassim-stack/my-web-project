FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Bibliotheque.Api/Bibliotheque.Api.csproj", "Bibliotheque.Api/"]
RUN dotnet restore "Bibliotheque.Api/Bibliotheque.Api.csproj"
COPY . .
WORKDIR "/src/Bibliotheque.Api"
RUN dotnet build "Bibliotheque.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bibliotheque.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
CMD ["dotnet", "Bibliotheque.Api.dll"]
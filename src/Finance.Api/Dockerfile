FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Grosik.slnx", "./"]
COPY ["src/Finance.Api/Finance.Api.csproj", "src/Finance.Api/"]
COPY ["src/Finance.Domain/Finance.Domain.csproj", "src/Finance.Domain/"]
COPY ["src/Finance.Infrastructure/Finance.Infrastructure.csproj", "src/Finance.Infrastructure/"]
COPY ["src/Finance.Localization/Finance.Localization.csproj", "src/Finance.Localization/"]
RUN dotnet restore "src/Finance.Api/Finance.Api.csproj"
COPY . .
RUN dotnet publish "src/Finance.Api/Finance.Api.csproj" --no-restore -c Release -o /app/publish

FROM build AS migrator
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.2
ENV PATH="/tools:${PATH}"
ENTRYPOINT ["dotnet-ef", "database", "update", "--configuration", "Release", "--no-build", "--project", "src/Finance.Infrastructure", "--startup-project", "src/Finance.Api"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Finance.Api.dll"]

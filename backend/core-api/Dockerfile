FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["OptiLifts.API/OptiLifts.API.csproj", "OptiLifts.API/"]
COPY ["OptiLifts.Application/OptiLifts.Application.csproj", "OptiLifts.Application/"]
COPY ["OptiLifts.Domain/OptiLifts.Domain.csproj", "OptiLifts.Domain/"]
COPY ["OptiLifts.Infrastructure/OptiLifts.Infrastructure.csproj", "OptiLifts.Infrastructure/"]

# restore dependencies of the project
RUN dotnet restore "OptiLifts.API/OptiLifts.API.csproj"

COPY . .
WORKDIR "/src/OptiLifts.API"

# build and publish the project, the useAppHost keeps it from being OS specific
RUN dotnet publish "OptiLifts.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app
USER app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OptiLifts.API.dll"]
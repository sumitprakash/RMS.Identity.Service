# Stage 1 - build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj(s) and restore as distinct layers
COPY ./src/RMS.Identity.Service.Api/*.csproj ./RMS.Identity.Service.Api/
# copy other project files if you split projects - adjust as needed
RUN dotnet restore ./RMS.Identity.Service.Api/RMS.Identity.Service.Api.csproj

# copy everything else and publish
COPY . .
WORKDIR /src/RMS.Identity.Service.Api
RUN dotnet publish -c Release -o /app/publish /p:PublishTrimmed=false

# Stage 2 - runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true
# environment variables for production can be set in compose or orchestrator
COPY --from=build /app/publish ./
# non-root user
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RMS.Identity.Service.Api.dll"]

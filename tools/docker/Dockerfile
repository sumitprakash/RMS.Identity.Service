# Stage 1 - build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy every referenced project before restore so Docker layer caching works.
COPY ./src/RMS.Identity.Service.Api/*.csproj ./RMS.Identity.Service.Api/
COPY ./src/RMS.Identity.Service.Application/*.csproj ./RMS.Identity.Service.Application/
COPY ./src/RMS.Identity.Service.Domain/*.csproj ./RMS.Identity.Service.Domain/
COPY ./src/RMS.Identity.Service.Infrastructure/*.csproj ./RMS.Identity.Service.Infrastructure/
COPY ./src/RMS.Identity.Service.Infrastructure.Abstractions/*.csproj ./RMS.Identity.Service.Infrastructure.Abstractions/
RUN dotnet restore ./RMS.Identity.Service.Api/RMS.Identity.Service.Api.csproj

# Copy solution source into the same layout used during restore.
COPY ./src/ ./
WORKDIR /src/RMS.Identity.Service.Api
RUN dotnet publish -c Release -o /app/publish /p:PublishTrimmed=false

# Stage 2 - runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish ./
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RMS.Identity.Service.Api.dll"]

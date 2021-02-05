# FROM microsoft/dotnet AS build-env
FROM mcr.microsoft.com/dotnet/core/sdk:latest AS build-env
WORKDIR /app

# Copy projects
COPY ./SyncImages ./SyncImages

# Publish assembly
RUN dotnet publish ./SyncImages -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:latest
WORKDIR /app

COPY --from=build-env /app/out .

CMD [ "dotnet", "SyncImages.dll" ]

# FROM microsoft/dotnet AS build-env
FROM mcr.microsoft.com/dotnet/sdk AS build-env
WORKDIR /app

ARG HASURA_GRAPHQL_ADMIN_SECRET

ENV HASURA_GRAPHQL_ADMIN_SECRET=${HASURA_GRAPHQL_ADMIN_SECRET}

# Copy projects
COPY ./SyncImages ./SyncImages

# Publish assembly
RUN dotnet publish ./SyncImages -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk
WORKDIR /app

COPY --from=build-env /app/out .

CMD [ "dotnet", "SyncImages.dll" ]

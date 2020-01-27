FROM mcr.microsoft.com/dotnet/core/sdk

RUN echo "Hello world!"

WORKDIR /app
COPY . .
ENTRYPOINT [ "dotnet", "run", "-p", "SyncImages/" ]

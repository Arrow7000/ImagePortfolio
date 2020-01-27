FROM microsoft/dotnet

RUN echo "Hello world!"

WORKDIR /app
COPY . .
ENTRYPOINT [ "dotnet", "run", "-p", "SyncImages/" ]
